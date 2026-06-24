using System;
using GiddyUp;
using GiddyUpCore.Compatibility;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core;

internal static class MountedRiderRenderNodeUtility
{

    public static Vector3 GetMountedRiderOffset(Pawn rider, Pawn mount, Rot4 rotation)
    {
        var riderData = rider.GetExtendedPawnData();
        var offset = Vector3.zero;
        if (riderData.drawOffset != -1)
            offset.z += riderData.drawOffset;

        var customOffsets = mount.def.GetModExtension<DrawingOffset>();
        if (customOffsets == null)
            return offset;

        return offset + customOffsets.GetOffsetByRotation(rotation);
    }

    public static Vector3 GetOffsetByRotation(this DrawingOffset offset, Rot4 rotation)
    {
        return rotation.AsInt switch
        {
            Rot4.NorthInt => offset.northOffset,
            Rot4.SouthInt => offset.southOffset,
            Rot4.EastInt => offset.eastOffset,
            Rot4.WestInt => offset.westOffset,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static void RefreshMountedAnimalGraphics(Pawn? animal)
    {
        animal?.Drawer?.renderer?.SetAllGraphicsDirty();
    }
}