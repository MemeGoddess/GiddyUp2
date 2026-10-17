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
