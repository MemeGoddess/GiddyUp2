using RimWorld;
using Verse;

namespace GiddyUp;

public sealed class ThoughtWorker_Precept_NoRecentMount : ThoughtWorker_Precept
{
    private const int MinorDays = 5;
    private const int MajorDays = 10;
    private const int SevereDays = 15;

    public override ThoughtState ShouldHaveThought(Pawn p)
    {
        if (!ModsConfig.IdeologyActive || !p.IsColonist || p.IsSlave)
            return ThoughtState.Inactive;

        var lastMountedTick = p.GetExtendedPawnData().lastMountedTick;

        var ticksSinceMount = Find.TickManager.TicksGame - lastMountedTick; 
        return ticksSinceMount switch
        {
            >= SevereDays * GenDate.TicksPerDay => ThoughtState.ActiveAtStage(2),
            >= MajorDays * GenDate.TicksPerDay => ThoughtState.ActiveAtStage(1),
            >= MinorDays * GenDate.TicksPerDay => ThoughtState.ActiveAtStage(0),
            _ => ThoughtState.Inactive
        };
    }
}