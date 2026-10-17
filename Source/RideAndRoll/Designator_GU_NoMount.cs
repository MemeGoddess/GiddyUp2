using RimWorld;
using Verse;
using GiddyUp;

namespace GiddyUpRideAndRoll;

// Cell logic moved to the shared Designator_GU_Clear / Designator_GU_Expand bases
internal class Designator_GU_NoMount_Clear : Designator_GU_Clear
{
    public Designator_GU_NoMount_Clear() : base(ResourceBank.AreaNoMount)
    {
        defaultLabel = "GU_RR_Designator_GU_NoMount_Clear_Label".Translate();
        defaultDesc = "GU_RR_Designator_GU_NoMount_Clear_Description".Translate();
        icon = ResourceBank.iconNoMountClear;
    }
}

internal class Designator_GU_NoMount_Expand : Designator_GU_Expand
{
    public Designator_GU_NoMount_Expand() : base(ResourceBank.AreaNoMount)
    {
        defaultLabel = "GU_RR_Designator_GU_NoMount_Expand_Label".Translate();
        defaultDesc = "GU_RR_Designator_GU_NoMount_Expand_Description".Translate();
        icon = ResourceBank.iconNoMountExpand;
    }

    // Kept from the original: this expand designator uses the filled-rectangle draw styles
    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public override bool DragDrawMeasurements => true;
}
