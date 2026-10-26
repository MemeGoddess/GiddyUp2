using GiddyUp;
using RimWorld;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpTakeFlight;

/// <summary>
/// Pawn flyer that carries a mount as its flying pawn and the mount's rider as a second passenger.
/// </summary>
public class MountedPawnFlyer : PawnFlyer
{
    private Pawn? rider;
    private bool riderWasDrafted;
    private bool riderCanFireAtWill = true;

    public Pawn? Rider => rider;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (respawningAfterLoad || def.pawnFlyer == null || Settings.takeFlightSpeed <= 0f)
            return;
        var minTicks = def.pawnFlyer.flightDurationMin.SecondsToTicks();
        ticksFlightTime = Mathf.Max(minTicks,
            Mathf.RoundToInt(ticksFlightTime * def.pawnFlyer.flightSpeed / Settings.takeFlightSpeed));
    }

    /// <summary>
    /// Despawns the rider and stores it in the flyer alongside the mount.
    /// </summary>
    public void AddRider(Pawn newRider)
    {
        rider = newRider;
        riderWasDrafted = newRider.Drafted;
        riderCanFireAtWill = newRider.drafter?.FireAtWill ?? true;
        if (newRider.Spawned)
            newRider.DeSpawn(DestroyMode.WillReplace);
        if (!GetDirectlyHeldThings().TryAdd(newRider))
            Log.Error("[Giddy-Up] Could not add " + newRider + " to a flyer.");
    }

    public override void RespawnPawn()
    {
        var mount = FlyingPawn;
        var map = Map;
        base.RespawnPawn();

        if (rider == null || map == null || !GetDirectlyHeldThings().Contains(rider))
            return;

        var cell = mount is { Spawned: true } && mount.Map == map ? mount.Position : Position;
        if (!GetDirectlyHeldThings().TryDrop(rider, cell, map, ThingPlaceMode.Direct, out _, null, null, false) &&
            !GetDirectlyHeldThings().TryDrop(rider, cell, map, ThingPlaceMode.Near, out _, null, null, false))
        {
            Log.Error("[Giddy-Up] Could not drop " + rider + " from a flyer.");
            return;
        }

        rider.Rotation = Rotation;
        if (mount is { Spawned: true, Dead: false, Downed: false } && rider.Spawned && !rider.Dead && !rider.Downed)
        {
            rider.GoMount(mount, MountUtility.GiveJobMethod.Instant);
        }
        else if (rider.IsMounted())
        {
            var riderData = rider.GetExtendedPawnData();
            if (riderData != null)
                rider.Dismount(mount ?? riderData.Mount, riderData, false, default, false, false);
        }

        if (rider.drafter != null)
        {
            rider.drafter.Drafted = riderWasDrafted;
            rider.drafter.FireAtWill = riderCanFireAtWill;
        }

        if (rider.Spawned)
            rider.jobs?.CheckForJobOverride();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref rider, "rider");
        Scribe_Values.Look(ref riderWasDrafted, "riderWasDrafted");
        Scribe_Values.Look(ref riderCanFireAtWill, "riderCanFireAtWill", true);
    }
}
