using GiddyUp;
using GiddyUpCore.Compatibility;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core;

internal static class MountedRiderRenderNodeUtility
{
    public static bool ShouldUseMountedRenderNode(Pawn rider)
    {
        return CompatibilityLoader.AnimalApparelInstalled &&
               rider.IsMounted() &&
               rider.GetExtendedPawnData().Mount is { } mount &&
               mount.GetExtendedPawnData().ReservedBy == rider &&
               mount.apparel?.WornApparel.Any() == true;
    }

    public static bool TryGetMountedRider(Pawn mount, out Pawn? rider)
    {
        rider = mount.GetExtendedPawnData().ReservedBy;
        return rider != null &&
               rider.IsMounted() &&
               rider.GetExtendedPawnData().Mount == mount &&
               ShouldUseMountedRenderNode(rider);
    }

    public static Vector3 GetMountedRiderOffset(Pawn rider, Pawn mount, Rot4 rotation)
    {
        var riderData = rider.GetExtendedPawnData();
        var offset = Vector3.zero;
        if (riderData.drawOffset != -1)
            offset.z += riderData.drawOffset;

        var customOffsets = mount.def.GetModExtension<DrawingOffset>();
        if (customOffsets == null)
            return offset;

        if (rotation == Rot4.North)
            return offset + customOffsets.northOffset;
        if (rotation == Rot4.South)
            return offset + customOffsets.southOffset;
        if (rotation == Rot4.East)
            return offset + customOffsets.eastOffset;
        return offset + customOffsets.westOffset;
    }

    public static void RefreshMountedAnimalGraphics(Pawn? animal)
    {
        if (!CompatibilityLoader.AnimalApparelInstalled || animal == null)
            return;

        animal.Drawer?.renderer?.SetAllGraphicsDirty();
    }
}