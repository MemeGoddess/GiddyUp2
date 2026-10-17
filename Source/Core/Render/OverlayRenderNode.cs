using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GiddyUp;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core.Render
{
    internal class OverlayRenderNode : PawnRenderNode
    {
        public ExtendedPawnData AnimalData { get; }
        public CompProperties_Overlay Properties;

        public OverlayRenderNode(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, CompProperties_Overlay properties) : base(pawn, props, tree)
        {
            Properties = properties;
            AnimalData = pawn.GetExtendedPawnData();
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn) => MeshPool.GetMeshSetForSize(1, 1);

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
                if (graphicData?.graphicClass == null)
                {
                    yield return null;
                    continue;
                }

                var bodyGraphic = pawn.drawer?.renderer?.BodyGraphic;
                var texPath = graphicData.texPath;
                if (overlay.allVariants?.Any() ?? false)
                {
                    var pawnVariant = bodyGraphic?.path.Split('/').Last();
                    var variantGraphicData = overlay.allVariants.FirstOrDefault(x =>
                        x.texPath.Split('/').Last().Split(overlay.stringDelimiter.ToCharArray())[0] == pawnVariant);
                    texPath = variantGraphicData?.texPath ?? texPath;
                }
                if (texPath.NullOrEmpty())
                {
                    yield return null;
                    continue;
                }

                //Build the graphic directly; writing into the def's GraphicData made the first pawn's colour stick for the species
                var shader = (graphicData.shaderType ?? ShaderTypeDefOf.Cutout).Shader;
                yield return GraphicDatabase.Get(graphicData.graphicClass, texPath, shader, graphicData.drawSize,
                    bodyGraphic?.color ?? Color.white, bodyGraphic?.colorTwo ?? Color.white, graphicData,
                    graphicData.shaderParameters, graphicData.maskPath);
            }
        }
    }
}
