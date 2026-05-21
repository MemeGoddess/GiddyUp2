using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core;

internal sealed class MountedRiderRenderNodeWorker : PawnRenderNodeWorker
{
    private const float RiderLayer = 55f;

    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        return node is MountedRiderRenderNode mountedNode &&
               mountedNode.Rider.Spawned &&
               MountedRiderRenderNodeUtility.ShouldUseMountedRenderNode(mountedNode.Rider);
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
        var offset = MountedRiderRenderNodeUtility.GetMountedRiderOffset(mountedNode.Rider, mountedNode.Mount,
            parms.facing);
        offset += node.DebugOffset;
        if (!parms.flags.FlagSet(PawnRenderFlags.Portrait) && node.TryGetAnimationOffset(parms, out var animationOffset))
            offset += animationOffset;
        return offset;
    }

    public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
    {
        return RiderLayer + node.debugLayerOffset;
    }

    public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
    {
        base.PostDraw(node, parms, mesh, matrix);

        if (!node.DebugEnabled)
            return;

        var mountedNode = (MountedRiderRenderNode)node;
        if (!MountedRiderRenderNodeUtility.ShouldUseMountedRenderNode(mountedNode.Rider))
            return;

        var drawLoc = matrix.MultiplyPoint3x4(Vector3.zero);
        mountedNode.Rider.Drawer.renderer.RenderPawnAt(drawLoc, parms.facing);
    }
}