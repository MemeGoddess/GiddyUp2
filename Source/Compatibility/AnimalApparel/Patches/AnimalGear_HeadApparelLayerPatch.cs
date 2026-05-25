using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUpCore.Compatibility.AnimalApparel.Patches;

[HarmonyPatch]
internal static class AnimalGear_HeadApparelLayerPatch
{
    private static bool Prepare() => CompatibilityLoader.AnimalApparelInstalled;
    private static Type? TargetType => Types.PawnRenderNode_Animal_Apparel;

    private static System.Reflection.MethodBase? TargetMethod()
    {
        if (!CompatibilityLoader.AnimalApparelInstalled || TargetType == null)
            return null;

        var constructor = AccessTools.Constructor(TargetType,
        [
            typeof(Pawn),
            typeof(PawnRenderNodeProperties),
            typeof(PawnRenderTree),
            typeof(Apparel)
        ]);

        if(constructor == null)
            Log.Error("[GiddyUp2] Patch to keep head armor above rider failed");
        return constructor;
    }

    private static void Prefix(PawnRenderNodeProperties props, Apparel apparel)
    {
        if (!ShouldRaiseHeadApparel(apparel))
            return;

        props.baseLayer += 10f;
    }

    private static bool ShouldRaiseHeadApparel(Apparel apparel)
    {
        return apparel?.def?.apparel?.bodyPartGroups != null &&
               apparel.def.apparel.bodyPartGroups.Contains(AnimalApparelDefOf.AnimalHead);
    }
}