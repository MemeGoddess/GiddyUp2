using GiddyUp;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace GiddyUpCore.SaddleUp
{
    public record MountPairing(Pawn Rider, Pawn Mount, float Distance);
    public class Coordinator(Map map) : MapComponent(map)
    {
        private bool WakeUp = false;
        private bool DoNow = false;
        private List<Pawn> MonitoredPawns = new();
        public override void MapComponentTick()
        {
            if (!WakeUp)
                return;

            WakeUp = false;

            if (DoNow)
            {
                DoNow = false;
                Find.TickManager.TogglePaused();
            }

            var pawns = MonitoredPawns.ToList();
            MonitoredPawns.Clear();
            var mounts = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                .Where<Pawn>(x => (x.RaceProps.Animal || (ModSettings_GiddyUp.mechanoidsEnabled && x.RaceProps.IsMechanoid)) &&
                                  x.IsEverMountable() &&
                                  (x.CurJobDef != ResourceBank.JobDefOf.Mounted || x.GetExtendedPawnData()?.ReservedBy?.CurJobDef != ResourceBank.JobDefOf.Mount)).ToList();

            // Rider, Mount
            var mappedMounts = new Dictionary<Pawn, Pawn>();

            // Master
            foreach (var pawn in pawns)
            {
                var masterOf = mounts.Where(mount => mount.playerSettings.Master == pawn).ToArray();
                if(!masterOf.Any())
                    continue;

                var closest = masterOf.MinBy(mount => Vector3.Distance(pawn.DrawPos, mount.DrawPos));
                if (closest == null) 
                    continue;

                mappedMounts[pawn] = closest;
                mounts.Remove(closest);
            }

            pawns.RemoveAll(mappedMounts.ContainsKey);
            //Next closest
            var mountDistances = pawns
                .SelectMany(rider =>
                    mounts.Select(mount => new MountPairing(rider, mount, Vector3.Distance(rider.DrawPos, mount.DrawPos))))
                .OrderBy(x => x.Distance)
                .ToList();
            
            var usedRiders = new HashSet<Pawn>();
            var usedMounts = new HashSet<Pawn>();
            var grouped = new List<MountPairing>();

            foreach (var pairing in mountDistances)
            {
                if (usedRiders.Contains(pairing.Rider) || usedMounts.Contains(pairing.Mount))
                    continue;

                grouped.Add(pairing);
                usedRiders.Add(pairing.Rider);
                usedMounts.Add(pairing.Mount);
            }

            mappedMounts.AddRange(grouped.ToDictionary(x => x!.Rider, x => x!.Mount));

            foreach (var (rider, mount) in mappedMounts)
            {
                var jobs = rider.jobs;
                var job = new Job(ResourceBank.JobDefOf.Mount, mount);
                job.count = 1;
                var tag = new JobTag?(JobTag.Misc);
                jobs.TryTakeOrderedJob(job, tag);
                SoundDef.Named("horsesnore").PlayOneShotOnCamera();
            }
        }

        public void MountPawn(Pawn pawn)
        {
            MonitoredPawns.Add(pawn);
            WakeUp = true;

            if (!Find.TickManager.Paused) 
                return;

            Find.TickManager.TogglePaused();
            DoNow = true;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}