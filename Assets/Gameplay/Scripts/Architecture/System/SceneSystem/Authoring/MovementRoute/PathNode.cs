using System;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public struct PathNode
    {
        public Vector2 Position;
        public PathSegmentType NextSegmentType;
        public Vector2 InTangent;
        public Vector2 OutTangent;
    }
}
