using System.Collections.Generic;
using System;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCore.Core;

internal sealed class MountedRiderRenderNodeWorker : PawnRenderNodeWorker
{
    private const float RiderLayer = 33f;
    //Draw behavior setting: the rider sits below the mount's body (layer 0) when the animal is set to draw in front.
    //Altitudes clamp at -10, so the rider's own layers are squeezed into -10..-0.5. Replaces the old DrawPos.y nudge.
    private const float BehindMountLayer = -10f;
    private const float BehindMountBand = 9.5f;

    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        return node is MountedRiderRenderNode mountedNode;
    }

    public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms,
        List<PawnGraphicDrawRequest> requests)
    {
        requests.Add(new PawnGraphicDrawRequest(node));
    }

    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        pivot = Vector3.zero;
        var mountedNode = (MountedRiderRenderNode)node;
        if (mountedNode.AnimalData.Rider == null)
            return pivot;
        var offset = MountedRiderRenderNodeUtility.GetMountedRiderOffset(mountedNode.AnimalData.Rider, mountedNode.Mount,
            parms.facing);
        offset += node.DebugOffset;
        if (!parms.flags.FlagSet(PawnRenderFlags.Portrait) && node.TryGetAnimationOffset(parms, out var animationOffset))
            offset += animationOffset;
        return offset;
    }

    public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
    {
        if (DrawsBehindMount(node, parms))
            return BehindMountLayer + node.debugLayerOffset;
        return RiderLayer + node.debugLayerOffset + (parms.facing == Rot4.North ? 10f : 0);
    }

    private static bool DrawsBehindMount(PawnRenderNode node, PawnDrawParms parms)
    {
        return parms.facing == Rot4.South && node is MountedRiderRenderNode mountedNode &&
               Settings.DrawRulesCache.Contains(mountedNode.Mount.def.shortHash);
    }

    public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
    {
        base.PostDraw(node, parms, mesh, matrix);

        if (!node.DebugEnabled)
            return;

        var mountedNode = (MountedRiderRenderNode)node;
        if (mountedNode.AnimalData.Rider == null)
            return;

        var drawLoc = matrix.MultiplyPoint3x4(Vector3.zero);
        var behindMount = DrawsBehindMount(node, parms);
        using var _ = MountedRiderRenderLayerCompression.Push(mountedNode.AnimalData.Rider, LayerFor(node, parms),
            behindMount ? BehindMountBand : MountedRiderRenderLayerCompression.DefaultCompressedBand);
        mountedNode.AnimalData.Rider.Drawer.renderer.RenderPawnAt(drawLoc, parms.facing);
    }
}