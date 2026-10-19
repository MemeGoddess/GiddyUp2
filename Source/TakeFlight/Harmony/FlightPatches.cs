using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpTakeFlight.Harmony;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
internal static class Patch_Pawn_GetGizmos
{
    private static bool Prepare() => Settings.takeFlightEnabled;

    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
    {
        foreach (var gizmo in __result)
            yield return gizmo;

        if (!Settings.localFlightEnabled && !Settings.worldFlightEnabled)
            yield break;
        if (!__instance.Spawned || !__instance.IsColonistPlayerControlled)
            yield break;
        if (!FlyerUtility.TryGetFlyingMount(__instance, out var mount))
            yield break;

        var load = LoadInfo.For(__instance, mount);
        if (Settings.localFlightEnabled)
            yield return MountedFlightUtility.MakeTakeFlightCommand(__instance, mount, load);
        if (Settings.worldFlightEnabled)
            yield return WorldFlightUtility.MakeMapWorldFlightCommand(__instance, mount, load);
    }
}

[HarmonyPatch(typeof(Caravan), nameof(Caravan.GetGizmos))]
internal static class Patch_Caravan_GetGizmos
{
    private static bool Prepare() => Settings.takeFlightEnabled;

    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Caravan __instance)
    {
        foreach (var gizmo in __result)
            yield return gizmo;

        if (!Settings.worldFlightEnabled || !__instance.IsPlayerControlled)
            yield break;
        var command = WorldFlightUtility.CaravanWorldFlightGizmo(__instance);
        if (command != null)
            yield return command;
    }
}

[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.JobTrackerTickInterval))]
internal static class Patch_Pawn_JobTracker_JobTrackerTickInterval
{
    private static bool Prepare() => Settings.takeFlightEnabled;

    public static bool Prefix(Pawn_JobTracker __instance)
    {
        var holder = __instance.pawn?.ParentHolder;
        if (holder == null)
            return true;
        return holder is not MountedPawnFlyer &&
               !(holder is ActiveTransporterInfo info && WorldFlightUtility.IsOurTransporter(info));
    }
}

[HarmonyPatch(typeof(TravellingTransporters), nameof(TravellingTransporters.TraveledPctStepPerTick), MethodType.Getter)]
internal static class Patch_TravellingTransporters_TraveledPctStepPerTick
{
    private static bool Prepare() => Settings.takeFlightEnabled;

    public static void Postfix(TravellingTransporters __instance, ref float __result)
    {
        if (__instance is TravellingMountedFlyers flyers && __result < 1f)
            __result *= flyers.SpeedFactor;
    }
}
