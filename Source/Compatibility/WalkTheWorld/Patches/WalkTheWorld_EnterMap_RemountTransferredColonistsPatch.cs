using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUpCore.Compatibility.WalkTheWorld.Patches;

[HarmonyPatch]
internal static class WalkTheWorld_EnterMap_RemountTransferredColonistsPatch
{
    private const string TargetName = "WalkTheWorld.WalkTheWorld:EnterMap";

    //A null TargetMethod makes Harmony throw and abort the rest of PatchAll, so check it here
    private static bool Prepare() => CompatibilityLoader.WalkTheWorldInstalled && AccessTools.Method(TargetName) != null;

    private static System.Reflection.MethodBase? TargetMethod() => AccessTools.Method(TargetName);

    //Bound by position so a renamed parameter in Walk the World doesn't break the patch
    private static void Postfix([HarmonyArgument(0)] Map targetMap)
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