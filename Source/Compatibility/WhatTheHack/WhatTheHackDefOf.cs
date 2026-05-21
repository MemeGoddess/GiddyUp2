using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace GiddyUpCore.Compatibility.WhatTheHack
{
    [DefOf]
    public static class WhatTheHackDefOf
    {
        [MayRequire(Extensions.WhatTheHackModId)]
        public static HediffDef WTH_MountedTurret;

        [MayRequire(Extensions.WhatTheHackModId)]
        public static HediffDef WTH_TargetingHacked;

        [MayRequire(Extensions.WhatTheHackModId)]
        public static HediffDef WTH_TargetingHackedPoorly;
    }
}
