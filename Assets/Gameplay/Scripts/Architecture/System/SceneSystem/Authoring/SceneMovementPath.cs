using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [DisallowMultipleComponent]
    [AddComponentMenu("场景/场景移动路径")]
    public sealed class SceneMovementPath : MonoBehaviour
    {
        [SerializeField, TableList]
        private List<MovementRouteData> _routes = new();

        [SerializeField, MinValue(0.01f)]
        private float _sampleSpacing = 0.05f;

        public IReadOnlyList<MovementRouteData> Routes => _routes;
        public float SampleSpacing => _sampleSpacing;

#if UNITY_EDITOR
        public List<MovementRouteData> EditorRoutes => _routes;
        public float EditorSampleSpacing
        {
            get => _sampleSpacing;
            set => _sampleSpacing = Mathf.Max(0.01f, value);
        }
#endif
    }
}
