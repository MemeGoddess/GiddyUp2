using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace GiddyUpCore.Mechanoids
{
    [DefOf]
    public static class WhatTheHackDefOf
    {
        [MayRequire(WhatTheHackCompatibility.WhatTheHackModId)]
        public static HediffDef WTH_MountedTurret;

        [MayRequire(WhatTheHackCompatibility.WhatTheHackModId)]
        public static HediffDef WTH_TargetingHacked;

        [MayRequire(WhatTheHackCompatibility.WhatTheHackModId)]
        public static HediffDef WTH_TargetingHackedPoorly;
    }
}
