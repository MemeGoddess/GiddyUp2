using HarmonyLib;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony;

[HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
internal static class Pawn_DrawTracker_DrawPos
{
    private static bool Prefix(Pawn_DrawTracker __instance, ref Vector3 __result)
    {
        if (!ExtendedDataStorage.isMounted.Contains(__instance.pawn.thingIDNumber))
            return true;

        __result = DrawOffset(__instance);
        return false;
    }

    public static Vector3 DrawOffset(Pawn_DrawTracker __instance)
    {
        var pawn = __instance.pawn;
        var pawnData = pawn.GetExtendedPawnData();

        //Failsafe. Should never happen but too dangerous to chance
        if (pawnData.Mount == null)
        {
            pawn.Dismount(null, pawnData, true);
            return Vector3.zero;
        }

        var offset = pawnData.Mount.Drawer.DrawPos;
        if (pawnData.drawOffset != -1)
            offset.z = offset.z + pawnData.drawOffset;
        //Apply custom offsets
        var rotation = pawn.rotationInt;
        var modX = pawnData.Mount.def.GetModExtension<DrawingOffset>();
        if (modX != null)
            offset += AddCustomOffsets(rotation, modX);

        if (rotation == Rot4.South && Settings.DrawRulesCache.Contains(pawnData.Mount.def.shortHash))
            offset.y -= 0.01f;
        else
            offset.y += 0.01f;

        return offset;
    }

    private static Vector3 AddCustomOffsets(Rot4 rotation, DrawingOffset customOffsets)
    {
        if (rotation == Rot4.North)
            return customOffsets.northOffset;
        if (rotation == Rot4.South)
            return customOffsets.southOffset;
        if (rotation == Rot4.East)
            return customOffsets.eastOffset;
        return customOffsets.westOffset;
    }
}