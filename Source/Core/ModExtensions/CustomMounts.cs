using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace GiddyUp;

//For PawnKindDef
internal class CustomMounts : DefModExtension
{
    public int mountChance = 0;
    public Dictionary<PawnKindDef, int> possibleMounts = new();
    public List<CustomMountApparel> apparel = new();

    public override void ResolveReferences(Def parentDef)
    {
        base.ResolveReferences(parentDef);
        possibleMounts.RemoveAll(x =>
        {
            if (x.Key.RaceProps.Animal ||
                (!ModSettings_GiddyUp.mechanoidsEnabled || x.Key.RaceProps.IsMechanoid)) 
                return false;

            Log.Warning(
                $"Attempted to add non-animal/non-mech '{x.Key.LabelCap}' to possible mounts for '{parentDef.LabelCap}', only animals/mechs are allowed.");
            return true;
        });
    }
}

internal class CustomMountApparel
{
    public ThingDef ThingDef;
    public ThingDef? Stuff;
}