using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace Gadolinium.PriorityClean.UniversalPatch;

public static class PriorityFilthUtility
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

    public static bool IsPriorityFilth(Filth filth)
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
}
