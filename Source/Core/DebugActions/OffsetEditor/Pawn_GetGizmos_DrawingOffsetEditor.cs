using System.Collections.Generic;
using GiddyUp;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core.DebugActions.OffsetEditor;

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
#if DEBUG
        return pawn.RaceProps.Animal || ModSettings_GiddyUp.mechanoidsEnabled && pawn.RaceProps.IsMechanoid;
#else
        return DebugSettings.godMode && (pawn.RaceProps.Animal || ModSettings_GiddyUp.mechanoidsEnabled && pawn.RaceProps.IsMechanoid);
#endif
    }
}