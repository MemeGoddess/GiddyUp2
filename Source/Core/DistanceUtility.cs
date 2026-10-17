using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUp;

public static class DistanceUtility
{
    public static IntVec3 GetFirstTarget(this Job job, TargetIndex index)
    {
        var queue = job.GetTargetQueue(index);
        if (queue.Count != 0)
            return queue[0].Cell;
        return job.GetTarget(index).Cell;
    }

    public static IntVec3 GetClosestAreaLoc(this Area area, IntVec3 sourceLocation)
    {
        var targetLoc = IntVec3.Invalid; // Was default (0,0,0), a real cell; an empty area now yields Invalid so callers' CanReach checks fail cleanly
        var minDistance = float.MaxValue;
        foreach (var loc in area.ActiveCells)
        {
            var distance = loc.DistanceTo(sourceLocation);
            if (distance < minDistance)
            {
                minDistance = distance;
                targetLoc = loc;
            }
        }

        return targetLoc;
    }

    public static bool DetermineTargets(this Job job, out IntVec3 firstTarget, out IntVec3 secondTarget)
    {
        var thinkResultJob = job;
        var thinkResultJobDef = thinkResultJob.def;
        if (thinkResultJobDef == JobDefOf.TendPatient || thinkResultJobDef == JobDefOf.Refuel ||
            thinkResultJobDef == JobDefOf.FixBrokenDownBuilding)
        {
            firstTarget = thinkResultJob.GetFirstTarget(TargetIndex.B);
            secondTarget = thinkResultJob.GetFirstTarget(TargetIndex.A);
        }
        else if (thinkResultJobDef == JobDefOf.DoBill && !thinkResultJob.targetQueueB.NullOrEmpty())
        {
            firstTarget = thinkResultJob.targetQueueB[0].Cell;
            secondTarget = thinkResultJob.GetFirstTarget(TargetIndex.A);
        }
        else
        {
            firstTarget = thinkResultJob.GetFirstTarget(TargetIndex.A);
            secondTarget = thinkResultJob.GetFirstTarget(TargetIndex.B);
        }

        if (!firstTarget.IsValid)
            return false;
        return true;
    }
}