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
