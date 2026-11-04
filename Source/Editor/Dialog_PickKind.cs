#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUp.Editor
{
    public class Dialog_PickKind : Window
    {
        private const float TitleHeight = 34f;
        private const float RowHeight = 26f;

        private readonly Action<PawnKindDef> pick;
        private readonly List<PawnKindDef> kinds;
        private readonly List<string> labels;
        private readonly QuickSearchWidget search = new QuickSearchWidget();
        private Vector2 scroll;

        public override Vector2 InitialSize
        {
            get { return new Vector2(560f, 640f); }
        }

        public override float Margin
        {
            get { return 0f; }
        }

        public Dialog_PickKind(Action<PawnKindDef> pick)
        {
            this.pick = pick;
            doWindowBackground = false;
            drawShadow = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            kinds = DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(k => k.race != null && k.race.race != null && k.race.race.Humanlike)
                .OrderBy(k => k.race.label).ThenBy(k => k.label).ToList();
            labels = kinds.Select(k => k.LabelCap.ToString() + " · " + k.race.LabelCap.ToString() + (k.modContentPack != null ? " · " + k.modContentPack.Name : "")).ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            var r = new Rect(0f, 0f, windowRect.width, windowRect.height);
            Style.Fill(r, Style.Outer);
            Style.TitleBar(new Rect(0f, 0f, r.width, TitleHeight), "GUE_PickKindTitle".Translate(), null);
            var searchRect = new Rect(14f, TitleHeight + 12f, r.width - 28f, 24f);
            search.OnGUI(searchRect);
            var list = new Rect(14f, searchRect.yMax + 10f, r.width - 28f, r.height - searchRect.yMax - 24f);
            Style.Fill(list, Style.Box);
            Style.Frame(list, Style.Edge);
            var shown = new List<int>();
            for (int i = 0; i < kinds.Count; i++)
            {
                if (search.filter.Matches(labels[i]))
                {
                    shown.Add(i);
                }
            }
            var inner = list.ContractedBy(1f);
            var view = new Rect(0f, 0f, inner.width - 16f, shown.Count * RowHeight);
            Widgets.BeginScrollView(inner, ref scroll, view);
            int first = Mathf.Max(0, Mathf.FloorToInt(scroll.y / RowHeight));
            int last = Mathf.Min(shown.Count - 1, Mathf.CeilToInt((scroll.y + inner.height) / RowHeight));
            for (int n = first; n <= last; n++)
            {
                int i = shown[n];
                var row = new Rect(0f, n * RowHeight, view.width, RowHeight);
                if (Mouse.IsOver(row))
                {
                    Widgets.DrawHighlight(row);
                }
                Style.Text(new Rect(row.x + 8f, row.y, row.width - 16f, row.height), labels[i], Style.Value);
                if (Widgets.ButtonInvisible(row))
                {
                    pick(kinds[i]);
                    Close();
                }
            }
            Widgets.EndScrollView();
            Style.Frame(r, Color.black);
        }
    }
}
