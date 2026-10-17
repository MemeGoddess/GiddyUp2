using GiddyUp;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUpRideAndRoll;

internal class Alert_NoDropAnimal : Alert
{
    //Frame-based gating; the old ticksGame % 20 check re-ran every frame while paused
    private const int RecheckIntervalFrames = 60;

    public Alert_NoDropAnimal()
    {
        defaultLabel = "GU_RR_NoDropAnimal_Label".Translate();
        defaultExplanation = "GU_RR_NoDropAnimal_Description".Translate();
    }

    public override AlertReport GetReport()
    {
        return ShouldAlert();
    }

    private bool _shouldAlertCached;
    private int _nextCheckFrame;

    private bool ShouldAlert()
    {
        var frame = Time.frameCount;
        if (frame < _nextCheckFrame)
            return _shouldAlertCached;
        _nextCheckFrame = frame + RecheckIntervalFrames;

        foreach (var map in Find.Maps)
        {
            map.GetGUAreas(out var areaNoMount, out var areaDropAnimal);
            // Cheap area checks first; the animal scan only runs when they pass
            if (areaNoMount == null || areaNoMount.innerGrid.TrueCount == 0 ||
                (areaDropAnimal != null && areaDropAnimal.innerGrid.TrueCount != 0))
                continue;

            if (HasUnropableColonyAnimal(map))
                return _shouldAlertCached = true;
        }

        return _shouldAlertCached = false;
    }

    /// <summary>
    /// Plain loop instead of LINQ Any to avoid the per-map delegate allocation.
    /// </summary>
    private static bool HasUnropableColonyAnimal(Map map)
    {
        var animals = map.mapPawns.SpawnedColonyAnimals;
        for (var i = 0; i < animals.Count; i++)
        {
            if (!AnimalPenUtility.NeedsToBeManagedByRope(animals[i]))
                return true;
        }

        return false;
    }
}
