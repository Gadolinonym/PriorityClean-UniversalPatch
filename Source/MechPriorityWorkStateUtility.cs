using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Gadolinium.PriorityClean.UniversalPatch;

// Maintains mech work-state parity when loading saves created before this patch existed.
public static class MechPriorityWorkStateUtility
{
    private static readonly HashSet<int> ProcessedPawnIds = new HashSet<int>();
    private static readonly WorkTypeDef PriorityCleaning = DefDatabase<WorkTypeDef>.GetNamedSilentFail("PriorityCleaning");

    private static int selfHealChangedCount;
    private static int selfHealSkippedCount;
    private static int selfHealErrorCount;

    public static int SelfHealChangedCount => selfHealChangedCount;
    public static int SelfHealSkippedCount => selfHealSkippedCount;
    public static int SelfHealErrorCount => selfHealErrorCount;

    public static void EnsurePriorityCleaningStateForColonyMech(Pawn pawn)
    {
        if (!IsColonyMechPawn(pawn))
        {
            return;
        }

        if (PriorityCleaning == null || WorkTypeDefOf.Cleaning == null)
        {
            selfHealSkippedCount++;
            return;
        }

        if (!TryGetWorkSettings(pawn, out Pawn_WorkSettings workSettings))
        {
            selfHealSkippedCount++;
            return;
        }

        int pawnId = pawn.thingIDNumber;
        // Only migrate each pawn once per session to avoid repeated writes/log spam.
        if (!ProcessedPawnIds.Add(pawnId))
        {
            return;
        }

        try
        {
            bool cleaningActive = IsWorkTypeActive(pawn, workSettings, WorkTypeDefOf.Cleaning);
            bool priorityCleaningActive = IsWorkTypeActive(pawn, workSettings, PriorityCleaning);
            if (!cleaningActive || priorityCleaningActive)
            {
                selfHealSkippedCount++;
                return;
            }

            // Mirror the pawn's existing Cleaning priority when enabling PriorityCleaning.
            int cleaningPriority = workSettings.GetPriority(WorkTypeDefOf.Cleaning);
            if (cleaningPriority <= 0)
            {
                cleaningPriority = 3;
            }

            workSettings.SetPriority(PriorityCleaning, cleaningPriority);
            // Clear cached disabled work-type state so the change is recognized immediately.
            pawn.Notify_DisabledWorkTypesChanged();
            selfHealChangedCount++;

            Log.Message(
                $"[PriorityClean Universal Patch] Mech work-state sync: pawnId={pawnId}, raceDef={pawn.def?.defName ?? "<null>"}, appliedPriority={cleaningPriority}.");
        }
        catch (Exception ex)
        {
            selfHealErrorCount++;
            Log.ErrorOnce(
                $"[PriorityClean Universal Patch] Failed mech work-state sync for pawnId={pawnId}: {ex}",
                1556031101);
        }
    }

    public static bool CanUsePriorityCleaningNow(Pawn pawn)
    {
        if (!IsColonyMechPawn(pawn) || PriorityCleaning == null)
        {
            return false;
        }

        if (!TryGetWorkSettings(pawn, out Pawn_WorkSettings workSettings))
        {
            return false;
        }

        return IsWorkTypeActive(pawn, workSettings, PriorityCleaning);
    }

    private static bool IsColonyMechPawn(Pawn pawn)
    {
        return pawn != null && pawn.IsColonyMech;
    }

    private static bool TryGetWorkSettings(Pawn pawn, out Pawn_WorkSettings workSettings)
    {
        workSettings = pawn?.workSettings;
        return workSettings != null;
    }

    private static bool IsWorkTypeActive(Pawn pawn, Pawn_WorkSettings workSettings, WorkTypeDef workType)
    {
        if (pawn == null || workSettings == null || workType == null)
        {
            return false;
        }

        if (pawn.WorkTypeIsDisabled(workType))
        {
            return false;
        }

        return workSettings.WorkIsActive(workType);
    }
}
