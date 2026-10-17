using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace GiddyUp;

public class Area_GU : Area
{
    private string _label;
    private Color _color;

    // Needs to exist for Scribe
    public Area_GU()
    {
    }
    
    public Area_GU(AreaManager areaManager, string label) : base(areaManager)
    {
        _color = new Color(Rand.Value, Rand.Value, Rand.Value);
        this._label = label;
    }

    public override string Label => _label;

    public override Color Color => _color;

    public override int ListPriority => 300;

    public override string GetUniqueLoadID()
    {
        //One such area per map, not per save: the label clashed between maps and logged "Id already used" on load
        return "Area_GU_" + ID;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look<string>(ref _label, "label", null, false);
        Scribe_Values.Look(ref _color, "color", default, false);
    }
}

[HarmonyPatch(typeof(Area), nameof(Area.Set))]
internal static class Patch_AreaSet
{
    private static void Postfix(Area __instance)
    {
        var label = __instance.Label;
        if (label is ResourceBank.AreaDropMount or ResourceBank.AreaNoMount) __instance.Map.UpdateAreaCache();
    }
}

[HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
internal static class Patch_MapFinalizeInit
{
    private static void Postfix(Map __instance)
    {
        __instance.UpdateAreaCache(true);
    }
}

//Drop the area cache entries of a removed map; they kept the whole map alive through its areas
[HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
internal static class Patch_DeinitAndRemoveMap
{
    private static void Prefix(Map? map)
    {
        if (map != null)
            ExtendedDataStorage.Singleton?.RemoveAreaCache(map);
    }
}