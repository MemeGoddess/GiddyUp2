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
        public static bool AnimalApparel = ModLister.AnyModActiveNoSuffix(["Ingendum.AnimalApparelFramework"]);

    }
}
