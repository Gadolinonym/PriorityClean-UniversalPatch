using RimWorld;
using Verse;
using Verse.AI;

namespace Gadolinium.PriorityClean.UniversalPatch;

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
            return base.ShouldSkip(pawn, forced);
        }

        return !pawn.IsColonyMech;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (pawn == null)
        {
            return false;
        }

        if (pawn.IsColonistPlayerControlled)
        {
            return base.HasJobOnThing(pawn, t, forced);
        }

        if (!pawn.IsColonyMech)
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
