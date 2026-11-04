#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUp.Editor
{
    public class GiddyUpEditorMod : Mod
    {
        public static GiddyUpEditorMod Instance;
        public static EditorSettings Settings;

        public GiddyUpEditorMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<EditorSettings>();
            var harmony = new HarmonyLib.Harmony("GiddyUp.Editor");
            Live.Patch(harmony);
            Preview.Patch(harmony);
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)), postfix: new HarmonyMethod(typeof(GiddyUpEditorMod), nameof(GetGizmos_Postfix)));
        }

        public override string SettingsCategory()
        {
            return "GUE_SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);
            list.Label("GUE_SettingsInfo".Translate(Settings.animals.Count(a => !a.IsEmpty)));
            if (list.ButtonText("GUE_Overview".Translate()))
            {
                Find.WindowStack.Add(new Dialog_EditOverview());
            }
            list.End();
        }

        public static void Save()
        {
            string file = (string)AccessTools.Method(typeof(LoadedModManager), "GetSettingsFilename").Invoke(null, new object[] { Instance.Content.FolderName, Instance.GetType().Name });
            string backup = file + ".bak";
            bool hadFile = File.Exists(file);
            if (hadFile)
            {
                File.Copy(file, backup, true);
            }
            Instance.WriteSettings();
            try
            {
                if (!File.Exists(file) || new FileInfo(file).Length == 0)
                {
                    throw new IOException(file);
                }
                System.Xml.Linq.XDocument.Load(file);
            }
            catch (Exception)
            {
                if (hadFile)
                {
                    File.Copy(backup, file, true);
                }
                throw;
            }
        }

        public static bool TrySave()
        {
            try
            {
                Save();
                return true;
            }
            catch (Exception ex)
            {
                Messages.Message("GUE_SaveFailed".Translate(ex.Message), MessageTypeDefOf.RejectInput, false);
                return false;
            }
        }

        public static void GetGizmos_Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (!Prefs.DevMode || __instance.RaceProps == null || !__instance.RaceProps.Animal || !__instance.Spawned)
            {
                return;
            }
            var pawn = __instance;
            __result = __result.AddItem(new Command_Action
            {
                defaultLabel = "GUE_Gizmo".Translate(),
                defaultDesc = "GUE_GizmoDesc".Translate(pawn.LabelCap),
                icon = ContentFinder<Texture2D>.Get("UI/QM_horseshoe_icon", false) ?? BaseContent.BadTex,
                action = () => Find.WindowStack.Add(new Dialog_MountEditor(pawn))
            });
        }
    }

    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            LongEventHandler.ExecuteWhenFinished(() => Live.ApplyAll(GiddyUpEditorMod.Settings));
        }
    }
}
