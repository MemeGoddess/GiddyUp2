using GiddyUp;
using HarmonyLib;
//using Multiplayer.API;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony;

[HarmonyPatch(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter),
    new Type[]
    {
        typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>), typeof(CaravanDropInventoryMode), typeof(bool)
    })]
internal class Patch_CaravanEnterMapUtility
{
    private static bool Prepare()
    {
        return Settings.caravansEnabled;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var removePawns = AccessTools.DeclaredMethod(typeof(Caravan), nameof(Caravan.RemoveAllPawns), Type.EmptyTypes);
        var pawns = AccessTools.DeclaredField(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.tmpPawns));
        var mount = typeof(Patch_CaravanEnterMapUtility).GetMethod(nameof(MountCaravanMounts));
        if (removePawns != null && pawns != null && pawns.IsStatic && pawns.FieldType == typeof(List<Pawn>) && mount != null)
        {
            var matches = code.FindAll(instruction =>
                (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && instruction.OperandIs(removePawns));
            if (matches.Count == 1)
            {
                var index = code.IndexOf(matches[0]);
                code.Insert(index + 1, new CodeInstruction(OpCodes.Ldsfld, pawns));
                code.Insert(index + 2, new CodeInstruction(OpCodes.Call, mount));
                return code;
            }
        }
        Log.Warning("[Giddy-Up] Caravan riders could not mount automatically. Other riding features are still available.");
        return code;
    }

    //[SyncMethod]
    public static void MountCaravanMounts(List<Pawn> pawns)
    {
        foreach (var pawn in pawns)
            if (pawn.IsColonist && pawn.Spawned)
                pawn.GoMount(null, MountUtility.GiveJobMethod.Instant);
    }
}
