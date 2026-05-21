using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core.Render
{
    internal class OverlayRenderNodeWorker : PawnRenderNodeWorker
    {
        
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            var enabled =  node.debugEnabled && node is OverlayRenderNode { Rider.Spawned: true, Overlay: not null } mountedNode &&
                   MountedRiderRenderNodeUtility.ShouldUseMountedRenderNode(mountedNode.Rider);
            return enabled;
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            var overlay = (node as OverlayRenderNode)?.Overlay.Prop.GetOverlay(parms.facing);
            if (overlay == null)
                return Vector3.one;
            var graphic = 
                (parms.pawn.gender == Gender.Female ? overlay.graphicDataFemale : overlay.graphicDataMale) ??
                           overlay.graphicDataDefault;
            if(graphic  == null) return Vector3.zero;
            var drawSize = graphic.drawSize;
            
            return (drawSize * 0.66f).ToVector3();
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            pivot = default;
            //return (node as OverlayRenderNode)?.Overlay.GetOffset() ?? Vector3.zero;
            var overlay = (node as OverlayRenderNode)?.Overlay.Prop.GetOverlay(parms.facing);
            if (overlay == null)
                return Vector3.zero;
            var offset = parms.pawn.gender == Gender.Female ? overlay.offsetFemale : overlay.offsetMale;
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
    }
}
