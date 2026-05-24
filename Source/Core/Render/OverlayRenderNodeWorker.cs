using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GiddyUp;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core.Render
{
    internal class OverlayRenderNodeWorker : PawnRenderNodeWorker
    {
        private bool init;
        private OverlayRenderNode? renderNode;
        private CompProperties_Overlay? overlayProperties;
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            var enabled =  node.debugEnabled && node is OverlayRenderNode { Rider.Spawned: true, Properties: not null } mountedNode &&
                   MountedRiderRenderNodeUtility.ShouldUseMountedRenderNode(mountedNode.Rider);
            return enabled;
        }

        //public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        //{
        //    var overlay = (node as OverlayRenderNode)?.Properties.Prop.GetOverlay(parms.facing);
        //    if (overlay == null)
        //        return Vector3.one;
        //    var graphic = 
        //        (parms.pawn.gender == Gender.Female ? overlay.graphicDataFemale : overlay.graphicDataMale) ??
        //                   overlay.graphicDataDefault;
        //    if(graphic  == null) return Vector3.zero;
        //    var drawSize = graphic.drawSize;
            
        //    return (drawSize * 0.66f).ToVector3();
        //}

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            pivot = default;
            renderNode ??= node as OverlayRenderNode;
            overlayProperties ??= renderNode?.Properties;
            var overlay = overlayProperties?.GetOverlay(parms.facing);
            if (overlay == null)
                return Vector3.zero;

            var offset = parms.pawn.gender == Gender.Female 
                ? overlay.offsetFemale 
                : overlay.offsetMale;
            if (offset == Vector3.zero)
                offset = overlay.offsetDefault;

            if (parms.pawn.Rotation == Rot4.West)
                offset.x = -offset.x;

            return offset;
        }

        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return 75 + node.debugLayerOffset;
        }

        public override Graphic GetGraphic(PawnRenderNode node, PawnDrawParms parms)
        {
            return node.Graphics[parms.facing.AsInt]!;
        }
    }
}
