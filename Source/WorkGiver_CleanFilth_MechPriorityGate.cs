using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gadolinium.PriorityClean.UniversalPatch;

public class WorkGiver_CleanFilth_MechPriorityGate : WorkGiver_CleanFilth
{
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn == null)
        {
            return true;
        }

        if (!pawn.IsColonyMech)
        {
            return base.ShouldSkip(pawn, forced);
        }

        if (base.ShouldSkip(pawn, forced))
        {
            return true;
        }

        MechPriorityWorkStateUtility.EnsurePriorityCleaningStateForColonyMech(pawn);
        if (!MechPriorityWorkStateUtility.CanUsePriorityCleaningNow(pawn))
        {
            return false;
        }

        return HasReachableReservablePriorityFilth(pawn, forced);
    }

    private static bool HasReachableReservablePriorityFilth(Pawn pawn, bool forced)
    {
        Map map = pawn.Map;
        if (map == null)
        {
            return false;
        }

        List<Thing> filthInHomeArea = map.listerFilthInHomeArea?.FilthInHomeArea;
        if (filthInHomeArea == null || filthInHomeArea.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < filthInHomeArea.Count; i++)
        {
            if (filthInHomeArea[i] is not Filth filth)
            {
                continue;
            }

            if (filth.Destroyed || filth.Map != map)
            {
                continue;
            }

            if (!map.areaManager.Home[filth.Position])
            {
                continue;
            }

            if (!PriorityFilthUtility.IsPriorityFilth(filth))
            {
                continue;
            }

            if (!pawn.CanReach(filth, PathEndMode.Touch, Danger.Deadly))
            {
                continue;
            }

            if (!pawn.CanReserve(filth, 1, -1, null, forced))
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
