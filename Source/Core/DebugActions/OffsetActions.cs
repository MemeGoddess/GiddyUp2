using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GiddyUp;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core.DebugActions;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
internal static class Pawn_GetGizmos_DrawingOffsetEditor
{
    private static readonly Texture2D GizmoIcon = ContentFinder<Texture2D>.Get("UI/QM_horseshoe_icon");

    private static void Postfix(ref IEnumerable<Gizmo> __result, Pawn __instance)
    {
        if (!ShouldShowFor(__instance))
            return;

        __result = __result.AddItem(new Command_Action
        {
            defaultLabel = "GU_DrawOffsetEditor_Gizmo_Label".Translate(),
            defaultDesc = "GU_DrawOffsetEditor_Gizmo_Description".Translate(__instance.LabelCap),
            icon = GizmoIcon,
            Order = -25f,
            action = () => Find.WindowStack.Add(new Dialog_EditDrawingOffsets(__instance))
        });
    }

    private static bool ShouldShowFor(Pawn pawn)
    {
        return pawn.RaceProps.Animal || ModSettings_GiddyUp.mechanoidsEnabled && pawn.RaceProps.IsMechanoid;
    }
}

internal sealed class Dialog_EditDrawingOffsets : Window
{
    private static readonly Rot4[] Rotations = [Rot4.North, Rot4.South, Rot4.East, Rot4.West];
    private static readonly string[] AxisLabels = ["X", "Y", "Z"];
    private const float ColumnGap = 12f;
    private const float PortraitHeight = 144f;
    private const float PreviewPadding = 8f;
    private const float NumericFieldWidth = 92f;

    private readonly Pawn pawn;
    private readonly ThingDef pawnDef;
    private OffsetState committedState;
    private readonly DrawingOffset workingOffset;
    private readonly Dictionary<Rot4, string[]> buffers = new();

    public override Vector2 InitialSize => new(1120f, 760f);

    public Dialog_EditDrawingOffsets(Pawn pawn)
    {
        this.pawn = pawn;
        pawnDef = pawn.def;
        committedState = CaptureCommittedState(pawnDef);
        workingOffset = CloneOffset(committedState.Offset);

        foreach (var rotation in Rotations)
            buffers[rotation] = CreateBuffers(GetOffset(rotation));

        doCloseX = true;
        doCloseButton = true;
        absorbInputAroundWindow = true;
        optionalTitle = "GU_DrawOffsetEditor_Title".Translate(pawn.LabelCap, pawnDef.defName);
    }

    public override void DoWindowContents(Rect inRect)
    {
        var headerRect = new Rect(inRect.x, inRect.y, inRect.width, 70f);
        DrawHeader(headerRect);

        var editorRect = new Rect(inRect.x, headerRect.yMax + 12f, inRect.width, 420f);
        DrawEditor(editorRect);

        var xmlRect = new Rect(inRect.x, editorRect.yMax + 12f, inRect.width, inRect.yMax - editorRect.yMax - 68f);
        DrawXmlPreview(xmlRect);

        var buttonRow = new Rect(inRect.x, inRect.yMax - 42f, inRect.width, 42f);
        DrawButtons(buttonRow);

        if (!OffsetEquals(workingOffset, GetAppliedOffset(pawnDef)))
            ApplyPreviewState();
    }

    public override void PostClose()
    {
        if (!OffsetEquals(workingOffset, committedState.Offset) || committedState.HasExtension != pawnDef.HasModExtension<DrawingOffset>())
            ApplyState(pawn, pawnDef, committedState);
    }

