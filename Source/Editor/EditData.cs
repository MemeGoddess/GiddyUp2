#nullable disable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace GiddyUp.Editor
{
    public class OverlayEdit : IExposable
    {
        public bool disabled;
        public bool hasOffsets;
        public Vector3 offsetDefault;
        public Vector3 offsetMale;
        public Vector3 offsetFemale;
        public float scale = 1f;

        public bool IsEmpty
        {
            get { return !disabled && !hasOffsets && Mathf.Approximately(scale, 1f); }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref disabled, "disabled", false);
            Scribe_Values.Look(ref hasOffsets, "hasOffsets", false);
            Scribe_Values.Look(ref offsetDefault, "offsetDefault", Vector3.zero);
            Scribe_Values.Look(ref offsetMale, "offsetMale", Vector3.zero);
            Scribe_Values.Look(ref offsetFemale, "offsetFemale", Vector3.zero);
            Scribe_Values.Look(ref scale, "scale", 1f);
        }
    }

    public class MaskPolygon : IExposable
    {
        public List<Vector2> points = new List<Vector2>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref points, "points", LookMode.Value);
            if (points == null)
            {
                points = new List<Vector2>();
            }
            points.RemoveAll(v => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y));
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = new Vector2(Mathf.Clamp01(points[i].x), Mathf.Clamp01(points[i].y));
            }
        }
    }

    public class MaskEdit : IExposable
    {
        public string variant;
        public int slot;
        public int version;
        public List<MaskPolygon> polygons = new List<MaskPolygon>();

        public string FileName
        {
            get { return variant + "~mask" + (slot == 0 ? "" : slot.ToString()) + "~v" + version + ".png"; }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref variant, "variant");
            Scribe_Values.Look(ref slot, "slot", 0);
            Scribe_Values.Look(ref version, "version", 0);
            Scribe_Collections.Look(ref polygons, "polygons", LookMode.Deep);
            if (polygons == null)
            {
                polygons = new List<MaskPolygon>();
            }
            polygons.RemoveAll(p => p == null);
            if (slot < 0 || slot > 2)
            {
                slot = 0;
            }
        }
    }

    public class AnimalEdit : IExposable
    {
        public string defName;
        public bool hasRider;
        public Vector3 north;
        public Vector3 south;
        public Vector3 east;
        public Vector3 west;
        public int mountable = -1;
        public int drawInFront = -1;
        public OverlayEdit front = new OverlayEdit();
        public OverlayEdit side = new OverlayEdit();
        public OverlayEdit back = new OverlayEdit();
        public List<MaskEdit> masks = new List<MaskEdit>();
        public float bodyScale = 1f;

        public OverlayEdit Slot(int slot)
        {
            return slot == 0 ? front : slot == 1 ? side : back;
        }

        public MaskEdit Mask(string variant, int slot)
        {
            for (int i = 0; i < masks.Count; i++)
            {
                if (masks[i] != null && masks[i].variant == variant && masks[i].slot == slot)
                {
                    return masks[i];
                }
            }
            return null;
        }

        public bool HasMasks(int slot)
        {
            for (int i = 0; i < masks.Count; i++)
            {
                if (masks[i] != null && masks[i].slot == slot)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsEmpty
        {
            get { return !hasRider && mountable < 0 && drawInFront < 0 && front.IsEmpty && side.IsEmpty && back.IsEmpty && masks.Count == 0 && Mathf.Approximately(bodyScale, 1f); }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref hasRider, "hasRider", false);
            Scribe_Values.Look(ref north, "north", Vector3.zero);
            Scribe_Values.Look(ref south, "south", Vector3.zero);
            Scribe_Values.Look(ref east, "east", Vector3.zero);
            Scribe_Values.Look(ref west, "west", Vector3.zero);
            Scribe_Values.Look(ref mountable, "mountable", -1);
            Scribe_Values.Look(ref drawInFront, "drawInFront", -1);
            Scribe_Deep.Look(ref front, "front");
            Scribe_Deep.Look(ref side, "side");
            Scribe_Deep.Look(ref back, "back");
            Scribe_Collections.Look(ref masks, "masks", LookMode.Deep);
            Scribe_Values.Look(ref bodyScale, "bodyScale", 1f);
            if (front == null)
            {
                front = new OverlayEdit();
            }
            if (side == null)
            {
                side = new OverlayEdit();
            }
            if (back == null)
            {
                back = new OverlayEdit();
            }
            if (masks == null)
            {
                masks = new List<MaskEdit>();
            }
            masks.RemoveAll(m => m == null || string.IsNullOrEmpty(m.variant));
            if (float.IsNaN(bodyScale) || bodyScale < 0.1f || bodyScale > 5f)
            {
                bodyScale = 1f;
            }
        }
    }

    public class EditorSettings : ModSettings
    {
        public List<AnimalEdit> animals = new List<AnimalEdit>();

        public AnimalEdit Get(string defName)
        {
            for (int i = 0; i < animals.Count; i++)
            {
                if (animals[i] != null && animals[i].defName == defName)
                {
                    return animals[i];
                }
            }
            return null;
        }

        public AnimalEdit GetOrCreate(string defName)
        {
            var e = Get(defName);
            if (e == null)
            {
                e = new AnimalEdit { defName = defName };
                animals.Add(e);
            }
            return e;
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var keep = animals.Where(a => a != null && !a.IsEmpty && !string.IsNullOrEmpty(a.defName)).ToList();
                Scribe_Collections.Look(ref keep, "animals", LookMode.Deep);
                return;
            }
            Scribe_Collections.Look(ref animals, "animals", LookMode.Deep);
            if (animals == null)
            {
                animals = new List<AnimalEdit>();
            }
            var seen = new HashSet<string>();
            animals.RemoveAll(a => a == null || string.IsNullOrEmpty(a.defName) || !seen.Add(a.defName));
        }
    }
}
