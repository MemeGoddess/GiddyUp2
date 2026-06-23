using GiddyUp;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GiddyUpCore.Core.DebugActions.OffsetEditor;

internal static class OffsetEditorWidgets
{
    private static readonly Color RangeControlTextColor = new(0.6f, 0.6f, 0.6f);

    private static int sliderDraggingId;
    private static float lastDragSliderSoundTime = -1f;

    public static float VerticalSlider(
        Rect rect,
        float value,
        float min,
        float max,
        bool middleAlignment = false,
        string label = null,
        string topAlignedLabel = null,
        string bottomAlignedLabel = null,
        float roundTo = -1f)
    {
        var newValue = value;
        if (middleAlignment || !label.NullOrEmpty())
            rect.x += Mathf.Round((rect.width - 10f) / 2f);
        if (!label.NullOrEmpty())
            rect.x += 5f;

        var dragId = Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(UI.GUIToScreenPoint(new Vector2(rect.x, rect.y)).GetHashCode(), rect.width), rect.height), min), max);

        var railRect = rect;
        railRect.yMin += 6f;
        railRect.yMax -= 6f;

        GUI.color = RangeControlTextColor;
        var atlasRect = new Rect(railRect.x + 2f, railRect.y, 8f, railRect.height);
        var rotatedRailRect = new Rect(atlasRect.center.x - (atlasRect.height / 2f), atlasRect.center.y - (atlasRect.width / 2f), atlasRect.height, atlasRect.width);
        var previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(-90f, atlasRect.center);
        Widgets.DrawAtlas(rotatedRailRect, ResourceBank.SliderRailAtlas);
        GUI.matrix = previousMatrix;
        GUI.color = Color.white;

        var handleY = Mathf.Clamp(railRect.yMax - 6f - railRect.height * Mathf.InverseLerp(min, max, newValue), railRect.yMin - 6f, railRect.yMax - 6f);
        GUI.DrawTexture(new Rect(atlasRect.center.x - 6f, handleY, 12f, 12f), ResourceBank.SliderHandle);

        if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect) && sliderDraggingId != dragId)
        {
            sliderDraggingId = dragId;
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
            Event.current.Use();
        }
        else if (Event.current.type == EventType.MouseUp)
        {
            sliderDraggingId = 0;
        }

        if (sliderDraggingId == dragId && UnityGUIBugsFixer.MouseDrag())
        {
            newValue = Mathf.Clamp((railRect.yMax - Event.current.mousePosition.y) / railRect.height * (max - min) + min, min, max);
            if (Event.current.type == EventType.MouseDrag)
                Event.current.Use();
        }

        if (!label.NullOrEmpty() || !topAlignedLabel.NullOrEmpty() || !bottomAlignedLabel.NullOrEmpty())
        {
            var anchor = Text.Anchor;
            var font = Text.Font;
            Text.Font = GameFont.Small;
            var labelWidth = label.NullOrEmpty() ? 18f : Text.CalcSize(label).x;
            rect.x = rect.x - labelWidth + 3f;

            if (!topAlignedLabel.NullOrEmpty())
            {
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(new Rect(rect.x, rect.y, rect.width + labelWidth, 18f), topAlignedLabel);
            }

            if (!bottomAlignedLabel.NullOrEmpty())
            {
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label(new Rect(rect.x, rect.yMax - 18f, rect.width + labelWidth, 18f), bottomAlignedLabel);
            }

            if (!label.NullOrEmpty())
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(rect.x, rect.center.y - 9f, rect.width + labelWidth, 18f), label);
            }

            Text.Anchor = anchor;
            Text.Font = font;
        }

        if (roundTo > 0f)
            newValue = Mathf.RoundToInt(newValue / roundTo) * roundTo;

        if (!Mathf.Approximately(value, newValue))
            CheckPlayDragSliderSound();

        return newValue;
    }

    private static void CheckPlayDragSliderSound()
    {
        if (Time.realtimeSinceStartup <= lastDragSliderSoundTime + 0.075f)
            return;

        SoundDefOf.DragSlider.PlayOneShotOnCamera();
        lastDragSliderSoundTime = Time.realtimeSinceStartup;
    }
}