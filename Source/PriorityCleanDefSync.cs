using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Gadolinium.PriorityClean.UniversalPatch;

// Synchronizes patched defs at startup so XML/runtime changes are present before normal job scanning begins.
[StaticConstructorOnStartup]
public static class PriorityCleanDefSync
{
    static PriorityCleanDefSync()
    {
        // Def databases are not stable until long events finish loading.
        LongEventHandler.ExecuteWhenFinished(SynchronizePriorityCleanDefs);
    }

    private static void SynchronizePriorityCleanDefs()
    {
        WorkTypeDef cleaning = WorkTypeDefOf.Cleaning;
        WorkTypeDef priorityCleaning = DefDatabase<WorkTypeDef>.GetNamedSilentFail("PriorityCleaning");
        if (cleaning == null || priorityCleaning == null)
        {
            Log.Warning("[PriorityClean Universal Patch] Missing Cleaning/PriorityCleaning WorkTypeDef. Universal sync skipped.");
            return;
        }

        // Route all PriorityCleaning workgivers through our compatibility class and allow mech execution.
        bool usedPriorityWorkGiverFallback;
        List<WorkGiverDef> priorityWorkGivers = FindPriorityWorkGivers(priorityCleaning, out usedPriorityWorkGiverFallback);
        int workGiverClassUpdates = 0;
        int workGiverMechFlagUpdates = 0;
        for (int i = 0; i < priorityWorkGivers.Count; i++)
        {
            SynchronizePriorityWorkGiverDef(priorityWorkGivers[i], ref workGiverClassUpdates, ref workGiverMechFlagUpdates);
        }

        int cleanFilthGateUpdates = SynchronizeCleanFilthGateWorkGiver(cleaning);

        bool madeNonColonistCompatible = false;
        if (priorityCleaning.requireCapableColonist)
        {
            priorityCleaning.requireCapableColonist = false;
            madeNonColonistCompatible = true;
        }

        int racesScanned = 0;
        int eligibleMechRaces = 0;
        int changedEligibleMechRaces = 0;
        int mechWorkTypeAdds = 0;
        int mechWorkTypeDuplicateRemovals = 0;
        int mechWorkTypeReorders = 0;
        int lifeStageAdds = 0;

        foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            RaceProperties race = thingDef.race;
            if (race == null)
            {
                continue;
            }

            racesScanned++;

            int raceLifeStageAdds = SyncLifeStageWorkSettings(race, cleaning, priorityCleaning);
            lifeStageAdds += raceLifeStageAdds;

            List<WorkTypeDef> mechEnabledWorkTypes = race.mechEnabledWorkTypes;
            bool isEligibleMechRace = mechEnabledWorkTypes != null && mechEnabledWorkTypes.Count > 0 && mechEnabledWorkTypes.Contains(cleaning);
            if (!isEligibleMechRace)
            {
                if (raceLifeStageAdds > 0)
                {
                    Log.Message(
                        $"[PriorityClean Universal Patch] Race sync: raceDef={thingDef.defName}, mechEligible=false, addedPriority=0, removedDuplicates=0, reordered=0, lifeStageAdds={raceLifeStageAdds}.");
                }

                continue;
            }

            eligibleMechRaces++;

            int raceAdds = 0;
            int raceDuplicateRemovals = 0;
            int raceReorders = 0;
            SyncMechEnabledWorkTypes(
                race,
                cleaning,
                priorityCleaning,
                ref raceAdds,
                ref raceDuplicateRemovals,
                ref raceReorders);

            mechWorkTypeAdds += raceAdds;
            mechWorkTypeDuplicateRemovals += raceDuplicateRemovals;
            mechWorkTypeReorders += raceReorders;

            if (raceAdds > 0 || raceDuplicateRemovals > 0 || raceReorders > 0 || raceLifeStageAdds > 0)
            {
                changedEligibleMechRaces++;
            }

            Log.Message(
                $"[PriorityClean Universal Patch] Race sync: raceDef={thingDef.defName}, mechEligible=true, addedPriority={raceAdds}, removedDuplicates={raceDuplicateRemovals}, reordered={raceReorders}, lifeStageAdds={raceLifeStageAdds}.");
        }

