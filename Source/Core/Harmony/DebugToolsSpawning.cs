using HarmonyLib;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony;

[HarmonyPatch(typeof(DebugToolsSpawning), "SpawnPawn")]
internal static class Patch_DebugToolsSpawning_SpawnPawn
{
    private static bool Prepare()
    {
        return Settings.caravansEnabled || Settings.battleMountsEnabled;
    }

    private static void Postfix(List<DebugActionNode> __result)
    {
        if (__result.NullOrEmpty())
            return;

        foreach (var node in __result)
        {
            if (node?.action == null)
                continue;

            var originalAction = node.action;
            node.action = () => ExecuteWithMountGeneration(originalAction);
        }
    }

    private static void ExecuteWithMountGeneration(Action originalAction)
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            originalAction();
            return;
        }

        var existingPawnIds = new HashSet<int>(map.mapPawns.AllPawnsSpawned.Select(pawn => pawn.thingIDNumber));
        originalAction();

        map = Find.CurrentMap;
        if (map == null)
            return;

        var spawnedPawns = map.mapPawns.AllPawnsSpawned
            .Where(pawn => !existingPawnIds.Contains(pawn.thingIDNumber))
            .ToList();
        if (spawnedPawns.Count == 0)
            return;

        var faction = spawnedPawns.FirstOrDefault(pawn => pawn.RaceProps.Humanlike)?.Faction;
        if (!ShouldGenerateMounts(faction))
            return;

        var points = spawnedPawns
            .Where(pawn => pawn.RaceProps.Humanlike)
            .Sum(pawn => pawn.kindDef.combatPower) * ResourceBank.CombatPowerFactor;
        if (points <= 0f)
            points = 1f;

        var parms = new IncidentParms
        {
            target = map,
            faction = faction,
            points = points
        };

        MountUtility.GenerateMounts(ref spawnedPawns, parms);
    }

    private static bool ShouldGenerateMounts(Faction? faction)
    {
        if (faction == null)
            return false;

        return faction.HostileTo(Faction.OfPlayer)
            ? Settings.battleMountsEnabled
            : Settings.caravansEnabled;
    }
}