using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using GiddyUpCore.Compatibility;

namespace GiddyUp;

internal class CompOverlay : ThingComp
{
    public CompProperties_Overlay Prop => props as CompProperties_Overlay ?? throw new InvalidOperationException();
    public bool _valid;
    private bool _incompatible;
    private Pawn _pawn;
    private readonly Dictionary<Rot4, (GraphicData, Vector3, Vector3, Vector3)> _graphicCache = new();

    public override void PostDraw()
    {
        if (_valid && _pawn.IsMountedAnimal() &&
            _graphicCache.TryGetValue(_pawn.Rotation == Rot4.East ? Rot4.West : _pawn.Rotation, out var cache))
        {
            var drawPos = parent.DrawPos;
            var offset = _pawn.gender == Gender.Female ? cache.Item2 : cache.Item3;
            if (offset == Vector3.zero)
                offset = cache.Item4;
            if (_pawn.Rotation == Rot4.West)
                offset.x = -offset.x;
            offset.y += GetOverlayAltitude();

            //Somehow the rotation is flipped, hence the use of GetOpposite.
            cache.Item1.Graphic.Draw(drawPos + offset, parent.Rotation, parent, 0f);
        }
        else if (!_valid && !_incompatible)
        {
            TryCache();
        }
    }

    private float GetOverlayAltitude()
    {
        if (_pawn.Rotation != Rot4.South)
            return 0.04375f;

        if (CompatibilityLoader.AnimalApparelInstalled && _pawn.apparel?.WornApparel.Any() == true)
            return 0.015f;

        return 0.08f;
    }
    
    private void TryCache()
    {
        if (parent is Pawn pawn)
        {
            _pawn = pawn;
            CacheGraphicData(Rot4.South);
            CacheGraphicData(Rot4.North);
            CacheGraphicData(Rot4.West);
        }
    }
    
    private void CacheGraphicData(Rot4 rotation)
    {
        var overlay = Prop.GetOverlay(rotation);
        if (overlay == null)
            return;

        var graphicData = (_pawn.gender == Gender.Female ? overlay.graphicDataFemale : overlay.graphicDataMale) ??
                          overlay.graphicDataDefault;
        if (graphicData == null)
            return;

        //support multi texture animals
        if (overlay.allVariants != null)
        {
            var graphicPath = _pawn.Drawer?.renderer?.SilhouetteGraphic?.path;
            if (graphicPath.NullOrEmpty())
                return;
            var graphicName = graphicPath!.Split('/').Last();
            var found = false;
            foreach (var variant in overlay.allVariants)
            {
                var variantName = variant.texPath.Split('/').Last().Split(overlay.stringDelimiter.ToCharArray())[0];

                if (graphicName == variantName)
                {
                    //set required properties
                    var texPath = variant.texPath;
                    variant.CopyFrom(graphicData);
                    variant.texPath = texPath;
                    graphicData = variant;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                _incompatible = true;
                return;
            }
        }

        _graphicCache.Add(rotation, (graphicData, overlay.offsetFemale, overlay.offsetMale, overlay.offsetDefault));
        _valid = true;
    }
}