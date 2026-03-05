using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace GiddyUpCore.Temporary
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
