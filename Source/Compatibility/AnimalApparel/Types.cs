using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GiddyUpCore.Compatibility.AnimalApparel
{
    internal static class Types
    {
        internal static Type? DynamicPawnRenderNodeSetup_Animal_Apparel = CompatibilityLoader.AnimalApparelInstalled
            ? AccessTools.TypeByName("AnimalGear.Graphics.DynamicPawnRenderNodeSetup_Animal_Apparel")
            : null;

        internal static Type? PawnRenderNode_Animal_Apparel = CompatibilityLoader.AnimalApparelInstalled
            ? AccessTools.TypeByName("AnimalGear.Graphics.PawnRenderNode_Animal_Apparel")
            : null;
    }
}
