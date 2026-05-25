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

        public delegate void _EnsureInitApparelTrackers(Pawn pawn);
        public delegate bool _CanEquipApparelThingDef(ThingDef thing, Pawn pawn, ref string cantReason);

        public static _EnsureInitApparelTrackers EnsureInitApparelTrackers = _ => { };
        public static _CanEquipApparelThingDef CanEquipApparelFromThingDef = DefaultCanEquipApparelFromThingDef;

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

            EnsureInitApparelTrackers = GetDelegate(helperType, "EnsureInitApparelTrackers", EnsureInitApparelTrackers, typeof(Pawn));
            CanEquipApparelFromThingDef = GetDelegate(helperType, "CanEquipApparel", CanEquipApparelFromThingDef, typeof(ThingDef), typeof(Pawn), typeof(string).MakeByRefType());
        }

        private static TDelegate GetDelegate<TDelegate>(Type helperType, string methodName, TDelegate defaultValue,
            params Type[] argumentTypes) where TDelegate : Delegate
        {
            var method = AccessTools.Method(helperType, methodName, argumentTypes);
            return method == null ? defaultValue : AccessTools.MethodDelegate<TDelegate>(method);
        }

        private static bool DefaultCanEquipApparelFromThingDef(ThingDef thing, Pawn pawn, ref string cantReason) => default;
    }
}