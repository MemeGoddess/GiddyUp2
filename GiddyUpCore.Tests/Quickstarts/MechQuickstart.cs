using DevTools.Testing;
using DevTools.Testing.Utils;
using RimWorld;
using Verse;

namespace GiddyUpCore.Tests;

[Quickstart("Mechanoid")]
public class MechQuickstart
{
    [SetUp]
    public void SetUp()
    {
        PawnKindDefOf.Mechanitor_Basic
            .AsBuilder()
            .WithName("Mechanitor")
            .WithWorkMaxed(WorkTypeDefOf.Crafting)
            .WithMech(PawnKindDef.Named("Mech_Lifter"))
            .Spawn();
    }
}