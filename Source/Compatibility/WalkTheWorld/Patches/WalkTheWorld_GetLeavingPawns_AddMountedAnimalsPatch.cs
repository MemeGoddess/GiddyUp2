using System.Collections.Generic;
using GiddyUp;
using HarmonyLib;
using Verse;

namespace GiddyUpCore.Compatibility.WalkTheWorld.Patches;

[HarmonyPatch]
internal static class WalkTheWorld_GetLeavingPawns_AddMountedAnimalsPatch
{
    private static bool Prepare() => CompatibilityLoader.WalkTheWorldInstalled;

    private static System.Reflection.MethodBase? TargetMethod()
    {
        return AccessTools.Method("WalkTheWorld.WalkTheWorld:GetLeavingPawns");
    }

    private static void Postfix(ref List<Pawn> __result)
    {
        if (__result == null || __result.Count == 0)
            return;

        var transferredPawnIds = new HashSet<int>();
        for (var index = 0; index < __result.Count; index++)
            transferredPawnIds.Add(__result[index].thingIDNumber);

        var mountsToTransfer = new List<Pawn>();
        for (var index = 0; index < __result.Count; index++)
        {
            var rider = __result[index];
            if (!rider.IsMounted())
                continue;

            var mount = rider.GetExtendedPawnData().Mount;
            if (mount == null || mount.Dead || !mount.Spawned || mount.Map != rider.Map)
                continue;

            if (transferredPawnIds.Add(mount.thingIDNumber))
                mountsToTransfer.Add(mount);
        }

        if (mountsToTransfer.Count != 0)
            __result.AddRange(mountsToTransfer);
    }
}