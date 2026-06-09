using System.Linq;
using GiddyUp;
using Verse;

namespace GiddyUpCore.Core;

internal sealed class MountedRiderRenderNode : PawnRenderNode
{
    public ExtendedPawnData AnimalData { get; }

    public Pawn Mount => tree.pawn;

    public MountedRiderRenderNode(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : base(pawn, props, tree)
    {
        AnimalData = pawn.GetExtendedPawnData();
    }

    public override GraphicMeshSet MeshSetFor(Pawn pawn) => null!;
}