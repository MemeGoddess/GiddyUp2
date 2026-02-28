using GiddyUp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace GiddyUpCore
{
    public class RemountFix(Map map) : MapComponent(map)
    {
        private bool ShouldRemount = false;
        public override void FinalizeInit()
        {
            ShouldRemount = true;
        }

        public override void MapComponentTick()
        {
            if(!ShouldRemount)
                return;

            ShouldRemount = false;
            foreach (var (_, data) in ExtendedDataStorage.Singleton.ExtendedPawnDataStore)
            {
                if (data.Pawn?.Spawned is not true)
                    continue;
                var pawn = data.Pawn;

                if (pawn.Map != map)
                    continue;

                if (data.ReservedMount == null)
                    continue;

                var mount = data.ReservedMount;
                if (mount.Dead || !mount.Spawned || mount.Faction != pawn.Faction || !pawn.CanReserve(mount))
                    continue;

                if (pawn.Map != mount.Map)
                    continue;

                if (pawn.CurJobDef == ResourceBank.JobDefOf.Mounted)
                    continue;

                pawn.GoMount(mount, MountUtility.GiveJobMethod.Instant);
            }
        }
    }
}
