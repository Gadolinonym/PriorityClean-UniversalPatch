using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Gadolinium.PriorityClean.UniversalPatch;

[StaticConstructorOnStartup]
public static class PriorityCleanDefSync
{
    static PriorityCleanDefSync()
    {
        LongEventHandler.ExecuteWhenFinished(SynchronizeLifeStageCleaningRestrictions);
    }

    private static void SynchronizeLifeStageCleaningRestrictions()
    {
        WorkTypeDef cleaning = WorkTypeDefOf.Cleaning;
        WorkTypeDef priorityCleaning = DefDatabase<WorkTypeDef>.GetNamedSilentFail("PriorityCleaning");
        if (cleaning == null || priorityCleaning == null)
        {
            return;
        }

        foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            RaceProperties race = thingDef.race;
            if (race == null || race.lifeStageWorkSettings == null || race.lifeStageWorkSettings.Count == 0)
            {
                continue;
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
                continue;
            }

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
                }
            }
        }
    }
}
