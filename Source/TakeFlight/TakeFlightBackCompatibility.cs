using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace GiddyUpTakeFlight;

/// <summary>
/// Maps def names from the standalone "Giddy-Up 2: Take Flight" addon to the integrated module's names.
/// </summary>
public class TakeFlightBackCompatibility : BackCompatibilityConverter
{
    private static readonly Dictionary<string, string> RenamedDefs = new()
    {
        { "Phon_MountedPawnFlyer", "GU_TF_MountedPawnFlyer" },
        { "Phon_FlightFatigue", "GU_TF_FlightFatigue" },
        { "Phon_MountedFlyerTransporter", "GU_TF_MountedFlyerTransporter" },
        { "Phon_MountedFlyerActive", "GU_TF_MountedFlyerActive" },
        { "Phon_MountedFlyerLeaving", "GU_TF_MountedFlyerLeaving" },
        { "Phon_MountedFlyerIncoming", "GU_TF_MountedFlyerIncoming" },
        { "Phon_TravellingMountedFlyers", "GU_TF_TravellingMountedFlyers" }
    };

    /// <summary>
    /// Adds the converter to the game's back compatibility chain.
    /// </summary>
    public static void Register()
    {
        if (!BackCompatibility.conversionChain.Any(c => c is TakeFlightBackCompatibility))
            BackCompatibility.conversionChain.Add(new TakeFlightBackCompatibility());
    }

    public override bool AppliesToVersion(int majorVer, int minorVer) => true;

    public override string? BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false,
        XmlNode? node = null) =>
        defName != null && RenamedDefs.TryGetValue(defName, out var newName) ? newName : null;

    public override Type? GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node) => null;

    public override void PostExposeData(object obj)
    {
    }
}
