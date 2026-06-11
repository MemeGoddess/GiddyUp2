using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCore.RideAndRoll.Harmony;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
public class Pawn_GetGizmos
{
    private static string label = "GU_RR_Gizmo_LeaveRider_Label".Translate();
    private static string desc = "GU_RR_Gizmo_LeaveRider_Description".Translate();
    private static bool Prepare()
    {
        return Settings.rideAndRollEnabled;
    }

    private static void Postfix(ref IEnumerable<Gizmo> __result, Pawn __instance)
    {
        if (__instance.CurJobDef == GiddyUp.ResourceBank.JobDefOf.WaitForRider)
            __result = __result.AddItem(new Command_Action
            {
                defaultLabel = label,
                defaultDesc = desc,
                icon = ContentFinder<Texture2D>.Get("UI/" + "LeaveRider", true),
                action = () => PawnEndCurrentJob(__instance)
            });
    }

    private static void PawnEndCurrentJob(Pawn pawn)
    {
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }
}