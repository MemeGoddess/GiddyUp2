using System.Reflection;
using GiddyUp;
using GiddyUpCore.Mechanoids;
using HarmonyLib;
using Verse;

namespace GiddyUpMechanoids
{
    [HarmonyPatch]
    static class WTH_Recipe_ModifyMechanoid_CanApplyOn
    {
        public static bool Prepare() => ModSettings_GiddyUp.mechanoidsEnabled && WhatTheHackCompatibility.WhatTheHackEnabled;

        public static MethodBase[] TargetMethods() =>
            [AccessTools.Method("WhatTheHack.Recipes.Recipe_ModifyMechanoid:CanApplyOn")];

        static void Postfix(RecipeWorker __instance, Pawn pawn, ref string reason, ref bool __result)
        {
            if (__instance.recipe != GU_Mech_DefOf.GU_Mech_InstallGiddyUpModule ||
                ModSettings_GiddyUp.MechSelectedCache.Contains(pawn.def.shortHash)) return;

            reason = "GU_BME_Reason_NotAllowed".Translate();
            __result = false;
        }
    }
}
