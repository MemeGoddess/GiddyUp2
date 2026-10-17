using GiddyUp;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace BattleMounts.Harmony;

[HarmonyPatch(typeof(IncidentWorker_Raid), nameof(IncidentWorker_Raid.TryGenerateRaidInfo))]
internal static class Patch_TryGenerateRaidInfo
{
    private static bool Prepare()
    {
        return Settings.battleMountsEnabled;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return instructions.MethodReplacer(
            AccessTools.Method(typeof(PawnsArrivalModeWorker), nameof(PawnsArrivalModeWorker.Arrive)),
            AccessTools.Method(typeof(Patch_TryGenerateRaidInfo), nameof(MountAnimals)));
    }

    public static void MountAnimals(PawnsArrivalModeWorker instance, List<Pawn> pawns, IncidentParms parms)
    {
        if (pawns.Count == 0)
            return;

        //Call the replaced worker directly instead of re-dereferencing parms.raidArrivalMode; filter shared with the ambush patch
        instance.Arrive(pawns, parms);
        if (!BattleMountsUtility.ShouldGenerateMountsFor(parms))
            return;
        MountUtility.GenerateMounts(ref pawns, parms);
    }
}