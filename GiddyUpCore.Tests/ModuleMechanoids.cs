using System.Collections;
using DevTools.Testing;
using DevTools.Testing.Utils;
using GiddyUp;
using GiddyUpCore.Mechanoids;
using GiddyUpCore.Tests.Utils;
using GiddyUpMechanoids;
using RimWorld;
using UnityEngine;
using Verse;
using static GiddyUp.IsMountableUtility;

namespace GiddyUpCore.Tests;

[TestFixture(TestType.Playing)]
public class ModuleMechanoids : RiderAndMountTest
{
    protected override Faction FactionOfPawns => Faction.OfPlayer;
    protected override PawnKindDef AnimalKind => PawnKindDefOf.Mech_Warqueen;

    [Test]
    public IEnumerable MechMountable()
    {
        // Can be mounted
        Expect.IsTrue(_mount.IsMountableMech(out var reason, _rider, true, true));
        Expect.AreEqual(Reason.CanMount, reason);
        Expect.IsFalse(_mount.IsInPoorCondition(), "Animal is in poor condition after spawning");

        // Does mount with Job
        Utility.ForceMountMech(_rider, _mount, _riderData);
        yield return _rider.WaitJob(ResourceBank.JobDefOf.Mount);
        yield return _mount.WaitJob(ResourceBank.JobDefOf.Mounted);
        ValidateIsMounted("job");

        Utility.DismountMech(_rider, _mount, _riderData);
        yield return new WaitTicks(1);
        ValidateIsNotMounted("job");

        // Shouldn't be mountable while hurt
        while (_mount.health.summaryHealth.SummaryHealthPercent >= 0.75)
            _mount.TakeDamage(new DamageInfo(DamageDefOf.Crush, 10f));

        Expect.IsTrue(_mount.IsInPoorCondition(), "Animal is not in poor condition after crushing");

        // Should be mountable after health reset
        _mount.health.Reset();
        Expect.IsFalse(_mount.IsInPoorCondition(),
            "Animal is still in poor condition after resetting health");

        // Shouldn't be mountable after infection
        _mount.health.AddHediff(HediffDefOf.WoundInfection, _mount.RaceProps.body.corePart);
        Expect.IsTrue(_mount.health.HasHediffsNeedingTend(),
            "Animal does not have tendable hediff after infection added");
        Expect.IsTrue(_mount.IsInPoorCondition(),
            "Animal is not in poor condition after getting an infection");

        // Should be mountable after infection tended
        var infection = _mount.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.WoundInfection);
        Expect.IsNotNull(infection, "Animal does not have infection");
        infection.Tended(1f, 1f);
        Expect.IsFalse(_mount.IsInPoorCondition(),
            "Animal is still in poor condition after infection tended");

        // Shouldn't be mountable if enemy
        _mount.health.Reset();
        _mount.SetFaction(Faction.OfAncientsHostile);
        Expect.IsFalse(_mount.IsMountable(out var factionReason, _rider, true, true));
        Expect.AreEqual(Reason.WrongFaction, factionReason);
    }

    [Test]
    public void RightClickMountable()
    {
        // Has mount option
        Find.Selector.Select(_rider);
        var options = FloatMenuMakerMap.GetOptions([_rider], _mount.DrawPos,out _);
        Expect.IsNotEmpty(options, "No right click options when testing mounting");
        Expect.IsTrue(
            options.Any(x => x.Label == "GUC_Mount".Translate()),
            $"No option to mount mech, options were: {string.Join(", ", options.Select(x => x.Label))}");

        // Has switch mount option
        var builder = PawnKindDefOf
            .Colonist
            .AsBuilder()
            .WithName("OtherRider")
            .WithMount(PawnKindDef.Named("Horse"));
        var otherRider = builder.Spawn();

        Find.Selector.Deselect(_rider);
        Find.Selector.Select(otherRider);
        options = FloatMenuMakerMap.GetOptions([otherRider], _mount.DrawPos,out _);
        Expect.IsNotEmpty(options, "No right click options when testing switch mounting");
        Expect.IsTrue(options
                .Any(x => x.Label == "GUC_SwitchMount".Translate()),
            $"No option to switch mech, options were: {string.Join(", ", options.Select(x => x.Label))}");
        builder.Destroy();
    }

    [Test]
    public IEnumerable Extensions()
    {
        var mechanitor = PawnKindDefOf.Mechanitor_Basic
            .AsBuilder()
            .WithName("Mechanitor")
            .WithWorkMaxed(WorkTypeDefOf.Crafting)
            .WithMech(PawnKindDef.Named("Mech_Lifter")).Spawn();
        var mech = mechanitor.Map.mapPawns.SpawnedColonyMechs[0];

        yield return new WaitUntil(() => mech.IsHacked());
        
        Expect.IsTrue(mech.IsHacked(), "Mech was not hacked");
        Expect.IsTrue(mech.IsHacked(mechanitor), "Mech was not hacked by Mechanitor");
        Expect.IsTrue(mech.IsActivated(), "Mech was not activated");
    }


}