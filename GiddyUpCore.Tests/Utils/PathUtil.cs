using Verse;

namespace GiddyUpCore.Tests.Utils;

public static class PathUtil
{
    public static float PathCost(this Pawn pawn, IntVec3 destination, IntVec3? start = null)
    {
        start ??= pawn.Position;
        var path = pawn.Map.pathFinder.FindPathNow(start.Value, destination, pawn);
        var cost = path?.Found is true ? path.TotalCost : 0f;
        path?.ReleaseToPool();
        return cost;
    }
}