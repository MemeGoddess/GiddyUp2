using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace GiddyUpCore
{
    [HarmonyPatch(typeof(LanguageDatabase), nameof(LanguageDatabase.SelectLanguage))]
    public static class StringCache
    {
        static Dictionary<string, string> cache = new();
        static void Postfix()
        {
            cache.Clear();
        }

        public static string Translate(this string str)
        {
            if (cache.TryGetValue(str, out var val))
                return val;

            val = TranslatorFormattedStringExtensions.Translate(str);
            cache[str] = val;
            return val;
        }
    }
}
