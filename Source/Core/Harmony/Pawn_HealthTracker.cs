using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUp.Harmony;

[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.MakeDowned))]
internal static class Patch_MakeDowned
{
    private static void Postfix(Pawn_HealthTracker __instance)
    {
        var pawn = __instance.pawn;
        if (pawn.Faction == null || ExtendedDataStorage.Singleton == null)
            return; //Null checking the singleton 'cause this could happen before the world sets up.

        if (!pawn.TryGetExtendedPawnData(out var pawnData))
            return; //Never involved with Giddy-Up, nothing to clean up
        if (pawn.RaceProps.Humanlike)
            pawn.InvoluntaryDismount(pawnData.ReservedMount, pawnData);
        else if (pawnData.ReservedBy != null)
        {
            var reservedBy = pawnData.ReservedBy;
            reservedBy.InvoluntaryDismount(pawn, reservedBy.GetExtendedPawnData());
            if (pawn.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer))
                pawn.SetFaction(null); //If an enemy animal is downed, make it a wild animal so it can be rescued.
        }
    }
}

[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.SetDead))]
internal static class Patch_SetDead
{
    private static void Postfix(Pawn_HealthTracker __instance)
    {
        var pawn = __instance.pawn;
        //Every death (insects, wild animals) created a store entry here; also the singleton may not exist yet
        if (ExtendedDataStorage.Singleton == null || !pawn.TryGetExtendedPawnData(out var pawnData))
            return;
        if (pawn.IsMounted())
        {
            pawn.Dismount(pawnData.Mount ?? pawnData.ReservedMount, pawnData, clearReservation: true);
        }
        else if (pawnData.Rider != null)
        {
            var rider = pawnData.Rider;
            var riderData = rider.GetExtendedPawnData();
            rider.Dismount(riderData.Mount ?? riderData.ReservedMount, riderData, clearReservation: true);
        }
    }
}