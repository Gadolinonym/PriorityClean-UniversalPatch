using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gadolinium.PriorityClean.UniversalPatch;

public class WorkGiver_PriorityClean_Universal : global::PriorityClean.WorkGiver_PriorityClean
{
    private static readonly MethodInfo IsPriorityFilthMethod = ResolveIsPriorityFilthMethod();

    private static MethodInfo ResolveIsPriorityFilthMethod()
    {
        Type workGiverType = typeof(global::PriorityClean.WorkGiver_PriorityClean);
        MethodInfo namedMethod = workGiverType.GetMethod("IsPriorityFilth", BindingFlags.Static | BindingFlags.NonPublic);
        if (namedMethod != null && namedMethod.ReturnType == typeof(bool))
        {
            return namedMethod;
        }

        return workGiverType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
            {
                if (method.ReturnType != typeof(bool))
                {
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 1 && typeof(Filth).IsAssignableFrom(parameters[0].ParameterType);
            });
    }

    private static bool IsPriorityFilthCompat(Filth filth)
    {
        if (filth == null)
        {
            return false;
        }

        if (IsPriorityFilthMethod == null)
        {
            Log.WarningOnce(
                "[PriorityClean Universal Patch] Could not find PriorityClean priority-filth helper. Colony mechs will skip PriorityCleaning.",
                1556027001);
            return false;
        }

        try
        {
            object result = IsPriorityFilthMethod.Invoke(null, new object[] { filth });
            return result is bool value && value;
        }
        catch (Exception ex)
        {
            Log.ErrorOnce(
                $"[PriorityClean Universal Patch] Failed invoking PriorityClean priority-filth helper: {ex}",
                1556027002);
            return false;
        }
    }

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
