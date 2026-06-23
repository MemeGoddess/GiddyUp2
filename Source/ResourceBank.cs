using RimWorld;
using Verse;
using UnityEngine;
using System;

namespace GiddyUp;

[StaticConstructorOnStartup]
public static class ResourceBank
{
    public static readonly Type RaidStrategyWorker_Siege = typeof(RaidStrategyWorker_Siege);

    public static Texture2D iconDropAnimalClear = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_DropAnimal_Clear");
    public static Texture2D iconDropAnimalExpand = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_DropAnimal_Expand");
    public static Texture2D iconNoMountClear = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_NoMount_Clear");
    public static Texture2D iconNoMountExpand = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_NoMount_Expand");
    public static Texture2D iconGizmoSaddleUp = ContentFinder<Texture2D>.Get("UI/QM_horseshoe_icon");

    public static Texture2D SliderRailAtlas = ContentFinder<Texture2D>.Get("UI/Buttons/SliderRail");
    public static Texture2D SliderHandle = ContentFinder<Texture2D>.Get("UI/Buttons/SliderHandle");

    public const string VisitorAreaDropMount = "GU_Car_Area_GU_DropAnimal_NPC";
    public const string AreaDropMount = "Gu_Area_DropMount";
    public const string AreaNoMount = "Gu_Area_NoMount";

    public const float DefaultSizeThreshold = 1.2f;
    public const float CombatPowerFactor = 2.0f;
    public const float AutoHitchDistance = 8.0f;
    public const float GuestSpotCheckRange = 120f;
    public const int MapEdgeIgnore = 8;

    /// <summary>
    /// Animal under this value are considered rideable by default
    /// </summary>
    public const float WildnessThreshold = 0.6f;
    
    [DefOf]
    public class ConceptDefOf
    {
        public static ConceptDef GUC_Animal_Handling = null!;
        public static ConceptDef BM_Mounting = null!;
        public static ConceptDef BM_Enemy_Mounting = null!;
    }

    [DefOf]
    public static class JobDefOf
    {
        public static JobDef Mount = null!;
        public static JobDef Mounted = null!;
        public static JobDef Dismount = null!;
        public static JobDef WaitForRider = null!;
    }

    [DefOf]
    public static class RoomRoleDefOf
    {
        public static RoomRoleDef Barn = null!;
    }
}