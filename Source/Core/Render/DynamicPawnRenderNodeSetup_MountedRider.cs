using System;
using System.Collections.Generic;
using GiddyUp;
using GiddyUpCore.Compatibility;
using GiddyUpCore.Core.Render;
using RimWorld;
using UnityEngine;
using Verse;
using Types = GiddyUpCore.Compatibility.AnimalApparel.Types;

namespace GiddyUpCore.Core;

internal sealed class DynamicPawnRenderNodeSetup_MountedRider : DynamicPawnRenderNodeSetup
{
    private readonly List<Type> after = new();
    public override List<Type> SetupAfter => after;

    public DynamicPawnRenderNodeSetup_MountedRider()
    {
        if(CompatibilityLoader.AnimalApparelInstalled && Types.DynamicPawnRenderNodeSetup_Animal_Apparel != null)
            after.Add(Types.DynamicPawnRenderNodeSetup_Animal_Apparel);
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

        if (pawn.TryGetComp<CompOverlay>(out var comp))
        {
            var overlayProps = new PawnRenderNodeProperties()
            {
                debugLabel = "Overlay",
                useGraphic = true,
                workerClass = typeof(OverlayRenderNodeWorker),
                nodeClass = typeof(OverlayRenderNode),
                baseLayer = 75f,
                texPath = "Things/Pawn/Horse2_overlay_south",
                drawSize = ParseHelper.FromStringVector2("2.1") * 0.66f,

            };

            yield return (new OverlayRenderNode(pawn, overlayProps, tree, comp, rider), bodyNode);
        }
    }
}