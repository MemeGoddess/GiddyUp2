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

    private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
    {
        foreach (var value in values)
            yield return value;

        if (__instance.CurJobDef == GiddyUp.ResourceBank.JobDefOf.WaitForRider)
            yield return new Command_Action
            {
                defaultLabel = label,
                defaultDesc = desc,
                icon = ContentFinder<Texture2D>.Get("UI/" + "LeaveRider", true),
                action = () => PawnEndCurrentJob(__instance)
            };
    }

    private static void PawnEndCurrentJob(Pawn pawn)
    {
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }
}