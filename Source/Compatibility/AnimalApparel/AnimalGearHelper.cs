using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
// ReSharper disable MemberCanBePrivate.Global

namespace GiddyUpCore.Compatibility.AnimalApparel
{
    public static class AnimalGearHelper
    {
        static AnimalGearHelper()
        {
            Setup();
        }

        public delegate bool _IsAnimal(Pawn pawn);
        public delegate bool _IsAnimalOfColony(Pawn pawn);
        public delegate bool _IsAnimalOfAFaction(Pawn pawn);
        public delegate void _EnsureInitApparelTrackers(Pawn pawn);
        public delegate List<ThingDef> _RequiredThingDefFromTags(ApparelProperties apparelProperties);
        public delegate bool _IsSapientAnimal(Pawn pawn);
        public delegate ThingDef _AnimalSourceFor(Pawn pawn);
        public delegate float _GetCachedSapientAnimalSize(Pawn pawn);
        public delegate bool _CanEquipApparelThingDef(ThingDef thing, Pawn pawn, ref string cantReason);
        public delegate bool _CanEquipApparelProperties(ApparelProperties properties, Pawn pawn, ref string cantReason);
        public delegate BodyDef _GetBodyDefForCoverageInfo(ThingDef thing);
        public delegate string _EquippableByStringFull(ThingDef thing);
        public delegate string _EquippableByString(ThingDef thing);
        public delegate bool _InvisibleForAnimal(ThingDef thing);

        public static _IsAnimal IsAnimal = _ => default;
        public static _IsAnimalOfColony IsAnimalOfColony = _ => default;
        public static _IsAnimalOfAFaction IsAnimalOfAFaction = _ => default;
        public static _EnsureInitApparelTrackers EnsureInitApparelTrackers = _ => { };
        public static _RequiredThingDefFromTags RequiredThingDefFromTags = _ => [];
        public static _IsSapientAnimal IsSapientAnimal = _ => default;
        public static _AnimalSourceFor AnimalSourceFor = _ => default;
        public static _GetCachedSapientAnimalSize GetCachedSapientAnimalSize = _ => default;
        public static _CanEquipApparelThingDef CanEquipApparelFromThingDef = DefaultCanEquipApparelFromThingDef;
        public static _CanEquipApparelProperties CanEquipApparelFromProperties = DefaultCanEquipApparelFromProperties;
        public static _GetBodyDefForCoverageInfo GetBodyDefForCoverageInfo = _ => default;
        public static _EquippableByStringFull EquippableByStringFull = _ => default;
        public static _EquippableByString EquippableByString = _ => default;
        public static _InvisibleForAnimal InvisibleForAnimal = _ => default;

        private static bool initialized;

        public static void Setup()
        {
            if (initialized)
                return;

            initialized = true;
            if (!CompatibilityLoader.AnimalApparelInstalled)
                return;

            var helperType = AccessTools.TypeByName("AnimalGear.AnimalGearHelper");
            if (helperType == null)
                return;

            IsAnimal = GetDelegate(helperType, "IsAnimal", IsAnimal, typeof(Pawn));
            IsAnimalOfColony = GetDelegate(helperType, "IsAnimalOfColony", IsAnimalOfColony, typeof(Pawn));
            IsAnimalOfAFaction = GetDelegate(helperType, "IsAnimalOfAFaction", IsAnimalOfAFaction, typeof(Pawn));
            EnsureInitApparelTrackers = GetDelegate(helperType, "EnsureInitApparelTrackers", EnsureInitApparelTrackers, typeof(Pawn));
            RequiredThingDefFromTags = GetDelegate(helperType, "RequiredThingDefFromTags", RequiredThingDefFromTags, typeof(ApparelProperties));
            IsSapientAnimal = GetDelegate(helperType, "IsSapientAnimal", IsSapientAnimal, typeof(Pawn));
            AnimalSourceFor = GetDelegate(helperType, "AnimalSourceFor", AnimalSourceFor, typeof(Pawn));
            GetCachedSapientAnimalSize = GetDelegate(helperType, "GetCachedSapientAnimalSize", GetCachedSapientAnimalSize, typeof(Pawn));
            CanEquipApparelFromThingDef = GetDelegate(helperType, "CanEquipApparel", CanEquipApparelFromThingDef, typeof(ThingDef), typeof(Pawn), typeof(string).MakeByRefType());
            CanEquipApparelFromProperties = GetDelegate(helperType, "CanEquipApparel", CanEquipApparelFromProperties, typeof(ApparelProperties), typeof(Pawn), typeof(string).MakeByRefType());
            GetBodyDefForCoverageInfo = GetDelegate(helperType, "GetBodyDefForCoverageInfo", GetBodyDefForCoverageInfo, typeof(ThingDef));
            EquippableByStringFull = GetDelegate(helperType, "EquippableByStringFull", EquippableByStringFull, typeof(ThingDef));
            EquippableByString = GetDelegate(helperType, "EquippableByString", EquippableByString, typeof(ThingDef));
            InvisibleForAnimal = GetDelegate(helperType, "InvisibleForAnimal", InvisibleForAnimal, typeof(ThingDef));
        }

        private static TDelegate GetDelegate<TDelegate>(Type helperType, string methodName, TDelegate defaultValue,
            params Type[] argumentTypes) where TDelegate : Delegate
        {
            var method = AccessTools.Method(helperType, methodName, argumentTypes);
            return method == null ? defaultValue : AccessTools.MethodDelegate<TDelegate>(method);
        }

        private static bool DefaultCanEquipApparelFromThingDef(ThingDef thing, Pawn pawn, ref string cantReason) => default;

        private static bool DefaultCanEquipApparelFromProperties(ApparelProperties properties, Pawn pawn,
            ref string cantReason) => default;
    }
}