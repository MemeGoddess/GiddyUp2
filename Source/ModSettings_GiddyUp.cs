using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GiddyUp;

public class ModSettings_GiddyUp : ModSettings
{
    //Core
    public static float handlingMovementImpact = 2.5f;
    public static float handlingAccuracyImpact = 0.5f;
    public static float bodySizeFilter = 0.2f;
    public static int accuracyPenalty = 10;
    public static Dictionary<string, float>? offsetCache;
    public static HashSet<string>? invertMountingRules; //These are only used on game start to setup the below, fast cache collections
    public static HashSet<string>? invertDrawRules; //These are only used on game start to setup the below, fast cache collections
    public static readonly HashSet<ushort> MountableCache = [];
    public static readonly HashSet<ushort> DrawRulesCache = [];

    //Ride and Roll
    public static bool rideAndRollEnabled = true;
    public static bool noMountedHunting;
    public static bool disableSlavePawnColumn;
    public static bool automountDisabledByDefault;
    public static bool logging;
    public static int minAutoMountDistance = 120;
    public static int autoHitchDistance = 50;
    public static int waitForRiderTimer = 10000;
    public static float injuredThreshold = 0.75f;

    //Battle Mounts
    public static bool battleMountsEnabled = true;
    public static int minHandlingLevel = 3;
    public static int enemyMountChance = 15;
    public static int enemyMountChancePreInd = 33;
    public static float inBiomeWeight = 20f;
    public static float outBiomeWeight = 10f;
    public static float nonWildWeight = 70f;

    //Caravans
    public static bool caravansEnabled = true;
    public static bool giveCaravanSpeed;
    public static bool ridePackAnimals = true;
    public static int visitorMountChance = 15;
    public static int visitorMountChancePreInd = 33;

    //Mechanoids
    public static bool mechanoidsEnabled = true;
    public static int mountChance = 40;
    public static string mountChanceBuffer;
    public static bool disregardCarryingCapacity = false;
    public static Dictionary<string, bool> mechSelector = new();

    //UI State
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

    public override void ExposeData()
    {
        //Core
        Scribe_Values.Look(ref handlingMovementImpact, "handlingMovementImpact", 2.5f);
        Scribe_Values.Look(ref handlingAccuracyImpact, "handlingAccuracyImpact", 0.5f);
        Scribe_Values.Look(ref accuracyPenalty, "accuracyPenalty", 10);
        Scribe_Collections.Look(ref offsetCache, "offsetCache", LookMode.Value);
        Scribe_Collections.Look(ref invertMountingRules, "invertMountingRules", LookMode.Value);
        Scribe_Collections.Look(ref invertDrawRules, "invertDrawRules", LookMode.Value);

        //Ride and Roll
        Scribe_Values.Look(ref rideAndRollEnabled, "rideAndRollEnabled", true);
        Scribe_Values.Look(ref minAutoMountDistance, "minAutoMountDistanceNew", 120);
        Scribe_Values.Look(ref autoHitchDistance, "autoHitchThreshold", 50);
        Scribe_Values.Look(ref injuredThreshold, "injuredThreshold", 0.75f);
        Scribe_Values.Look(ref waitForRiderTimer, "waitForRiderTimer", 10000);
        Scribe_Values.Look(ref noMountedHunting, "noMountedHunting");
        Scribe_Values.Look(ref disableSlavePawnColumn, "disableSlavePawnColumn");
        Scribe_Values.Look(ref automountDisabledByDefault, "automountDisabledByDefault");

        //Battle Mounts
        Scribe_Values.Look(ref battleMountsEnabled, "battleMountsEnabled", true);
        Scribe_Values.Look(ref minHandlingLevel, "minHandlingLevel", 3);
        Scribe_Values.Look(ref enemyMountChance, "enemyMountChance", 15);
        Scribe_Values.Look(ref enemyMountChancePreInd, "enemyMountChancePreInd", 33);
        Scribe_Values.Look(ref inBiomeWeight, "inBiomeWeight", 20f);
        Scribe_Values.Look(ref outBiomeWeight, "outBiomeWeight", 10f);
        Scribe_Values.Look(ref nonWildWeight, "nonWildWeight", 70);

        //Caravans
        Scribe_Values.Look(ref caravansEnabled, "caravansEnabled", true);
        Scribe_Values.Look(ref visitorMountChance, "visitorMountChance", 15);
        Scribe_Values.Look(ref visitorMountChancePreInd, "visitorMountChancePreInd", 33);
        Scribe_Values.Look(ref giveCaravanSpeed, "giveCaravanSpeed");
        Scribe_Values.Look(ref ridePackAnimals, "ridePackAnimals", true);

        //Mechanoids
        Scribe_Values.Look(ref mechanoidsEnabled, "mechanoidsEnabled", true);
        Scribe_Values.Look(ref mountChance, "mountChance", 40);
        Scribe_Values.Look(ref disregardCarryingCapacity, "disregardCarryingCapacity");
        Scribe_Collections.Look(ref mechSelector, "mechSelector", LookMode.Value, LookMode.Value);

        //UI State
        Scribe_Values.Look(ref _tabsHandler, "tabsHandler");

        base.ExposeData();
    }
}