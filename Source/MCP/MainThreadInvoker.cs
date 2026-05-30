using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GiddyUpCore.MCP
{
    [HarmonyPatch(typeof(Root), nameof(Root.Update))]
    internal static class MainThreadInvoker
    {
        private static ConcurrentQueue<Task> WorkQueue = new();

        internal static async Task<bool> InvokeOnMainThread(Task task, CancellationToken? token = null)
        {
            WorkQueue.Enqueue(task);

            while (token is not { IsCancellationRequested: true } && !task.IsCompleted)
            {
                await Task.Delay(200);
            }

            if (token != null)
                task.Wait(token.Value);
            else
                return task.Wait(5000);
            task.Dispose();

            return task.IsCompleted;
        }

        [HarmonyPatch(typeof(Root), nameof(Root.Update))]
        internal static class Root_Update_Patch
        {
            public static void Postfix()
            {
                while (WorkQueue.TryDequeue(out var task))
                {
                    Log.Warning("Doing tasks");
                    task.RunSynchronously();
                }
            }
        }
    }
}
