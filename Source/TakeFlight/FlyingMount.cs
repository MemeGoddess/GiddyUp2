using Verse;

namespace GiddyUpTakeFlight;

/// <summary>
/// Marks an animal race as a flying mount (or, with canFly false, as one that never flies).
/// </summary>
public class FlyingMount : DefModExtension
{
    public bool canFly = true;
    public float rangeFactor = 1f;
    public float cooldownFactor = 1f;

    public int worldFlightRange = -1;
    public float maxRangeMultiplier = -1f;
    public float worldFlightSpeed = -1f;
    public SimpleCurve? rangeByLoad;
}
