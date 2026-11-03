#nullable disable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;
using UnityEngine;

namespace GiddyUp.Editor
{
    public class Dialog_EditOverview : Window
    {
        private const float TitleHeight = 34f;
        private const float FooterHeight = 44f;
        private const float RowHeight = 46f;

        private Vector2 scroll;
        private string confirm;

        public override Vector2 InitialSize
        {
            get { return new Vector2(720f, 620f); }
        }

        public override float Margin
        {
            get { return 0f; }
        }

        public Dialog_EditOverview()
        {
            doWindowBackground = false;
            drawShadow = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            draggable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var r = new Rect(0f, 0f, windowRect.width, windowRect.height);
            Style.Fill(r, Style.Outer);
            Style.TitleBar(new Rect(0f, 0f, r.width, TitleHeight), "GUE_OverviewTitle".Translate(), null);
            var list = new Rect(14f, TitleHeight + 14f, r.width - 28f, r.height - TitleHeight - FooterHeight - 28f);
            Style.Fill(list, Style.Box);
            Style.Frame(list, Style.Edge);
            var edits = GiddyUpEditorMod.Settings.animals.Where(a => !a.IsEmpty).OrderBy(a => Label(a)).ToList();
            if (edits.Count == 0)
            {
                Style.Text(list.ContractedBy(12f), "GUE_OverviewEmpty".Translate(), Style.Faint, GameFont.Small, TextAnchor.UpperLeft);
            }
            else
            {
                var inner = list.ContractedBy(1f);
                var view = new Rect(0f, 0f, inner.width - 16f, edits.Count * RowHeight);
                Widgets.BeginScrollView(inner, ref scroll, view);
                for (int i = 0; i < edits.Count; i++)
                {
                    Row(new Rect(0f, i * RowHeight, view.width, RowHeight), edits[i]);
                }
                Widgets.EndScrollView();
            }
            Footer(new Rect(0f, r.height - FooterHeight, r.width, FooterHeight), edits);
            Style.Frame(r, Color.black);
        }

        private static string Label(AnimalEdit e)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(e.defName);
            return def != null ? def.LabelCap.ToString() : e.defName;
        }

        private void Row(Rect r, AnimalEdit e)
        {
            if (Mouse.IsOver(r))
            {
                Widgets.DrawHighlight(r);
            }
            Style.Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), Style.Edge);
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(e.defName);
            float bw = 120f;
            var reset = new Rect(r.xMax - bw - 10f, r.y + 11f, bw, 24f);
            var open = new Rect(reset.x - bw - 8f, reset.y, bw, 24f);
            var text = new Rect(r.x + 12f, r.y + 4f, open.x - r.x - 20f, 20f);
            string title = def != null ? def.LabelCap.ToString() + "  ·  " + e.defName : e.defName + "  ·  " + "GUE_OverviewMissing".Translate();
            Style.Text(text, title, Style.Value);
            Style.Text(new Rect(text.x, text.yMax, text.width, 18f), Summary(e), Style.Faint, GameFont.Tiny);
            var animal = FindAnimal(def);
            if (Style.Button(open, "GUE_OverviewEdit".Translate(), false, animal == null) && animal != null)
            {
                Close();
                Find.WindowStack.Add(new Dialog_MountEditor(animal));
            }
            if (animal == null)
            {
                TooltipHandler.TipRegion(open, "GUE_OverviewEditTip".Translate());
            }
            bool asking = confirm == e.defName;
            if (Style.Button(reset, (asking ? "GUE_OverviewConfirm" : "GUE_OverviewReset").Translate(), asking))
            {
                if (asking)
                {
                    ResetAnimal(e);
                    confirm = null;
                }
                else
                {
                    confirm = e.defName;
                }
            }
        }

        private static string Summary(AnimalEdit e)
        {
            var parts = new List<string>();
            if (e.hasRider)
            {
                parts.Add("GUE_SumRider".Translate());
            }
            if (e.mountable >= 0)
            {
                parts.Add((e.mountable == 1 ? "GUE_SumMountable" : "GUE_SumNotMountable").Translate());
            }
            if (e.drawInFront >= 0)
            {
                parts.Add("GUE_SumFront".Translate());
            }
            if (!e.front.IsEmpty || !e.side.IsEmpty || !e.back.IsEmpty)
            {
                parts.Add("GUE_SumOverlay".Translate());
            }
            if (e.masks.Count > 0)
            {
                parts.Add("GUE_SumMasks".Translate(e.masks.Count));
            }
            if (!Mathf.Approximately(e.bodyScale, 1f))
            {
                parts.Add("GUE_SumScale".Translate(e.bodyScale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)));
            }
            return string.Join(", ", parts);
        }

        private static Pawn FindAnimal(ThingDef def)
        {
            if (def == null || Find.CurrentMap == null)
            {
                return null;
            }
            var pawns = Find.CurrentMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].def == def)
                {
                    return pawns[i];
                }
            }
            return null;
        }

        private void Footer(Rect r, List<AnimalEdit> edits)
        {
            Style.Footer(r);
            float bw = 190f;
            var close = new Rect(r.xMax - bw - 14f, r.y + 8f, bw, r.height - 16f);
            var all = new Rect(close.x - bw - 10f, close.y, bw, close.height);
            bool asking = confirm == "*";
            if (Style.Button(all, (asking ? "GUE_OverviewConfirm" : "GUE_OverviewResetAll").Translate(), asking, edits.Count == 0) && edits.Count > 0)
            {
                if (asking)
                {
                    foreach (var e in edits)
                    {
                        ResetAnimal(e);
                    }
                    confirm = null;
                }
                else
                {
                    confirm = "*";
                }
            }
            if (Style.Button(close, "GUE_Close".Translate()))
            {
                Close();
            }
            Style.Text(new Rect(r.x + 14f, r.y, all.x - r.x - 24f, r.height), "GUE_OverviewHint".Translate(), Style.Faint, GameFont.Tiny);
        }

        public static void ResetAnimal(AnimalEdit e)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(e.defName);
            foreach (var w in Find.WindowStack.Windows.ToList())
            {
                if (w is Dialog_MaskEditor mask && mask.Def == def || w is Dialog_MountEditor mount && mount.Def == def)
                {
                    w.Close(false);
                }
            }
            if (def != null)
            {
                Live.Forget(def, e);
            }
            GiddyUpEditorMod.Settings.animals.Remove(e);
            string dir = Path.Combine(Live.ConfigDir, "Masks", e.defName);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            GiddyUpEditorMod.TrySave();
        }
    }
}
