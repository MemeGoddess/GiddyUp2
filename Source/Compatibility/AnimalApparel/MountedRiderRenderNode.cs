using System.Collections.Generic;
using System.Linq;
using GiddyUp;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Compatibility.AnimalApparel;

internal static class MountedRiderRenderNodeUtility
{
    public static bool ShouldUseMountedRenderNode(Pawn rider)
    {
        return CompatibilityLoader.AnimalApparelInstalled &&
               rider.IsMounted() &&
               rider.GetExtendedPawnData().Mount is { } mount &&
               mount.GetExtendedPawnData().ReservedBy == rider &&
               mount.apparel?.WornApparel.Any() == true;
    }

    public static bool TryGetMountedRider(Pawn mount, out Pawn? rider)
    {
        rider = mount.GetExtendedPawnData().ReservedBy;
        return rider != null &&
               rider.IsMounted() &&
               rider.GetExtendedPawnData().Mount == mount &&
               ShouldUseMountedRenderNode(rider);
    }

    public static Vector3 GetMountedRiderOffset(Pawn rider, Pawn mount, Rot4 rotation)
    {
        var riderData = rider.GetExtendedPawnData();
        var offset = Vector3.zero;
        if (riderData.drawOffset != -1)
            offset.z += riderData.drawOffset;

        var customOffsets = mount.def.GetModExtension<DrawingOffset>();
        if (customOffsets == null)
            return offset;

        if (rotation == Rot4.North)
            return offset + customOffsets.northOffset;
        if (rotation == Rot4.South)
            return offset + customOffsets.southOffset;
        if (rotation == Rot4.East)
            return offset + customOffsets.eastOffset;
        return offset + customOffsets.westOffset;
    }

    public static void RefreshMountedAnimalGraphics(Pawn? animal)
    {
        if (!CompatibilityLoader.AnimalApparelInstalled || animal == null)
            return;

        animal.Drawer?.renderer?.SetAllGraphicsDirty();
    }
}

internal sealed class MountedRiderRenderNode : PawnRenderNode
{
    public Pawn Rider { get; }

    public Pawn Mount => tree.pawn;

    public MountedRiderRenderNode(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Pawn rider)
        : base(pawn, props, tree)
    {
        Rider = rider;
    }

    public override GraphicMeshSet MeshSetFor(Pawn pawn) => null!;
}

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

internal sealed class DynamicPawnRenderNodeSetup_MountedRider : DynamicPawnRenderNodeSetup
{
    public override List<System.Type> SetupAfter
    {
        get
        {
            var animalGearSetup = AccessTools.TypeByName("AnimalGear.Graphics.DynamicPawnRenderNodeSetup_Animal_Apparel");
            return animalGearSetup == null ? null : [animalGearSetup];
        }
    }

    public override bool HumanlikeOnly => false;

    public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn,
        PawnRenderTree tree)
    {
        if (!MountedRiderRenderNodeUtility.TryGetMountedRider(pawn, out var rider) || rider == null)
            yield break;

        if (!tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Body, out var bodyNode) || bodyNode == null)
            yield break;

        var props = new PawnRenderNodeProperties
        {
            debugLabel = "Mounted rider",
            useGraphic = false,
            workerClass = typeof(MountedRiderRenderNodeWorker),
            nodeClass = typeof(MountedRiderRenderNode),
            baseLayer = 71f
        };

        yield return (new MountedRiderRenderNode(pawn, props, tree, rider), bodyNode);
    }
}

[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.DynamicDrawPhaseAt))]
internal static class PawnRenderer_DynamicDrawPhaseAt_MountedRider
{
    private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnField =
        AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

    private static bool Prefix(PawnRenderer __instance, DrawPhase phase)
    {
        if (phase != DrawPhase.ParallelPreDraw && phase != DrawPhase.Draw)
            return true;

        return !MountedRiderRenderNodeUtility.ShouldUseMountedRenderNode(PawnField(__instance));
    }
}