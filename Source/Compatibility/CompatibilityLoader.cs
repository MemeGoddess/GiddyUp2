using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GiddyUpCore.Compatibility
{
    public static class CompatibilityLoader
    {
        public static void Setup()
        {
            if (WhatTheHackInstalled)
                WhatTheHackCompat.Up();
            if(AnimalApparelInstalled)
                AnimalApparelCompat.Up();
        }

        public const string AnimalApparelIDsCSV = "Ingendum.AnimalApparelFramework";
        public static string[] AnimalApparelIDs = AnimalApparelIDsCSV.Split(',');
        public static bool AnimalApparelInstalled = ModLister.AnyModActiveNoSuffix(AnimalApparelIDs);

        private class AnimalApparelCompat
        {
            internal static void Up()
            {
                AnimalApparel.AnimalGearHelper.Setup();
            }
        }

        public const string WhatTheHackIDsCSV = "zal.whatthehack";
        public static string[] WhatTheHackIDs = WhatTheHackIDsCSV.Split(',');
        public static bool WhatTheHackInstalled = ModLister.AnyModActiveNoSuffix(WhatTheHackIDs);

        private class WhatTheHackCompat
        {
            internal static void Up()
            {
                WhatTheHack.Extensions.Setup();
            }
        }

        public const string WalkTheWorldIDsCSV = "addvans.WalkTheWorld";
        public static string[] WalkTheWorldIDs = WalkTheWorldIDsCSV.Split(',');
        public static bool WalkTheWorldInstalled = ModLister.AnyModActiveNoSuffix(WalkTheWorldIDs);

    }
}
