using GiddyUp.Ideology;
using RimWorld;
using Verse;

namespace GiddyUp;

public class ThoughtWorker_Precept_NoRecentMount : ThoughtWorker_Precept
{
    private NoRecentMount? noRecentMountExtension;

    public override ThoughtState ShouldHaveThought(Pawn p)
    {
        if (!ModsConfig.IdeologyActive || !p.IsColonist || p.IsSlave)
            return ThoughtState.Inactive;

        noRecentMountExtension ??= def.GetModExtension<NoRecentMount>() ?? new NoRecentMount();
        var lastMountedTick = p.GetExtendedPawnData().GetLastMountedTick(noRecentMountExtension.acceptableMountDefs);

        var ticksSinceMount = Find.TickManager.TicksGame - lastMountedTick;
        if (ticksSinceMount >= noRecentMountExtension.SevereDays * GenDate.TicksPerDay)
            return ThoughtState.ActiveAtStage(2);
        if (ticksSinceMount >= noRecentMountExtension.MajorDays * GenDate.TicksPerDay)
            return ThoughtState.ActiveAtStage(1);
        if (ticksSinceMount >= noRecentMountExtension.MinorDays * GenDate.TicksPerDay)
            return ThoughtState.ActiveAtStage(0);
        return ThoughtState.Inactive;
    }
}