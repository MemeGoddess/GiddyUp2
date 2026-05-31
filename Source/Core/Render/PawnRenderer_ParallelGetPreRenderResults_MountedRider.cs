using HarmonyLib;
using Verse;

namespace GiddyUpCore.Core;

[HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
internal static class PawnRenderer_ParallelGetPreRenderResults_MountedRider
{
    private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnField =
        AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

    private static void Prefix(PawnRenderer __instance, ref bool disableCache)
    {
        if (MountedRiderRenderLayerCompression.IsActiveFor(PawnField(__instance)))
            disableCache = true;
    }
}