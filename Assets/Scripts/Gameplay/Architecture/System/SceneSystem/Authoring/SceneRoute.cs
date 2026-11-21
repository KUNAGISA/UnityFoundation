using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [Serializable, InlineProperty, HideLabel]
    public struct SceneRoute
    {
        [SerializeField, LabelText("绑定路线")]
        private bool _enabled;

        [SerializeField, LabelText("路线归属"), SceneRouteId, ShowIf("_enabled")]
        private int _routeId;

        public bool Enabled => _enabled;
        public int RouteId => _routeId;
    }
}
