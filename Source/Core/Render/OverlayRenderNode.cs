using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GiddyUp;
using Verse;

namespace GiddyUpCore.Core.Render
{
    internal class OverlayRenderNode : PawnRenderNode
    {
        public Pawn Rider { get; }
        public CompOverlay Overlay;

        public OverlayRenderNode(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, CompOverlay overlay, Pawn rider) : base(pawn, props, tree)
        {
            Overlay = overlay;
            Rider = rider;
        }
    }
}
