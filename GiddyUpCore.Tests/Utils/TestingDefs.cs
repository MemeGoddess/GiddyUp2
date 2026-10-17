using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUpCore.Tests;

[HarmonyPatch]
internal static class TestingDefs
{
    internal static FieldInfo JobTrackerPawn = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");
    internal static TraitDef Dummy = new TraitDef()
    {
        defName =  "Dummy",
        generated = true,
        degreeDatas = [new TraitDegreeData()]
    };

    static TestingDefs()
    {
        var harmony = new Harmony("GiddyUp.Core.Tests");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
    
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.HealthScale), MethodType.Getter)]
    [HarmonyPostfix]
    internal static void DummyHealthPostfix(Pawn __instance, ref float __result)
    {
        if (__instance.story?.traits?.HasTrait(TestingDefs.Dummy) is true)
            __result *= 10f;
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "TryFindAndStartJob")]
    [HarmonyPrefix]
    internal static bool JobTrackerTickIntervalPrefix(Pawn_JobTracker __instance) => 
        (JobTrackerPawn.GetValue(__instance) as Pawn)?.story?.traits?.HasTrait(Dummy) is not true;
}