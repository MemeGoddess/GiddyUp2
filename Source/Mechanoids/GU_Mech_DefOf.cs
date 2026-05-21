using GiddyUpCore.Compatibility.WhatTheHack;
using RimWorld;
using Verse;

namespace GiddyUpMechanoids
{
    [DefOf]
    public static class GU_Mech_DefOf
    {
        [MayRequire(Extensions.WhatTheHackModId)]
        public static HediffDef GU_Mech_GiddyUpModule;
        [MayRequire(Extensions.WhatTheHackModId)]
        public static RecipeDef GU_Mech_InstallGiddyUpModule;
    }
}
