using GiddyUp.Jobs;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using GiddyUpCore.RideAndRoll;
using Verse;
using UnityEngine;
using static GiddyUp.ModSettings_GiddyUp;
using Color = UnityEngine.Color;

namespace GiddyUp;

public class Mod_GiddyUp : Mod
{
#if DEBUG
    public static Mod_GiddyUp Instance;
#endif
    private static int coreLineNumber, mechLineNumber;
    private readonly QuickSearchWidget search = new QuickSearchWidget();

    private string? drawBehavior;
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
        var tabs = new List<TabRecord>
        {
            new("GUC_Core_Tab".Translate(), delegate { selectedTab = SelectedTab.Core; search.Reset(); },
                selectedTab is SelectedTab.Core or SelectedTab.BodySize or SelectedTab.DrawBehavior),
            new("GUC_RnR_Tab".Translate(), delegate { selectedTab = SelectedTab.Rnr; },
                selectedTab == SelectedTab.Rnr),
            new("GUC_BattleMounts_Tab".Translate(), delegate { selectedTab = SelectedTab.BattleMounts; },
                selectedTab == SelectedTab.BattleMounts),
            new("GUC_Caravans_Tab".Translate(), delegate { selectedTab = SelectedTab.Caravans; },
                selectedTab == SelectedTab.Caravans),
            new("GU_Mechanoids_Tab".Translate(), delegate { selectedTab = SelectedTab.Mechanoids; search.Reset(); },
                selectedTab == SelectedTab.Mechanoids),
            //new("GUC_Ideology_Tab".Translate(), delegate { selectedTab = SelectedTab.Ideo;},
            //        selectedTab == SelectedTab.Ideo)
        };

        var currentTabRect = new Rect(0f, Text.LineHeight + 6, inRect.width, inRect.height - Text.LineHeight - 6);
        Widgets.DrawMenuSection(currentTabRect);

        new Rect(0f, 0, inRect.width, Text.LineHeight).SplitVerticallyWithMargin(out var tabsHeader,
            out var refreshCache, out _, 4f, rightWidth: Text.LineHeight);
        if (Widgets.ButtonImageWithBG(refreshCache, TexUI.RotRightTex, new Vector2(16, 16)))
            offsetCache = null;
        TooltipHandler.TipRegion(refreshCache, () => "GU_Reset_Cache".Translate(), 427985);
        DrawTabs(tabsHeader, tabs);
        
        
        switch (selectedTab)
        {
            case SelectedTab.Core:
            case SelectedTab.BodySize:
            case SelectedTab.DrawBehavior:
                DrawCore(currentTabRect);
                break;
            case SelectedTab.Rnr:
                DrawRnR(currentTabRect);
                break;
            case SelectedTab.BattleMounts:
                DrawBattleMounts(currentTabRect);
                break;
            case SelectedTab.Caravans:
                DrawCaravan(currentTabRect);
                break;
            case SelectedTab.Mechanoids:
                DrawMechanoid(currentTabRect);
                break;
            case SelectedTab.Ideo:
                DrawIdeo(currentTabRect);
                break;
        }

