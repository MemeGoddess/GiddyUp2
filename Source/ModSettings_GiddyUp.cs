using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GiddyUp;

public class ModSettings_GiddyUp : ModSettings
{
    public static float handlingMovementImpact = 2.5f,
        bodySizeFilter = 0.2f,
        handlingAccuracyImpact = 0.5f,
        inBiomeWeight = 20f,
        outBiomeWeight = 10f,
        nonWildWeight = 70f,
        injuredThreshold = 0.75f;

    public static int accuracyPenalty = 10,
        minAutoMountDistance = 120,
        minHandlingLevel = 3,
        enemyMountChance = 15,
        enemyMountChancePreInd = 33,
        visitorMountChance = 15,
        visitorMountChancePreInd = 33,
        autoHitchDistance = 50,
        waitForRiderTimer = 10000;

    public static bool rideAndRollEnabled = true,
        battleMountsEnabled = true,
        caravansEnabled = true,
        noMountedHunting,
        logging,
        giveCaravanSpeed,
        automountDisabledByDefault,
        disableSlavePawnColumn,
        ridePackAnimals = true;

    public static HashSet<string>?
        invertMountingRules,
        invertDrawRules; //These are only used on game start to setup the below, fast cache collections

    public static readonly HashSet<ushort> MountableCache = [];
    public static readonly HashSet<ushort> DrawRulesCache = [];
    private static string? _tabsHandler;
    public static Vector2 scrollPos;
    public static SelectedTab selectedTab = SelectedTab.BodySize;

    public enum SelectedTab
    {
        BodySize,
        DrawBehavior,
        Core,
        Rnr,
        BattleMounts,
        Caravans
    };
    
    public static Dictionary<string, float>? offsetCache;
    
    public override void ExposeData()
    {
        Scribe_Values.Look(ref handlingMovementImpact, "handlingMovementImpact", 2.5f);
        Scribe_Values.Look(ref handlingAccuracyImpact, "handlingAccuracyImpact", 0.5f);
        Scribe_Values.Look(ref accuracyPenalty, "accuracyPenalty", 10);
        Scribe_Values.Look(ref minAutoMountDistance, "minAutoMountDistanceNew", 120);
        Scribe_Values.Look(ref minHandlingLevel, "minHandlingLevel", 3);
        Scribe_Values.Look(ref enemyMountChance, "enemyMountChance", 15);
        Scribe_Values.Look(ref enemyMountChancePreInd, "enemyMountChancePreInd", 33);
        Scribe_Values.Look(ref inBiomeWeight, "inBiomeWeight", 20f);
        Scribe_Values.Look(ref outBiomeWeight, "outBiomeWeight", 10f);
        Scribe_Values.Look(ref nonWildWeight, "nonWildWeight", 70);
        Scribe_Values.Look(ref visitorMountChance, "visitorMountChance", 15);
        Scribe_Values.Look(ref visitorMountChancePreInd, "visitorMountChancePreInd", 33);
        Scribe_Values.Look(ref autoHitchDistance, "autoHitchThreshold", 50);
        Scribe_Values.Look(ref _tabsHandler, "tabsHandler");
        Scribe_Values.Look(ref rideAndRollEnabled, "rideAndRollEnabled", true);
        Scribe_Values.Look(ref battleMountsEnabled, "battleMountsEnabled", true);
        Scribe_Values.Look(ref caravansEnabled, "caravansEnabled", true);
        Scribe_Values.Look(ref noMountedHunting, "noMountedHunting");
        Scribe_Values.Look(ref disableSlavePawnColumn, "disableSlavePawnColumn");
        Scribe_Values.Look(ref automountDisabledByDefault, "automountDisabledByDefault");
        Scribe_Values.Look(ref giveCaravanSpeed, "giveCaravanSpeed");
        Scribe_Values.Look(ref ridePackAnimals, "ridePackAnimals", true);
        Scribe_Values.Look(ref injuredThreshold, "injuredThreshold", 0.75f);
        Scribe_Values.Look(ref waitForRiderTimer, "waitForRiderTimer", 10000);
        Scribe_Collections.Look(ref invertMountingRules, "invertMountingRules", LookMode.Value);
        Scribe_Collections.Look(ref invertDrawRules, "invertDrawRules", LookMode.Value);
        Scribe_Collections.Look(ref offsetCache, "offsetCache", LookMode.Value);

        base.ExposeData();
    }
}