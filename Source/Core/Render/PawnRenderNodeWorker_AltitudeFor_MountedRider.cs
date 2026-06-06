using HarmonyLib;
using Verse;

namespace GiddyUpCore.Core;

[HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.AltitudeFor))]
internal static class PawnRenderNodeWorker_AltitudeFor_MountedRider
{
    private static bool Prefix(PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms, ref float __result)
    {
        if (!MountedRiderRenderLayerCompression.IsActiveFor(parms.pawn))
            return true;

        var layer = __instance.LayerFor(node, parms);
        foreach (var subWorker in node.Props.SubWorkers)
            subWorker.TransformLayer(node, parms, ref layer);

        if (!MountedRiderRenderLayerCompression.TryGetCompressedAltitude(parms.pawn, layer, out __result))
            return true;

        return false;
    }
}