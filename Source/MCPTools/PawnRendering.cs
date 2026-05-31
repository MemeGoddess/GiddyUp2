using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GiddyUp;
using GiddyUpCore.Core;
using GiddyUpCore.Core.Render;
using RimMCP.Tools;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUpCore.MCP
{
    internal static class PawnRendering
    {
        [MCPTool("get_offsets", "Get Pawn Offsets",
            "Returns the Vector3 Offsets by Animal DefName to be used for GiddyUp.DrawingOffset modExtensions")]
        internal static Dictionary<string, Offset> GetOffsets()
        {
            var mounted = Find.CurrentMap.mapPawns.ColonyAnimals
                .Select(x =>
                {
                    if (!x.IsMountedAnimal(out _))
                        return new KeyValuePair<string, Offset?>(x.def.defName, null);

                    if (!x.Drawer.renderer.renderTree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Body, out var body))
                        return new KeyValuePair<string, Offset?>(x.def.defName, null);

                    var mount = body.children.FirstOrDefault(x => x is MountedRiderRenderNode);
                    if (mount == null)
                        return new KeyValuePair<string, Offset?>(x.def.defName, null);

                    if (mount.DebugOffset == Vector3.zero)
                        return new KeyValuePair<string, Offset?>(x.def.defName, null);

                    return new KeyValuePair<string, Offset?>(x.def.defName, new Offset(mount.DebugOffset, x.def.modContentPack.Name));
                })
                .ToList();

            var grouped = mounted
                .Where(x => x.Value != null)
                .GroupBy(x => x.Key)
                .ToList();

            if (grouped.Count != mounted.Count)
                Log.WarningOnce($"There were {mounted.Count - grouped.Count} duplicates", 387923);

            return grouped.ToDictionary(x => x.Key,
                x => x.FirstOrDefault()!.Value)!;
        }

        [MCPTool("get_textures", "Gets Texture Paths",
            "Returns the Texture Path by Animal DefName to be used for Overlays")]
        internal static Dictionary<string, Texture> GetTextures()
        {
            var mounted = Find.CurrentMap.mapPawns.ColonyAnimals
                .Select(x =>
                {
                    if (!x.IsMountedAnimal(out _))
                        return new KeyValuePair<string, Texture?>(x.def.defName, null);

                    return new KeyValuePair<string, Texture?>(x.def.defName,
                            new Texture(x.RaceProps.AnyPawnKind.lifeStages.Last().bodyGraphicData, x.def.modContentPack.Name));
                })
                .Where(x => x.Value != null)
                .GroupBy(x => x.Key)
                .ToList();
            return mounted.ToDictionary(x => x.Key, x => x.FirstOrDefault().Value)!;
        }
    }

    public record Offset
    {
        public string northOffset;
        public string southOffset;
        public string eastOffset;
        public string westOffset;
        public string ModContentPack;

        public Offset(Vector3 all, string modContentPack)
        {
            northOffset = all.ToString();
            southOffset = all.ToString();
            eastOffset = all.ToString();
            westOffset = all.ToString();
            ModContentPack = modContentPack;
        }
    }

    public record Texture
    {
        public GraphicDataSlim graphicDataDefault;

        public Texture(GraphicData graphic, string ModContentPack)
        {
            this.ModContentPack = ModContentPack;
            var newGraphic = new GraphicData();
            newGraphic.CopyFrom(graphic);
            newGraphic.texPath += "_south";
            graphicDataDefault = GraphicDataSlim.ToSlim(newGraphic);
        }

        public string ModContentPack { get; init; }

    }

    public record GraphicDataSlim(string texPath, string graphicClass, string drawSize, string drawRotated)
    {
        public static GraphicDataSlim ToSlim(GraphicData data)
        {
            return new GraphicDataSlim(data.texPath, "Graphic_Single", data.drawSize.x.ToString(), "false");
        }
    }
}
