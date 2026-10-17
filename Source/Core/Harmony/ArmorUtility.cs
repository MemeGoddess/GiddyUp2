using HarmonyLib;
using Verse;

namespace GiddyUp.Harmony;

[HarmonyPatch(typeof(ArmorUtility), nameof(ArmorUtility.ApplyArmor))]
internal class Patch_ApplyArmor
{
    public static void Postfix(ref float armorRating, ref float damAmount, ref bool metalArmor, Pawn pawn)
    {
        if (!pawn.IsMountedAnimal()) 
            return;
        
        var modExt = pawn.kindDef.GetModExtension<CustomStats>();
        if (modExt == null) 
            return;
        
        armorRating *= modExt.armorModifier;
        damAmount /= modExt.armorModifier;
        metalArmor = modExt.useMetalArmor;
    }
}