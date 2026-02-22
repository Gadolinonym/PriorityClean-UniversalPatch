using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Gadolinium.PriorityClean.UniversalPatch;

[StaticConstructorOnStartup]
public static class PriorityCleanDefSync
{
    static PriorityCleanDefSync()
    {
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

        WorkGiverDef priorityWorkGiver;
        int workGiverClassUpdates = SynchronizeWorkGiverClass(out priorityWorkGiver);
        int workGiverPriorityTweaks = SynchronizeWorkGiverPriority(priorityWorkGiver);
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

            SyncMechEnabledWorkTypes(
                race,
                cleaning,
                priorityCleaning,
                ref mechWorkTypeAdds,
                ref mechWorkTypeDuplicateRemovals,
                ref mechWorkTypeReorders);
            lifeStageAdds += SyncLifeStageWorkSettings(race, cleaning, priorityCleaning);
        }

        bool bumpedNaturalPriority = false;
        if (priorityCleaning.naturalPriority <= cleaning.naturalPriority)
        {
            priorityCleaning.naturalPriority = cleaning.naturalPriority + 1;
            bumpedNaturalPriority = true;
        }

        bool madeNonColonistCompatible = false;
        if (priorityCleaning.requireCapableColonist)
        {
            priorityCleaning.requireCapableColonist = false;
            madeNonColonistCompatible = true;
        }

        ThingDef cleansweeper = DefDatabase<ThingDef>.GetNamedSilentFail("Mech_Cleansweeper");
        bool cleansweeperHasPriority = cleansweeper?.race?.mechEnabledWorkTypes?.Contains(priorityCleaning) == true;
        WorkGiverDef cleanFilth = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CleanFilth");
        WorkGiverDef cleanClearPollution = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CleanClearPollution");
        int priorityWorkGiverPriority = priorityWorkGiver?.priorityInType ?? -1;
        bool priorityWorkGiverMechCapable = priorityWorkGiver?.canBeDoneByMechs ?? false;
        int cleanFilthPriority = cleanFilth?.priorityInType ?? -1;
        int cleanPollutionPriority = cleanClearPollution?.priorityInType ?? -1;

        Log.Message(
            $"[PriorityClean Universal Patch] Def sync complete: workgiverClassUpdates={workGiverClassUpdates}, workgiverPriorityTweaks={workGiverPriorityTweaks}, priorityWorkgiverPriorityInType={priorityWorkGiverPriority}, priorityWorkgiverCanBeDoneByMechs={priorityWorkGiverMechCapable}, cleanFilthPriorityInType={cleanFilthPriority}, cleanClearPollutionPriorityInType={cleanPollutionPriority}, mechAdds={mechWorkTypeAdds}, mechDuplicateRemovals={mechWorkTypeDuplicateRemovals}, mechReorders={mechWorkTypeReorders}, lifeStageAdds={lifeStageAdds}, naturalPriorityBumped={bumpedNaturalPriority}, requireCapableColonistSetFalse={madeNonColonistCompatible}, cleansweeperHasPriorityCleaning={cleansweeperHasPriority}.");
    }

    private static int SynchronizeWorkGiverClass(out WorkGiverDef priorityWorkGiver)
    {
        WorkGiverDef workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail("PriorityCleanFilth")
            ?? DefDatabase<WorkGiverDef>.GetNamedSilentFail("PriorityClean");
        priorityWorkGiver = workGiver;

        if (workGiver == null)
        {
            Log.Warning("[PriorityClean Universal Patch] Could not find PriorityClean workgiver def (PriorityCleanFilth/PriorityClean).");
            return 0;
        }

        Type compatType = typeof(WorkGiver_PriorityClean_Universal);
        if (workGiver.giverClass == compatType)
        {
            return 0;
        }

        workGiver.giverClass = compatType;
        return 1;
    }

    private static int SynchronizeWorkGiverPriority(WorkGiverDef priorityWorkGiver)
    {
        if (priorityWorkGiver == null)
        {
            return 0;
        }

        WorkGiverDef cleanFilth = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CleanFilth");
        WorkGiverDef cleanClearPollution = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CleanClearPollution");

        int baseline = 0;
        if (cleanFilth != null && cleanFilth.priorityInType > baseline)
        {
            baseline = cleanFilth.priorityInType;
        }

        if (cleanClearPollution != null && cleanClearPollution.priorityInType > baseline)
        {
            baseline = cleanClearPollution.priorityInType;
        }

        int tweaks = 0;
        int desiredPriorityInType = baseline + 1;
        if (priorityWorkGiver.priorityInType < desiredPriorityInType)
        {
            priorityWorkGiver.priorityInType = desiredPriorityInType;
            tweaks++;
        }

        if (!priorityWorkGiver.canBeDoneByMechs)
        {
            priorityWorkGiver.canBeDoneByMechs = true;
            tweaks++;
        }

        return tweaks;
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
