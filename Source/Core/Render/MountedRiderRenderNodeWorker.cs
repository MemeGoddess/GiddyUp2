using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core;

internal sealed class MountedRiderRenderNodeWorker : PawnRenderNodeWorker
{
    private const float RiderLayer = 33f;
    private const PawnRenderFlags ForwardedRenderFlags = PawnRenderFlags.Portrait |
                                                        PawnRenderFlags.DrawNow |
                                                        PawnRenderFlags.Cache |
                                                        PawnRenderFlags.NeverAimWeapon |
                                                        PawnRenderFlags.StylingStation;

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
        return RiderLayer + node.debugLayerOffset + (parms.facing == Rot4.North ? 10f : 0);
    }

    public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
    {
        base.PostDraw(node, parms, mesh, matrix);

        if (!node.DebugEnabled)
            return;

        var mountedNode = (MountedRiderRenderNode)node;
        if (mountedNode.AnimalData.Rider == null)
            return;

        RenderRider(node, mountedNode.AnimalData.Rider, parms, matrix);
    }

    private static void RenderRider(PawnRenderNode node, Pawn rider, PawnDrawParms parentParms, Matrix4x4 matrix)
    {
        var renderer = rider.Drawer.renderer;
        var flags = BuildFlags(rider, parentParms.flags);
        var posture = rider.GetPosture();
        var facing = posture == PawnPosture.Standing || rider.Crawling
            ? parentParms.facing
            : renderer.LayingFacing();
        var angle = posture == PawnPosture.Standing ? 0f : renderer.BodyAngle(flags);

        var rootMatrix = matrix;
        var bodyDrawOffset = rider.ageTracker.CurLifeStage.bodyDrawOffset;
        if (bodyDrawOffset != Vector3.zero)
            rootMatrix *= Matrix4x4.Translate(bodyDrawOffset);

        if (angle != 0f)
            rootMatrix *= Matrix4x4.Rotate(Quaternion.AngleAxis(angle, Vector3.up));

        var riderParms = new PawnDrawParms
        {
            pawn = rider,
            matrix = rootMatrix,
            facing = facing,
            rotDrawMode = renderer.CurRotDrawMode,
            posture = posture,
            flags = flags,
            tint = parentParms.tint * renderer.flasher.CurColor.ToTransparent(InvisibilityUtility.GetAlpha(rider)) * (node.tree.debugTint ?? Color.white),
            bed = rider.CurrentBed(),
            carriedThing = rider.carryTracker?.CarriedThing,
            dead = rider.Dead,
            crawling = rider.Crawling,
            swimming = rider.Swimming
        };

        renderer.renderTree.EnsureInitialized(flags);
        renderer.renderTree.ParallelPreDraw(riderParms);
        renderer.renderTree.Draw(riderParms);
    }

    private static PawnRenderFlags BuildFlags(Pawn rider, PawnRenderFlags parentFlags)
    {
        var flags = parentFlags & ForwardedRenderFlags;

        if (!rider.health.hediffSet.HasHead)
            flags |= PawnRenderFlags.HeadStump;

        if (rider.IsPsychologicallyInvisible())
            flags |= PawnRenderFlags.Invisible;

        if (rider.Swimming)
            return flags | PawnRenderFlags.NoBody;

        return flags | PawnRenderFlags.Headgear | PawnRenderFlags.Clothes;
    }
}