using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace GiddyUp.Ideology;

public sealed class NoRecentMount : DefModExtension
{
    [UsedImplicitly] public List<ThingDef>? acceptableMountDefs;
    public float MinorDays = 5f;
    public float MajorDays = 10f;
    public float SevereDays = 15f;
}
