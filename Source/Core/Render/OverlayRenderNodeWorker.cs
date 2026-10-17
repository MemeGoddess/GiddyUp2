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
    //Workers are shared per type, so nothing about a specific node or species may be cached in fields here
    internal class OverlayRenderNodeWorker : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            var enabled =
                node.debugEnabled &&
                node is OverlayRenderNode { Properties: not null } mountedNode &&
                mountedNode.AnimalData.Rider != null &&
                mountedNode.graphics[parms.facing.AsInt] != null;
            return enabled;
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            var a = Vector3.one;
            var bodyGraphic = parms.pawn.drawer?.renderer?.BodyGraphic;
            var drawSize = bodyGraphic?.drawSize ?? GetGraphic(node, parms).drawSize;
            a.x *= node.Props.drawSize.x * node.debugScale * drawSize.x;
            a.z *= node.Props.drawSize.y * node.debugScale * drawSize.y;

            if (!parms.flags.FlagSet(PawnRenderFlags.Portrait))
            {
                if (node.TryGetAnimationScale(parms, out var offset))
                    a = a.ScaledBy(offset);
            }

            if (node.Props.drawData != null)
                a *= node.Props.drawData.ScaleFor(parms.pawn);

            return a;
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            pivot = default;
            //Resolved per call: the worker is shared by every species and this runs from the parallel pre-draw
            var overlay = (node as OverlayRenderNode)?.Properties?.GetOverlay(parms.facing);
            if (overlay == null)
                return Vector3.zero;

            var offset = parms.pawn.gender == Gender.Female 
                ? overlay.offsetFemale 
                : overlay.offsetMale;
            if (offset == Vector3.zero)
                offset = overlay.offsetDefault;

            //Mirror on the facing being drawn, not the pawn's current rotation (portraits, laying down)
            if (parms.facing == Rot4.West)
                offset.x = -offset.x;

            offset += node.debugOffset;

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
