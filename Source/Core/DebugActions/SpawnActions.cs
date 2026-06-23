using System.Collections.Generic;
using System.Linq;
using GiddyUp;
using LudeonTK;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace GiddyUpCore.Core.DebugActions
{

    internal class SpawnActions
    {
        [DebugAction("GiddyUp", "Spawn Rideable Mounted Animals", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        internal static void SpawnMounted()
        {
            DoSpawn(true, Setup.AllAnimals);
        }

        [DebugAction("GiddyUp", "Spawn Rideable Mounted Mechs", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        internal static void SpawnMountedMechs()
        {
            DoSpawn(true, Setup.AllMechs);
        }

        [DebugAction("GiddyUp", "Spawn All Mounted Animals", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        internal static void SpawnAll()
        {
            DoSpawn(false, Setup.AllAnimals);
        }

        [DebugAction("GiddyUp", "Spawn All Mounted Mechs", allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMap)]
        internal static void SpawnAllMechs()
        {
            DoSpawn(false, Setup.AllMechs);

        }

        private static void DoSpawn(bool allowed, List<ThingDef?> list)
        {
            var location = UI.MouseCell();
            var cache = ModSettings_GiddyUp.MountableCache;
            var animalKinds = DefDatabase<PawnKindDef>.AllDefs
                .Where(x => !allowed ? list.Contains(x.race) : cache.Contains(x.race.shortHash))
                .ToList();

            var colonistKind = PawnKindDefOf.Colonist;
            var map = Find.CurrentMap;
            var letterString = string.Empty;
            var letter = string.Empty;
            var index = 0;
            var leftRight = false;
            var row = 0;
            foreach (var animalKind in animalKinds)
            {
                var current = location + new IntVec3((leftRight ? -1 : 1) * index, 0, row);
                leftRight = !leftRight;
                if (leftRight)
                    index++;
                if (!TryFindClosestFreeOnXAxis(current, map, out var closest))
                {
                    index = 0;
                    leftRight = false;
                    row += 3;
                    current = location + new IntVec3((leftRight ? -1 : 1) * index, 0, row);
                    if (!TryFindClosestFreeOnXAxis(current, map, out closest))
                    {
                        Log.Error("Unable to find a free spot to spawn mounts");
                        return;
                    }
                }

                var colonist = PawnGenerator.GeneratePawn(colonistKind, FactionUtility.DefaultFactionFrom(colonistKind.defaultFactionDef));
                var animal = PawnGenerator.GeneratePawn(animalKind);

                if (animal.RaceProps.IsMechanoid)
                    colonist.health.AddHediff(HediffDefOf.MechlinkImplant);

                animal.mechanitor = new Pawn_MechanitorTracker
                {
                    pawn = colonist
                };

                if (colonist == null)
                {
                    Log.Error("Colonist was null when spawning mounts");
                    return;
                }

                if (animal == null)
                {
                    Log.Error("Animal was null when spawning mounts");
                    continue;
                }

                GenSpawn.Spawn(colonist, closest, map, Rot4.South);
                GenSpawn.Spawn(animal, closest, map, Rot4.South);
                PostPawnSpawn(colonist);
                colonist.drafter.Drafted = true;

                InteractionWorker_RecruitAttempt.DoRecruit(colonist, animal, out letterString, out letter, false, false);
                colonist.GoMount(animal, MountUtility.GiveJobMethod.Instant);

            }
        }

        private static bool TryFindClosestFreeOnXAxis(IntVec3 origin, Map map, out IntVec3 result)
        {
            result = IntVec3.Invalid;

            if (!origin.InBounds(map))
                return false;

            // If the starting cell is already free, keep it.
            if (origin.Standable(map))
            {
                result = origin;
                return true;
            }

            int maxOffset = System.Math.Max(origin.x, map.Size.x - 1 - origin.x);

            for (int offset = 1; offset <= maxOffset; offset++)
            {
                IntVec3 east = new IntVec3(origin.x + offset, 0, origin.z);
                if (east.InBounds(map) && east.Standable(map))
                {
                    result = east;
                    return true;
                }

                IntVec3 west = new IntVec3(origin.x - offset, 0, origin.z);
                if (west.InBounds(map) && west.Standable(map))
                {
                    result = west;
                    return true;
                }
            }

            return false;
        }

        private static void PostPawnSpawn(Pawn pawn)
        {
            if (pawn.Spawned && pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
            {
                Lord lord = null;

                if (pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Any(p => p != pawn))
                {
                    Pawn existingPawn = (Pawn)GenClosest.ClosestThing_Global(
                        pawn.Position,
                        pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction),
                        validator: thing => thing != pawn && ((Pawn)thing).GetLord() != null
                    );

                    lord = existingPawn != null ? existingPawn.GetLord() : null;
                }

                if (lord == null || !lord.CanAddPawn(pawn))
                {
                    lord = LordMaker.MakeNewLord(
                        pawn.Faction,
                        new LordJob_DefendPoint(pawn.Position),
                        Find.CurrentMap
                    );
                }

                if (lord != null && lord.LordJob.CanAutoAddPawns)
                {
                    lord.AddPawn(pawn);
                }
            }

            pawn.Rotation = Rot4.South;
        }
    }
}
