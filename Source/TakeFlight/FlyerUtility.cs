using System.Collections.Generic;
using GiddyUp;
using RimWorld;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpTakeFlight;

public static class FlyerUtility
{
    private const string VefFloatingCompName = "CompProperties_Floating";
    private const float RecoverSearchRadius = 12f;

    private static readonly Dictionary<ThingDef, FlyingMount?> ExtensionCache = new();

    /// <summary>
    /// Fills the flying race cache from defaults plus the player's overrides.
    /// </summary>
    public static void BuildCache(List<ThingDef?> animals)
    {
        Settings.invertFlyingRules ??= [];
        Settings.FlyingCache.Clear();
        foreach (var def in animals)
        {
            if (def == null)
                continue;
            var flyer = DefaultIsFlyer(def);
            if (Settings.invertFlyingRules.Contains(def.defName))
                flyer = !flyer;
            if (flyer)
                Settings.FlyingCache.Add(def.shortHash);
        }
    }

    /// <summary>
    /// Stores every race whose flying state differs from its default, so the cache can be rebuilt next start.
    /// </summary>
    public static void RebuildInversions(List<ThingDef?> animals)
    {
        Settings.invertFlyingRules = [];
        foreach (var def in animals)
        {
            if (def == null)
                continue;
            if (Settings.FlyingCache.Contains(def.shortHash) != DefaultIsFlyer(def))
                Settings.invertFlyingRules.Add(def.defName);
        }
    }

    /// <summary>
    /// Cached lookup of the FlyingMount extension on a race. Null when the race has none.
    /// </summary>
    public static FlyingMount? ExtensionFor(ThingDef? def)
    {
        if (def == null)
            return null;
        if (!ExtensionCache.TryGetValue(def, out var extension))
        {
            extension = def.GetModExtension<FlyingMount>();
            ExtensionCache[def] = extension;
        }
        return extension;
    }

    /// <summary>
    /// Whether the race flies before player overrides are applied.
    /// </summary>
    public static bool DefaultIsFlyer(ThingDef def)
    {
        var extension = ExtensionFor(def);
        if (extension != null)
            return extension.canFly;

        if (def.statBases != null && def.statBases.GetStatValueFromList(StatDefOf.MaxFlightTime, 0f) > 0f)
            return true;

        if (def.comps == null)
            return false;

        foreach (var t in def.comps)
            if (t?.GetType().Name == VefFloatingCompName)
                return true;

        return false;
    }

    public static bool IsFlyer(ThingDef? def) => def != null && Settings.FlyingCache.Contains(def.shortHash);

    public static bool TryGetFlyingMount(Pawn? rider, out Pawn mount)
    {
        mount = null!;
        if (rider == null || !ExtendedDataStorage.isMounted.Contains(rider.thingIDNumber))
            return false;
        mount = rider.GetExtendedPawnData()?.Mount!;
        return mount != null && IsFlyer(mount.def);
    }

    public static bool IsRidingFlyer(Pawn rider) => TryGetFlyingMount(rider, out _);

    public static float FlightRangeFor(Pawn mount)
    {
        var factor = ExtensionFor(mount.def)?.rangeFactor ?? 1f;
        return Mathf.Max(2f, Settings.flightRange * factor);
    }

    public static int CooldownTicksFor(Pawn mount)
    {
        var factor = ExtensionFor(mount.def)?.cooldownFactor ?? 1f;
        return Mathf.RoundToInt(Settings.flightCooldownHours * GenDate.TicksPerHour * factor);
    }

    /// <summary>
    /// Moves a pawn left standing on a cell it can't walk on (deep water after a dismount) to the nearest
    /// standable cell. Does nothing if no such cell is found nearby.
    /// </summary>
    public static void MoveOffUnwalkableCell(Pawn? pawn)
    {
        if (pawn == null || !pawn.Spawned || pawn.Dead)
            return;
        var map = pawn.Map;
        var origin = pawn.Position;
        if (origin.WalkableBy(map, pawn))
            return;

        var pattern = GenRadial.RadialPattern;
        var count = Mathf.Min(pattern.Length, GenRadial.NumCellsInRadius(RecoverSearchRadius));
        for (var i = 1; i < count; i++)
        {
            var cell = origin + pattern[i];
            if (!cell.InBounds(map) || !cell.Standable(map) || !cell.WalkableBy(map, pawn))
                continue;
            pawn.Position = cell;
            pawn.Notify_Teleported(false);
            return;
        }
    }
}
