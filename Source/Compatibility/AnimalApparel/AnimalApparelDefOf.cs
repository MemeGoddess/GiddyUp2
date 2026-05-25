using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using static GiddyUpCore.Compatibility.CompatibilityLoader;

namespace GiddyUpCore.Compatibility.AnimalApparel
{
    [DefOf]
    public static class AnimalApparelDefOf
    {
        [MayRequireAnyOf(AnimalApparelIDsCSV)] 
        public static BodyPartGroupDef AnimalHead;

    }
}
