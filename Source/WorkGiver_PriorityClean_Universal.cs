using System;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gadolinium.PriorityClean.UniversalPatch;

public class WorkGiver_PriorityClean_Universal : global::PriorityClean.WorkGiver_PriorityClean
{
    private static readonly MethodInfo IsPriorityFilthMethod = typeof(global::PriorityClean.WorkGiver_PriorityClean)
        .GetMethod("IsPriorityFilth", BindingFlags.Static | BindingFlags.NonPublic);

    private static bool IsPriorityFilthCompat(Filth filth)
    {
        if (filth == null || IsPriorityFilthMethod == null)
        {
            return false;
        }

        try
        {
            object result = IsPriorityFilthMethod.Invoke(null, new object[] { filth });
            return result is bool value && value;
        }
        catch (Exception ex)
        {
            Log.ErrorOnce($"[PriorityClean Universal Patch] Failed invoking PriorityClean IsPriorityFilth: {ex}", 1879645041);
            return false;
        }
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

        Filth filth = t as Filth;
        if (filth == null)
        {
            return false;
        }

        if (!IsPriorityFilthCompat(filth))
        {
            return false;
        }

        if (filth.Map == null || !filth.Map.areaManager.Home[filth.Position])
        {
            return false;
        }

        return pawn.CanReserve(filth, 1, -1, null, forced);
    }
}
