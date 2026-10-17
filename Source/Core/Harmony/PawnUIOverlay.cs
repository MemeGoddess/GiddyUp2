using HarmonyLib;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace GiddyUp.Harmony;

//This just moves their label down so it's below the mounted animal
[HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
internal class Patch_DrawPawnGUIOverlay
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var codes = instructions.ToList();
        var label = generator.DefineLabel();
        var drawPawnLabel = AccessTools.Method(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel), new System.Type[]
        {
            typeof(Pawn),
            typeof(Vector2),
            typeof(float),
            typeof(float),
            typeof(Dictionary<string, string>),
            typeof(GameFont),
            typeof(bool),
            typeof(bool)
        });

        var found = false;
        for (var i = 0; i < codes.Count - 1; i++)
        {
            if (!codes[i].Calls(drawPawnLabel))
                continue;
            codes[i + 1].labels.Add(label);
            found = true;
            break;
        }

        //A label that was never placed makes the ILGenerator throw; leave the method alone if the anchor is missing
        if (!found)
        {
            Log.Warning("[Giddy-Up] Could not find GenMapUI.DrawPawnLabel in PawnUIOverlay.DrawPawnGUIOverlay; mounted riders' labels won't be moved.");
            return codes;
        }

        codes.Insert(0, new CodeInstruction(OpCodes.Brfalse_S, label));
        codes.Insert(0, new CodeInstruction(OpCodes.Call, typeof(Patch_DrawPawnGUIOverlay).GetMethod(nameof(OffsetLabel))));
        codes.Insert(0, new CodeInstruction(OpCodes.Ldarg_0));
        return codes;
    }

    public static bool OffsetLabel(PawnUIOverlay __instance)
    {
        if (!__instance.pawn.IsMounted())
            return true;
        var data = __instance.pawn.GetExtendedPawnData();

        var pos = GenMapUI.LabelDrawPosFor(__instance.pawn, -(data.drawOffset + 0.75f));
        GenMapUI.DrawPawnLabel(__instance.pawn, pos, 1f, 9999f, null, GameFont.Tiny, true, true);
        return false;
    }
}