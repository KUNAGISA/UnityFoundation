using System;
using DG.Tweening;
using Gameplay.Rendering;
using Gameplay.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace Gameplay
{
    public sealed class UIRoot : MonoBehaviour
    {
        [Serializable]
        public sealed class LayerConfig
        {
            [SerializeField]
            private UILayer _layer;
            public UILayer Layer => _layer;
            public UILayerId LayerId => _layer.ToLayerId();

            [SerializeField]
            private Transform _backgroundRoot = null;
            public Transform BackgroundRoot => _backgroundRoot;

            [SerializeField]
            private Transform _foregroundRoot = null;
            public Transform ForegroundRoot => _foregroundRoot;

            [SerializeField]
            private int _sortingOrder;
            public int SortingOrder => _sortingOrder;

        }

        [SerializeField]
        private Camera _backgroundCamera = null;
        public Camera BackgroundCamera => _backgroundCamera;

        [SerializeField]
        private Camera _foregroundCamera = null;
        public Camera ForegroundCamera => _foregroundCamera;

        [SerializeField]
        private InputSystemUIInputModule _inputModule = null;
        public InputSystemUIInputModule InputModule => _inputModule;

        [SerializeField]
        private DualKawaseBlurRendererFeature _blurFeature = null;
        public DualKawaseBlurRendererFeature BlurFeature => _blurFeature;

        [SerializeField, MinValue(0f)]
        private float _blurTransitionDuration = 0.2f;
        public float BlurTransitionDuration => _blurTransitionDuration;

        [SerializeField]
        private Ease _blurTransitionEase = Ease.InOutSine;
        public Ease BlurTransitionEase => _blurTransitionEase;

        [SerializeField, TableList]
        private LayerConfig[] _layers = Array.Empty<LayerConfig>();
        public ReadOnlySpan<LayerConfig> Layers => _layers;

        public bool TryGetLayer(UILayerId layerId, out LayerConfig layer)
        {
            for (var index = 0; index < _layers.Length; index++)
            {
                if (_layers[index] != null && _layers[index].LayerId == layerId)
                {
                    layer = _layers[index];
                    return true;
                }
            }

            layer = null;
            return false;
        }
    }
}
