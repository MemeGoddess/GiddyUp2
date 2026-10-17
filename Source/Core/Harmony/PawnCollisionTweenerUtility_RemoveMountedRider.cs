using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using GiddyUp;
using HarmonyLib;
using Verse;

namespace GiddyUpCore.Core.Harmony;

[HarmonyPatch(typeof(PawnCollisionTweenerUtility), "GetPawnsStandingAtOrAboutToStandAt")]
// ReSharper disable once UnusedType.Global
public static class PawnCollisionTweenerUtility_RemoveMountedRider
{
    private const string _featureFailString = "Mounted Riders will show slightly off-center of cell when not moving";

    [HarmonyTranspiler]
    // ReSharper disable once UnusedMember.Global
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);

        var pawnType = typeof(Pawn);
        var isMounted =
            AccessTools.Method(typeof(StorageUtility), nameof(StorageUtility.IsMounted));

        Label? label = null;
        matcher.MatchEndForward(
                new CodeMatch(x => x.opcode == OpCodes.Isinst && (Type)x.operand == pawnType),
                new CodeMatch(x => x.opcode == OpCodes.Stloc_S),
                new CodeMatch(x =>
                    x.opcode == OpCodes.Ldloc_S &&
                    (x.operand as LocalBuilder)?.LocalType == pawnType),
                new CodeMatch(x => x.Branches(out label)))
            .ThrowIfInvalid(_featureFailString + ": Unable to find injection point");

        matcher.InsertAfter(new CodeInstruction(OpCodes.Ldloc_S, matcher.InstructionAt(-1).operand),
                new CodeInstruction(OpCodes.Call, isMounted), //IsMounted is static; callvirt only worked because Mono tolerates it
                new CodeInstruction(OpCodes.Brtrue_S, label))
            .ThrowIfInvalid(
                _featureFailString + ": Unable to skip collision check on mounted pawns");

        return matcher.Instructions();
    }
}