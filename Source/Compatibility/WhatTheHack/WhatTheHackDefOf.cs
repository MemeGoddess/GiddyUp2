using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using static GiddyUpCore.Compatibility.CompatibilityLoader;

namespace GiddyUpCore.Compatibility.WhatTheHack
{
    [DefOf]
    public static class WhatTheHackDefOf
    {
        [MayRequireAnyOf(WhatTheHackIDsCSV)]
        public static HediffDef WTH_MountedTurret;

        [MayRequireAnyOf(WhatTheHackIDsCSV)]
        public static HediffDef WTH_TargetingHacked;

        [MayRequireAnyOf(WhatTheHackIDsCSV)]
        public static HediffDef WTH_TargetingHackedPoorly;
    }
}
