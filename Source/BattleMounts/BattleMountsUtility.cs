using GiddyUp;
using RimWorld;

namespace BattleMounts;

public static class BattleMountsUtility
{
    /// <summary>
    /// Shared raid filter: only edge walk-in (or unresolved) arrivals that are not sieges get mounts.
    /// </summary>
    public static bool ShouldGenerateMountsFor(IncidentParms parms)
    {
        return (parms.raidArrivalMode == null || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkIn) &&
               (parms.raidStrategy == null || parms.raidStrategy.workerClass != ResourceBank.RaidStrategyWorker_Siege);
    }
}