        GUI.EndGroup();
    }


    private void DrawCore(Rect inRect)
    {
        if (selectedTab == SelectedTab.Core)
            selectedTab = SelectedTab.BodySize;

        var options = new Listing_Standard();
        var view = inRect.ContractedBy(15f);
        options.Begin(view);

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


        var disregardsRow = options.GetRect(Text.LineHeight);
        disregardsRow.SplitVerticallyWithMargin(out var disregardsCapacity, out var disregardsAge, 4f);

        // Disregard Capacity
        if (Mouse.IsOver(disregardsCapacity))
            Widgets.DrawHighlight(disregardsCapacity);
        TooltipHandler.TipRegion(disregardsCapacity, () => "GUM_DisCarCapText".Translate(), 8542);
        Widgets.CheckboxLabeled(disregardsCapacity, "GUM_DisCarCap".Translate(), ref disregardAnimalCarryingCapacity);

        //Disregard Age
        if (Mouse.IsOver(disregardsAge))
            Widgets.DrawHighlight(disregardsAge);
        Widgets.CheckboxLabeled(disregardsAge, "GUC_DisAgeCap".Translate(), ref disregardAnimalAge);

        //========Setup tabs=========
        var tabs = new List<TabRecord>
        {
            new("GUC_Mountable_Tab".Translate(), delegate { selectedTab = SelectedTab.BodySize; },
                selectedTab == SelectedTab.BodySize),
            new("GUC_DrawBehavior_Tab".Translate(),
                delegate { selectedTab = SelectedTab.DrawBehavior; }, selectedTab == SelectedTab.DrawBehavior)
        };
        var tabsRow = options.GetRect(Text.LineHeight);

        DrawTabs(tabsRow, tabs);

        options.Gap(6f);
        var mountableFilterRect = options.GetRect(view.height - options.CurHeight);

        //========Between tabs and scroll body=========
        Widgets.DrawMenuSection(mountableFilterRect);
        var tabView = mountableFilterRect.ContractedBy(1f);
        var mountOptions = new Listing_Standard();
        
        mountOptions.Begin(tabView);
        mountOptions.Gap();
        var filtersRect = mountOptions.GetRect(Text.LineHeight);
        filtersRect = filtersRect.LeftPartPixels(filtersRect.width - 20f);
        filtersRect = filtersRect.ContractedBy(4f, 0f);
        drawBehavior ??= "GUC_DrawBehavior_Description".Translate();
        filtersRect.SplitVerticallyWithMargin(out var searchRect, out var filterRect, out var _, compressibleMargin: 4f,
            rightWidth: selectedTab == SelectedTab.DrawBehavior ? Text.CalcSize(drawBehavior).x : filtersRect.width / 2);
        filterRect.width += 20f;
        if (selectedTab == SelectedTab.BodySize)
        {
            search.OnGUI(searchRect);
            Widgets.HorizontalSlider(filterRect, ref bodySizeFilter, new FloatRange(0f, 5f),
                "GUC_BodySizeFilter_Title".Translate(bodySizeFilter.ToString()), 0.1f);
        }
        else
        {
            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(filterRect, drawBehavior);
            Text.Anchor = anchor;
            search.OnGUI(searchRect);
        }
        //========Search widget=========
        
        var animalsForViewing = Setup.AllAnimals.Where(animal =>
            search.filter.Matches(animal?.label)
            || search.filter.Matches(animal?.defName)
            || search.filter.Matches(animal?.modContentPack?.Name)).ToList();
        mountOptions.Gap(4f);
        var previousHeight = mountOptions.CurHeight;
        mountOptions.End();

        //========Scroll area=========
        var scrollOptions = new Listing_Standard();
        var scrollView = tabView.BottomPartPixels(tabView.height - previousHeight);
        var mountableFilterInnerRect = scrollView with
        {
            width = scrollView.width - 20f,
            height = coreLineNumber * OptionsDrawUtility.LineHeight
        };
        var scrollY = coreScrollPos.y;
        var min = scrollY == 0 ? 0 : (int)Mathf.Floor(scrollY / OptionsDrawUtility.LineHeight);
        var max = (int)Mathf.Ceil(scrollView.height / OptionsDrawUtility.LineHeight) + min;
        var viewRange = new IntRange(min - 1, max + 1);

        Widgets.BeginScrollView(scrollView, ref coreScrollPos, mountableFilterInnerRect);
        scrollOptions.Begin(mountableFilterInnerRect);
        scrollOptions.DrawList(animalsForViewing, selectedTab == SelectedTab.BodySize ? MountableCache : DrawRulesCache, viewRange, out coreLineNumber);
        scrollOptions.End();
        Widgets.EndScrollView();
        options.End();

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

            options.CheckboxLabeled("GU_Enable_SaddleUp".Translate(), ref saddleUpEnabled, "GU_Enable_SaddleUp_Description".Translate());

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

        options.CheckboxLabeled("GUM_DisCarCap".Translate(), ref disregardMechCarryingCapacity,
            "GUM_DisCarCapText".Translate());

        options.GapLine();
        options.Gap();
        options.Label("GUM_AllowedMechs".Translate());
        //========Search widget=========
        var searchRect = options.GetRect(Text.LineHeight);
        searchRect.width -= 20f; // Scroll bar alignment
        search.OnGUI(searchRect);
        var mechsForViewing = Setup.AllMechs.Where(mech =>
            search.filter.Matches(mech?.label)
            || search.filter.Matches(mech?.defName)
            || search.filter.Matches(mech?.modContentPack?.Name)).ToList();
        var previousControls = options.CurHeight;
        options.End();
        
        var scrollView = display.BottomPartPixels(display.height - previousControls);
        var innerRect = scrollView with
        {
            width = scrollView.width - 20f,
            height = mechLineNumber * OptionsDrawUtility.LineHeight
        };

        var min = mechScrollPos.y == 0 ? 0 : (int)Mathf.Floor(mechScrollPos.y / OptionsDrawUtility.LineHeight) - 1;
        var max = (int)Mathf.Ceil(scrollView.height / OptionsDrawUtility.LineHeight) + min + 2;
        var viewRange = new IntRange(min - 1, max + 1);

        Widgets.BeginScrollView(scrollView, ref mechScrollPos, innerRect);
        options.Begin(innerRect);
        options.DrawList(mechsForViewing, MechSelectedCache, viewRange, out mechLineNumber);
        options.End();
        Widgets.EndScrollView();
    }

    private void DrawIdeo(Rect rect)
    {
        var options = new Listing_Standard();
        var display = rect.ContractedBy(15f);
        options.Begin(display);

        options.CheckboxLabeled("GU_Enable_Ideology".Translate(),  ref ideoEnabled, "GU_Enable_Ideology_Description".Translate());

        options.End();
    }

    private static Color SelectedColor = new Color(0.5f, 1f, 0.5f, 1f);
    private void DrawTabs(Rect rect, List<TabRecord> tabs)
    {
        var buttons = tabs.Count;
        var rects = SplitRectangle(rect, buttons, 4f);

        var color = GUI.color;
        for (var index = 0; index < rects.Length; index++)
        {
            var button = rects[index];
            var tab = tabs[index];

            if (tab.Selected)
                GUI.color = SelectedColor;
            if (Widgets.ButtonText(button, tab.label))
                tab.clickedAction();
            GUI.color = color;
        }
    }

    private Rect[] SplitRectangle(Rect rect, int count, float margin)
    {
        var rects = new Rect[count];
        var totalMargin = margin * (count - 1);
        var usableWidth = rect.width - totalMargin;
        var rectWidth = usableWidth / count;

        for (var i = 0; i < count; i++)
        {
            var xPosition = rect.x + (i * (rectWidth + margin));
            rects[i] = new Rect(xPosition, rect.y, rectWidth, rect.height);
        }

        return rects;
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
        search.Reset();
        base.WriteSettings();
    }
}