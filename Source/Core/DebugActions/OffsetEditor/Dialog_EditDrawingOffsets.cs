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

namespace GiddyUpCore.Core.DebugActions.OffsetEditor;

internal sealed class Dialog_EditDrawingOffsets : Window
{
    private static readonly Rot4[] Rotations = [Rot4.North, Rot4.South, Rot4.East, Rot4.West];
    private static readonly string[] AxisLabels = ["X", "Y", "Z"];
    private const float ColumnGap = 12f;
    private const float PortraitHeight = 160f;
    private const float PortraitGap = 60f;
    private const float PreviewPadding = 8f;

    private readonly Pawn pawn;
    private readonly ThingDef pawnDef;
    private DrawingOffset workingOffset;
    private Vector3? northOffset, southOffset, eastOffset, westOffset;
    private readonly Dictionary<Rot4, string[]> buffers = new();

    public override Vector2 InitialSize => new(1120f, 760f);

    public Dialog_EditDrawingOffsets(Pawn pawn)
    {
        this.pawn = pawn;
        pawnDef = pawn.def;
        workingOffset = GetDrawingOffset(pawnDef);

        doCloseX = true;
        doCloseButton = false;
        //absorbInputAroundWindow = true;
        optionalTitle = "GU_DrawOffsetEditor_Title".Translate(pawn.LabelCap, pawnDef.defName);
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
        var changed = false;

        var center = rect.center;

        var offset = PortraitGap + PortraitHeight / 2 ; 
        foreach (var rotation in Rot4.AllRotations)
        {
            var rotationCenter = center + (rotation.AsVector2 * offset);
            var rotationRect = new Rect(rotationCenter.x - (PortraitHeight / 2), rotationCenter.y - (PortraitHeight / 2), PortraitHeight,
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

            var portraitCenter = rotationRect.center;
            var textHeight = Text.LineHeight;
            var zBox = new Rect(rotationRect.x + 20f, portraitCenter.y - (textHeight / 2), rotationRect.width / 2, textHeight);
            var xBox = new Rect(rotationRect.x, rotationRect.y + rotationRect.height - 20f - textHeight,
                rotationRect.width, textHeight);
            Widgets.Label(zBox, $"{newZ:F}");
            var textAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(xBox, $"{newX:F}");
            Text.Anchor = textAnchor;

            if (previousX != newX || previousZ != newZ)
            {
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
        //Widgets.DrawBoxSolid(rect, Color.red);
        var previewRect = rect.ContractedBy(PreviewPadding);
        var portrait = PortraitsCache.Get(pawn, previewRect.size, rotation, cameraZoom: 0.6f, supersample: true, compensateForUIScale: true);
        Widgets.DrawTextureFitted(previewRect, portrait, 1f);
    }

    private void DrawButtons(Rect rect)
    {
        const float buttonWidth = 180f;
        var copyRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
        var saveRect = new Rect(copyRect.xMax + 12f, rect.y, buttonWidth, rect.height);
        var resetRect = new Rect(saveRect.xMax + 12f, rect.y, buttonWidth, rect.height);

        if (Widgets.ButtonText(saveRect, "GU_DrawOffsetEditor_SavePatch".Translate()))
            BuildFullXml();

        if (Widgets.ButtonText(resetRect, "GU_DrawOffsetEditor_Reset".Translate()))
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

    private string GetPatchFilePath()
    {
        var modRoot = LoadedModManager.ModHandles.OfType<Mod_GiddyUp>().FirstOrDefault()?.Content.RootDir
                      ?? pawnDef.modContentPack?.RootDir
                      ?? throw new InvalidOperationException("Unable to resolve the Giddy-Up mod root.");
        var versionFolder = VersionControl.CurrentVersionStringWithoutBuild;
        var fileName = pawnDef.modContentPack.Name + "Offsets.xml";

        return Path.Combine(modRoot, versionFolder, "Patches", fileName);
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

    private readonly record struct OffsetState(bool HasExtension, DrawingOffset Offset);
}