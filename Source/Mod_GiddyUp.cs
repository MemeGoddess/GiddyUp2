using GiddyUp.Jobs;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using GiddyUpCore.RideAndRoll;
using Verse;
using UnityEngine;
using static GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp;

public class Mod_GiddyUp : Mod
{
#if DEBUG
    public static Mod_GiddyUp Instance;
#endif
    private static int coreLineNumber, mechLineNumber;
    public Mod_GiddyUp(ModContentPack content) : base(content)
    {
        GetSettings<ModSettings_GiddyUp>();
#if DEBUG
        Instance = this;
#endif
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        //========Setup tabs=========
        GUI.BeginGroup(inRect);
        var tabs = new List<TabRecord>();
        tabs.Add(new TabRecord("GUC_Core_Tab".Translate(), delegate { selectedTab = SelectedTab.Core; },
            selectedTab == SelectedTab.Core || selectedTab == SelectedTab.BodySize ||
            selectedTab == SelectedTab.DrawBehavior));
        tabs.Add(new TabRecord("GUC_RnR_Tab".Translate(), delegate { selectedTab = SelectedTab.Rnr; },
            selectedTab == SelectedTab.Rnr));
        tabs.Add(new TabRecord("GUC_BattleMounts_Tab".Translate(), delegate { selectedTab = SelectedTab.BattleMounts; },
            selectedTab == SelectedTab.BattleMounts));
        tabs.Add(new TabRecord("GUC_Caravans_Tab".Translate(), delegate { selectedTab = SelectedTab.Caravans; },
            selectedTab == SelectedTab.Caravans));
        tabs.Add(new TabRecord("GU_Mechanoids_Tab".Translate(), delegate { selectedTab = SelectedTab.Mechanoids; },
            selectedTab == SelectedTab.Mechanoids));

        var rect = new Rect(0f, 32f, inRect.width, inRect.height - 32f);
        Widgets.DrawMenuSection(rect);
        TabDrawer.DrawTabs(new Rect(0f, 32f, inRect.width, Text.LineHeight), tabs);

        switch (selectedTab)
        {
            case SelectedTab.Core:
            case SelectedTab.BodySize:
            case SelectedTab.DrawBehavior:
                DrawCore(inRect, out tabs);
                break;
            case SelectedTab.Rnr:
                DrawRnR(rect);
                break;
            case SelectedTab.BattleMounts:
                DrawBattleMounts(rect);
                break;
            case SelectedTab.Caravans:
                DrawCaravan(rect);
                break;
            case SelectedTab.Mechanoids:
                DrawMechanoid(rect);
                break;
        }

        GUI.EndGroup();
    }


    private void DrawCore(Rect inRect, out List<TabRecord>? tabs)
    {
        if (selectedTab == SelectedTab.Core)
            selectedTab = SelectedTab.BodySize;

        var options = new Listing_Standard();
        options.Begin(inRect.ContractedBy(15f));

        options.Label(
            "GUC_HandlingMovementImpact_Title".Translate("0", "10", "2.5", handlingMovementImpact.ToString()), -1f,
            "GUC_HandlingMovementImpact_Description".Translate());
        handlingMovementImpact = options.Slider((float)Math.Round(handlingMovementImpact, 1), 0f, 10f);

        options.Label("GUC_AccuracyPenalty_Title".Translate("0", "100", "10", accuracyPenalty.ToString()), -1f,
            "GUC_AccuracyPenalty_Description".Translate());
        accuracyPenalty = (int)options.Slider(accuracyPenalty, 0f, 100f);

        options.Label(
            "GUC_HandlingAccuracyImpact_Title".Translate("0", "2", "0.5", handlingAccuracyImpact.ToString()), -1f,
            "GUC_HandlingAccuracyImpact_Description".Translate());
        handlingAccuracyImpact = options.Slider((float)Math.Round(handlingAccuracyImpact, 1), 0f, 2f);

        options.Gap();

        if (options.ButtonText("GU_Reset_Cache".Translate()))
            offsetCache = null;

        //Record positioning before closing out the lister...
        var mountableFilterRect = inRect.ContractedBy(15f);
        mountableFilterRect.y = options.curY + 90f;
        mountableFilterRect.height = inRect.height - options.curY - 105f; //Use remaining space

        options.End();

        //========Setup tabs=========
        tabs = new List<TabRecord>();
        tabs.Add(new TabRecord("GUC_Mountable_Tab".Translate(), delegate { selectedTab = SelectedTab.BodySize; },
            selectedTab == SelectedTab.BodySize));
        tabs.Add(new TabRecord("GUC_DrawBehavior_Tab".Translate(),
            delegate { selectedTab = SelectedTab.DrawBehavior; }, selectedTab == SelectedTab.DrawBehavior));

        Widgets.DrawMenuSection(mountableFilterRect); //Used to make the background light grey with white border
        TabDrawer.DrawTabs(
            new Rect(mountableFilterRect.x, mountableFilterRect.y, mountableFilterRect.width, Text.LineHeight),
            tabs);

        //========Between tabs and scroll body=========
        options.Begin(new Rect(mountableFilterRect.x + 10, mountableFilterRect.y + 10,
            mountableFilterRect.width - 10f, mountableFilterRect.height - 10f));
        if (selectedTab == SelectedTab.BodySize)
        {
            options.Label("GUC_BodySizeFilter_Title".Translate("0", "5", "0.2", bodySizeFilter.ToString()), -1f,
                "GUC_BodySizeFilter_Description".Translate());
            bodySizeFilter = options.Slider((float)Math.Round(bodySizeFilter, 1), 0f, 5f);
        }
        else
        {
            options.Label("GUC_DrawBehavior_Description".Translate());
        }

        options.End();
        //========Scroll area=========
        mountableFilterRect.y += 60f;
        mountableFilterRect.yMax -= 60f;
        var mountableFilterInnerRect = new Rect(0f, 0f, mountableFilterRect.width - 30f,
            (coreLineNumber + 2) * 22f);
        Widgets.BeginScrollView(mountableFilterRect, ref coreScrollPos, mountableFilterInnerRect, true);
        options.Begin(mountableFilterInnerRect);
        options.DrawList(Setup.AllAnimals, selectedTab == SelectedTab.BodySize ? MountableCache : DrawRulesCache, out coreLineNumber);
        options.End();
        Widgets.EndScrollView();
    }

