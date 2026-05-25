using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUpCore.Compatibility.WalkTheWorld.Patches;

[HarmonyPatch]
internal static class WalkTheWorld_EnterMap_RemountTransferredColonistsPatch
{
    private static bool Prepare() => CompatibilityLoader.WalkTheWorldInstalled;

    private static System.Reflection.MethodBase? TargetMethod()
    {
        return AccessTools.Method("WalkTheWorld.WalkTheWorld:EnterMap");
    }

    private static void Postfix(Map targetMap)
    {
        var playerPawns = targetMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
        for (var index = 0; index < playerPawns.Count; index++)
        {
            var pawn = playerPawns[index];
            if (!pawn.IsColonist || pawn.IsMounted())
                continue;

            var pawnData = pawn.GetExtendedPawnData();
            var mount = pawnData.Mount ?? pawnData.ReservedMount;
            if (mount is not { Dead: false, Spawned: true } || mount.Map != targetMap || !pawn.CanReserve(mount))
                continue;

            pawn.GoMount(mount, MountUtility.GiveJobMethod.Instant);
        }
    }
}