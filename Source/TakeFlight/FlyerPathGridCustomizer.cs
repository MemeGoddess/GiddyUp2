using LudeonTK;
using Unity.Collections;
using Verse;

namespace GiddyUpTakeFlight;

/// <summary>
/// Tags path requests from riders on flying mounts. Adds no costs itself; it keeps their grid jobs separate from
/// normal pawns so the flying cost grid can be swapped in.
/// </summary>
public sealed class FlyerPathGridCustomizer : PathRequest.IPathGridCustomizer
{
    public static readonly FlyerPathGridCustomizer Instance = new();

    private static NativeArray<ushort> emptyGrid;

    private FlyerPathGridCustomizer()
    {
    }

    public NativeArray<ushort> GetOffsetGrid()
    {
        if (!emptyGrid.IsCreated)
            emptyGrid = NativeArrayUtility.EmptyArray<ushort>();
        return emptyGrid;
    }
}
