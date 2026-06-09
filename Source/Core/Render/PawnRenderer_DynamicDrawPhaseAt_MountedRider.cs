using GiddyUp;
using HarmonyLib;
using Verse;

namespace GiddyUpCore.Core;

[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.DynamicDrawPhaseAt))]
internal static class PawnRenderer_DynamicDrawPhaseAt_MountedRider
{
    private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnField =
        AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

    private static bool Prefix(PawnRenderer __instance, DrawPhase phase)
    {
        if (phase != DrawPhase.ParallelPreDraw && phase != DrawPhase.Draw)
            return true;
        var pawn = PawnField(__instance);
        return pawn.GetExtendedPawnData().Mount == null;
    }
}