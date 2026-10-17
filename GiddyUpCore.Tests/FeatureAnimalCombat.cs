using System.Collections;
using DevTools.Testing;
using GiddyUp;
using RimWorld;
using Verse;

namespace GiddyUpCore.Tests;

[TestFixture(TestType.Playing)]
public class FeatureAnimalCombat : RiderAndMountTest
{
    protected override Faction FactionOfPawns => Faction.OfPlayer;
    protected override PawnKindDef AnimalKind => PawnKindDef.Named("Bear_Grizzly");
    private Pawn _dummy = null!;

    [TearDown]
    public void Teardown() => 
        _dummy?.Destroy();

    [Test]
    public IEnumerator AnimalAttackWhileMounted()
    {
        _rider.GoMount(_mount, MountUtility.GiveJobMethod.Instant);
        yield return new WaitTicks(1);
        ValidateIsMounted("setup");
        _rider.drafter.Drafted = true;
        
        // Baseline
        _dummy = SpawnDummy(_rider.Position + new IntVec3(1, 0, 0));
        yield return new WaitTicks(200);
        Expect.IsFalse(_dummy.health.hediffSet.hediffs.Any(x => x.sourceDef == _mount.def), "Dummy has damage from animal before training");
        
        // Animal attacking
        _dummy.health.Reset();
        _mount.training.Train(TrainableDefOf.Obedience, _rider, true);
        _rider.Dismount(_mount, _riderData, true);
        yield return new WaitTicks(1);
        _rider.GoMount(_mount, MountUtility.GiveJobMethod.Instant);
        yield return new WaitTicks(1000);
        Expect.IsTrue(_dummy.health.hediffSet.hediffs.Any(x => x.sourceDef == _mount.def), "Dummy has no damage from animal after training");
    }

    private Pawn SpawnDummy(IntVec3 spawnLocation)
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Pirate, Faction.OfAncientsHostile);
        pawn.story.traits.GainTrait(new Trait(TestingDefs.Dummy));
        pawn.thinker = null; // Disable jobs?
        GenSpawn.Spawn(pawn, spawnLocation, Find.CurrentMap);
        return pawn;
    }
}