using System.Collections.Generic;
using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUpCore.Compatibility.AnimalApparel.Patches;

[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
internal static class Pawn_JobTracker_TryTakeOrderedJobPatch
{
    private static bool Prepare() => CompatibilityLoader.AnimalApparelInstalled;

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return instructions.MethodReplacer(
            AccessTools.Method(typeof(Job), nameof(Job.JobIsSameAs), [typeof(Pawn), typeof(Job)]),
            AccessTools.Method(typeof(Pawn_JobTracker_TryTakeOrderedJobPatch), nameof(JobIsSameAsUnlessMountedRemoveApparelQueued)));
    }

    private static bool JobIsSameAsUnlessMountedRemoveApparelQueued(Job currentJob, Pawn pawn, Job queuedJob)
    {
        if (queuedJob.def == ResourceBank.JobDefOf.Mounted &&
            pawn.jobs.jobQueue.FirstOrFallback(null)?.job.def == JobDefOf.RemoveApparel &&
            pawn.GetExtendedPawnData().Pawn != null)
        {
            return false;
        }

        return currentJob.JobIsSameAs(pawn, queuedJob);
    }
}