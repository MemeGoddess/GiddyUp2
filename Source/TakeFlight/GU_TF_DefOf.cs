using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GiddyUpTakeFlight;

[DefOf]
public static class GU_TF_DefOf
{
    public static ThingDef GU_TF_MountedPawnFlyer = null!;
    public static HediffDef GU_TF_FlightFatigue = null!;

    public static ThingDef GU_TF_MountedFlyerTransporter = null!;
    public static ThingDef GU_TF_MountedFlyerActive = null!;
    public static ThingDef GU_TF_MountedFlyerLeaving = null!;
    public static ThingDef GU_TF_MountedFlyerIncoming = null!;
    public static WorldObjectDef GU_TF_TravellingMountedFlyers = null!;

    static GU_TF_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(GU_TF_DefOf));
    }
}
