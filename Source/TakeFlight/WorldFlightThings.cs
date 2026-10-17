using System.Collections.Generic;
using GiddyUp;
using GiddyUpCore.Core;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GiddyUpTakeFlight;

/// <summary>
/// Travelling world object for flying mounts and their riders. Moves at the group's flight speed instead of
/// transport pod speed.
/// </summary>
public class TravellingMountedFlyers : TravellingTransporters
{
    private const float VanillaRadiansPerTick = 0.00025f;

    private float tilesPerDay = -1f;
    private float cachedSpeedFactor = -1f;

    public float SpeedFactor
    {
        get
        {
            if (cachedSpeedFactor > 0f)
                return cachedSpeedFactor;
            if (tilesPerDay <= 0f)
                tilesPerDay = WorldFlightUtility.GroupSpeed(Pawns);
            var radiansPerTile = 1f / Mathf.Max(0.0001f, Find.WorldGrid.ApproxDistanceInTiles(Tile.Layer, 1f));
            cachedSpeedFactor = tilesPerDay / GenDate.TicksPerDay * radiansPerTile / VanillaRadiansPerTick;
            return cachedSpeedFactor;
        }
    }

    public void SetTilesPerDay(float value)
    {
        tilesPerDay = value;
        cachedSpeedFactor = -1f;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref tilesPerDay, "tilesPerDay", -1f);
    }
}

public static class MountedPairDrawUtility
{
    /// <summary>
    /// Draws the mount and rider held in a flight container at a position, rider on top of the mount.
    /// </summary>
    public static void DrawPair(ThingOwner? contents, DrawPhase phase, Vector3 drawLoc)
    {
        if (contents == null)
            return;
        Pawn? mount = null;
        Pawn? rider = null;
        for (var i = 0; i < contents.Count; i++)
        {
            if (contents[i] is not Pawn pawn)
                continue;
            if (pawn.RaceProps.Humanlike)
                rider ??= pawn;
            else
                mount ??= pawn;
        }

        if (mount != null)
            mount.DynamicDrawPhaseAt(phase, drawLoc);
        if (rider == null)
            return;
        var riderLoc = drawLoc;
        if (mount != null)
        {
            riderLoc += MountedRiderRenderNodeUtility.GetMountedRiderOffset(rider, mount, mount.Rotation);
            riderLoc.y += Altitudes.AltInc;
        }
        rider.DynamicDrawPhaseAt(phase, riderLoc);
    }
}

public class MountedFlyerLeaving : FlyShipLeaving
{
    public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
    {
        if (innerContainer.Count > 0)
            MountedPairDrawUtility.DrawPair(Contents?.innerContainer, phase, drawLoc);
        base.DynamicDrawPhaseAt(phase, drawLoc, flip);
    }

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        DrawDropSpotShadow();
    }
}

public class MountedFlyerIncoming : Skyfaller, IActiveTransporter
{
    public ActiveTransporterInfo? Contents =>
        innerContainer.Count > 0 ? (innerContainer[0] as ActiveTransporter)?.Contents : null;

    public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
    {
        MountedPairDrawUtility.DrawPair(Contents?.innerContainer, phase, drawLoc);
        base.DynamicDrawPhaseAt(phase, drawLoc, flip);
    }

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        DrawDropSpotShadow();
    }

    public override void Impact()
    {
        for (var i = 0; i < 4; i++)
            FleckMaker.ThrowDustPuff(Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f), Map, 1.5f);
        base.Impact();
    }
}

/// <summary>
/// Lands a flying pair: spawns mount and rider on its first tick, re-mounts them, then removes itself.
/// </summary>
public class MountedFlyerActive : ActiveTransporter
{
    private static readonly List<Pawn> TmpLanded = new List<Pawn>();

    public override void Tick()
    {
        if (Contents != null && Spawned)
            Land();
    }

    private void Land()
    {
        var map = Map;
        var cell = Position;
        var container = Contents.innerContainer;
        TmpLanded.Clear();

        for (var i = container.Count - 1; i >= 0; i--)
        {
            var thing = container[i];
            if (!container.TryDrop(thing, cell, map, ThingPlaceMode.Near, out var placed, null, null, false))
                continue;
            if (placed is Pawn pawn)
                TmpLanded.Add(pawn);
        }

        foreach (var rider in TmpLanded)
        {
            if (!rider.RaceProps.Humanlike || !rider.Spawned || rider.Downed || rider.Dead)
                continue;
            var mount = rider.GetExtendedPawnData()?.ReservedMount;
            if (mount == null || !TmpLanded.Contains(mount) || !mount.Spawned || mount.Downed || mount.Dead)
                continue;
            mount.Position = rider.Position;
            rider.GoMount(mount, MountUtility.GiveJobMethod.Instant);
        }

        foreach (var pawn in TmpLanded)
            if (pawn.IsColonist && pawn.Spawned && !map.IsPlayerHome && pawn.drafter != null)
                pawn.drafter.Drafted = true;

        TmpLanded.Clear();
        if (container.Count == 0)
            Destroy();
    }
}