        Log.Message(
            $"[PriorityClean Universal Patch] Def sync complete: priorityWorkGiverCount={priorityWorkGivers.Count}, priorityWorkGiverFallbackUsed={usedPriorityWorkGiverFallback}, priorityWorkGiverClassUpdates={workGiverClassUpdates}, priorityWorkGiverMechFlagUpdates={workGiverMechFlagUpdates}, cleanFilthGateUpdates={cleanFilthGateUpdates}, racesScanned={racesScanned}, eligibleMechRaces={eligibleMechRaces}, changedEligibleMechRaces={changedEligibleMechRaces}, mechAdds={mechWorkTypeAdds}, mechDuplicateRemovals={mechWorkTypeDuplicateRemovals}, mechReorders={mechWorkTypeReorders}, lifeStageAdds={lifeStageAdds}, requireCapableColonistSetFalse={madeNonColonistCompatible}, mechSelfHealChanged={MechPriorityWorkStateUtility.SelfHealChangedCount}, mechSelfHealSkipped={MechPriorityWorkStateUtility.SelfHealSkippedCount}, mechSelfHealErrors={MechPriorityWorkStateUtility.SelfHealErrorCount}.");
    }

    private static List<WorkGiverDef> FindPriorityWorkGivers(WorkTypeDef priorityCleaning, out bool usedFallback)
    {
        List<WorkGiverDef> workGivers = DefDatabase<WorkGiverDef>.AllDefsListForReading
            .Where(workGiver => workGiver.workType == priorityCleaning)
            .ToList();

        if (workGivers.Count > 0)
        {
            usedFallback = false;
            return workGivers;
        }

        usedFallback = true;
        WorkGiverDef byFilthDefName = DefDatabase<WorkGiverDef>.GetNamedSilentFail("PriorityCleanFilth");
        WorkGiverDef byLegacyDefName = DefDatabase<WorkGiverDef>.GetNamedSilentFail("PriorityClean");
        if (byFilthDefName != null)
        {
            workGivers.Add(byFilthDefName);
        }

        if (byLegacyDefName != null && !workGivers.Contains(byLegacyDefName))
        {
            workGivers.Add(byLegacyDefName);
        }

        if (workGivers.Count == 0)
        {
            Log.Warning("[PriorityClean Universal Patch] Could not find PriorityClean workgiver defs by workType or known fallback defNames.");
        }

        return workGivers;
    }

    private static void SynchronizePriorityWorkGiverDef(WorkGiverDef workGiver, ref int classUpdates, ref int mechFlagUpdates)
    {
        if (workGiver == null)
        {
            return;
        }

        string previousClass = workGiver.giverClass?.FullName ?? "<null>";
        bool classChanged = false;
        bool mechFlagChanged = false;
        Type compatType = typeof(WorkGiver_PriorityClean_Universal);
        if (workGiver.giverClass != compatType)
        {
            workGiver.giverClass = compatType;
            classUpdates++;
            classChanged = true;
        }

        if (!workGiver.canBeDoneByMechs)
        {
            workGiver.canBeDoneByMechs = true;
            mechFlagUpdates++;
            mechFlagChanged = true;
        }

        if (classChanged || mechFlagChanged)
        {
            Log.Message(
                $"[PriorityClean Universal Patch] Priority workgiver sync: defName={workGiver.defName}, classChanged={classChanged}, previousClass={previousClass}, newClass={workGiver.giverClass?.FullName ?? "<null>"}, mechFlagChanged={mechFlagChanged}, canBeDoneByMechs={workGiver.canBeDoneByMechs}.");
        }
    }

    private static int SynchronizeCleanFilthGateWorkGiver(WorkTypeDef cleaning)
    {
        WorkGiverDef cleanFilthWorkGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CleanFilth");
        if (cleanFilthWorkGiver == null)
        {
            cleanFilthWorkGiver = DefDatabase<WorkGiverDef>.AllDefsListForReading.FirstOrDefault(workGiver =>
                workGiver.workType == cleaning &&
                workGiver.giverClass == typeof(WorkGiver_CleanFilth));
        }

        if (cleanFilthWorkGiver == null)
        {
            Log.Warning("[PriorityClean Universal Patch] Could not find CleanFilth workgiver to attach mech priority gate.");
            return 0;
        }

        Type gateType = typeof(WorkGiver_CleanFilth_MechPriorityGate);
        if (cleanFilthWorkGiver.giverClass == gateType)
        {
            return 0;
        }

        string previousClass = cleanFilthWorkGiver.giverClass?.FullName ?? "<null>";
        cleanFilthWorkGiver.giverClass = gateType;
        Log.Message(
            $"[PriorityClean Universal Patch] CleanFilth gate sync: defName={cleanFilthWorkGiver.defName}, previousClass={previousClass}, newClass={gateType.FullName}.");
        return 1;
    }

    private static void SyncMechEnabledWorkTypes(
        RaceProperties race,
        WorkTypeDef cleaning,
        WorkTypeDef priorityCleaning,
        ref int mechWorkTypeAdds,
        ref int mechWorkTypeDuplicateRemovals,
        ref int mechWorkTypeReorders)
    {
        List<WorkTypeDef> mechEnabled = race.mechEnabledWorkTypes;
        if (mechEnabled == null || mechEnabled.Count == 0 || !mechEnabled.Contains(cleaning))
        {
            return;
        }

        if (!mechEnabled.Contains(priorityCleaning))
        {
            mechEnabled.Add(priorityCleaning);
            mechWorkTypeAdds++;
        }

        int seenPriority = 0;
        for (int i = mechEnabled.Count - 1; i >= 0; i--)
        {
            if (mechEnabled[i] != priorityCleaning)
            {
                continue;
            }

            seenPriority++;
            if (seenPriority > 1)
            {
                mechEnabled.RemoveAt(i);
                mechWorkTypeDuplicateRemovals++;
            }
        }

        int cleaningIndex = mechEnabled.IndexOf(cleaning);
        int priorityIndex = mechEnabled.IndexOf(priorityCleaning);
        if (cleaningIndex >= 0 && priorityIndex > cleaningIndex)
        {
            mechEnabled.RemoveAt(priorityIndex);
            cleaningIndex = mechEnabled.IndexOf(cleaning);
            mechEnabled.Insert(cleaningIndex, priorityCleaning);
            mechWorkTypeReorders++;
        }
    }

    private static int SyncLifeStageWorkSettings(RaceProperties race, WorkTypeDef cleaning, WorkTypeDef priorityCleaning)
    {
        if (race.lifeStageWorkSettings == null || race.lifeStageWorkSettings.Count == 0)
        {
            return 0;
        }

        List<int> cleaningMinAges = new List<int>();
        for (int i = 0; i < race.lifeStageWorkSettings.Count; i++)
        {
            LifeStageWorkSettings settings = race.lifeStageWorkSettings[i];
            if (settings != null && settings.workType == cleaning && !cleaningMinAges.Contains(settings.minAge))
            {
                cleaningMinAges.Add(settings.minAge);
            }
        }

        if (cleaningMinAges.Count == 0)
        {
            return 0;
        }

        // Keep PriorityCleaning min-age restrictions aligned with Cleaning for races that define both.
        int additions = 0;
        for (int i = 0; i < cleaningMinAges.Count; i++)
        {
            int minAge = cleaningMinAges[i];
            bool hasPriorityRule = false;

            for (int j = 0; j < race.lifeStageWorkSettings.Count; j++)
            {
                LifeStageWorkSettings existing = race.lifeStageWorkSettings[j];
                if (existing != null && existing.workType == priorityCleaning && existing.minAge == minAge)
                {
                    hasPriorityRule = true;
                    break;
                }
            }

            if (!hasPriorityRule)
            {
                race.lifeStageWorkSettings.Add(new LifeStageWorkSettings
                {
                    workType = priorityCleaning,
                    minAge = minAge
                });
                additions++;
            }
        }

        return additions;
    }
}
