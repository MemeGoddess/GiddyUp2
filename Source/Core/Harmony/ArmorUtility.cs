using System.Linq;
using HarmonyLib;
using Verse;

namespace GiddyUp.Harmony;

//The armor rating is already scaled by StatPart_Armor, so scaling it again here applied armorModifier twice. This only
//marks the mount's hide as metal if its kind asks for it, and is only patched in when some PawnKind uses that option.
[HarmonyPatch(typeof(ArmorUtility), nameof(ArmorUtility.ApplyArmor))]
internal static class Patch_ApplyArmor
{
    private static bool Prepare() =>
        DefDatabase<PawnKindDef>.AllDefsListForReading.Any(x => x.GetModExtension<CustomStats>()?.useMetalArmor == true);

    private static void Postfix(Pawn pawn, ref bool metalArmor)
    {
        if (metalArmor || pawn == null || !pawn.IsMountedAnimal())
            return;
        if (pawn.kindDef.GetModExtension<CustomStats>() is { useMetalArmor: true })
            metalArmor = true;
    }
}
