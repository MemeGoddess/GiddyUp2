#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GiddyUp.Editor
{
    public static class ArmorOverlay
    {
        private const float Lift = 0.0005f;

        private sealed class Entry
        {
            public readonly List<Material>[] perFacing = new List<Material>[4];
        }

        private static Type overlayNodeType;
        private static readonly ConditionalWeakTable<PawnRenderNode, Entry> entries = new ConditionalWeakTable<PawnRenderNode, Entry>();
        private static readonly Dictionary<string, Texture2D> masked = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<Texture, Color32[]> pixelCache = new Dictionary<Texture, Color32[]>();
        private static readonly Dictionary<Texture, Vector2Int> sizeCache = new Dictionary<Texture, Vector2Int>();
        private static readonly List<PawnRenderNode> apparelNodes = new List<PawnRenderNode>();

        public static void Apply(HarmonyLib.Harmony harmony, Type overlayNode)
        {
            overlayNodeType = overlayNode;
            if (overlayNodeType == null)
            {
                return;
            }
            harmony.Patch(AccessTools.Method(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.PostDraw)), postfix: new HarmonyMethod(typeof(ArmorOverlay), nameof(PostDraw_Postfix)));
        }

        public static void Clear()
        {
            foreach (var tex in masked.Values)
            {
                Graveyard.Bury(tex);
            }
            masked.Clear();
            pixelCache.Clear();
            sizeCache.Clear();
        }

        public static void PostDraw_Postfix(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
        {
            if (node == null || node.GetType() != overlayNodeType || mesh == null)
            {
                return;
            }
            try
            {
                var mats = MaterialsFor(node, parms.facing);
                if (mats == null || mats.Count == 0)
                {
                    return;
                }
                var lifted = Matrix4x4.Translate(new Vector3(0f, Lift, 0f)) * matrix;
                for (int i = 0; i < mats.Count; i++)
                {
                    GenDraw.DrawMeshNowOrLater(mesh, lifted, mats[i], parms.DrawNow);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[Giddy-Up Editor] Armor overlay: " + ex.Message, 0x4E31A7);
            }
        }

        private static List<Material> MaterialsFor(PawnRenderNode node, Rot4 facing)
        {
            var entry = entries.GetOrCreateValue(node);
            int index = facing.AsInt;
            if (entry.perFacing[index] != null)
            {
                return entry.perFacing[index];
            }
            var list = new List<Material>();
            entry.perFacing[index] = list;
            var graphics = node.Graphics;
            var overlay = graphics != null && index < graphics.Count ? graphics[index] : null;
            var maskTex = overlay != null ? overlay.MatAt(Rot4.North).mainTexture : null;
            if (maskTex == null || node.tree == null || node.tree.rootNode == null)
            {
                return list;
            }
            apparelNodes.Clear();
            Collect(node.tree.rootNode);
            var texRot = facing == Rot4.West ? Rot4.East : facing;
            foreach (var apparelNode in apparelNodes)
            {
                var graphic = apparelNode.PrimaryGraphic;
                var mat = graphic != null ? graphic.MatAt(texRot) : null;
                var tex = mat != null ? mat.mainTexture : null;
                if (tex == null)
                {
                    continue;
                }
                var cut = Masked(tex, maskTex);
                if (cut == null)
                {
                    continue;
                }
                var req = new MaterialRequest(cut, mat.shader, mat.color)
                {
                    colorTwo = mat.HasProperty("_ColorTwo") ? mat.GetColor("_ColorTwo") : Color.white,
                    maskTex = mat.HasProperty("_MaskTex") ? mat.GetTexture("_MaskTex") as Texture2D : null
                };
                list.Add(MaterialPool.MatFrom(req));
            }
            apparelNodes.Clear();
            return list;
        }

        private static void Collect(PawnRenderNode n)
        {
            if (n.apparel != null && n.PrimaryGraphic != null)
            {
                apparelNodes.Add(n);
            }
            if (n.children == null)
            {
                return;
            }
            for (int i = 0; i < n.children.Length; i++)
            {
                if (n.children[i] != null)
                {
                    Collect(n.children[i]);
                }
            }
        }

        private static Texture2D Masked(Texture apparel, Texture mask)
        {
            string key = apparel.GetInstanceID() + "|" + mask.GetInstanceID();
            Texture2D result;
            if (masked.TryGetValue(key, out result))
            {
                return result;
            }
            masked[key] = null;
            Vector2Int aSize, mSize;
            var a = Pixels(apparel, out aSize);
            var m = Pixels(mask, out mSize);
            if (a == null || m == null)
            {
                return null;
            }
            var outPx = new Color32[a.Length];
            bool any = false;
            for (int y = 0; y < aSize.y; y++)
            {
                int my = Mathf.Min(mSize.y - 1, y * mSize.y / aSize.y);
                for (int x = 0; x < aSize.x; x++)
                {
                    int mx = Mathf.Min(mSize.x - 1, x * mSize.x / aSize.x);
                    int i = y * aSize.x + x;
                    if (m[my * mSize.x + mx].a > 8 && a[i].a > 0)
                    {
                        outPx[i] = a[i];
                        any = true;
                    }
                }
            }
            if (!any)
            {
                return null;
            }
            result = new Texture2D(aSize.x, aSize.y, TextureFormat.RGBA32, true);
            result.name = "GUE_ArmorOverlay_" + apparel.name;
            result.filterMode = FilterMode.Trilinear;
            result.SetPixels32(outPx);
            result.Apply(true, false);
            masked[key] = result;
            return result;
        }

        private static Color32[] Pixels(Texture tex, out Vector2Int size)
        {
            Color32[] px;
            if (pixelCache.TryGetValue(tex, out px))
            {
                size = sizeCache[tex];
                return px;
            }
            size = default;
            var t2 = tex as Texture2D;
            if (t2 == null)
            {
                return null;
            }
            try
            {
                px = t2.GetPixels32();
                size = new Vector2Int(t2.width, t2.height);
            }
            catch (Exception)
            {
                px = null;
            }
            if (px == null || px.Length == 0)
            {
                string file = t2.CoreTexture != null ? t2.CoreTexture.SourcePath : null;
                if (file == null || !File.Exists(file) || !file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                copy.LoadImage(File.ReadAllBytes(file));
                px = copy.GetPixels32();
                size = new Vector2Int(copy.width, copy.height);
                Graveyard.Bury(copy);
            }
            pixelCache[tex] = px;
            sizeCache[tex] = size;
            return px;
        }
    }
}