    private void DrawRnR(Rect rect)
    {
        var options = new Listing_Standard();
        options.Begin(rect.ContractedBy(15f));

        options.CheckboxLabeled("GU_Enable_RnR".Translate(), ref rideAndRollEnabled,
            "GU_Enable_RnR_Description".Translate());
        if (rideAndRollEnabled)
        {
            options.Gap();
            options.GapLine(); //=============================
            options.Gap();

            options.Label(
                "GU_RR_MinAutoMountDistance_Title".Translate("0", "500", "120", minAutoMountDistance.ToString()),
                -1f, "GU_RR_MinAutoMountDistance_Description".Translate());
            minAutoMountDistance = (int)options.Slider(minAutoMountDistance, 20f, 500f);

            options.Label("GU_RR_AutoHitchDistance_Title".Translate("0", "200", "50", autoHitchDistance.ToString()),
                -1f, "GU_RR_AutoHitchDistance_Description".Translate());
            autoHitchDistance = (int)options.Slider(autoHitchDistance, 0f, 200f);

            options.Label(
                "GU_RR_InjuredThreshold_Title".Translate("0", "100", "75",
                    Math.Round(injuredThreshold * 100f).ToString()), -1f,
                "GU_RR_InjuredThreshold_Description".Translate());
            injuredThreshold = options.Slider(injuredThreshold, 0f, 1f);

            options.Label(
                "GU_RR_WaitForRiderTimer_Title".Translate("1000", "30000", "10000",
                    Math.Round(waitForRiderTimer / 2500f, 1)), -1f,
                "GU_RR_WaitForRiderTimer_Description".Translate());
            waitForRiderTimer = (int)options.Slider(waitForRiderTimer, 0f, 30000f);

            options.CheckboxLabeled("GU_RR_NoMountedHunting_Title".Translate(), ref noMountedHunting,
                "GU_RR_NoMountedHunting_Description".Translate());
            options.CheckboxLabeled("GU_RR_DisableSlavePawnColumn_Title".Translate(), ref disableSlavePawnColumn,
                "GU_RR_DisableSlavePawnColumn_Description".Translate());
            options.CheckboxLabeled("GU_RR_AutomountDisabledByDefault_Title".Translate(),
                ref automountDisabledByDefault, "GU_RR_AutomountDisabledByDefault_Description".Translate());
            if (Prefs.DevMode)
                options.CheckboxLabeled("Enable dev mode logging", ref logging);
        }

        options.End();
    }

