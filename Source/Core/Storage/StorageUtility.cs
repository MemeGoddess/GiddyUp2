using Verse;

namespace GiddyUp;

public static class StorageUtility
{
    //Needs to be its own class as an extension
    public static ExtendedPawnData GetExtendedPawnData(this Pawn? pawn) => ExtendedDataStorage.Singleton.GetExtendedPawnData(pawn);

    //For reads in hot paths: doesn't allocate a store entry for every pawn that is merely looked at
    public static bool TryGetExtendedPawnData(this Pawn? pawn, out ExtendedPawnData data) =>
        ExtendedDataStorage.Singleton.TryGetExtendedPawnData(pawn, out data);

    public static bool IsMounted(this Thing pawn)
    {
        return ExtendedDataStorage.isMounted.Contains(pawn.thingIDNumber);
    }

    //Equiv version of the vanilla GetLabeled method, but it avoids iterating the list twice
    public static void UpdateAreaCache(this Map map, bool reset = false) => ExtendedDataStorage.Singleton.UpdateAreaCache(map, reset);

    public static void GetGUAreas(this Map map, out Area? areaNoMount, out Area? areaDropAnimal)
    {
        ExtendedDataStorage.Singleton.AreaNoMount.TryGetValue(map.uniqueID, out areaNoMount);
        ExtendedDataStorage.Singleton.AreaDropAnimal.TryGetValue(map.uniqueID, out areaDropAnimal);
    }

    public static bool CanRideAt(this IntVec3 cell, Area? noMountArea)
    {
        if (noMountArea == null)
            return true;
        return !noMountArea.innerGrid[noMountArea.Map.cellIndices.CellToIndex(cell)];
    }

    public static bool CanRide(this Pawn pawn)
    {
        return pawn.GetExtendedPawnData().canRide;
    }
}