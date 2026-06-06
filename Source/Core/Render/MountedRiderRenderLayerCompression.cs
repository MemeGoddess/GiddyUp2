using System;
using UnityEngine;
using Verse;

namespace GiddyUpCore.Core;

internal static class MountedRiderRenderLayerCompression
{
    private const float OverlayLayer = 75f;
    private const float LayerMargin = 0.5f;
    private const float MinSourceLayer = -10f;
    private const float MaxSourceLayer = 90f;
    private const float DefaultCompressedBand = 12f;

    [ThreadStatic] private static Context? current;

    public static IDisposable Push(Pawn rider, float mountedLayer)
    {
        var previous = current;
        current = new Context(rider, mountedLayer);
        return new Scope(previous);
    }

    public static bool IsActiveFor(Pawn pawn)
    {
        return current?.Rider == pawn;
    }

    public static bool TryGetCompressedAltitude(Pawn pawn, float sourceLayer, out float altitude)
    {
        var context = current;
        if (context == null || context.Rider != pawn)
        {
            altitude = default;
            return false;
        }

        var availableBand = Mathf.Max(0f, OverlayLayer - context.MountedLayer - LayerMargin);
        if (availableBand <= 0f)
        {
            altitude = PawnRenderUtility.AltitudeForLayer(context.MountedLayer);
            return true;
        }

        var band = Mathf.Min(DefaultCompressedBand, availableBand);
        var clampedLayer = Mathf.Clamp(sourceLayer, MinSourceLayer, MaxSourceLayer);
        var normalizedLayer = Mathf.InverseLerp(MinSourceLayer, MaxSourceLayer, clampedLayer);
        var compressedLayer = context.MountedLayer + normalizedLayer * band;
        altitude = PawnRenderUtility.AltitudeForLayer(compressedLayer);
        return true;
    }

    private sealed class Context
    {
        public readonly Pawn Rider;
        public readonly float MountedLayer;

        public Context(Pawn rider, float mountedLayer)
        {
            Rider = rider;
            MountedLayer = mountedLayer;
        }
    }

    private readonly struct Scope : IDisposable
    {
        private readonly Context? previous;

        public Scope(Context? previous)
        {
            this.previous = previous;
        }

        public void Dispose()
        {
            current = previous;
        }
    }
}