using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GiddyUp;
using RimWorld;
using UnityEngine;
using Verse;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace GiddyUpCore.Core.DebugActions.OffsetEditor;

internal sealed class Dialog_EditDrawingOffsets : Window
{
    private const float ColumnGap = 12f;
    private const float PortraitHeight = 200f;
    private const float PortraitGap = 85f;
    private const float PreviewPadding = 4f;

    private readonly Pawn pawn;
    private readonly ThingDef pawnDef;
    private DrawingOffset workingOffset;
    private Vector3? northOffset, southOffset, eastOffset, westOffset;
    private Dictionary<string, Vector2> labelSizes = new();
    private TaggedString title, save, reset;

    public override Vector2 InitialSize => new(1120f, 760f);

    public Dialog_EditDrawingOffsets(Pawn pawn)
    {
        this.pawn = pawn;
        pawnDef = pawn.def;
        workingOffset = GetDrawingOffset(pawnDef);

        doCloseX = true;
        doCloseButton = false;

        title = "GU_DrawOffsetEditor_Title".Translate();
        save = "GU_DrawOffsetEditor_SavePatch".Translate();
        reset = "GU_DrawOffsetEditor_Reset".Translate();
    }

    public override void DoWindowContents(Rect inRect)
    {
        inRect.SplitHorizontallyWithMargin(out var headerRect, out inRect, out _, ColumnGap, 70f);

        inRect.SplitHorizontallyWithMargin(out var bodyRect, out var buttonRow, out _, ColumnGap, inRect.height - 42f - ColumnGap);

        DrawHeader(headerRect);
        DrawEditor(bodyRect);
        DrawButtons(buttonRow);
    }

    public override void PreOpen()
    {
        base.PreOpen();

        
        var current = GetDrawingOffset(pawnDef);
        eastOffset = current.eastOffset;
        northOffset = current.northOffset;
        southOffset = current.southOffset;
        westOffset = current.westOffset;

        ApplyState(pawn);
    }

