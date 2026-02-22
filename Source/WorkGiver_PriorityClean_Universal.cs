using RimWorld;
using Verse;
using Verse.AI;

namespace Gadolinium.PriorityClean.UniversalPatch;

// Extends PriorityClean's workgiver so colony mechs can run the same priority-filth checks as colonists.
public class WorkGiver_PriorityClean_Universal : global::PriorityClean.WorkGiver_PriorityClean
{
    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (pawn == null)
        {
            return true;
        }

        if (pawn.IsColonistPlayerControlled)
        {
            // Preserve upstream behavior for colonists.
            return base.ShouldSkip(pawn, forced);
        }

        if (!pawn.IsColonyMech)
        {
            return true;
        }

        MechPriorityWorkStateUtility.EnsurePriorityCleaningStateForColonyMech(pawn);
        return !MechPriorityWorkStateUtility.CanUsePriorityCleaningNow(pawn);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (pawn == null)
        {
            return false;
        }

        if (pawn.IsColonistPlayerControlled)
        {
            // Preserve upstream behavior for colonists.
            return base.HasJobOnThing(pawn, t, forced);
        }

        if (!pawn.IsColonyMech)
        {
            return false;
        }

        MechPriorityWorkStateUtility.EnsurePriorityCleaningStateForColonyMech(pawn);
        if (!MechPriorityWorkStateUtility.CanUsePriorityCleaningNow(pawn))
        {
            return false;
        }

        if (t is not Filth filth)
        {
            return false;
        }

        if (!PriorityFilthUtility.IsPriorityFilth(filth))
        {
            return false;
        }

        if (filth.Map == null || !filth.Map.areaManager.Home[filth.Position])
        {
            return false;
        }

        if (!pawn.CanReach(filth, PathEndMode.Touch, Danger.Deadly))
        {
            return false;
        }

        return pawn.CanReserve(filth, 1, -1, null, forced);
    }
}
