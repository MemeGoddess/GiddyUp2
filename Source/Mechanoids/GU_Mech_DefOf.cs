using GiddyUpCore.Mechanoids;
using RimWorld;
using Verse;

namespace GiddyUpMechanoids
{
    [DefOf]
    public static class GU_Mech_DefOf
    {
        [MayRequire(WhatTheHackCompatibility.WhatTheHackModId)]
        public static HediffDef GU_Mech_GiddyUpModule;
        [MayRequire(WhatTheHackCompatibility.WhatTheHackModId)]
        public static RecipeDef GU_Mech_InstallGiddyUpModule;
    }
}
