using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public sealed class MovementRouteData
    {
        public int RouteId;
        public Color Color = Color.white;
        [ListDrawerSettings(DefaultExpandedState = false, DraggableItems = false, HideAddButton = true, HideRemoveButton = true)]
        public List<PathNode> Nodes = new();
    }
}
