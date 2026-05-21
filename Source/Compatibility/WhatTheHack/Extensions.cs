using GiddyUp;
using GiddyUpMechanoids;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Compatibility.WhatTheHack
{
    public static class Extensions
    {
        public delegate bool _IsHacked(Pawn pawn);
        public delegate bool _IsActivated(Pawn pawn);

        public static _IsHacked IsHacked = _ => default;
        public static _IsActivated IsActivated = _ => default;

        public const string WhatTheHackModId = "zal.whatthehack";
        public static bool WhatTheHackEnabled = ModLister.AnyModActiveNoSuffix([WhatTheHackModId]);
        public static void Setup()
        {
            if (!ModSettings_GiddyUp.mechanoidsEnabled || !WhatTheHackEnabled)
                return;

            IsHacked = AccessTools.MethodDelegate<_IsHacked>(
                AccessTools.Method("WhatTheHack.Extensions:IsHacked"));

            IsActivated = AccessTools.MethodDelegate<_IsActivated>(
                AccessTools.Method("WhatTheHack.Extensions:IsActivated"));

            foreach (var mech in GiddyUp.Setup.AllMechs)
            {
                if (!mech.recipes.Contains(GU_Mech_DefOf.GU_Mech_InstallGiddyUpModule))
                    mech.recipes.Add(GU_Mech_DefOf.GU_Mech_InstallGiddyUpModule);
            }
        }
    }
}
