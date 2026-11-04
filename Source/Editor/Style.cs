#nullable disable
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GiddyUp.Editor
{
    public static class Style
    {
        public static readonly Color Outer = Hex("151b1e");
        public static readonly Color Bar = Hex("0f1416");
        public static readonly Color Box = Hex("202224");
        public static readonly Color ImageGround = Hex("131618");
        public static readonly Color Edge = Hex("3b3f41");
        public static readonly Color Head = Hex("e6d7b4");
        public static readonly Color Title = Hex("efe9dc");
        public static readonly Color Value = Hex("d8d6d0");
        public static readonly Color Label = Hex("9a9c9b");
        public static readonly Color Faint = Hex("6c706f");
        public static readonly Color Selected = Hex("4a4a32");
        public static readonly Color SelectedEdge = Hex("b58a45");
        public static readonly Color SelectedText = Hex("f0dcae");
        public static readonly Color State = Hex("d2a44e");
        public static readonly Color GhostOver = Hex("35383a");
        public static readonly Color GhostGround = Hex("2b2d2e");
        public static readonly Color ToggleOver = Hex("2a2c2e");
        public static readonly Color Track = Hex("3a3a3a");
        public static readonly Color Thumb = Hex("bbbbbb");
        public static readonly Color TickMark = Hex("9fd070");
        public static readonly Color TickEdge = Hex("777777");
        public static readonly Color TickGround = Hex("111111");
        public static readonly Color InfoGround = Hex("3a2b14");
        public static readonly Color InfoEdge = Hex("c8862e");
        public static readonly Color InfoText = Hex("f2d8ae");
        public static readonly Color Mask = new Color(0.82f, 0.31f, 0.24f, 0.45f);
        public static readonly Color Path = Hex("f0dcae");

        public const float RowHeight = 22f;
        public const float LabelWidth = 118f;

        private static readonly Dictionary<string, string> words = new Dictionary<string, string>();
        private static LoadedLanguage wordsLanguage;

        public static string W(string key)
        {
            if (wordsLanguage != LanguageDatabase.activeLanguage)
            {
                words.Clear();
                wordsLanguage = LanguageDatabase.activeLanguage;
            }
            string t;
            if (!words.TryGetValue(key, out t))
            {
                t = key.Translate().ToString();
                words[key] = t;
            }
            return t;
        }

        private static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + hex, out c);
            return c;
        }

        public static void Fill(Rect r, Color c)
        {
            Widgets.DrawBoxSolid(r, c);
        }

        public static void Frame(Rect r, Color c)
        {
            GUI.color = c;
            Widgets.DrawBox(r, 1);
            GUI.color = Color.white;
        }

        public static void Text(Rect r, string text, Color c, GameFont f = GameFont.Small, TextAnchor a = TextAnchor.MiddleLeft)
        {
            Verse.Text.Font = f;
            Verse.Text.Anchor = a;
            GUI.color = c;
            Widgets.Label(r, (text ?? "").Truncate(r.width));
            GUI.color = Color.white;
            Verse.Text.Anchor = TextAnchor.UpperLeft;
            Verse.Text.Font = GameFont.Small;
        }

        public static float Width(string text, GameFont f = GameFont.Small)
        {
            Verse.Text.Font = f;
            float w = Verse.Text.CalcSize(text ?? "").x;
            Verse.Text.Font = GameFont.Small;
            return w;
        }

        public static void TitleBar(Rect r, string title, string right)
        {
            Fill(r, Bar);
            Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), Edge);
            Text(new Rect(r.x + 12f, r.y, r.width - 24f, r.height), title, Title);
            if (!right.NullOrEmpty())
            {
                Text(new Rect(r.x + 12f, r.y, r.width - 50f, r.height), right, Faint, GameFont.Tiny, TextAnchor.MiddleRight);
            }
        }

        public static void Footer(Rect r)
        {
            Fill(r, Bar);
            Fill(new Rect(r.x, r.y, r.width, 1f), Edge);
        }

        public static void SectionHead(Rect r, string text)
        {
            Text(r, text, Head);
            Fill(new Rect(r.x, r.yMax - 1f, r.width, 1f), Edge);
        }

        public static void SubHead(Rect r, string text)
        {
            Text(r, text, Label, GameFont.Tiny);
        }

        public static Rect Row(Rect r, string label, float labelWidth = LabelWidth)
        {
            Text(new Rect(r.x, r.y, labelWidth, r.height), label, Label);
            return new Rect(r.x + labelWidth + 6f, r.y, r.width - labelWidth - 6f, r.height);
        }

        public static bool Button(Rect r, string text, bool on = false, bool faint = false)
        {
            bool over = Mouse.IsOver(r) && !faint;
            Widgets.DrawAtlas(r, on ? Widgets.ButtonBGAtlasClick : Widgets.ButtonBGAtlas);
            if (on)
            {
                Widgets.DrawHighlightSelected(r);
            }
            else if (over)
            {
                Widgets.DrawHighlight(r);
            }
            GUI.color = faint ? new Color(1f, 1f, 1f, 0.35f) : over || on ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            Verse.Text.Anchor = TextAnchor.MiddleCenter;
            string shortText = text.Truncate(r.width - 6f);
            if (shortText != text)
            {
                TooltipHandler.TipRegion(r, text);
            }
            Widgets.Label(r, shortText);
            Verse.Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            return !faint && Widgets.ButtonInvisible(r);
        }

        public static bool Ghost(Rect r, string text)
        {
            bool over = Mouse.IsOver(r);
            Fill(r, over ? GhostOver : GhostGround);
            Frame(r, Edge);
            Text(r, text, Value, GameFont.Tiny, TextAnchor.MiddleCenter);
            return Widgets.ButtonInvisible(r);
        }

        public static bool Chip(Rect r, string text, bool on)
        {
            bool over = Mouse.IsOver(r);
            Frame(r, on ? SelectedEdge : over ? Label : Edge);
            Text(r, text, on ? SelectedText : Label, GameFont.Tiny, TextAnchor.MiddleCenter);
            return Widgets.ButtonInvisible(r);
        }

        public static int Toggle(Rect r, IList<string> names, int on)
        {
            int n = names.Count;
            float w = (r.width - (n - 1) * 2f) / n;
            int clicked = -1;
            for (int i = 0; i < n; i++)
            {
                var k = new Rect(r.x + i * (w + 2f), r.y, w, r.height);
                bool over = Mouse.IsOver(k);
                Fill(k, i == on ? Selected : over ? ToggleOver : Box);
                Frame(k, i == on ? SelectedEdge : Edge);
                Text(k, names[i], i == on ? SelectedText : Label, GameFont.Small, TextAnchor.MiddleCenter);
                if (Widgets.ButtonInvisible(k))
                {
                    clicked = i;
                }
            }
            return clicked;
        }

        public static bool Tick(Rect row, bool on)
        {
            var k = new Rect(row.x, row.y + (row.height - 14f) / 2f, 14f, 14f);
            Fill(k, TickGround);
            Frame(k, Mouse.IsOver(k) ? Value : TickEdge);
            if (on)
            {
                GUI.color = TickMark;
                GUI.DrawTexture(k.ContractedBy(1f), Widgets.CheckboxOnTex);
                GUI.color = Color.white;
            }
            return Widgets.ButtonInvisible(k.ExpandedBy(2f));
        }

        public static bool TickRow(Rect r, string label, ref bool value)
        {
            var v = Row(r, label);
            bool clicked = Tick(v, value);
            if (clicked)
            {
                value = !value;
            }
            return clicked;
        }

        public static float Slider(Rect r, float fraction)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive, r);
            var e = Event.current;
            float result = fraction;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && r.ExpandedBy(0f, 6f).Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        result = Mathf.Clamp01((e.mousePosition.x - r.x - 4f) / Mathf.Max(1f, r.width - 8f));
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        result = Mathf.Clamp01((e.mousePosition.x - r.x - 4f) / Mathf.Max(1f, r.width - 8f));
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
            float y = r.y + r.height / 2f - 2f;
            Fill(new Rect(r.x, y, r.width, 4f), Track);
            float x = r.x + Mathf.Clamp01(result) * (r.width - 8f);
            Fill(new Rect(x, r.y + r.height / 2f - 7f, 8f, 14f), Thumb);
            return result;
        }

        public static bool ValueRow(Rect r, string label, ref float value, float min, float max, float step, float original, float labelWidth = LabelWidth)
        {
            var v = Row(r, label, labelWidth);
            float before = value;
            var minus = new Rect(v.x, v.y + 1f, 20f, v.height - 2f);
            var plus = new Rect(v.xMax - 20f, v.y + 1f, 20f, v.height - 2f);
            var num = new Rect(plus.x - 52f, v.y, 48f, v.height);
            var track = new Rect(minus.xMax + 6f, v.y, num.x - minus.xMax - 12f, v.height);
            if (Ghost(minus, "-"))
            {
                value = Mathf.Clamp(Mathf.Round((value - step) / step) * step, min, max);
            }
            if (Ghost(plus, "+"))
            {
                value = Mathf.Clamp(Mathf.Round((value + step) / step) * step, min, max);
            }
            float was = Mathf.InverseLerp(min, max, value);
            float f = Slider(track, was);
            if (!Mathf.Approximately(f, was))
            {
                value = Mathf.Clamp(Mathf.Round(Mathf.Lerp(min, max, f) / step) * step, min, max);
            }
            bool changed = !Mathf.Approximately(value, original);
            Text(num, value.ToString("0.00"), changed ? State : Value, GameFont.Small, TextAnchor.MiddleRight);
            return !Mathf.Approximately(before, value);
        }

        public static float Info(Rect r, string text)
        {
            Verse.Text.Font = GameFont.Tiny;
            float h = Verse.Text.CalcHeight(text, r.width - 16f) + 10f;
            Verse.Text.Font = GameFont.Small;
            var box = new Rect(r.x, r.y, r.width, h);
            Fill(box, InfoGround);
            Fill(new Rect(box.x, box.y, 3f, box.height), InfoEdge);
            Verse.Text.Font = GameFont.Tiny;
            GUI.color = InfoText;
            Widgets.Label(new Rect(box.x + 10f, box.y + 5f, box.width - 16f, box.height - 10f), text);
            GUI.color = Color.white;
            Verse.Text.Font = GameFont.Small;
            return h;
        }

        public static float Faintext(Rect r, string text)
        {
            Verse.Text.Font = GameFont.Tiny;
            float h = Verse.Text.CalcHeight(text, r.width);
            GUI.color = Faint;
            Widgets.Label(new Rect(r.x, r.y, r.width, h), text);
            GUI.color = Color.white;
            Verse.Text.Font = GameFont.Small;
            return h;
        }

        public static void Line(Vector2 a, Vector2 b, Color c, float width)
        {
            Widgets.DrawLine(a, b, c, width);
        }
    }
}
