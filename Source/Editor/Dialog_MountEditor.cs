#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GiddyUp;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUp.Editor
{
    public class Dialog_MountEditor : Window
    {
        private const float TitleHeight = 34f;
        private const float FooterHeight = 44f;
        private const float PanelWidth = 400f;
        private const float Portrait = 168f;
        private static readonly float[] Steps = { 0.01f, 0.05f, 0.1f };
        private static readonly float[] Zooms = { 0.35f, 0.5f, 0.75f };

        private readonly Pawn animal;
        private readonly ThingDef def;
        private readonly AnimalEdit edit;
        private bool previewOwned;
        private Pawn previewRider;
        private PawnKindDef previewKind;
        private int step = 1;
        private int zoom = 1;
        private int slot;
        private bool splitGender;
        private Vector2 scroll;

        public override Vector2 InitialSize
        {
            get { return new Vector2(1180f, 800f); }
        }

        public override float Margin
        {
            get { return 0f; }
        }

        public ThingDef Def
        {
            get { return def; }
        }

        public Dialog_MountEditor(Pawn animal)
        {
            this.animal = animal;
            def = animal.def;
            edit = GiddyUpEditorMod.Settings.GetOrCreate(def.defName);
            if (!edit.hasRider)
            {
                edit.north = Live.OriginalRider(def, Rot4.North);
                edit.south = Live.OriginalRider(def, Rot4.South);
                edit.east = Live.OriginalRider(def, Rot4.East);
                edit.west = Live.OriginalRider(def, Rot4.West);
            }
            doWindowBackground = false;
            drawShadow = true;
            doCloseX = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            draggable = true;
            forcePause = true;
            PortraitsCache.SetDirty(animal);
        }

        public override void PreClose()
        {
            base.PreClose();
            DiscardPreview();
            PortraitsCache.SetDirty(animal);
            GiddyUpEditorMod.TrySave();
        }

        private Pawn Rider
        {
            get
            {
                Thing rider;
                return animal.IsMountedAnimal(out rider) ? rider as Pawn : null;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var r = new Rect(0f, 0f, windowRect.width, windowRect.height);
            Style.Fill(r, Style.Outer);
            string mod = def.modContentPack != null ? def.modContentPack.Name : "";
            Style.TitleBar(new Rect(0f, 0f, r.width, TitleHeight), "GUE_Title".Translate(def.LabelCap, mod).ToString(), def.defName);
            var body = new Rect(0f, TitleHeight, r.width, r.height - TitleHeight - FooterHeight);
            Previews(new Rect(body.x, body.y, body.width - PanelWidth, body.height));
            Style.Fill(new Rect(body.xMax - PanelWidth, body.y, 1f, body.height), Style.Edge);
            Panel(new Rect(body.xMax - PanelWidth + 1f, body.y, PanelWidth - 1f, body.height).ContractedBy(14f));
            Footer(new Rect(0f, r.height - FooterHeight, r.width, FooterHeight));
            Style.Frame(r, Color.black);
        }

        private void Previews(Rect r)
        {
            var c = r.center;
            float cell = Portrait + 56f;
            Direction(new Rect(c.x - Portrait / 2f, r.y + 10f, Portrait, cell), Rot4.North);
            Direction(new Rect(c.x - Portrait / 2f, r.y + 10f + 2f * cell + 8f, Portrait, cell), Rot4.South);
            Direction(new Rect(c.x - Portrait / 2f - cell - 24f, r.y + 10f + cell + 4f, Portrait, cell), Rot4.West);
            Direction(new Rect(c.x + Portrait / 2f + 24f + (cell - Portrait), r.y + 10f + cell + 4f, Portrait, cell), Rot4.East);

            var mid = new Rect(c.x - Portrait / 2f, r.y + 10f + cell + 10f, Portrait, cell - 12f);
            float y = mid.y;
            Style.SubHead(new Rect(mid.x, y, mid.width, 16f), "GUE_Step".Translate());
            y += 16f;
            int s = Style.Toggle(new Rect(mid.x, y, mid.width, 22f), Steps.Select(v => v.ToString("0.00")).ToList(), step);
            if (s >= 0)
            {
                step = s;
            }
            y += 30f;
            Style.SubHead(new Rect(mid.x, y, mid.width, 16f), "GUE_Zoom".Translate());
            y += 16f;
            int z = Style.Toggle(new Rect(mid.x, y, mid.width, 22f), new List<string> { "GUE_ZoomFar".Translate(), "GUE_ZoomMid".Translate(), "GUE_ZoomNear".Translate() }, zoom);
            if (z >= 0)
            {
                zoom = z;
                PortraitsCache.SetDirty(animal);
            }
            y += 32f;
            if (Style.Ghost(new Rect(mid.x, y, mid.width, 22f), "GUE_MirrorEastWest".Translate()))
            {
                edit.hasRider = true;
                edit.west = new Vector3(-edit.east.x, edit.east.y, edit.east.z);
                Live.ApplyOffsets(def, edit, animal);
            }
            y += 26f;
            if (Style.Ghost(new Rect(mid.x, y, mid.width, 22f), "GUE_MirrorWestEast".Translate()))
            {
                edit.hasRider = true;
                edit.east = new Vector3(-edit.west.x, edit.west.y, edit.west.z);
                Live.ApplyOffsets(def, edit, animal);
            }
        }

        private void Direction(Rect r, Rot4 rot)
        {
            var img = new Rect(r.x, r.y, r.width, Portrait);
            Style.Fill(img, Style.ImageGround);
            Style.Frame(img, Style.Edge);
            Preview.Draw(img.ContractedBy(2f), animal, previewRider, rot, Zooms[zoom]);
            Style.Text(new Rect(img.x + 6f, img.y + 2f, img.width, 18f), DirectionLabel(rot), Style.Label, GameFont.Tiny);

            var v = Get(rot);
            var o = Live.OriginalRider(def, rot);
            float x = v.x, zz = v.z;
            bool changed = Style.ValueRow(new Rect(r.x, img.yMax + 6f, r.width, 22f), "X", ref x, -1.5f, 1.5f, Steps[step], o.x, 16f);
            changed |= Style.ValueRow(new Rect(r.x, img.yMax + 30f, r.width, 22f), "Z", ref zz, -1.5f, 1.5f, Steps[step], o.z, 16f);
            if (changed)
            {
                edit.hasRider = true;
                Set(rot, new Vector3(x, v.y, zz));
                Live.ApplyOffsets(def, edit, animal);
            }
        }

        private static string DirectionLabel(Rot4 rot)
        {
            return (rot == Rot4.North ? "GUE_North" : rot == Rot4.South ? "GUE_South" : rot == Rot4.East ? "GUE_East" : "GUE_West").Translate();
        }

        private Vector3 Get(Rot4 rot)
        {
            return rot == Rot4.North ? edit.north : rot == Rot4.South ? edit.south : rot == Rot4.East ? edit.east : edit.west;
        }

        private void Set(Rot4 rot, Vector3 v)
        {
            if (rot == Rot4.North)
            {
                edit.north = v;
            }
            else if (rot == Rot4.South)
            {
                edit.south = v;
            }
            else if (rot == Rot4.East)
            {
                edit.east = v;
            }
            else
            {
                edit.west = v;
            }
        }

        private void Panel(Rect r)
        {
            var view = new Rect(0f, 0f, r.width - 16f, 1020f);
            Widgets.BeginScrollView(r, ref scroll, view);
            float y = 0f;
            float w = view.width;

            Style.SectionHead(new Rect(0f, y, w, 24f), "GUE_RiderHead".Translate());
            y += 30f;
            var rider = Rider;
            if (rider != null)
            {
                var row = Style.Row(new Rect(0f, y, w, 22f), "GUE_Rider".Translate());
                Style.Text(new Rect(row.x, row.y, row.width, row.height), rider.LabelShortCap, Style.Value);
                y += 28f;
            }
            else
            {
                if (previewRider != null)
                {
                    var row = Style.Row(new Rect(0f, y, w, 22f), "GUE_PreviewRider".Translate());
                    Style.Text(new Rect(row.x, row.y, row.width - 90f, row.height), previewKind != null ? previewKind.LabelCap.ToString() : previewRider.LabelShortCap.ToString(), Style.Value);
                    if (Style.Ghost(new Rect(row.xMax - 84f, row.y + 1f, 84f, 20f), "GUE_Remove".Translate()))
                    {
                        DiscardPreview();
                        PortraitsCache.SetDirty(animal);
                    }
                    y += 28f;
                }
                float half = (w - 8f) / 2f;
                if (Style.Button(new Rect(0f, y, half, 26f), "GUE_PickRider".Translate()))
                {
                    PickRider();
                }
                if (Style.Button(new Rect(half + 8f, y, half, 26f), "GUE_PickKind".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_PickKind(SetKind));
                }
                y += 32f;
                y += Style.Info(new Rect(0f, y, w, 0f), "GUE_NoRiderInfo".Translate()) + 8f;
            }

            Style.SectionHead(new Rect(0f, y, w, 24f), "GUE_RulesHead".Translate());
            y += 30f;
            bool mountable = edit.mountable < 0 ? Live.OriginalMountable(def) : edit.mountable == 1;
            if (Style.TickRow(new Rect(0f, y, w, 22f), "GUE_Mountable".Translate(), ref mountable))
            {
                edit.mountable = mountable == Live.OriginalMountable(def) ? -1 : mountable ? 1 : 0;
                Live.Apply(def, edit, true);
            }
            y += 26f;
            bool front = edit.drawInFront < 0 ? Live.OriginalDrawInFront(def) : edit.drawInFront == 1;
            if (Style.TickRow(new Rect(0f, y, w, 22f), "GUE_DrawInFront".Translate(), ref front))
            {
                edit.drawInFront = front == Live.OriginalDrawInFront(def) ? -1 : front ? 1 : 0;
                Live.Apply(def, edit, true);
            }
            TooltipHandler.TipRegion(new Rect(0f, y, w, 22f), "GUE_DrawInFrontTip".Translate());
            y += 34f;

            Style.SectionHead(new Rect(0f, y, w, 24f), "GUE_SizeHead".Translate());
            y += 30f;
            float bodyScale = edit.bodyScale;
            if (Style.ValueRow(new Rect(0f, y, w, 22f), "GUE_BodyScale".Translate(), ref bodyScale, 0.5f, 2f, Steps[step], 1f))
            {
                edit.bodyScale = Mathf.Round(bodyScale * 100f) / 100f;
                Live.Apply(def, edit, true);
            }
            y += 28f;
            var size = Live.OriginalBodySize(def);
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            y += Style.Faintext(new Rect(0f, y, w, 0f), "GUE_BodyScaleInfo".Translate((size * edit.bodyScale).x.ToString("0.##", ci), size.x.ToString("0.##", ci))) + 8f;

            Style.SectionHead(new Rect(0f, y, w, 24f), "GUE_OverlayHead".Translate());
            y += 30f;
            int sl = Style.Toggle(new Rect(0f, y, w, 24f), new List<string> { "GUE_SlotFront".Translate(), "GUE_SlotSide".Translate(), "GUE_SlotBack".Translate() }, slot);
            if (sl >= 0)
            {
                slot = sl;
            }
            y += 32f;
            var props = def.GetCompProperties<CompProperties_Overlay>();
            var current = props != null ? Live.GetSlot(props, slot) : null;
            var oe = edit.Slot(slot);
            bool exists = current != null || oe.disabled || Live.HasOriginalSlot(def, slot) || edit.HasMasks(slot);
            if (!exists)
            {
                y += Style.Faintext(new Rect(0f, y, w, 0f), "GUE_NoOverlay".Translate()) + 8f;
            }
            else
            {
                bool shown = !oe.disabled;
                if (Style.TickRow(new Rect(0f, y, w, 22f), "GUE_Show".Translate(), ref shown))
                {
                    oe.disabled = !shown;
                    Live.Apply(def, edit, true);
                }
                y += 26f;
                if (current != null || edit.HasMasks(slot))
                {
                    if (!oe.hasOffsets)
                    {
                        oe.offsetDefault = current != null ? current.offsetDefault : Vector3.zero;
                        oe.offsetMale = current != null ? current.offsetMale : Vector3.zero;
                        oe.offsetFemale = current != null ? current.offsetFemale : Vector3.zero;
                    }
                    if (!splitGender && (oe.offsetMale != Vector3.zero || oe.offsetFemale != Vector3.zero))
                    {
                        splitGender = true;
                    }
                    bool split = splitGender;
                    if (Style.TickRow(new Rect(0f, y, w, 22f), "GUE_SplitGender".Translate(), ref split))
                    {
                        splitGender = split;
                        if (!split)
                        {
                            oe.hasOffsets = true;
                            oe.offsetMale = Vector3.zero;
                            oe.offsetFemale = Vector3.zero;
                            Live.ApplyOffsets(def, edit, animal);
                        }
                    }
                    y += 30f;
                    if (splitGender)
                    {
                        y = OffsetRows(y, w, "GUE_Male", ref oe.offsetMale, Live.OriginalSlotOffset(def, slot, 1), oe);
                        y = OffsetRows(y, w, "GUE_Female", ref oe.offsetFemale, Live.OriginalSlotOffset(def, slot, 2), oe);
                    }
                    else
                    {
                        y = OffsetRows(y, w, null, ref oe.offsetDefault, Live.OriginalSlotOffset(def, slot, 0), oe);
                    }
                    float scale = oe.scale;
                    if (Style.ValueRow(new Rect(0f, y, w, 22f), "GUE_Scale".Translate(), ref scale, 0.5f, 1.5f, 0.01f, 1f))
                    {
                        oe.scale = scale;
                        Live.ApplyOffsets(def, edit, animal);
                    }
                    y += 30f;
                }
            }
            bool hasMask = edit.HasMasks(slot);
            if (Style.Button(new Rect(0f, y, w, 26f), (hasMask ? "GUE_EditMask" : "GUE_DrawMask").Translate()))
            {
                Find.WindowStack.Add(new Dialog_MaskEditor(animal, edit, slot, previewRider));
            }
            y += 32f;
            y += Style.Faintext(new Rect(0f, y, w, 0f), (slot == 0 ? "GUE_MaskHint" : slot == 2 ? "GUE_MaskHintBack" : "GUE_MaskHintSide").Translate()) + 8f;
            y += Style.Faintext(new Rect(0f, y, w, 0f), "GUE_SlotHint".Translate()) + 8f;
            Widgets.EndScrollView();
        }

        private float OffsetRows(float y, float w, string header, ref Vector3 value, Vector3 original, OverlayEdit oe)
        {
            if (header != null)
            {
                Style.SubHead(new Rect(0f, y, w, 16f), header.Translate());
                y += 18f;
            }
            float x = value.x, z = value.z;
            bool changed = Style.ValueRow(new Rect(0f, y, w, 22f), "GUE_OffsetX".Translate(), ref x, -1.5f, 1.5f, Steps[step], original.x);
            changed |= Style.ValueRow(new Rect(0f, y + 26f, w, 22f), "GUE_OffsetZ".Translate(), ref z, -1.5f, 1.5f, Steps[step], original.z);
            if (changed)
            {
                value = new Vector3(x, value.y, z);
                oe.hasOffsets = true;
                Live.ApplyOffsets(def, edit, animal);
            }
            return y + 56f;
        }

        private void PickRider()
        {
            var options = new List<FloatMenuOption>();
            foreach (var p in animal.Map.mapPawns.FreeColonistsSpawned)
            {
                if (p.Downed || p.IsMounted())
                {
                    continue;
                }
                var pawn = p;
                options.Add(new FloatMenuOption(pawn.LabelShortCap, () =>
                {
                    DiscardPreview();
                    previewRider = pawn;
                    previewOwned = false;
                    PortraitsCache.SetDirty(animal);
                }));
            }
            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("GUE_NoColonist".Translate(), null));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void SetKind(PawnKindDef kind)
        {
            DiscardPreview();
            try
            {
                var faction = kind.defaultFactionDef != null ? Find.FactionManager.FirstFactionOfDef(kind.defaultFactionDef) : null;
                previewRider = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false));
                previewKind = kind;
                previewOwned = true;
            }
            catch (Exception ex)
            {
                previewRider = null;
                Messages.Message("GUE_KindFailed".Translate(kind.LabelCap), MessageTypeDefOf.RejectInput, false);
                Log.Warning("[Giddy-Up Editor] " + kind.defName + ": " + ex.Message);
            }
            PortraitsCache.SetDirty(animal);
        }

        private void DiscardPreview()
        {
            if (previewRider == null)
            {
                return;
            }
            try
            {
                if (previewOwned && !previewRider.Discarded)
                {
                    previewRider.Discard(true);
                }
            }
            catch (Exception)
            {
            }
            previewRider = null;
            previewKind = null;
            previewOwned = false;
        }

        private void Footer(Rect r)
        {
            Style.Footer(r);
            float bw = 190f;
            var close = new Rect(r.xMax - bw - 14f, r.y + 8f, bw, r.height - 16f);
            var export = new Rect(close.x - bw - 10f, close.y, bw, close.height);
            var reset = new Rect(export.x - bw - 10f, close.y, bw, close.height);
            var overview = new Rect(reset.x - bw - 10f, close.y, bw, close.height);
            if (Style.Button(overview, "GUE_Overview".Translate()))
            {
                Find.WindowStack.Add(new Dialog_EditOverview());
            }
            if (Style.Button(reset, "GUE_Reset".Translate()))
            {
                Dialog_EditOverview.ResetAnimal(edit);
                Find.WindowStack.Add(new Dialog_MountEditor(animal));
            }
            if (Style.Button(export, "GUE_Export".Translate()))
            {
                Export();
            }
            if (Style.Button(close, "GUE_Close".Translate()))
            {
                Close();
            }
            Style.Text(new Rect(r.x + 14f, r.y, overview.x - r.x - 24f, r.height), "GUE_SavedWhere".Translate(), Style.Faint, GameFont.Tiny);
        }

        private void Export()
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Patch>\n");
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            string V(Vector3 v) => string.Format(ci, "({0:0.00}, {1:0.00}, {2:0.00})", v.x, v.y, v.z);
            int exported = 0;
            if (edit.hasRider)
            {
                exported++;
                string ext = "Defs/ThingDef[defName=\"" + def.defName + "\"]/modExtensions/li[@Class=\"GiddyUp.DrawingOffset\"]";
                var fields = new[] { ("northOffset", edit.north), ("southOffset", edit.south), ("eastOffset", edit.east), ("westOffset", edit.west) };
                sb.Append("\t<Operation Class=\"PatchOperationConditional\">\n\t\t<xpath>").Append(ext).Append("</xpath>\n");
                sb.Append("\t\t<match Class=\"PatchOperationSequence\">\n\t\t\t<operations>\n");
                foreach (var f in fields)
                {
                    string first = ext + "[1]";
                    sb.Append("\t\t\t\t<li Class=\"PatchOperationConditional\">\n\t\t\t\t\t<xpath>").Append(first).Append("/").Append(f.Item1).Append("</xpath>\n");
                    sb.Append("\t\t\t\t\t<match Class=\"PatchOperationReplace\">\n\t\t\t\t\t\t<xpath>").Append(first).Append("/").Append(f.Item1).Append("</xpath>\n");
                    sb.Append("\t\t\t\t\t\t<value><").Append(f.Item1).Append(">").Append(V(f.Item2)).Append("</").Append(f.Item1).Append("></value>\n\t\t\t\t\t</match>\n");
                    sb.Append("\t\t\t\t\t<nomatch Class=\"PatchOperationAdd\">\n\t\t\t\t\t\t<xpath>").Append(first).Append("</xpath>\n");
                    sb.Append("\t\t\t\t\t\t<value><").Append(f.Item1).Append(">").Append(V(f.Item2)).Append("</").Append(f.Item1).Append("></value>\n\t\t\t\t\t</nomatch>\n\t\t\t\t</li>\n");
                }
                sb.Append("\t\t\t</operations>\n\t\t</match>\n");
                sb.Append("\t\t<nomatch Class=\"GiddyUp.PatchOperationDrawingOffset\">\n");
                sb.Append("\t\t\t<def>").Append(def.defName).Append("</def>\n\t\t\t<value>\n\t\t\t\t<li Class=\"GiddyUp.DrawingOffset\">\n");
                foreach (var f in fields)
                {
                    sb.Append("\t\t\t\t\t<").Append(f.Item1).Append(">").Append(V(f.Item2)).Append("</").Append(f.Item1).Append(">\n");
                }
                sb.Append("\t\t\t\t</li>\n\t\t\t</value>\n\t\t</nomatch>\n\t</Operation>\n");
            }
            if (!Mathf.Approximately(edit.bodyScale, 1f))
            {
                string[] stageFields = { "bodyGraphicData", "femaleGraphicData", "dessicatedBodyGraphicData" };
                foreach (var kind in DefDatabase<PawnKindDef>.AllDefsListForReading.Where(k => k.race == def && k.lifeStages != null))
                {
                    for (int n = 0; n < kind.lifeStages.Count; n++)
                    {
                        foreach (var field in stageFields)
                        {
                            var gd = typeof(PawnKindLifeStage).GetField(field)?.GetValue(kind.lifeStages[n]) as GraphicData;
                            if (gd == null)
                            {
                                continue;
                            }
                            exported++;
                            string basePath = "Defs/PawnKindDef[defName=\"" + kind.defName + "\"]/lifeStages/li[" + (n + 1) + "]/" + field;
                            string value = string.Format(ci, "<drawSize>({0:0.###}, {1:0.###})</drawSize>", gd.drawSize.x, gd.drawSize.y);
                            sb.Append("\t<Operation Class=\"PatchOperationConditional\">\n\t\t<success>Always</success>\n");
                            sb.Append("\t\t<xpath>").Append(basePath).Append("/drawSize</xpath>\n");
                            sb.Append("\t\t<match Class=\"PatchOperationReplace\">\n\t\t\t<xpath>").Append(basePath).Append("/drawSize</xpath>\n");
                            sb.Append("\t\t\t<value>").Append(value).Append("</value>\n\t\t</match>\n");
                            sb.Append("\t\t<nomatch Class=\"PatchOperationAdd\">\n\t\t\t<xpath>").Append(basePath).Append("</xpath>\n");
                            sb.Append("\t\t\t<value>").Append(value).Append("</value>\n\t\t</nomatch>\n");
                            sb.Append("\t</Operation>\n");
                        }
                    }
                }
            }
            string[] names = { "overlayFront", "overlaySide", "overlayBack" };
            for (int i = 0; i < 3; i++)
            {
                var oe = edit.Slot(i);
                if (!oe.hasOffsets || !Live.HasOriginalSlot(def, i))
                {
                    continue;
                }
                exported++;
                string basePath = "Defs/ThingDef[defName=\"" + def.defName + "\"]/comps/li[@Class=\"GiddyUp.CompProperties_Overlay\"]/" + names[i];
                foreach (var pair in new[] { ("offsetDefault", oe.offsetDefault), ("offsetMale", oe.offsetMale), ("offsetFemale", oe.offsetFemale) })
                {
                    sb.Append("\t<Operation Class=\"PatchOperationConditional\">\n");
                    sb.Append("\t\t<xpath>").Append(basePath).Append("/").Append(pair.Item1).Append("</xpath>\n");
                    sb.Append("\t\t<match Class=\"PatchOperationReplace\">\n\t\t\t<xpath>").Append(basePath).Append("/").Append(pair.Item1).Append("</xpath>\n");
                    sb.Append("\t\t\t<value><").Append(pair.Item1).Append(">").Append(V(pair.Item2)).Append("</").Append(pair.Item1).Append("></value>\n\t\t</match>\n");
                    sb.Append("\t\t<nomatch Class=\"PatchOperationAdd\">\n\t\t\t<xpath>").Append(basePath).Append("</xpath>\n");
                    sb.Append("\t\t\t<value><").Append(pair.Item1).Append(">").Append(V(pair.Item2)).Append("</").Append(pair.Item1).Append("></value>\n\t\t</nomatch>\n");
                    sb.Append("\t</Operation>\n");
                }
            }
            sb.Append("</Patch>\n");
            var missing = new List<string>();
            if (edit.mountable >= 0)
            {
                missing.Add("GUE_ExNotMountable".Translate());
            }
            if (edit.drawInFront >= 0)
            {
                missing.Add("GUE_ExNotFront".Translate());
            }
            if (edit.front.disabled || edit.side.disabled || edit.back.disabled)
            {
                missing.Add("GUE_ExNotHidden".Translate());
            }
            if (!Mathf.Approximately(edit.front.scale, 1f) || !Mathf.Approximately(edit.side.scale, 1f) || !Mathf.Approximately(edit.back.scale, 1f))
            {
                missing.Add("GUE_ExNotScale".Translate());
            }
            if (edit.masks.Count > 0)
            {
                missing.Add("GUE_ExNotMasks".Translate());
            }
            for (int i = 0; i < 3; i++)
            {
                if (edit.Slot(i).hasOffsets && !Live.HasOriginalSlot(def, i))
                {
                    missing.Add("GUE_ExNotNewOverlay".Translate());
                    break;
                }
            }
            if (exported == 0)
            {
                Messages.Message("GUE_ExportNothing".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            string dir = Path.Combine(Live.ConfigDir, "Export");
            string file = Path.Combine(dir, def.defName + ".xml");
            try
            {
                Directory.CreateDirectory(dir);
                string tmp = file + ".tmp";
                File.WriteAllText(tmp, sb.ToString().Replace("\n", "\r\n"));
                if (File.Exists(file))
                {
                    File.Replace(tmp, file, file + ".bak");
                }
                else
                {
                    File.Move(tmp, file);
                }
            }
            catch (Exception ex)
            {
                Messages.Message("GUE_SaveFailed".Translate(ex.Message), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Messages.Message("GUE_Exported".Translate(file), MessageTypeDefOf.TaskCompletion, false);
            if (missing.Count > 0)
            {
                Messages.Message("GUE_ExportMissing".Translate(string.Join(", ", missing)), MessageTypeDefOf.CautionInput, false);
            }
        }
    }
}
