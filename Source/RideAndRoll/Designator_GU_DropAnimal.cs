using GiddyUp;
using Verse;

namespace GiddyUpRideAndRoll;

// Cell logic moved to the shared Designator_GU_Clear / Designator_GU_Expand bases
internal class Designator_GU_DropAnimal_Clear : Designator_GU_Clear
{
    public Designator_GU_DropAnimal_Clear() : base(ResourceBank.AreaDropMount)
    {
        defaultLabel = "GU_RR_Designator_GU_DropAnimal_Clear_Label".Translate();
        defaultDesc = "GU_RR_Designator_GU_DropAnimal_Clear_Description".Translate();
        icon = ResourceBank.iconDropAnimalClear;
    }
}

internal class Designator_GU_DropAnimal_Expand : Designator_GU_Expand
{
    public Designator_GU_DropAnimal_Expand() : base(ResourceBank.AreaDropMount)
    {
        defaultLabel = "GU_RR_Designator_GU_DropAnimal_Expand_Label".Translate();
        defaultDesc = "GU_RR_Designator_GU_DropAnimal_Expand_Description".Translate();
        icon = ResourceBank.iconDropAnimalExpand;
    }

    // Drop-animal spots additionally exclude impassable cells (kept from the original)
    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        return base.CanDesignateCell(c).Accepted && !c.Impassable(Map);
    }
}
