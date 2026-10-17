using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using GiddyUp;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace BattleMounts.Harmony;

[HarmonyPatch(typeof(IncidentWorker_Ambush), nameof(IncidentWorker_Ambush.DoExecute))]
internal static class Patch_IncidentWorker_Ambush
{
    private static bool Prepare()
    {
        return Settings.battleMountsEnabled;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var done = false;
        foreach (var code in instructions)
        {
            yield return code;
            if (!done && code.operand as MethodInfo == AccessTools.Method(typeof(IncidentWorker_Ambush),
                    nameof(IncidentWorker_Ambush.PostProcessGeneratedPawnsAfterSpawning)))
            {
                yield return new CodeInstruction(OpCodes.Ldarga_S, 2); //load generated pawns as parameter
                yield return new CodeInstruction(OpCodes.Ldarg_1); //load incidentparms as parameter
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(Patch_IncidentWorker_Ambush).GetMethod(nameof(MountAnimals)));
                done = true;
            }
        }
    }

    public static void MountAnimals(ref List<Pawn> list, IncidentParms parms)
    {
        //Only allow raids that are edge walk ins, except sieges (filter shared with the raid patch)
        if (list.Count == 0 || !BattleMountsUtility.ShouldGenerateMountsFor(parms))
            return;
        MountUtility.GenerateMounts(ref list, parms);
    }
}