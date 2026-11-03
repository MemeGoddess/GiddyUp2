#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GiddyUp;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUp.Editor
{
    public static class Live
    {
        private sealed class Original
        {
            public bool hadOffset;
            public Vector3 north, south, east, west;
            public CompProperties_Overlay props;
            public bool addedProps;
            public CompProperties_Overlay.GraphicOverlay[] slots = new CompProperties_Overlay.GraphicOverlay[3];
            public Vector3[] offsetDefault = new Vector3[3];
            public Vector3[] offsetMale = new Vector3[3];
            public Vector3[] offsetFemale = new Vector3[3];
            public bool mountable;
            public bool drawInFront;
            public List<GraphicData> bodyData = new List<GraphicData>();
            public List<Vector2> bodySizes = new List<Vector2>();
        }

        private static readonly System.Reflection.FieldInfo[] stageGraphics = typeof(PawnKindLifeStage).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(GraphicData)).ToArray();

        private static readonly Dictionary<ThingDef, Original> originals = new Dictionary<ThingDef, Original>();
        private static readonly Dictionary<ushort, AnimalEdit> active = new Dictionary<ushort, AnimalEdit>();
        private static readonly Dictionary<string, int> loadedVersion = new Dictionary<string, int>();
        private static readonly Dictionary<ushort, Dictionary<string, string>> maskPaths = new Dictionary<ushort, Dictionary<string, string>>();
        private static AccessTools.FieldRef<object, CompProperties_Overlay> nodeProps;
        private static Type compOverlayType;
        private static AccessTools.FieldRef<ThingWithComps, List<ThingComp>> compsRef;
        private static AccessTools.FieldRef<GraphicData, Graphic> cachedGraphic;

        public static string ConfigDir
        {
            get { return Path.Combine(GenFilePaths.ConfigFolderPath, "GiddyUpEditor"); }
        }

        public static string MaskDir(ThingDef def)
        {
            return Path.Combine(ConfigDir, "Masks", def.defName);
        }

        public static void Patch(HarmonyLib.Harmony harmony)
        {
            var node = AccessTools.TypeByName("GiddyUpCore.Core.Render.OverlayRenderNode");
            var worker = AccessTools.TypeByName("GiddyUpCore.Core.Render.OverlayRenderNodeWorker");
            compOverlayType = AccessTools.TypeByName("GiddyUp.CompOverlay");
            compsRef = AccessTools.FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");
            cachedGraphic = AccessTools.FieldRefAccess<GraphicData, Graphic>("cachedGraphic");
            if (node == null || worker == null)
            {
                Log.Warning("[Giddy-Up Editor] Overlay render classes not found, overlay offsets stay as Giddy-Up draws them.");
                return;
            }
            nodeProps = AccessTools.FieldRefAccess<CompProperties_Overlay>(node, "Properties");
            harmony.Patch(AccessTools.Method(worker, "OffsetFor"), prefix: new HarmonyMethod(typeof(Live), nameof(OffsetFor_Prefix)));
            harmony.Patch(AccessTools.Method(node, "GraphicsFor"), postfix: new HarmonyMethod(typeof(Live), nameof(GraphicsFor_Postfix)));
            harmony.Patch(AccessTools.Method(worker, "ScaleFor"), postfix: new HarmonyMethod(typeof(Live), nameof(ScaleFor_Postfix)));
            ArmorOverlay.Apply(harmony, node);
            harmony.Patch(AccessTools.Method(typeof(UIRoot), nameof(UIRoot.UIRootOnGUI)), postfix: new HarmonyMethod(typeof(Graveyard), nameof(Graveyard.Tick)));
        }

        public static bool OffsetFor_Prefix(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot, ref Vector3 __result)
        {
            pivot = default(Vector3);
            var props = nodeProps(node);
            var overlay = props != null ? props.GetOverlay(parms.facing) : null;
            AnimalEdit scaled;
            active.TryGetValue(parms.pawn.def.shortHash, out scaled);
            Vector3 offset;
            if (overlay != null)
            {
                offset = parms.pawn.gender == Gender.Female ? overlay.offsetFemale : overlay.offsetMale;
                if (offset == Vector3.zero)
                {
                    offset = overlay.offsetDefault;
                }
            }
            else if (scaled != null && scaled.Slot(SlotFor(parms.facing)).hasOffsets)
            {
                var oe = scaled.Slot(SlotFor(parms.facing));
                offset = parms.pawn.gender == Gender.Female ? oe.offsetFemale : oe.offsetMale;
                if (offset == Vector3.zero)
                {
                    offset = oe.offsetDefault;
                }
            }
            else
            {
                offset = Vector3.zero;
            }
            if (scaled != null && !Mathf.Approximately(scaled.bodyScale, 1f))
            {
                offset *= scaled.bodyScale;
            }
            if (parms.facing == Rot4.West)
            {
                offset.x = -offset.x;
            }
            __result = offset + node.debugOffset;
            return false;
        }

        public static void GraphicsFor_Postfix(Pawn pawn, ref IEnumerable<Graphic> __result)
        {
            Dictionary<string, string> paths;
            if (pawn == null || !maskPaths.TryGetValue(pawn.def.shortHash, out paths) || paths.Count == 0)
            {
                return;
            }
            var body = pawn.Drawer != null && pawn.Drawer.renderer != null ? pawn.Drawer.renderer.BodyGraphic : null;
            if (body == null)
            {
                return;
            }
            string variant = body.path.Split('/').Last();
            AnimalEdit e;
            active.TryGetValue(pawn.def.shortHash, out e);
            var list = __result != null ? __result.ToList() : new List<Graphic>();
            while (list.Count < 4)
            {
                list.Add(null);
            }
            for (int i = 0; i < 4; i++)
            {
                int slot = SlotFor(new Rot4(i));
                string path;
                if (e != null && e.Slot(slot).disabled || !paths.TryGetValue(slot + "|" + variant, out path))
                {
                    continue;
                }
                list[i] = GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Cutout, Vector2.one, body.color, body.colorTwo);
            }
            __result = list;
        }

        public static void ScaleFor_Postfix(PawnDrawParms parms, ref Vector3 __result)
        {
            AnimalEdit e;
            if (parms.pawn == null || !active.TryGetValue(parms.pawn.def.shortHash, out e))
            {
                return;
            }
            float s = e.Slot(SlotFor(parms.facing)).scale;
            if (!Mathf.Approximately(s, 1f))
            {
                __result.x *= s;
                __result.z *= s;
            }
        }

        public static int SlotFor(Rot4 rot)
        {
            return rot == Rot4.South ? 0 : rot == Rot4.North ? 2 : 1;
        }

        public static void ApplyAll(EditorSettings settings)
        {
            foreach (var e in settings.animals.ToList())
            {
                try
                {
                    var def = e != null && !string.IsNullOrEmpty(e.defName) ? DefDatabase<ThingDef>.GetNamedSilentFail(e.defName) : null;
                    if (def != null)
                    {
                        Apply(def, e, false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[Giddy-Up Editor] " + (e != null ? e.defName : "?") + ": " + ex.Message);
                }
            }
        }

        private static Original Snapshot(ThingDef def)
        {
            Original o;
            if (originals.TryGetValue(def, out o))
            {
                return o;
            }
            o = new Original();
            var ext = def.GetModExtension<DrawingOffset>();
            if (ext != null)
            {
                o.hadOffset = true;
                o.north = ext.northOffset;
                o.south = ext.southOffset;
                o.east = ext.eastOffset;
                o.west = ext.westOffset;
            }
            o.props = def.GetCompProperties<CompProperties_Overlay>();
            if (o.props != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var slot = GetSlot(o.props, i);
                    o.slots[i] = slot;
                    if (slot != null)
                    {
                        o.offsetDefault[i] = slot.offsetDefault;
                        o.offsetMale[i] = slot.offsetMale;
                        o.offsetFemale[i] = slot.offsetFemale;
                    }
                }
            }
            foreach (var kind in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (kind.race != def || kind.lifeStages == null)
                {
                    continue;
                }
                foreach (var stage in kind.lifeStages)
                {
                    foreach (var field in stageGraphics)
                    {
                        var gd = field.GetValue(stage) as GraphicData;
                        if (gd != null && !o.bodyData.Contains(gd))
                        {
                            o.bodyData.Add(gd);
                            o.bodySizes.Add(gd.drawSize);
                        }
                    }
                }
            }
            o.mountable = ModSettings_GiddyUp.MountableCache.Contains(def.shortHash);
            o.drawInFront = ModSettings_GiddyUp.DrawRulesCache.Contains(def.shortHash);
            originals[def] = o;
            return o;
        }

        public static CompProperties_Overlay.GraphicOverlay GetSlot(CompProperties_Overlay p, int slot)
        {
            return slot == 0 ? p.overlayFront : slot == 1 ? p.overlaySide : p.overlayBack;
        }

        private static void SetSlot(CompProperties_Overlay p, int slot, CompProperties_Overlay.GraphicOverlay value)
        {
            if (slot == 0)
            {
                p.overlayFront = value;
            }
            else if (slot == 1)
            {
                p.overlaySide = value;
            }
            else
            {
                p.overlayBack = value;
            }
        }

        public static Vector2 OriginalBodySize(ThingDef def)
        {
            var o = Snapshot(def);
            return o.bodySizes.Count > 0 ? o.bodySizes[o.bodySizes.Count - 1] : Vector2.one;
        }

        private static void ApplyScale(AnimalEdit e, Original o)
        {
            for (int i = 0; i < o.bodyData.Count; i++)
            {
                var size = o.bodySizes[i] * e.bodyScale;
                if (o.bodyData[i].drawSize != size)
                {
                    o.bodyData[i].drawSize = size;
                    ResetGraphic(o.bodyData[i]);
                }
            }
        }

        public static bool OriginalMountable(ThingDef def)
        {
            return Snapshot(def).mountable;
        }

        public static bool OriginalDrawInFront(ThingDef def)
        {
            return Snapshot(def).drawInFront;
        }

        public static Vector3 OriginalRider(ThingDef def, Rot4 rot)
        {
            var o = Snapshot(def);
            return rot == Rot4.North ? o.north : rot == Rot4.South ? o.south : rot == Rot4.East ? o.east : o.west;
        }

        public static bool HasOriginalSlot(ThingDef def, int slot)
        {
            return Snapshot(def).slots[slot] != null;
        }

        public static Vector3 OriginalSlotOffset(ThingDef def, int slot, int which)
        {
            var o = Snapshot(def);
            return which == 1 ? o.offsetMale[slot] : which == 2 ? o.offsetFemale[slot] : o.offsetDefault[slot];
        }

        public static void Apply(ThingDef def, AnimalEdit e, bool refresh)
        {
            var o = Snapshot(def);
            var ext = def.GetModExtension<DrawingOffset>();
            if (e.hasRider)
            {
                if (ext == null)
                {
                    ext = new DrawingOffset();
                    if (def.modExtensions == null)
                    {
                        def.modExtensions = new List<DefModExtension>();
                    }
                    def.modExtensions.Add(ext);
                }
                ext.northOffset = e.north;
                ext.southOffset = e.south;
                ext.eastOffset = e.east;
                ext.westOffset = e.west;
            }
            else if (ext != null)
            {
                if (o.hadOffset)
                {
                    ext.northOffset = o.north;
                    ext.southOffset = o.south;
                    ext.eastOffset = o.east;
                    ext.westOffset = o.west;
                }
                else
                {
                    def.modExtensions.Remove(ext);
                }
            }

            ApplyScale(e, o);
            bool mountable = e.mountable < 0 ? o.mountable : e.mountable == 1;
            if (mountable)
            {
                if (ModSettings_GiddyUp.MountableCache.Add(def.shortHash))
                {
                    Setup.CalculateCaravanSpeed(def);
                }
            }
            else if (ModSettings_GiddyUp.MountableCache.Remove(def.shortHash))
            {
                Setup.CalculateCaravanSpeed(def, true);
            }
            bool front = e.drawInFront < 0 ? o.drawInFront : e.drawInFront == 1;
            if (front)
            {
                ModSettings_GiddyUp.DrawRulesCache.Add(def.shortHash);
            }
            else
            {
                ModSettings_GiddyUp.DrawRulesCache.Remove(def.shortHash);
            }

            var props = def.GetCompProperties<CompProperties_Overlay>();
            if (props == null && e.masks.Count > 0 && def.comps != null)
            {
                props = new CompProperties_Overlay();
                def.comps.Add(props);
                o.props = props;
                o.addedProps = true;
            }
            ApplyMasks(def, e);
            if (props != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var edit = e.Slot(i);
                    var slot = o.slots[i];
                    if (slot == null)
                    {
                        SetSlot(props, i, null);
                        continue;
                    }
                    if (edit.hasOffsets)
                    {
                        slot.offsetDefault = edit.offsetDefault;
                        slot.offsetMale = edit.offsetMale;
                        slot.offsetFemale = edit.offsetFemale;
                    }
                    else
                    {
                        slot.offsetDefault = o.offsetDefault[i];
                        slot.offsetMale = o.offsetMale[i];
                        slot.offsetFemale = o.offsetFemale[i];
                    }
                    SetSlot(props, i, edit.disabled ? null : slot);
                }
            }

            if (e.IsEmpty)
            {
                active.Remove(def.shortHash);
            }
            else
            {
                active[def.shortHash] = e;
            }
            if (refresh)
            {
                Refresh(def);
            }
        }

        private static void ApplyMasks(ThingDef def, AnimalEdit e)
        {
            ArmorOverlay.Clear();
            var wanted = new HashSet<string>();
            var paths = new Dictionary<string, string>();
            foreach (var m in e.masks)
            {
                string path = RegisterMask(def, m);
                if (path != null)
                {
                    wanted.Add(path);
                    paths[m.slot + "|" + m.variant] = path;
                }
            }
            if (paths.Count > 0)
            {
                maskPaths[def.shortHash] = paths;
            }
            else
            {
                maskPaths.Remove(def.shortHash);
            }
            DropMasks(def, wanted);
        }

        public static string VirtualPath(ThingDef def, MaskEdit m)
        {
            return "GiddyUpEditor/" + def.defName + "/" + m.variant + "~mask" + m.slot;
        }

        private static string RegisterMask(ThingDef def, MaskEdit m)
        {
            string file = Path.Combine(MaskDir(def), m.FileName);
            if (!File.Exists(file))
            {
                return null;
            }
            string key = VirtualPath(def, m);
            var holder = GiddyUpEditorMod.Instance.Content.GetContentHolder<Texture2D>();
            Texture2D tex;
            int loaded;
            bool known = holder.contentList.TryGetValue(key, out tex) && tex != null;
            if (!known || !loadedVersion.TryGetValue(key, out loaded) || loaded != m.version)
            {
                var bytes = File.ReadAllBytes(file);
                if (!known)
                {
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    tex.name = m.variant + "~mask" + m.slot;
                    tex.filterMode = FilterMode.Trilinear;
                    holder.contentList[key] = tex;
                }
                tex.LoadImage(bytes);
                tex.Apply(true, false);
                loadedVersion[key] = m.version;
            }
            return key;
        }

        private static void DropMasks(ThingDef def, HashSet<string> keep)
        {
            var holder = GiddyUpEditorMod.Instance.Content.GetContentHolder<Texture2D>();
            string prefix = "GiddyUpEditor/" + def.defName + "/";
            foreach (var key in holder.contentList.Keys.Where(k => k.StartsWith(prefix) && (keep == null || !keep.Contains(k))).ToList())
            {
                Graveyard.Bury(holder.contentList[key]);
                holder.contentList.Remove(key);
                loadedVersion.Remove(key);
            }
        }

        public static void ApplyOffsets(ThingDef def, AnimalEdit e, Pawn preview)
        {
            Snapshot(def);
            if (e.hasRider)
            {
                var ext = def.GetModExtension<DrawingOffset>();
                if (ext == null)
                {
                    ext = new DrawingOffset();
                    if (def.modExtensions == null)
                    {
                        def.modExtensions = new List<DefModExtension>();
                    }
                    def.modExtensions.Add(ext);
                }
                ext.northOffset = e.north;
                ext.southOffset = e.south;
                ext.eastOffset = e.east;
                ext.westOffset = e.west;
            }
            var props = def.GetCompProperties<CompProperties_Overlay>();
            if (props != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var slot = GetSlot(props, i);
                    var edit = e.Slot(i);
                    if (slot != null && edit.hasOffsets)
                    {
                        slot.offsetDefault = edit.offsetDefault;
                        slot.offsetMale = edit.offsetMale;
                        slot.offsetFemale = edit.offsetFemale;
                    }
                }
            }
            if (e.IsEmpty)
            {
                active.Remove(def.shortHash);
            }
            else
            {
                active[def.shortHash] = e;
            }
            if (preview != null)
            {
                PortraitsCache.SetDirty(preview);
            }
        }

        public static void Refresh(ThingDef def)
        {
            var props = def.GetCompProperties<CompProperties_Overlay>();
            if (props != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var slot = GetSlot(props, i);
                    if (slot == null)
                    {
                        continue;
                    }
                    ResetGraphic(slot.graphicDataDefault);
                    ResetGraphic(slot.graphicDataMale);
                    ResetGraphic(slot.graphicDataFemale);
                    if (slot.allVariants != null)
                    {
                        foreach (var v in slot.allVariants)
                        {
                            ResetGraphic(v);
                        }
                    }
                }
            }
            if (Find.Maps == null)
            {
                return;
            }
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn.def != def)
                    {
                        continue;
                    }
                    if (props != null && compOverlayType != null && pawn.AllComps.All(c => c.GetType() != compOverlayType))
                    {
                        var comp = (ThingComp)Activator.CreateInstance(compOverlayType);
                        comp.parent = pawn;
                        comp.Initialize(props);
                        compsRef(pawn).Add(comp);
                    }
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                    PortraitsCache.SetDirty(pawn);
                }
            }
        }

        private static void ResetGraphic(GraphicData gd)
        {
            if (gd != null)
            {
                cachedGraphic(gd) = null;
            }
        }

        public static void Forget(ThingDef def, AnimalEdit e)
        {
            var empty = new AnimalEdit { defName = def.defName };
            Apply(def, empty, true);
            active.Remove(def.shortHash);
            DropMasks(def, null);
        }
    }
}
