#nullable disable
using System;
using System.Reflection;
using GiddyUp;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUp.Editor
{
    public static class Preview
    {
        private static MethodInfo animalData;

        public static void Patch(HarmonyLib.Harmony harmony)
        {
            var node = AccessTools.TypeByName("GiddyUpCore.Core.MountedRiderRenderNode");
            var worker = AccessTools.TypeByName("GiddyUpCore.Core.MountedRiderRenderNodeWorker");
            animalData = node != null ? AccessTools.PropertyGetter(node, "AnimalData") : null;
            var postDraw = worker != null ? AccessTools.Method(worker, "PostDraw") : null;
            if (animalData == null || postDraw == null)
            {
                Log.Warning("[Giddy-Up Editor] Rider render classes not found, previews show the animal only.");
                return;
            }
            harmony.Patch(postDraw, prefix: new HarmonyMethod(typeof(Preview), nameof(PostDraw_Prefix)));
        }

        public static bool PostDraw_Prefix(PawnRenderNode node, PawnDrawParms parms, Matrix4x4 matrix)
        {
            if (!parms.flags.FlagSet(PawnRenderFlags.Portrait))
            {
                return true;
            }
            if (!node.DebugEnabled)
            {
                return false;
            }
            var data = animalData.Invoke(node, null) as ExtendedPawnData;
            var rider = data != null ? data.Rider : null;
            if (rider == null)
            {
                return false;
            }
            try
            {
                rider.Drawer.renderer.RenderCache(parms.facing, 0f, matrix.MultiplyPoint3x4(Vector3.zero), true, true, true, true);
            }
            catch (Exception ex)
            {
                Log.WarningOnce("[Giddy-Up Editor] Rider preview: " + ex.Message, 81723401);
            }
            return false;
        }

        public static void Draw(Rect img, Pawn animal, Pawn previewRider, Rot4 rot, float zoom)
        {
            var data = animal.GetExtendedPawnData();
            bool swap = previewRider != null && data.Rider == null;
            var riderData = swap ? previewRider.GetExtendedPawnData() : null;
            var oldOffset = swap ? riderData.drawOffset : 0f;
            if (swap)
            {
                data.Rider = previewRider;
                riderData.drawOffset = TextureUtility.FetchCache(animal);
                PortraitsCache.SetDirty(animal);
            }
            try
            {
                var tex = PortraitsCache.Get(animal, new Vector2(img.width, img.height), rot, default(Vector3), zoom);
                GUI.DrawTexture(img, tex, ScaleMode.StretchToFill);
            }
            finally
            {
                if (swap)
                {
                    data.Rider = null;
                    riderData.drawOffset = oldOffset;
                }
            }
        }
    }
}
