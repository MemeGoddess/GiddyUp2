using HarmonyLib;
using Verse;

// Moved from Source/Temporary into Compatibility; namespace updated to match, unused usings dropped
namespace GiddyUpCore.Compatibility.YayosAnimation
{
    [StaticConstructorOnStartup]
    public class YayosAnimation_YesItsMe
    {
        private static bool Installed() => ModLister.AnyModActiveNoSuffix(["com.yayo.yayoAni.continued"]);

        static YayosAnimation_YesItsMe()
        {
            try
            {
                if (!Installed())
                    return;

                var usingGiddyUp = AccessTools.Field("YayoAnimation.Core:usingGiddyUp");

                if (usingGiddyUp == null)
                    return;

                var val = usingGiddyUp.GetValue(null);

                if (val is not false)
                    return;

                usingGiddyUp.SetValue(null, true);
                Log.Message(
                    "Applied temporary patch to Yayo's Animation to allow it to recognise 'Giddy-Up 2 - Continued'");
            }
            catch
            {
                // ignored
            }
        }
    }
}
