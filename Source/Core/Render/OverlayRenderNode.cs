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
        public CompProperties_Overlay Properties;

        public OverlayRenderNode(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, CompProperties_Overlay properties, Pawn rider) : base(pawn, props, tree)
        {
            Properties = properties;
            Rider = rider;
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn) => MeshPool.GetMeshSetForSize(1f, 1f);

        public override IEnumerable<Graphic?> GraphicsFor(Pawn pawn)
        {
            foreach (var rotation in Rot4.AllRotations)
            {
                var overlay = Properties.GetOverlay(rotation);
                if (overlay == null)
                {
                    yield return null;
                    continue;
                }
                 
                var graphicData =
                    (pawn.gender == Gender.Female
                        ? overlay.graphicDataFemale
                        : overlay.graphicDataMale)
                    ?? overlay.graphicDataDefault;

                if (overlay.allVariants.Any())
                {
                    var pawnVariant = pawn.drawer.renderer.BodyGraphic.path
                        .Split('/').Last();

                    var variantGraphicData = overlay.allVariants.FirstOrDefault(x =>
                        x.texPath.Split('/').Last().Split(overlay.stringDelimiter.ToCharArray())[0] == pawnVariant);

                    var textPath = variantGraphicData?.texPath ?? graphicData?.texPath;
                    
                    if (variantGraphicData != null)
                    {
                        variantGraphicData.CopyFrom(graphicData);
                        variantGraphicData.texPath = textPath;
                    }
                    graphicData = variantGraphicData;
                }

                yield return graphicData?.Graphic;
            }
        }
    }
}