    private void DrawHeader(Rect rect)
    {
        var inner = rect.ContractedBy(12f);

        var font = Text.Font;
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inner.x, inner.y, inner.width, 32f), title.Formatted(pawn.LabelCap, pawnDef.modContentPack.Name, pawnDef.defName));
        Text.Font = font;
    }

    private void DrawEditor(Rect rect)
    {
        var changed = false;

        var center = rect.center;
        var color = GUI.color;
        var offset = PortraitGap + PortraitHeight / 2; 
        var textHeight = Text.LineHeight;

        foreach (var rotation in Rot4.AllRotations)
        {
            var portraitCenter = center + (rotation.AsVector2 * offset);
            var rotationRect = new Rect(portraitCenter.x - (PortraitHeight / 2), portraitCenter.y - (PortraitHeight / 2), PortraitHeight,
                PortraitHeight);
            var rotationOffset = workingOffset.GetOffsetByRotation(rotation);

            var previousZ = rotationOffset.z;
            var previousX = rotationOffset.x;
            var newZ = Mathf.RoundToMultipleOf(
                OffsetEditorWidgets.VerticalSlider(new Rect(rotationRect.x, rotationRect.y + 20f, 20f, rotationRect.height - 40f),
                    rotationOffset.z, -1f, 1f), 0.05f);
            var newX = Widgets.HorizontalSlider(
                new Rect(rotationRect.x + 20f, rotationRect.y + rotationRect.height - 20f, rotationRect.width - 40f,
                    20f), rotationOffset.x, -1, 1, roundTo: 0.05f);

            var zLabel = $"{newZ:F}";
            var xLabel = $"{newX:F}";
            var zBox = new Rect(rotationRect.x - 10f - GetLabelSize(zLabel).x , portraitCenter.y - (textHeight / 2), rotationRect.width / 2, textHeight);
            var xBox = new Rect(rotationRect.x, rotationRect.y + rotationRect.height,
                rotationRect.width, textHeight);
            var original = GetOffsetByDirection(rotation);
            var zChanged = newZ != original.z;
            var xChanged = newX != original.x;

            if (!zChanged)
                GUI.color = new Color(1, 1, 1, 0.6f);
            Widgets.Label(zBox, zLabel);
            GUI.color = color;

            var textAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.LowerCenter;
            if (!xChanged)
                GUI.color = new Color(1, 1, 1, 0.6f);
            Widgets.Label(xBox, xLabel);
            GUI.color = color;
            Text.Anchor = textAnchor;

            if (previousX != newX || previousZ != newZ)
            {
                Log.Message("Updating");
                switch (rotation.AsInt)
                {
                    case Rot4.NorthInt:
                        workingOffset.northOffset = new Vector3(newX, 0, newZ);
                        break;
                    case Rot4.SouthInt:
                        workingOffset.southOffset = new Vector3(newX, 0, newZ);
                        break;
                    case Rot4.WestInt:
                        workingOffset.westOffset = new Vector3(newX, 0, newZ);
                        break;
                    case Rot4.EastInt:
                        workingOffset.eastOffset = new Vector3(newX, 0, newZ);
                        break;
                }

                changed = true;
            }
            DrawPortrait(rotationRect, rotation);
        }

        if (changed)
            ApplyState(pawn);
    }

    private void DrawPortrait(Rect rect, Rot4 rotation)
    {
        var previewRect = rect.ContractedBy(PreviewPadding);
        var portrait = PortraitsCache.Get(pawn, previewRect.size, rotation, cameraZoom: 0.5f, supersample: true, compensateForUIScale: true);
        Widgets.DrawTextureFitted(previewRect, portrait, 1f);
    }

    private void DrawButtons(Rect rect)
    {
        const float buttonWidth = 180f;
        var saveRect = new Rect(rect.center.x - buttonWidth - 6f, rect.y, buttonWidth, rect.height);
        var resetRect = new Rect(rect.center.x + 6f, rect.y, buttonWidth, rect.height);

        if (Widgets.ButtonText(saveRect, save))
            BuildFullXml();

        if (Widgets.ButtonText(resetRect, reset))
            ResetWorkingOffset();
    }

    private void ResetWorkingOffset()
    {
        workingOffset.eastOffset = eastOffset ?? Vector3.zero;
        workingOffset.northOffset = northOffset ?? Vector3.zero;
        workingOffset.southOffset = southOffset ?? Vector3.zero;
        workingOffset.westOffset = westOffset ?? Vector3.zero;
        ApplyState(pawn);
    }

    private static void ApplyState(Pawn pawn)
    {
        PortraitsCache.SetDirty(pawn);
        MountedRiderRenderNodeUtility.RefreshMountedAnimalGraphics(pawn);
    }

    private Vector2 GetLabelSize(string label)
    {
        if (labelSizes.TryGetValue(label, out var size))
            return size;

        size = Text.CalcSize(label);
        labelSizes[label] = size;
        return size;
    }

    private Vector3 GetOffsetByDirection(Rot4 rot)
    {
        return rot.AsInt switch
        {
            Rot4.EastInt => eastOffset ?? Vector3.zero,
            Rot4.NorthInt => northOffset ?? Vector3.zero,
            Rot4.SouthInt => southOffset ?? Vector3.zero,
            Rot4.WestInt => westOffset ?? Vector3.zero,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static DrawingOffset GetDrawingOffset(ThingDef def)
    {
        var drawingOffset = def.GetModExtension<DrawingOffset>();
        if (drawingOffset != null) 
            return drawingOffset;
        drawingOffset = new DrawingOffset();
        def.modExtensions ??= [];
        def.modExtensions.Add(drawingOffset);
        return drawingOffset;
    }

    private string BuildExtensionXml(string defName, DrawingOffset offset, bool isCore)
    {
        var north = offset.northOffset;
        var south = offset.southOffset;
        var west = offset.westOffset;
        var east = offset.eastOffset;
        var tag = isCore ? "Operation" : "li";
        var xml =
$@"<{tag} Class=""GiddyUp.PatchOperationDrawingOffset"">
    <def>{defName}</def>
    <value>
        <li Class=""GiddyUp.DrawingOffset"">
            <northOffset>({north.x:0.00}, {north.y:0.00}, {north.z:0.00})</northOffset>
            <southOffset>({south.x:0.00}, {south.y:0.00}, {south.z:0.00})</southOffset>
            <eastOffset>({east.x:0.00}, {east.y:0.00}, {east.z:0.00})</eastOffset>
            <westOffset>({west.x:0.00}, {west.y:0.00}, {west.z:0.00})</westOffset>
        </li>
    </value>
</{tag}>";
        return xml;
    }

    private string BuildPatch(string patches, string? mod)
    {
        if (mod == null)
            return
$"""
 <?xml version="1.0" encoding="utf-8" ?>
 <Patch>
 {patches.Replace("\n", "\n\t")}
 </Patch>
 """;
        return
$"""
<?xml version="1.0" encoding="utf-8" ?>
<Patch>

	<Operation Class="PatchOperationFindMod">
		<mods>
            <li>{mod}</li>
		</mods>
		<match Class="PatchOperationSequence">
			<success>Always</success>
			<operations>
                {patches.Replace("\n", "\n\t\t\t\t")}
            </operations>
		</match>
	</Operation>
</Patch>

""";
    }

    private void BuildFullXml()
    {
        var animals = FilteredAnimals();
        var mountables = animals.GroupBy(x => x.modContentPack).ToList();
        var modRoot = LoadedModManager.ModHandles.OfType<Mod_GiddyUp>().FirstOrDefault()?.Content.RootDir
                      ?? pawnDef.modContentPack?.RootDir
                      ?? throw new InvalidOperationException("Unable to resolve the Giddy-Up mod root.");
        var versionFolder = VersionControl.CurrentVersionStringWithoutBuild;
        var rootFolder = Path.Combine(modRoot, versionFolder, "Patches");
        if (!Directory.Exists(rootFolder))
            throw new InvalidOperationException("Patches folder doesn't exist");

        foreach (var mountable in mountables)
        {
            if(mountable.Key == null)
                continue;

            var fileName = string.Join("", mountable.Key.Name.Split([..Path.GetInvalidFileNameChars(), ' '])) + "Offsets.xml";

            var path =  Path.Combine(rootFolder, fileName);

            var isCore = mountable.Key.Name == "Core";

            var patches = mountable.Select(x =>
                x == null ? "" : BuildExtensionXml(x.defName, x.GetModExtension<DrawingOffset>()!, isCore));

            var xml = BuildPatch(string.Join('\n', patches), isCore ? null : mountable.Key.Name);


            if(File.Exists(path))
                Log.Warning($"Overwriting patches for '{mountable.Key.Name}");

            File.WriteAllText(path, xml);
        }
    }

    private List<ThingDef> FilteredAnimals()
    {
        List<ThingDef?> animals = [.. Setup.AllAnimals, .. Setup.AllMechs];
        animals.RemoveAll(x =>
        {
            var extension = x?.GetModExtension<DrawingOffset>();
            if (extension == null)
                return true;
            return extension.northOffset == Vector3.zero &&
                   extension.eastOffset == Vector3.zero &&
                   extension.southOffset == Vector3.zero &&
                   extension.westOffset == Vector3.zero;
        });

        return animals!;
    }
}