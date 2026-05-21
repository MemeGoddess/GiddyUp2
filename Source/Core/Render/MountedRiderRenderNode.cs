using System.Linq;
using Verse;

namespace GiddyUpCore.Core;

internal sealed class MountedRiderRenderNode : PawnRenderNode
{
    public Pawn Rider { get; }

    public Pawn Mount => tree.pawn;

    public MountedRiderRenderNode(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Pawn rider)
        : base(pawn, props, tree)
    {
        Rider = rider;
    }

    public override GraphicMeshSet MeshSetFor(Pawn pawn) => null!;
}