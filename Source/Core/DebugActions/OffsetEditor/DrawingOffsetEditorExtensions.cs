using GiddyUp;
using UnityEngine;

namespace GiddyUpCore.Core.DebugActions.OffsetEditor;

internal static class DrawingOffsetEditorExtensions
{
    public static Vector3 NorthOffset(this DrawingOffset offset) => offset.northOffset;

    public static Vector3 SouthOffset(this DrawingOffset offset) => offset.southOffset;

    public static Vector3 EastOffset(this DrawingOffset offset) => offset.eastOffset;

    public static Vector3 WestOffset(this DrawingOffset offset) => offset.westOffset;
}
