using System;
using Verse;

namespace GiddyUp;

internal class CompOverlay : ThingComp
{
    // What's my purpose?
    // You pass Props
    // Oh my god
    public CompProperties_Overlay Prop => props as CompProperties_Overlay ?? throw new InvalidOperationException();
}