using GiddyUp;
using HarmonyLib;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpTakeFlight.Harmony;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetPathContext))]
internal static class Patch_Pawn_GetPathContext
{
    private static bool Prepare() => Settings.takeFlightEnabled;

    public static void Postfix(Pawn __instance, Pathing pathing, ref PathingContext __result)
    {
        if (!Settings.flyersIgnoreTerrain || pathing?.Flying == null)
            return;
        if (FlyerUtility.IsRidingFlyer(__instance))
            __result = pathing.Flying;
    }
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.WaterCellCost), MethodType.Getter)]
internal static class Patch_Pawn_WaterCellCost
{
    private static bool Prepare() => Settings.takeFlightEnabled;

    public static void Postfix(Pawn __instance, ref int? __result)
    {
        if (!Settings.flyersIgnoreTerrain || __result == 1)
            return;
        if (FlyerUtility.IsRidingFlyer(__instance))
            __result = 1;
    }
}

[HarmonyPatch(typeof(PathFinder), nameof(PathFinder.CreateRequest), typeof(IntVec3), typeof(LocalTargetInfo), typeof(IntVec3?), typeof(TraverseParms), typeof(PathFinderCostTuning?), typeof(PathEndMode), typeof(Pawn), typeof(PathRequest.IPathGridCustomizer))]
internal static class Patch_PathFinder_CreateRequest
{
    private static bool Prepare() => Settings.takeFlightEnabled;

    public static void Postfix(PathRequest __result, Pawn? pawn)
    {
        if (__result == null || pawn == null || __result.customizer != null || !Settings.flyersIgnoreTerrain)
            return;
        if (FlyerUtility.IsRidingFlyer(pawn))
            __result.customizer = FlyerPathGridCustomizer.Instance;
    }
}

[HarmonyPatch(typeof(PathFinderMapData), nameof(PathFinderMapData.ParameterizeGridJob))]
internal static class Patch_PathFinderMapData_ParameterizeGridJob
{
    private static bool Prepare() => Settings.takeFlightEnabled;

    public static void Postfix(PathFinderMapData __instance, PathRequest? request, ref PathGridJob job)
    {
        if (request?.customizer is not FlyerPathGridCustomizer)
            return;
        var flyingCost = __instance.flyingCost;
        if (flyingCost != null)
            job.pathGridDirect = flyingCost.Data;
    }
}

[HarmonyPatch(typeof(MountUtility), nameof(MountUtility.Dismount))]
internal static class Patch_MountUtility_Dismount
{
    private static bool Prepare() => Settings.takeFlightEnabled;

    public static void Postfix(Pawn rider, Pawn? animal)
    {
        if (MountedFlightUtility.Launching || !UnityData.IsInMainThread)
            return;
        FlyerUtility.MoveOffUnwalkableCell(rider);
        FlyerUtility.MoveOffUnwalkableCell(animal);
    }
}
