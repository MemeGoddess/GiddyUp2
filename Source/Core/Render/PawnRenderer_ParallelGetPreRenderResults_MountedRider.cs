using HarmonyLib;
using Verse;

namespace GiddyUpCore.Core;

[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.ParallelGetPreRenderResults))]
internal static class PawnRenderer_ParallelGetPreRenderResults_MountedRider
{
    private static void Prefix(PawnRenderer __instance, ref bool disableCache)
    {
        if (MountedRiderRenderLayerCompression.IsActiveFor(__instance.pawn))
            disableCache = true;
    }
}