    private void DrawBattleMounts(Rect rect)
    {
        var options = new Listing_Standard();
        options.Begin(rect.ContractedBy(15f));

        options.CheckboxLabeled("GU_Enable_BattleMounts".Translate(), ref battleMountsEnabled,
            "GU_Enable_BattleMounts_Description".Translate());
        if (battleMountsEnabled)
        {
            options.Gap();
            options.GapLine(); //=============================
            options.Gap();

            options.Label("BM_MinHandlingLevel_Title".Translate("0", "20", "3", minHandlingLevel.ToString()), -1f,
                "BM_MinHandlingLevel_Description".Translate());
            minHandlingLevel = (int)options.Slider(minHandlingLevel, 0f, 20f);

            options.Label("BM_EnemyMountChance_Title".Translate("0", "100", "15", enemyMountChance.ToString()), -1f,
                "BM_EnemyMountChance_Description".Translate());
            enemyMountChance = (int)options.Slider(enemyMountChance, 0f, 100f);

            options.Label(
                "BM_EnemyMountChanceTribal_Title".Translate("0", "100", "33", enemyMountChancePreInd.ToString()),
                -1f, "BM_EnemyMountChanceTribal_Description".Translate());
            enemyMountChancePreInd = (int)options.Slider(enemyMountChancePreInd, 0f, 100f);

            options.Label("BM_InBiomeWeight_Title".Translate("0", "100", "20", inBiomeWeight.ToString()), -1f,
                "BM_InBiomeWeight_Description".Translate());
            inBiomeWeight = options.Slider((float)Math.Round(inBiomeWeight), 0f, 100f);

            options.Label("BM_OutBiomeWeight_Title".Translate("0", "100", "10", outBiomeWeight.ToString()), -1f,
                "BM_OutBiomeWeight_Description".Translate());
            outBiomeWeight = (int)options.Slider((float)Math.Round(outBiomeWeight), 0f, 100f);

            options.Label("BM_NonWildWeight_Title".Translate("0", "100", "70", nonWildWeight.ToString()), -1f,
                "BM_NonWildWeight_Description".Translate());
            nonWildWeight = (int)options.Slider((float)Math.Round(nonWildWeight), 0f, 100f);
        }

        options.End();
    }

    private void DrawCaravan(Rect rect)
    {
        var options = new Listing_Standard();
        options.Begin(rect.ContractedBy(15f));

        options.CheckboxLabeled("GU_Enable_Caravans".Translate(), ref caravansEnabled,
            "GU_Enable_Caravans_Description".Translate());
        if (caravansEnabled)
        {
            options.Gap();
            options.GapLine(); //=============================
            options.Gap();

            options.Label(
                "GU_Car_visitorMountChance_Title".Translate("0", "100", "15", visitorMountChance.ToString()), -1f,
                "GU_Car_visitorMountChance_Description".Translate());
            visitorMountChance = (int)options.Slider(visitorMountChance, 0f, 100f);

            options.Label(
                "GU_Car_visitorMountChanceTribal_Title".Translate("0", "100", "33",
                    visitorMountChancePreInd.ToString()), -1f,
                "GU_Car_visitorMountChanceTribal_Description".Translate());
            visitorMountChancePreInd = (int)options.Slider(visitorMountChancePreInd, 0f, 100f);

            options.CheckboxLabeled("GU_Car_GiveCaravanSpeed_Title".Translate(), ref giveCaravanSpeed,
                "GU_Car_GiveCaravanSpeed_Description".Translate());
            options.CheckboxLabeled("GU_Car_RidePackAnimals_Title".Translate(), ref ridePackAnimals,
                "GU_Car_RidePackAnimals_Description".Translate());
        }

        options.End();
    }

    private void DrawMechanoid(Rect rect)
    {
        var options = new Listing_Standard();
        var display = rect.ContractedBy(15f);
        options.Begin(display);

        options.CheckboxLabeled("GU_Enable_Mechanoids".Translate(), ref mechanoidsEnabled, "GU_Enable_Mechanoids_Description".Translate());

        if (!mechanoidsEnabled)
        {
            options.End();
            return;
        }

        options.Gap();
        options.GapLine(); //=============================
        options.Gap();

        options.Label("GU_BME_MountChance_Title".Translate("0", "100", "40", mountChance.ToString()),
            tooltip: "GU_BME_MountChance_Description".Translate());
        mountChance = (int)options.Slider(mountChance, 0f, 100f);

        options.CheckboxLabeled("GUM_DisCarCap".Translate(), ref disregardCarryingCapacity,
            "GUM_DisCarCapText".Translate());

        options.GapLine();
        options.Gap();
        options.Label("GUM_AllowedMechs".Translate());

        var previousControls = options.CurHeight;
        
        options.End();

        var scrollView = display.BottomPartPixels(display.height - previousControls);
        var innerRect = new Rect(0f, 0f, scrollView.width - 30f, (mechLineNumber) * 22f);

        Widgets.BeginScrollView(scrollView, ref mechScrollPos, innerRect);
        options.Begin(innerRect);
        options.DrawList(Setup.AllMechs, MechSelectedCache, out mechLineNumber);
        options.End();
        Widgets.EndScrollView();
    }

    public override string SettingsCategory() => "Giddy-Up";

    public override void WriteSettings()
    {
        try
        {
            Setup.RebuildInversions();
            Setup.ProcessPawnKinds();
            if (giveCaravanSpeed)
                for (var i = 0; i < Setup.AllAnimals.Count; i++)
                    Setup.CalculateCaravanSpeed(Setup.AllAnimals[i], true);

            JobDriver_Mounted.SetAllowedJob(JobDefOf.Hunt, !noMountedHunting);
        }
        catch (Exception ex)
        {
            Log.Error("[Giddy-Up] Error writing Giddy-Up settings. Skipping...\n" + ex);
        }

        base.WriteSettings();
    }
}