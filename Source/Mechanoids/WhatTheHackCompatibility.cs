using GiddyUp;
using GiddyUpMechanoids;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace GiddyUpCore.Mechanoids
{
    public static class WhatTheHackCompatibility
    {
        public delegate bool _IsHacked(Pawn pawn);
        public delegate bool _IsActivated(Pawn pawn);

        public static _IsHacked IsHacked = _ => false;
        public static _IsActivated IsActivated = _ => false;

        public const string WhatTheHackModId = "zal.whatthehack";
        public static bool WhatTheHackEnabled = ModLister.AnyModActiveNoSuffix([WhatTheHackModId]);
        public static bool HasRequiredDefs =>
            GU_Mech_DefOf.GU_Mech_InstallGiddyUpModule != null &&
            GU_Mech_DefOf.GU_Mech_GiddyUpModule != null &&
            WhatTheHackDefOf.WTH_MountedTurret != null &&
            WhatTheHackDefOf.WTH_TargetingHacked != null &&
            WhatTheHackDefOf.WTH_TargetingHackedPoorly != null;

        public static bool HasRequiredMethods =>
            AccessTools.Method("WhatTheHack.Extensions:IsHacked") != null &&
            AccessTools.Method("WhatTheHack.Extensions:IsActivated") != null;

        public static bool CanUseInterop => WhatTheHackEnabled && HasRequiredDefs && HasRequiredMethods;

        public static void Setup()
        {
            if (!ModSettings_GiddyUp.mechanoidsEnabled || !WhatTheHackEnabled)
                return;

            if (!HasRequiredDefs)
            {
                Log.Warning("[GiddyUp-Mechs] WhatTheHack detected, but one or more required defs are missing. Mech mounting integration will stay disabled.");
                return;
            }

            var isHackedMethod = AccessTools.Method("WhatTheHack.Extensions:IsHacked");
            var isActivatedMethod = AccessTools.Method("WhatTheHack.Extensions:IsActivated");
            if (isHackedMethod == null || isActivatedMethod == null)
            {
                Log.Warning("[GiddyUp-Mechs] WhatTheHack detected, but required extension methods were not found. Mech mounting integration will stay disabled.");
                return;
            }

            IsHacked = AccessTools.MethodDelegate<_IsHacked>(isHackedMethod);
            IsActivated = AccessTools.MethodDelegate<_IsActivated>(isActivatedMethod);

            var recipe = GU_Mech_DefOf.GU_Mech_InstallGiddyUpModule;

            foreach (var mech in GiddyUp.Setup.AllMechs)
            {
                if (mech == null)
                    continue;

                mech.recipes ??= [];
                if (!mech.recipes.Contains(recipe))
                    mech.recipes.Add(recipe);
            }
        }
    }
}
