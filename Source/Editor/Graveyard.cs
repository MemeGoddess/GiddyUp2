#nullable disable
using System.Collections.Generic;
using UnityEngine;

namespace GiddyUp.Editor
{
    public static class Graveyard
    {
        private const int Frames = 30;
        private const float Seconds = 1f;

        private struct Grave
        {
            public Object obj;
            public int frame;
            public float time;
        }

        private static readonly List<Grave> queue = new List<Grave>();

        public static void Bury(Object obj)
        {
            if (obj != null)
            {
                queue.Add(new Grave { obj = obj, frame = Time.frameCount, time = Time.realtimeSinceStartup });
            }
        }

        public static void Tick()
        {
            if (queue.Count == 0 || Event.current == null || Event.current.type != EventType.Layout)
            {
                return;
            }
            int frame = Time.frameCount;
            float now = Time.realtimeSinceStartup;
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                var g = queue[i];
                if (frame - g.frame < Frames || now - g.time < Seconds)
                {
                    continue;
                }
                queue.RemoveAt(i);
                if (g.obj != null)
                {
                    Object.Destroy(g.obj);
                }
            }
        }
    }
}
