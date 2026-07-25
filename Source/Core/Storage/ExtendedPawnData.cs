using System.Collections.Generic;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;


namespace GiddyUp;

public class ExtendedPawnData : IExposable
{
    private Pawn? _pawn;
    public Pawn? Pawn => _pawn;
    
    private Pawn? _mount;
    public Pawn? Mount
    {
        get => _mount;
        set => _mount = value;
    }

    public int ID;

    public bool selectedForCaravan = false, canRide = true;
    public float drawOffset;
    public int lastMountedTick = 0;
    private Dictionary<ThingDef, int>? _lastMountedTickByMountDef;
    public Automount automount = Automount.Anyone;

    public enum Automount
    {
        False,
        Anyone,
        Colonists,
        Slaves
    };

    // ReSharper disable once UnusedMember.Global
    public ExtendedPawnData()
    {
    }
    
    public ExtendedPawnData(Pawn? pawn)
    {
        _pawn = pawn;
        ID = pawn?.thingIDNumber ?? -1;
        if (Settings.automountDisabledByDefault)
            automount = Automount.False;
    }

    //Used by the rider and mount respectively. This creates a short-term association, like for example of a rider hops off for a few moments.
    private Pawn? _reservedMount;
    public Pawn? ReservedMount
    {
        get => _reservedMount;
        set
        {
            if (Settings.logging && value == null)
                Log.Message("[Giddy-Up] " + Pawn?.Label + " no longer reserved to  " + (_reservedMount?.Label ?? "NULL"));
            else if (Settings.logging && value != null)
                Log.Message("[Giddy-Up] " + Pawn?.Label + " now reserved to  " + (value?.Label ?? "NULL"));
            _reservedMount = value;
        }
    }

    private Pawn? _reservedBy;

    public Pawn? ReservedBy
    {
        get => _reservedBy;
        set
        {
            if (Settings.logging && value == null)
                Log.Message("[Giddy-Up] " + Pawn?.Label + " no longer reserved to  " + (_reservedBy?.Label ?? "NULL"));
            else if (Settings.logging && value != null)
                Log.Message("[Giddy-Up] " + Pawn?.Label + " now reserved to  " + (value?.Label ?? "NULL"));
            _reservedBy = value;
        }
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref _pawn, "pawn");
        Scribe_References.Look(ref _mount, "mount");
        Scribe_References.Look(ref _reservedBy, "reservedBy");
        Scribe_References.Look(ref _reservedMount, "reservedMount");

        Scribe_Values.Look(ref ID, "ID");
        Scribe_Values.Look(ref automount, "automount", Automount.Anyone);
        Scribe_Values.Look(ref drawOffset, "drawOffset");
        Scribe_Values.Look(ref lastMountedTick, "lastMountedTick", 0);
        Scribe_Collections.Look(ref _lastMountedTickByMountDef, "lastMountedTickByMountDef", LookMode.Def, LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (Mount != null)
                ExtendedDataStorage.isMounted.Add(ID);
            if (Settings.disableSlavePawnColumn && automount == Automount.Slaves)
                automount = Automount.False;

            //Remove any invalid entries that somehow slipped into the store.
            ExtendedDataStorage.Singleton.CleanupExtendedPawnDataFromStore(this);
        }
    }

    public void NotifyMountedAt(int tick, ThingDef? mountDef = null)
    {
        lastMountedTick = tick;
        if (mountDef == null)
            return;

        _lastMountedTickByMountDef ??= new();
        _lastMountedTickByMountDef[mountDef] = tick;
    }

    public int GetLastMountedTick(IReadOnlyList<ThingDef>? acceptedMountDefs)
    {
        if (acceptedMountDefs == null || acceptedMountDefs.Count == 0)
            return lastMountedTick;

        if (_lastMountedTickByMountDef == null || _lastMountedTickByMountDef.Count == 0)
            return 0;

        var bestTick = 0;
        foreach (var mountDef in acceptedMountDefs)
        {
            if (mountDef != null && _lastMountedTickByMountDef.TryGetValue(mountDef, out var tick) && tick > bestTick)
                bestTick = tick;
        }

        return bestTick;
    }
}