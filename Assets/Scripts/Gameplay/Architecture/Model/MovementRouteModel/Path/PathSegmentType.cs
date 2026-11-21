using UnityEngine;

namespace Gameplay
{
    public enum PathSegmentType : byte
    {
        [InspectorName("线段")]
        Line = 0,
        [InspectorName("曲线")]
        Bezier = 1
    }
}
