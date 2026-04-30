using GiddyUp;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using GiddyUpCore.SaddleUp;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SaddleUp
{
    [HarmonyPatchCategory(nameof(PatchCategoryModule.SaddleUp))]
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public class Pawn_GetGizmos_SU2
    {
        private static void Postfix(ref IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (!ModSettings_GiddyUp.saddleUpEnabled)
                return;

            if (__instance is { HomeFaction: { IsPlayer: true }, Drafted: true, IsColonist: true })
                __result = __result.AddItem(CreateGizmo_ToggleMount(__instance));
        }

        private static Gizmo CreateGizmo_ToggleMount(Pawn __instance)
        {
            var extendedPawnData = ExtendedDataStorage.Singleton.GetExtendedPawnData(__instance);
            return extendedPawnData is { Mount: not null } ? CreateGizmo_Dismount(__instance) : CreateGizmo_GoAndMount(__instance);
        }

        private static Gizmo CreateGizmo_GoAndMount(Pawn __instance)
        {
            var str = "";
            var flag = false;
            if (__instance.Downed)
            {
                flag = true;
                str = "QM_Mount_Downed".Translate();
            }
            var gizmoGoAndMount = new Command_Action
            {
                defaultLabel = "GUC_Mount".Translate(),
                defaultDesc = "QM_Mount_Description".Translate(),
                hotKey = KeyBindingDefOf.Misc5,
                Disabled = flag,
                Order = 11.5f,
                icon = ContentFinder<Texture2D>.Get("UI/QM_horseshoe_icon"),
                disabledReason = str,
                action = () => __instance.Map.GetComponent<Coordinator>().MountPawn(__instance),
            };
            return gizmoGoAndMount;
        }

        private static Gizmo CreateGizmo_Dismount(Pawn __instance)
        {
            var gizmoDismount = new Command_Action
            {
                defaultLabel = "GUC_Dismount".Translate(),
                defaultDesc = "QM_Dismount_Description".Translate(),
                hotKey = KeyBindingDefOf.Misc6,
                Order = 11.5f,
                icon = ContentFinder<Texture2D>.Get("UI/QM_horseshoe_x_icon"),
                action = () => DismountPawn(__instance)
            };
            return gizmoDismount;
        }

        public static void DismountPawn(Pawn pawn)
        {
            var extendedPawnData = ExtendedDataStorage.Singleton.GetExtendedPawnData(pawn);
            var jobs = pawn.jobs;
            var job = new Job(ResourceBank.JobDefOf.Dismount, extendedPawnData.Mount);
            job.count = 1;
            var tag = new JobTag?(JobTag.Misc);
            jobs.TryTakeOrderedJob(job, tag);
        }
    }
}
