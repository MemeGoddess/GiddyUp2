#nullable disable
using LudeonTK;
using UnityEngine;
using Verse;

namespace GiddyUp.Editor
{
    public static class DebugTools
    {
        private static Pawn Selected
        {
            get
            {
                var cell = UI.MouseCell();
                var map = Find.CurrentMap;
                if (map == null || !cell.InBounds(map))
                {
                    return null;
                }
                foreach (var t in cell.GetThingList(map))
                {
                    var p = t as Pawn;
                    if (p != null && p.RaceProps.Animal)
                    {
                        return p;
                    }
                }
                return null;
            }
        }

        [DebugAction("Giddy-Up Editor", "Open mount editor", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OpenEditor()
        {
            var p = Selected;
            if (p != null && p.RaceProps.Animal)
            {
                Find.WindowStack.Add(new Dialog_MountEditor(p));
            }
        }

        [DebugAction("Giddy-Up Editor", "Open mask editor", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OpenMask()
        {
            var p = Selected;
            if (p != null && p.RaceProps.Animal)
            {
                Find.WindowStack.Add(new Dialog_MaskEditor(p, GiddyUpEditorMod.Settings.GetOrCreate(p.def.defName), 0));
            }
        }
    }
}