    private void DrawHeader(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        var inner = rect.ContractedBy(12f);

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inner.x, inner.y, inner.width, 32f), pawn.LabelCap);
        Text.Font = GameFont.Small;

        var metadata = "GU_DrawOffsetEditor_Metadata".Translate(pawnDef.defName, pawnDef.modContentPack?.Name ?? "Unknown");
        Widgets.Label(new Rect(inner.x, inner.y + 34f, inner.width, 24f), metadata);
        Widgets.Label(new Rect(inner.x, inner.y + 54f, inner.width, 24f), GetPatchFilePath());
    }

    private void DrawEditor(Rect rect)
    {
        var columnWidth = (rect.width - ColumnGap * 3f) / 4f;
        var changed = false;

        for (var index = 0; index < Rotations.Length; index++)
        {
            var columnRect = new Rect(rect.x + index * (columnWidth + ColumnGap), rect.y, columnWidth, rect.height);
            var previous = GetOffset(Rotations[index]);
            DrawRotationEditor(columnRect, Rotations[index], ref previous);
            if (previous != GetOffset(Rotations[index]))
            {
                SetOffset(Rotations[index], previous);
                SyncBuffers(Rotations[index], previous);
                changed = true;
            }
        }

        if (changed)
            ApplyPreviewState();
    }

    private void DrawRotationEditor(Rect rect, Rot4 rotation, ref Vector3 offset)
    {
        Widgets.DrawMenuSection(rect);
        var inner = rect.ContractedBy(10f);

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(inner.x, inner.y, inner.width, 24f), rotation.ToStringHuman());
        Text.Anchor = TextAnchor.UpperLeft;

        var portraitRect = new Rect(inner.x, inner.y + 28f, inner.width, PortraitHeight);
        DrawPortrait(portraitRect, rotation);

        var tableRect = new Rect(inner.x, portraitRect.yMax + 10f, inner.width, 110f);
        DrawOffsetFields(tableRect, ref offset, buffers[rotation]);
    }

    private void DrawPortrait(Rect rect, Rot4 rotation)
    {
        Widgets.DrawBoxSolid(rect, Widgets.WindowBGFillColor);
        var previewRect = rect.ContractedBy(PreviewPadding);
        var portrait = PortraitsCache.Get(pawn, previewRect.size, rotation, cameraZoom: 0.6f, supersample: true, compensateForUIScale: true);
        Widgets.DrawTextureFitted(previewRect, portrait, 1f);
    }

    private static void DrawOffsetFields(Rect rect, ref Vector3 offset, string[] buffer)
    {
        var value = offset;
        for (var axis = 0; axis < AxisLabels.Length; axis++)
        {
            var rowRect = new Rect(rect.x, rect.y + axis * 34f, rect.width, 30f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rowRect.x, rowRect.y, 22f, rowRect.height), AxisLabels[axis]);
            Text.Anchor = TextAnchor.UpperLeft;
            var fieldRect = new Rect(rowRect.xMax - NumericFieldWidth, rowRect.y, NumericFieldWidth, rowRect.height);

            switch (axis)
            {
                case 0:
                    Widgets.TextFieldNumeric(fieldRect, ref value.x, ref buffer[axis], -10f, 10f);
                    break;
                case 1:
                    Widgets.TextFieldNumeric(fieldRect, ref value.y, ref buffer[axis], -10f, 10f);
                    break;
                default:
                    Widgets.TextFieldNumeric(fieldRect, ref value.z, ref buffer[axis], -10f, 10f);
                    break;
            }
        }

        offset = value;
    }

    private void DrawXmlPreview(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        var inner = rect.ContractedBy(10f);
        Widgets.Label(new Rect(inner.x, inner.y, inner.width, 24f), "GU_DrawOffsetEditor_XmlPreview".Translate());
        Widgets.TextArea(new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f), BuildPatchXml(), true);
    }

    private void DrawButtons(Rect rect)
    {
        const float buttonWidth = 180f;
        var copyRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
        var saveRect = new Rect(copyRect.xMax + 12f, rect.y, buttonWidth, rect.height);
        var resetRect = new Rect(saveRect.xMax + 12f, rect.y, buttonWidth, rect.height);

        if (Widgets.ButtonText(copyRect, "GU_DrawOffsetEditor_CopyXml".Translate()))
        {
            GUIUtility.systemCopyBuffer = BuildPatchXml();
            Messages.Message("GU_DrawOffsetEditor_Copied".Translate(pawnDef.defName), MessageTypeDefOf.TaskCompletion, false);
        }

        if (Widgets.ButtonText(saveRect, "GU_DrawOffsetEditor_SavePatch".Translate()))
            SavePatch();

        if (Widgets.ButtonText(resetRect, "GU_DrawOffsetEditor_Reset".Translate()))
            ResetWorkingOffset();
    }

    private void ResetWorkingOffset()
    {
        SetOffset(Rot4.North, committedState.Offset.northOffset);
        SetOffset(Rot4.South, committedState.Offset.southOffset);
        SetOffset(Rot4.East, committedState.Offset.eastOffset);
        SetOffset(Rot4.West, committedState.Offset.westOffset);

        foreach (var rotation in Rotations)
            SyncBuffers(rotation, GetOffset(rotation));

        ApplyState(pawn, pawnDef, committedState);
    }

    private void SavePatch()
    {
        try
        {
            var patchPath = GetPatchFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(patchPath) ?? throw new InvalidOperationException());
            File.WriteAllText(patchPath, BuildPatchXml(), Encoding.UTF8);
            committedState = new OffsetState(true, CloneOffset(workingOffset));
            ApplyState(pawn, pawnDef, committedState);
            Messages.Message("GU_DrawOffsetEditor_Saved".Translate(patchPath), MessageTypeDefOf.TaskCompletion, false);
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to save drawing offset patch for {pawnDef.defName}: {exception}");
            Messages.Message("GU_DrawOffsetEditor_SaveFailed".Translate(pawnDef.defName), MessageTypeDefOf.RejectInput, false);
        }
    }

    private void ApplyPreviewState()
    {
        ApplyState(pawn, pawnDef, new OffsetState(true, CloneOffset(workingOffset)));
    }

    private static void ApplyState(Pawn pawn, ThingDef def, OffsetState state)
    {
        def.modExtensions ??= [];
        def.modExtensions.RemoveAll(extension => extension is DrawingOffset);
        if (state.HasExtension)
            def.modExtensions.Add(CloneOffset(state.Offset));

        PortraitsCache.SetDirty(pawn);
        MountedRiderRenderNodeUtility.RefreshMountedAnimalGraphics(pawn);
    }

    private static OffsetState CaptureCommittedState(ThingDef def)
    {
        var existing = def.GetModExtension<DrawingOffset>();
        return existing == null ? new OffsetState(false, new DrawingOffset()) : new OffsetState(true, CloneOffset(existing));
    }

    private string BuildPatchXml()
    {
        var defName = EscapeXml(pawnDef.defName);
        var extensionXml = BuildExtensionXml();
        return $"<?xml version=\"1.0\" encoding=\"utf-8\" ?>{Environment.NewLine}<Patch>{Environment.NewLine}\t<Operation Class=\"PatchOperationConditional\">{Environment.NewLine}\t\t<xpath>Defs/ThingDef[defName=\"{defName}\"]/modExtensions/li[@Class=\"GiddyUp.DrawingOffset\"]</xpath>{Environment.NewLine}\t\t<match Class=\"PatchOperationReplace\">{Environment.NewLine}\t\t\t<xpath>Defs/ThingDef[defName=\"{defName}\"]/modExtensions/li[@Class=\"GiddyUp.DrawingOffset\"]</xpath>{Environment.NewLine}\t\t\t<value>{Environment.NewLine}{extensionXml}{Environment.NewLine}\t\t\t</value>{Environment.NewLine}\t\t</match>{Environment.NewLine}\t\t<nomatch Class=\"PatchOperationAddModExtension\">{Environment.NewLine}\t\t\t<xpath>Defs/ThingDef[defName=\"{defName}\"]</xpath>{Environment.NewLine}\t\t\t<value>{Environment.NewLine}{extensionXml}{Environment.NewLine}\t\t\t</value>{Environment.NewLine}\t\t</nomatch>{Environment.NewLine}\t</Operation>{Environment.NewLine}</Patch>{Environment.NewLine}";
    }

    private string BuildExtensionXml()
    {
        var builder = new StringBuilder();
        builder.AppendLine("\t\t\t\t<li Class=\"GiddyUp.DrawingOffset\">");
        builder.AppendLine($"\t\t\t\t\t<northOffset>{FormatOffset(workingOffset.northOffset)}</northOffset>");
        builder.AppendLine($"\t\t\t\t\t<southOffset>{FormatOffset(workingOffset.southOffset)}</southOffset>");
        builder.AppendLine($"\t\t\t\t\t<eastOffset>{FormatOffset(workingOffset.eastOffset)}</eastOffset>");
        builder.AppendLine($"\t\t\t\t\t<westOffset>{FormatOffset(workingOffset.westOffset)}</westOffset>");
        builder.Append("\t\t\t\t</li>");
        return builder.ToString();
    }

    private string GetPatchFilePath()
    {
        var modRoot = LoadedModManager.ModHandles.OfType<Mod_GiddyUp>().FirstOrDefault()?.Content.RootDir
                      ?? pawnDef.modContentPack?.RootDir
                      ?? throw new InvalidOperationException("Unable to resolve the Giddy-Up mod root.");
        var versionFolder = VersionControl.CurrentVersionStringWithoutBuild;
        return Path.Combine(modRoot, versionFolder, "Patches", $"ZZZ_GiddyUpOffsetEditor_{SanitizeFileName(pawnDef.defName)}.xml");
    }

    private Vector3 GetOffset(Rot4 rotation)
    {
        return rotation.AsInt switch
        {
            0 => workingOffset.NorthOffset(),
            2 => workingOffset.SouthOffset(),
            1 => workingOffset.EastOffset(),
            _ => workingOffset.WestOffset()
        };
    }

    private void SetOffset(Rot4 rotation, Vector3 value)
    {
        switch (rotation.AsInt)
        {
            case 0:
                workingOffset.northOffset = value;
                break;
            case 2:
                workingOffset.southOffset = value;
                break;
            case 1:
                workingOffset.eastOffset = value;
                break;
            default:
                workingOffset.westOffset = value;
                break;
        }
    }

    private static DrawingOffset GetAppliedOffset(ThingDef def)
    {
        return def.GetModExtension<DrawingOffset>() ?? new DrawingOffset();
    }

    private static DrawingOffset CloneOffset(DrawingOffset offset)
    {
        return new DrawingOffset
        {
            northOffset = offset.northOffset,
            southOffset = offset.southOffset,
            eastOffset = offset.eastOffset,
            westOffset = offset.westOffset
        };
    }

    private static string[] CreateBuffers(Vector3 vector)
    {
        return
        [
            FormatFloat(vector.x),
            FormatFloat(vector.y),
            FormatFloat(vector.z)
        ];
    }

    private void SyncBuffers(Rot4 rotation, Vector3 vector)
    {
        buffers[rotation][0] = FormatFloat(vector.x);
        buffers[rotation][1] = FormatFloat(vector.y);
        buffers[rotation][2] = FormatFloat(vector.z);
    }

    private static string FormatOffset(Vector3 vector)
    {
        return $"({FormatFloat(vector.x)}, {FormatFloat(vector.y)}, {FormatFloat(vector.z)})";
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static bool OffsetEquals(DrawingOffset left, DrawingOffset right)
    {
        return left.northOffset == right.northOffset
            && left.southOffset == right.southOffset
            && left.eastOffset == right.eastOffset
            && left.westOffset == right.westOffset;
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;");
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(invalidChars.Contains(character) ? '_' : character);
        return builder.ToString();
    }

    private readonly record struct OffsetState(bool HasExtension, DrawingOffset Offset);
}

internal static class DrawingOffsetEditorExtensions
{
    public static Vector3 NorthOffset(this DrawingOffset offset) => offset.northOffset;

    public static Vector3 SouthOffset(this DrawingOffset offset) => offset.southOffset;

    public static Vector3 EastOffset(this DrawingOffset offset) => offset.eastOffset;

    public static Vector3 WestOffset(this DrawingOffset offset) => offset.westOffset;
}
