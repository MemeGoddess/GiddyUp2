using DevTools.Testing;
using DevTools.Testing.Utils;
using GiddyUp;
using RimWorld;
using Verse;

namespace GiddyUpCore.Tests.Utils;

public static class GiddyUpPawnBuilder
{
    public static TestPawnBuilder WithMount(this TestPawnBuilder builder, PawnKindDef mountDef, bool mounted = true)
    {
        Pawn mount = null;
        builder.WithPostSpawn((pawn, _) =>
        {
            mount = PawnGenerator.GeneratePawn(mountDef, pawn.Faction);
            GenSpawn.Spawn(mount, pawn.Position, pawn.Map);
            mount.Name = new NameSingle(pawn.Name.ToStringShort + "'s Mount");
            if(mounted)
                pawn.GoMount(mount, MountUtility.GiveJobMethod.Instant);
            
            builder.WithCleanup(pawn =>
            {
                var riderData = pawn.GetExtendedPawnData();

                pawn.InvoluntaryDismount(mount, riderData);
                if(mount.Spawned)
                    mount.Destroy();
            });
        });
        
        return builder;
    }

    public static TestPawnBuilder WithCustomMounts(this TestPawnBuilder builder)
    {
        builder.WithPostSpawn((pawn, _) =>
        {
            List<Pawn> pawns = [pawn];

            if (pawn.Faction.HostileTo(Faction.OfPlayer) && !ModSettings_GiddyUp.battleMountsEnabled)
                return;
            
            var parms = new IncidentParms
            {
                target = pawn.Map,
                faction = pawn.Faction,
                points = pawn.kindDef.combatPower * ResourceBank.CombatPowerFactor
            };
            if(parms.points <= 0f)
                parms.points = 1f;
            MountUtility.GenerateMounts(ref pawns, parms);
            
            if (pawns.Count == 1)
                return;
            var mount = pawns.Last();
            
            Expect.IsTrue(mount.IsEverMountable(), "Generated mount isn't ever mountable");
            builder.WithCleanup((x) =>
            {
                var riderData = pawn.GetExtendedPawnData();

                pawn.InvoluntaryDismount(mount, riderData);
                if(mount.Spawned)
                    mount.Destroy();
            });
        });
        
        return builder;
    }
}