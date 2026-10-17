using HarmonyLib;
using Verse;

namespace GiddyUpCore.Tests.Utils;

[StaticConstructorOnStartup]
public static class Patcher
{
    static Patcher()
    {
        var harmony = new Harmony("GiddyUp.Core.Tests");
        harmony.PatchAll();
    }
}