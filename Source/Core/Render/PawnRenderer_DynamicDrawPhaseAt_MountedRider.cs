using GiddyUp;
using HarmonyLib;
using Verse;

namespace GiddyUpCore.Core;

//The rider is drawn by its mount's render node, so skip its own draw phases while mounted
[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.DynamicDrawPhaseAt))]
internal static class PawnRenderer_DynamicDrawPhaseAt_MountedRider
{
    private static bool Prefix(PawnRenderer __instance, DrawPhase phase)
    {
        if (phase != DrawPhase.ParallelPreDraw && phase != DrawPhase.Draw)
            return true;
        var pawn = __instance.pawn;
        if (!pawn.IsMounted())
            return true;

        //Mount gone or elsewhere: draw the rider normally (no Dismount here, this runs off the main thread)
        var mount = pawn.GetExtendedPawnData().Mount;
        return mount is not { Spawned: true } || mount.Map != pawn.Map;
    }
}
