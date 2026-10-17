using System.Collections.Generic;
using Verse;

namespace GiddyUp;

//For ThingDef
public class ResearchRestrictions : DefModExtension
{
    public ResearchProjectDef? researchProjectDefToAttack = null;
    public List<ResearchProjectDef> researchProjectDefs = [];
}