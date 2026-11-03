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
    public class Dialog_MaskEditor : Window
    {
        private const float TitleHeight = 34f;
        private const float FooterHeight = 44f;
        private const float SideWidth = 360f;
        private const float Handle = 9f;
        private const int MaxHistory = 60;

        private readonly Pawn animal;
        private readonly ThingDef def;
        private readonly AnimalEdit edit;
        private readonly int slot;
        private readonly Rot4 rot;
        private readonly string variant;
        private readonly MaskEdit mask;
        private readonly Pawn previewRider;
        private readonly List<List<MaskPolygon>> undo = new List<List<MaskPolygon>>();
        private readonly List<List<MaskPolygon>> redo = new List<List<MaskPolygon>>();
        private Texture shown;
        private Texture2D readable;
        private Color32[] source;
        private int width;
        private int height;
        private Texture2D coverage;
        private int current = -1;
        private int dragPoly = -1;
        private int dragPoint = -1;
        private bool dirty;
        private float zoomFactor = 1f;
        private Vector2 pan;
        private bool panning;
        private string problem;
        private MaskPolygon openPoly;
        private bool confirmCopy;
        private Dictionary<string, string> variants;

        private static readonly AccessTools.FieldRef<AlternateGraphic, string> altTexPath = AccessTools.FieldRefAccess<AlternateGraphic, string>("texPath");

        public override Vector2 InitialSize
        {
            get { return new Vector2(1180f, 820f); }
        }

        public override float Margin
        {
            get { return 0f; }
        }

        public ThingDef Def
        {
            get { return def; }
        }

        public static Rot4 RotFor(int slot)
        {
            return slot == 0 ? Rot4.South : slot == 2 ? Rot4.North : Rot4.East;
        }

        public Dialog_MaskEditor(Pawn animal, AnimalEdit edit, int slot, Pawn previewRider = null)
        {
            this.previewRider = previewRider;
            this.animal = animal;
            def = animal.def;
            this.edit = edit;
            this.slot = slot;
            rot = RotFor(slot);
            var body = animal.Drawer.renderer.BodyGraphic;
            variant = body != null ? Path.GetFileName(body.path) : def.defName;
            mask = edit.Mask(variant, slot);
            if (mask == null)
            {
                mask = new MaskEdit { variant = variant, slot = slot };
            }
            current = mask.polygons.Count - 1;
            doWindowBackground = false;
            drawShadow = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            draggable = true;
            forcePause = true;
            LoadSource(body);
            RebuildCoverage();
        }

        private static Texture2D ReadableCopy(Texture2D tex)
        {
            string file = tex.CoreTexture != null ? tex.CoreTexture.SourcePath : null;
            if (file == null || !File.Exists(file) || !file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            copy.LoadImage(File.ReadAllBytes(file));
            return copy;
        }

        private void LoadSource(Graphic body)
        {
            try
            {
                var tex = body != null ? body.MatAt(rot).mainTexture as Texture2D : null;
                shown = tex;
                if (tex == null)
                {
                    problem = "GUE_MaskNoTexture".Translate();
                    return;
                }
                readable = ReadableCopy(tex);
                var src = readable ?? tex;
                width = src.width;
                height = src.height;
                source = src.GetPixels32();
                if (readable != null)
                {
                    shown = readable;
                }
            }
            catch (Exception ex)
            {
                problem = "GUE_MaskNoTexture".Translate() + " " + ex.Message;
                source = null;
                if (readable != null)
                {
                    shown = null;
                    Graveyard.Bury(readable);
                    readable = null;
                }
            }
        }

        public override void PreClose()
        {
            base.PreClose();
            if (coverage != null)
            {
                Graveyard.Bury(coverage);
                coverage = null;
            }
            if (readable != null)
            {
                Graveyard.Bury(readable);
                readable = null;
            }
            shown = null;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var r = new Rect(0f, 0f, windowRect.width, windowRect.height);
            Style.Fill(r, Style.Outer);
            string dir = (slot == 0 ? "GUE_SlotFront" : slot == 2 ? "GUE_SlotBack" : "GUE_SlotSide").Translate();
            Style.TitleBar(new Rect(0f, 0f, r.width, TitleHeight), "GUE_MaskTitle".Translate(def.LabelCap, variant).ToString() + " · " + dir, def.defName);
            var body = new Rect(0f, TitleHeight, r.width, r.height - TitleHeight - FooterHeight);
            KeyInput();
            Canvas(new Rect(body.x + 14f, body.y + 14f, body.width - SideWidth - 28f, body.height - 28f));
            Style.Fill(new Rect(body.xMax - SideWidth, body.y, 1f, body.height), Style.Edge);
            Side(new Rect(body.xMax - SideWidth + 15f, body.y + 14f, SideWidth - 29f, body.height - 28f));
            Footer(new Rect(0f, r.height - FooterHeight, r.width, FooterHeight));
            Style.Frame(r, Color.black);
            if (dirty && Event.current.type == EventType.MouseUp)
            {
                Commit();
            }
        }

        private void KeyInput()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || !(e.control || e.command))
            {
                return;
            }
            if (e.keyCode == KeyCode.Z && !e.shift)
            {
                Undo();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Y || e.keyCode == KeyCode.Z && e.shift)
            {
                Redo();
                e.Use();
            }
        }

        private static List<MaskPolygon> Copy(List<MaskPolygon> polys)
        {
            return polys.Select(p => new MaskPolygon { points = new List<Vector2>(p.points) }).ToList();
        }

        private void Remember()
        {
            undo.Add(Copy(mask.polygons));
            if (undo.Count > MaxHistory)
            {
                undo.RemoveAt(0);
            }
            redo.Clear();
        }

        private void Restore(List<MaskPolygon> state)
        {
            mask.polygons = Copy(state);
            openPoly = null;
            dragPoint = -1;
            dragPoly = -1;
            current = mask.polygons.Count - 1;
            Changed();
        }

        private void Undo()
        {
            if (undo.Count == 0)
            {
                return;
            }
            redo.Add(Copy(mask.polygons));
            var state = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            Restore(state);
        }

        private void Redo()
        {
            if (redo.Count == 0)
            {
                return;
            }
            undo.Add(Copy(mask.polygons));
            var state = redo[redo.Count - 1];
            redo.RemoveAt(redo.Count - 1);
            Restore(state);
        }

        private Rect ImageRect(Rect area)
        {
            float aspect = height > 0 ? (float)width / height : 1f;
            float w = area.width, h = area.width / aspect;
            if (h > area.height)
            {
                h = area.height;
                w = h * aspect;
            }
            return new Rect(area.center.x - w / 2f, area.center.y - h / 2f, w, h);
        }

        private Vector2 ToScreen(Rect img, Vector2 uv)
        {
            return new Vector2(img.x + uv.x * img.width, img.y + uv.y * img.height);
        }

        private Vector2 ToUv(Rect img, Vector2 screen)
        {
            return new Vector2(Mathf.Clamp01((screen.x - img.x) / img.width), Mathf.Clamp01((screen.y - img.y) / img.height));
        }

        private void Canvas(Rect area)
        {
            Style.Fill(area, Style.ImageGround);
            Style.Frame(area, Style.Edge);
            if (problem != null)
            {
                Style.Info(area.ContractedBy(20f), problem);
                return;
            }
            var group = area.ContractedBy(1f);
            GUI.BeginGroup(group);
            var local = new Rect(0f, 0f, group.width, group.height);
            var fit = ImageRect(local.ContractedBy(10f));
            var img = new Rect(fit.center.x - fit.width * zoomFactor / 2f + pan.x, fit.center.y - fit.height * zoomFactor / 2f + pan.y, fit.width * zoomFactor, fit.height * zoomFactor);
            Checker(img, local);
            if (shown != null)
            {
                GUI.DrawTexture(img, shown, ScaleMode.StretchToFill);
            }
            if (coverage != null)
            {
                GUI.color = Style.Mask;
                GUI.DrawTexture(img, coverage, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }
            Style.Frame(img, Style.Edge);
            DrawPolygons(img);
            DrawEdgeHint(img);
            ZoomInput(local, fit, img);
            HandleInput(img);
            GUI.EndGroup();
        }

        private void ZoomInput(Rect local, Rect fit, Rect img)
        {
            var e = Event.current;
            if (e.type == EventType.ScrollWheel && local.Contains(e.mousePosition))
            {
                float next = Mathf.Clamp(zoomFactor * (e.delta.y < 0f ? 1.25f : 0.8f), 1f, 8f);
                var uv = new Vector2((e.mousePosition.x - img.x) / img.width, (e.mousePosition.y - img.y) / img.height);
                float w = fit.width * next, h = fit.height * next;
                pan = new Vector2(e.mousePosition.x - uv.x * w - (fit.center.x - w / 2f), e.mousePosition.y - uv.y * h - (fit.center.y - h / 2f));
                zoomFactor = next;
                if (Mathf.Approximately(zoomFactor, 1f))
                {
                    pan = Vector2.zero;
                }
                e.Use();
            }
            else if (e.type == EventType.MouseDown && local.Contains(e.mousePosition) && (e.button == 2 || e.button == 0 && e.alt))
            {
                panning = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && panning)
            {
                pan += e.delta;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && panning)
            {
                panning = false;
                e.Use();
            }
        }

        private static void Checker(Rect img, Rect clip)
        {
            const float cell = 16f;
            var a = new Color(0.18f, 0.19f, 0.2f);
            var b = new Color(0.22f, 0.23f, 0.24f);
            var visible = Rect.MinMaxRect(Mathf.Max(img.x, clip.x), Mathf.Max(img.y, clip.y), Mathf.Min(img.xMax, clip.xMax), Mathf.Min(img.yMax, clip.yMax));
            if (visible.width <= 0f || visible.height <= 0f)
            {
                return;
            }
            int i0 = Mathf.Max(0, Mathf.FloorToInt((visible.x - img.x) / cell));
            int i1 = Mathf.CeilToInt((visible.xMax - img.x) / cell);
            int j0 = Mathf.Max(0, Mathf.FloorToInt((visible.y - img.y) / cell));
            int j1 = Mathf.CeilToInt((visible.yMax - img.y) / cell);
            if (i1 - i0 > 2000 || j1 - j0 > 2000)
            {
                return;
            }
            for (int j = j0; j < j1; j++)
            {
                float y = Mathf.Max(img.y + j * cell, visible.y);
                float yEnd = Mathf.Min(img.y + (j + 1) * cell, visible.yMax);
                for (int i = i0; i < i1; i++)
                {
                    float x = Mathf.Max(img.x + i * cell, visible.x);
                    float xEnd = Mathf.Min(img.x + (i + 1) * cell, visible.xMax);
                    if (xEnd > x && yEnd > y)
                    {
                        Style.Fill(new Rect(x, y, xEnd - x, yEnd - y), (i + j) % 2 == 1 ? a : b);
                    }
                }
            }
        }

        private void DrawPolygons(Rect img)
        {
            for (int p = 0; p < mask.polygons.Count; p++)
            {
                var pts = mask.polygons[p].points;
                bool sel = p == current;
                var col = sel ? Style.Path : new Color(Style.Path.r, Style.Path.g, Style.Path.b, 0.5f);
                for (int i = 0; i < pts.Count; i++)
                {
                    bool last = i == pts.Count - 1;
                    if (last && !IsPolyClosed(p))
                    {
                        break;
                    }
                    var a = ToScreen(img, pts[i]);
                    var b = ToScreen(img, pts[(i + 1) % pts.Count]);
                    Style.Line(a, b, col, sel ? 2f : 1f);
                }
                if (sel && !IsPolyClosed(p) && pts.Count > 0 && Mouse.IsOver(img))
                {
                    Style.Line(ToScreen(img, pts[pts.Count - 1]), Event.current.mousePosition, new Color(col.r, col.g, col.b, 0.5f), 1f);
                }
                for (int i = 0; i < pts.Count; i++)
                {
                    var s = ToScreen(img, pts[i]);
                    var h = new Rect(s.x - Handle / 2f, s.y - Handle / 2f, Handle, Handle);
                    bool first = i == 0 && sel && !IsPolyClosed(p) && pts.Count >= 3;
                    Style.Fill(h, Mouse.IsOver(h.ExpandedBy(3f)) || first ? Style.SelectedEdge : sel ? Style.Path : Style.Faint);
                    Style.Frame(h, Color.black);
                }
            }
        }

        private void DrawEdgeHint(Rect img)
        {
            int hp, hi, ep, ei;
            Vector2 at;
            var mouse = Event.current.mousePosition;
            if (dragPoint >= 0 || panning || !img.Contains(mouse) || HitPoint(img, mouse, out hp, out hi) || !HitAnyEdge(img, mouse, out ep, out ei, out at))
            {
                return;
            }
            var h = new Rect(at.x - 4f, at.y - 4f, 8f, 8f);
            Style.Fill(h, Style.SelectedEdge);
            Style.Frame(h, Color.black);
        }

        private bool IsPolyClosed(int p)
        {
            return p != current || mask.polygons[p].points.Count >= 3 && openPoly != mask.polygons[p];
        }

        private void HandleInput(Rect img)
        {
            var e = Event.current;
            var mouse = e.mousePosition;
            if (e.type == EventType.MouseDown && img.ExpandedBy(Handle).Contains(mouse))
            {
                int hp, hi;
                if (HitPoint(img, mouse, out hp, out hi))
                {
                    if (e.button == 1)
                    {
                        Remember();
                        mask.polygons[hp].points.RemoveAt(hi);
                        if (mask.polygons[hp].points.Count == 0)
                        {
                            mask.polygons.RemoveAt(hp);
                            if (openPoly != null && !mask.polygons.Contains(openPoly))
                            {
                                openPoly = null;
                            }
                            current = mask.polygons.Count - 1;
                        }
                        Changed();
                        e.Use();
                        return;
                    }
                    if (e.button == 0)
                    {
                        if (openPoly != null && mask.polygons[hp] == openPoly && hi == 0 && openPoly.points.Count >= 3)
                        {
                            Remember();
                            openPoly = null;
                            current = hp;
                            Changed();
                            e.Use();
                            return;
                        }
                        Remember();
                        current = hp;
                        dragPoly = hp;
                        dragPoint = hi;
                        e.Use();
                        return;
                    }
                }
                if (e.button == 0)
                {
                    int ep, ei;
                    Vector2 at;
                    if (HitAnyEdge(img, mouse, out ep, out ei, out at))
                    {
                        Remember();
                        mask.polygons[ep].points.Insert(ei + 1, ToUv(img, at));
                        current = ep;
                        dragPoly = ep;
                        dragPoint = ei + 1;
                        RebuildCoverage();
                        dirty = true;
                        e.Use();
                        return;
                    }
                }
                if (e.button == 0 && img.Contains(mouse))
                {
                    Remember();
                    if (openPoly == null)
                    {
                        openPoly = new MaskPolygon();
                        mask.polygons.Add(openPoly);
                        current = mask.polygons.Count - 1;
                    }
                    openPoly.points.Add(ToUv(img, mouse));
                    if (openPoly.points.Count >= 3)
                    {
                        Changed();
                    }
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDrag && dragPoint >= 0)
            {
                mask.polygons[dragPoly].points[dragPoint] = ToUv(img, mouse);
                RebuildCoverage();
                dirty = true;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && dragPoint >= 0)
            {
                dragPoint = -1;
                dragPoly = -1;
                dirty = true;
            }
        }

        private bool HitPoint(Rect img, Vector2 mouse, out int poly, out int index)
        {
            for (int p = mask.polygons.Count - 1; p >= 0; p--)
            {
                var pts = mask.polygons[p].points;
                for (int i = 0; i < pts.Count; i++)
                {
                    if ((ToScreen(img, pts[i]) - mouse).sqrMagnitude <= (Handle + 3f) * (Handle + 3f) / 4f + 9f)
                    {
                        poly = p;
                        index = i;
                        return true;
                    }
                }
            }
            poly = -1;
            index = -1;
            return false;
        }

        private bool HitAnyEdge(Rect img, Vector2 mouse, out int poly, out int index, out Vector2 at)
        {
            poly = -1;
            index = -1;
            at = mouse;
            float bestD = 36f;
            for (int p = 0; p < mask.polygons.Count; p++)
            {
                var pts = mask.polygons[p].points;
                bool open = mask.polygons[p] == openPoly;
                int edges = open ? pts.Count - 1 : pts.Count;
                if (pts.Count < 2)
                {
                    continue;
                }
                for (int i = 0; i < edges; i++)
                {
                    var a = ToScreen(img, pts[i]);
                    var b = ToScreen(img, pts[(i + 1) % pts.Count]);
                    var ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(mouse - a, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
                    var q = a + ab * t;
                    float d = (q - mouse).sqrMagnitude;
                    if (d < bestD && t > 0.02f && t < 0.98f)
                    {
                        bestD = d;
                        poly = p;
                        index = i;
                        at = q;
                    }
                }
            }
            return poly >= 0;
        }

        private void Changed()
        {
            RebuildCoverage();
            dirty = true;
            Commit();
        }

        private List<List<Vector2>> ClosedPolygons()
        {
            var list = new List<List<Vector2>>();
            foreach (var p in mask.polygons)
            {
                if (p.points.Count >= 3)
                {
                    list.Add(p.points);
                }
            }
            return list;
        }

        private bool[] Raster()
        {
            return Raster(ClosedPolygons(), width, height);
        }

        private static bool[] Raster(List<List<Vector2>> polys, int width, int height)
        {
            var bits = new bool[width * height];
            foreach (var pts in polys)
            {
                float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
                foreach (var v in pts)
                {
                    minX = Mathf.Min(minX, v.x);
                    maxX = Mathf.Max(maxX, v.x);
                    minY = Mathf.Min(minY, v.y);
                    maxY = Mathf.Max(maxY, v.y);
                }
                int x0 = Mathf.Clamp(Mathf.FloorToInt(minX * width), 0, width - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(maxX * width), 0, width - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(minY * height), 0, height - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(maxY * height), 0, height - 1);
                for (int yTop = y0; yTop <= y1; yTop++)
                {
                    float py = (yTop + 0.5f) / height;
                    for (int x = x0; x <= x1; x++)
                    {
                        float px = (x + 0.5f) / width;
                        bool odd = false;
                        for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
                        {
                            if ((pts[i].y > py) != (pts[j].y > py) && px < (pts[j].x - pts[i].x) * (py - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                            {
                                odd = !odd;
                            }
                        }
                        if (odd)
                        {
                            bits[(height - 1 - yTop) * width + x] = true;
                        }
                    }
                }
            }
            return bits;
        }

        private void RebuildCoverage()
        {
            if (source == null)
            {
                return;
            }
            var bits = Raster();
            var px = new Color32[bits.Length];
            for (int i = 0; i < bits.Length; i++)
            {
                px[i] = bits[i] && source[i].a > 0 ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
            if (coverage == null || coverage.width != width || coverage.height != height)
            {
                if (coverage != null)
                {
                    Graveyard.Bury(coverage);
                }
                coverage = new Texture2D(width, height, TextureFormat.RGBA32, false);
                coverage.filterMode = FilterMode.Point;
            }
            coverage.SetPixels32(px);
            coverage.Apply(false, false);
        }

        private void Commit()
        {
            if (source == null)
            {
                dirty = false;
                return;
            }
            var bits = Raster();
            bool any = false;
            var px = new Color32[bits.Length];
            for (int i = 0; i < bits.Length; i++)
            {
                px[i] = bits[i] ? source[i] : new Color32(0, 0, 0, 0);
                any |= bits[i] && source[i].a > 0;
            }
            string dir = Live.MaskDir(def);
            string oldFile = Path.Combine(dir, mask.FileName);
            int oldVersion = mask.version;
            bool wasListed = edit.masks.Contains(mask);
            string newFile = null;
            try
            {
                if (any)
                {
                    Directory.CreateDirectory(dir);
                    mask.version++;
                    newFile = Path.Combine(dir, mask.FileName);
                    string tmp = newFile + ".tmp";
                    File.WriteAllBytes(tmp, Png.Encode(px, width, height));
                    if (File.Exists(newFile))
                    {
                        File.Delete(newFile);
                    }
                    File.Move(tmp, newFile);
                    if (!wasListed)
                    {
                        edit.masks.Add(mask);
                    }
                }
                else
                {
                    edit.masks.Remove(mask);
                }
                GiddyUpEditorMod.Save();
            }
            catch (Exception ex)
            {
                mask.version = oldVersion;
                if (wasListed && !edit.masks.Contains(mask))
                {
                    edit.masks.Add(mask);
                }
                else if (!wasListed)
                {
                    edit.masks.Remove(mask);
                }
                try
                {
                    if (newFile != null && File.Exists(newFile))
                    {
                        File.Delete(newFile);
                    }
                }
                catch (Exception)
                {
                }
                dirty = false;
                Messages.Message("GUE_SaveFailed".Translate(ex.Message), MessageTypeDefOf.RejectInput, false);
                return;
            }
            dirty = false;
            try
            {
                if (oldFile != newFile && File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                }
            }
            catch (Exception)
            {
            }
            Live.Apply(def, edit, true);
        }

        private Dictionary<string, string> OtherVariants()
        {
            var found = new Dictionary<string, string>();
            void Add(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                string name = path.Split('/').Last();
                string suffix = slot == 0 ? "_south" : slot == 2 ? "_north" : "_east";
                if (name != variant && !found.ContainsKey(name) && ContentFinder<Texture2D>.Get(path + suffix, false) != null)
                {
                    found[name] = path;
                }
            }
            foreach (var kind in DefDatabase<PawnKindDef>.AllDefsListForReading)
            {
                if (kind.race != def)
                {
                    continue;
                }
                if (kind.lifeStages != null)
                {
                    foreach (var stage in kind.lifeStages)
                    {
                        Add(stage.bodyGraphicData != null ? stage.bodyGraphicData.texPath : null);
                        Add(stage.femaleGraphicData != null ? stage.femaleGraphicData.texPath : null);
                    }
                }
                if (kind.alternateGraphics != null)
                {
                    foreach (var alt in kind.alternateGraphics)
                    {
                        Add(altTexPath(alt));
                    }
                }
            }
            return found;
        }

        private void CopyToVariants()
        {
            string suffix = slot == 0 ? "_south" : slot == 2 ? "_north" : "_east";
            string dir = Live.MaskDir(def);
            var polys = ClosedPolygons();
            var oldFiles = new List<string>();
            int copied = 0;
            var failed = new List<string>();
            foreach (var pair in variants)
            {
                var tex = ContentFinder<Texture2D>.Get(pair.Value + suffix, false);
                if (tex == null)
                {
                    failed.Add(pair.Key);
                    continue;
                }
                Texture2D copy = null;
                try
                {
                    copy = ReadableCopy(tex);
                    var src = copy ?? tex;
                    int w = src.width, h = src.height;
                    var pixels = src.GetPixels32();
                    var bits = Raster(polys, w, h);
                    var px = new Color32[bits.Length];
                    bool any = false;
                    for (int i = 0; i < bits.Length; i++)
                    {
                        px[i] = bits[i] ? pixels[i] : new Color32(0, 0, 0, 0);
                        any |= bits[i] && pixels[i].a > 0;
                    }
                    if (!any)
                    {
                        failed.Add(pair.Key);
                        continue;
                    }
                    var target = edit.Mask(pair.Key, slot);
                    if (target == null)
                    {
                        target = new MaskEdit { variant = pair.Key, slot = slot };
                        edit.masks.Add(target);
                    }
                    oldFiles.Add(Path.Combine(dir, target.FileName));
                    Directory.CreateDirectory(dir);
                    target.version++;
                    string file = Path.Combine(dir, target.FileName);
                    File.WriteAllBytes(file + ".tmp", Png.Encode(px, w, h));
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                    File.Move(file + ".tmp", file);
                    target.polygons = Copy(mask.polygons);
                    copied++;
                }
                catch (Exception)
                {
                    failed.Add(pair.Key);
                }
                finally
                {
                    if (copy != null)
                    {
                        Graveyard.Bury(copy);
                    }
                }
            }
            if (!edit.masks.Contains(mask))
            {
                edit.masks.Add(mask);
            }
            if (!GiddyUpEditorMod.TrySave())
            {
                return;
            }
            foreach (var old in oldFiles)
            {
                try
                {
                    if (File.Exists(old) && edit.masks.All(m => Path.Combine(dir, m.FileName) != old))
                    {
                        File.Delete(old);
                    }
                }
                catch (Exception)
                {
                }
            }
            Live.Apply(def, edit, true);
            Messages.Message("GUE_MaskCopied".Translate(copied), MessageTypeDefOf.TaskCompletion, false);
            if (failed.Count > 0)
            {
                Messages.Message("GUE_MaskCopyFailed".Translate(string.Join(", ", failed)), MessageTypeDefOf.CautionInput, false);
            }
        }

        private void Side(Rect r)
        {
            float y = r.y;
            Style.SectionHead(new Rect(r.x, y, r.width, 24f), "GUE_MaskPreview".Translate());
            y += 30f;
            float s = Mathf.Min(r.width, 300f);
            var img = new Rect(r.x + (r.width - s) / 2f, y, s, s);
            Style.Fill(img, Style.ImageGround);
            Style.Frame(img, Style.Edge);
            Preview.Draw(img.ContractedBy(2f), animal, previewRider, rot, 0.5f);
            y += s + 10f;
            if (!animal.IsMountedAnimal() && previewRider == null)
            {
                y += Style.Info(new Rect(r.x, y, r.width, 0f), "GUE_MaskNeedsRider".Translate()) + 8f;
            }
            Style.SectionHead(new Rect(r.x, y, r.width, 24f), "GUE_MaskTools".Translate());
            y += 30f;
            float half = (r.width - 8f) / 2f;
            if (Style.Ghost(new Rect(r.x, y, half, 22f), "GUE_MaskNewArea".Translate()))
            {
                if (openPoly != null && openPoly.points.Count < 3)
                {
                    mask.polygons.Remove(openPoly);
                }
                openPoly = null;
                current = -1;
            }
            if (Style.Ghost(new Rect(r.x + half + 8f, y, half, 22f), "GUE_MaskDeleteArea".Translate()) && current >= 0 && current < mask.polygons.Count)
            {
                Remember();
                if (mask.polygons[current] == openPoly)
                {
                    openPoly = null;
                }
                mask.polygons.RemoveAt(current);
                current = mask.polygons.Count - 1;
                Changed();
            }
            y += 28f;
            if (Style.Ghost(new Rect(r.x, y, r.width, 22f), "GUE_MaskClear".Translate()) && mask.polygons.Count > 0)
            {
                Remember();
                mask.polygons.Clear();
                openPoly = null;
                current = -1;
                Changed();
            }
            y += 28f;
            if (variants == null)
            {
                variants = OtherVariants();
            }
            bool canCopy = variants.Count > 0 && ClosedPolygons().Count > 0;
            string copyLabel = confirmCopy ? "GUE_OverviewConfirm".Translate().ToString() : "GUE_MaskCopyVariants".Translate(variants.Count).ToString();
            var copyRect = new Rect(r.x, y, r.width, 22f);
            if (Style.Button(copyRect, copyLabel, confirmCopy, !canCopy) && canCopy)
            {
                if (confirmCopy)
                {
                    confirmCopy = false;
                    CopyToVariants();
                }
                else
                {
                    confirmCopy = true;
                }
            }
            TooltipHandler.TipRegion(copyRect, "GUE_MaskCopyVariantsTip".Translate(string.Join(", ", variants.Keys)));
            y += 28f;
            if (Style.Button(new Rect(r.x, y, half, 22f), "GUE_MaskUndo".Translate(), false, undo.Count == 0))
            {
                Undo();
            }
            if (Style.Button(new Rect(r.x + half + 8f, y, half, 22f), "GUE_MaskRedo".Translate(), false, redo.Count == 0))
            {
                Redo();
            }
            y += 34f;
            Style.SectionHead(new Rect(r.x, y, r.width, 24f), "GUE_MaskZoom".Translate());
            y += 30f;
            float third = (r.width - 16f) / 3f;
            if (Style.Ghost(new Rect(r.x, y, third, 22f), "−"))
            {
                zoomFactor = Mathf.Max(1f, zoomFactor * 0.8f);
                pan *= 0.8f;
            }
            Style.Text(new Rect(r.x + third + 8f, y, third, 22f), Mathf.RoundToInt(zoomFactor * 100f) + " %", Style.Value, GameFont.Small, TextAnchor.MiddleCenter);
            if (Style.Ghost(new Rect(r.x + 2f * third + 16f, y, third, 22f), "+"))
            {
                zoomFactor = Mathf.Min(8f, zoomFactor * 1.25f);
                pan *= 1.25f;
            }
            y += 26f;
            if (Style.Ghost(new Rect(r.x, y, r.width, 22f), "GUE_MaskFit".Translate()))
            {
                zoomFactor = 1f;
                pan = Vector2.zero;
            }
            y += 34f;
            y += Style.Faintext(new Rect(r.x, y, r.width, 0f), "GUE_MaskHelp".Translate()) + 8f;
            Style.Faintext(new Rect(r.x, y, r.width, 0f), "GUE_MaskVariant".Translate(variant));
        }

        private void Footer(Rect r)
        {
            Style.Footer(r);
            var close = new Rect(r.xMax - 204f, r.y + 8f, 190f, r.height - 16f);
            if (Style.Button(close, "GUE_Close".Translate()))
            {
                if (openPoly != null && openPoly.points.Count < 3)
                {
                    mask.polygons.Remove(openPoly);
                    openPoly = null;
                }
                Commit();
                Close();
            }
            Style.Text(new Rect(r.x + 14f, r.y, close.x - r.x - 24f, r.height), "GUE_MaskSavedWhere".Translate(), Style.Faint, GameFont.Tiny);
        }
    }
}
