using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUpCore.Compatibility.AnimalApparel.Patches;

[HarmonyPatch]
internal static class AnimalGear_HeadApparelLayerPatch
{
    private static readonly BodyPartGroupDef AnimalHead = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("AnimalHead");
    private static bool Prepare() => CompatibilityLoader.AnimalApparelInstalled;
    private static Type TargetType => Types.PawnRenderNode_Animal_Apparel!;

    private static System.Reflection.MethodBase? TargetMethod()
    {
        if (!CompatibilityLoader.AnimalApparelInstalled || TargetType == null)
            return null;

        return AccessTools.Constructor(TargetType,
        [
            typeof(Pawn),
            typeof(PawnRenderNodeProperties),
            typeof(PawnRenderTree),
            typeof(Apparel)
        ]);
    }

    private static void Prefix(PawnRenderNodeProperties props, Apparel apparel)
    {
        if (!ShouldRaiseHeadApparel(apparel))
            return;

        props.baseLayer += 10f;
    }

    private static bool ShouldRaiseHeadApparel(Apparel apparel)
    {
        return AnimalHead != null &&
               apparel?.def?.apparel?.bodyPartGroups != null &&
               apparel.def.apparel.bodyPartGroups.Contains(AnimalHead);
    }
}