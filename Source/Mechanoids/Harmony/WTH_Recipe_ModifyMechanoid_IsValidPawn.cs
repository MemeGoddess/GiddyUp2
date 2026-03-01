using GiddyUp;
using GiddyUpCore.Mechanoids;
using HarmonyLib;
using Verse;

namespace GiddyUpMechanoids;

[HarmonyPatch("WhatTheHack.Recipes.Recipe_ModifyMechanoid", "IsValidPawn")]
class WTH_Recipe_ModifyMechanoid_IsValidPawn
{
    public static bool Prepare() =>
        ModSettings_GiddyUp.mechanoidsEnabled && WhatTheHackCompatibility.WhatTheHackEnabled;

    static bool Prefix(RecipeWorker __instance, Pawn pawn, ref bool __result)
    { 
        __result = pawn.IsHacked() && !pawn.health.hediffSet.HasHediff(__instance.recipe.addsHediff);
        return false;
    }
}