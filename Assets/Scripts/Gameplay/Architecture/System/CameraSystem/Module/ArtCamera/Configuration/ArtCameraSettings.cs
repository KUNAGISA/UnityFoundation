using System;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay
{
    [Serializable]
    public struct ArtCameraLayerConfig
    {
        [SerializeField, LabelText("镜头层级")]
        private ArtCameraLayer _layer;
        public ArtCameraLayer Layer => _layer;

        [SerializeField, LabelText("优先级偏移")]
        private int _priorityOffset;
        public int PriorityOffset => _priorityOffset;
    }

    [Serializable]
    public struct ArtCameraLayerBlendConfig
    {
        [SerializeField, LabelText("来源层级")]
        private ArtCameraLayer _from;
        public ArtCameraLayer From => _from;

        [SerializeField, LabelText("目标层级")]
        private ArtCameraLayer _to;
        public ArtCameraLayer To => _to;

        [SerializeField, InlineProperty, LabelText("混合")]
        private CameraBlendConfig _blend;
        public CameraBlendConfig Blend => _blend;
    }

    [CreateAssetMenu(menuName = "Gameplay/Camera/Art Camera Settings")]
    public sealed class ArtCameraSettings : ScriptableObject
    {
        [SerializeField, InlineProperty, LabelText("默认混合")]
        private CameraBlendConfig _defaultBlend = new CameraBlendConfig(CinemachineBlendDefinition.Styles.EaseInOut, 0.4f);

        [SerializeField, TableList(AlwaysExpanded = true), LabelText("层级配置")]
        private ArtCameraLayerConfig[] _layers = Array.Empty<ArtCameraLayerConfig>();

        [SerializeField, TableList(AlwaysExpanded = true), LabelText("层间混合")]
        private ArtCameraLayerBlendConfig[] _blends = Array.Empty<ArtCameraLayerBlendConfig>();

        [FormerlySerializedAs("_releaseTimeout")]
        [SerializeField, MinValue(0.1f), LabelText("Live 警告间隔")]
        private float _liveWarningInterval = 5f;
        public float LiveWarningInterval => Mathf.Max(0.1f, _liveWarningInterval);

        public int ResolvePriority(ArtCameraLayer layer, in CameraPriorityRange range)
        {
            for (var index = 0; index < _layers.Length; ++index)
            {
                if (_layers[index].Layer == layer)
                {
                    var priority = range.ResolveOffset(_layers[index].PriorityOffset);
                    if (!range.Contains(priority))
                    {
                        throw new InvalidOperationException($"Art camera layer [{layer}] resolves outside its provider priority range.");
                    }
                    return priority;
                }
            }
            throw new InvalidOperationException($"Art camera layer [{layer}] is not configured.");
        }

        public CinemachineBlendDefinition ResolveBlend(ArtCameraLayer from, ArtCameraLayer to)
        {
            for (var index = 0; index < _blends.Length; ++index)
            {
                if (_blends[index].From == from && _blends[index].To == to)
                {
                    return _blends[index].Blend.ToDefinition();
                }
            }
            return _defaultBlend.ToDefinition();
        }

        public void Validate(in CameraPriorityRange range)
        {
            if (_layers == null || _layers.Length != (int)ArtCameraLayer.CriticalCutscene + 1)
            {
                throw new InvalidOperationException("Art camera settings must configure every ArtCameraLayer exactly once.");
            }
            for (var index = 0; index < _layers.Length; ++index)
            {
                var priority = range.ResolveOffset(_layers[index].PriorityOffset);
                if (!range.Contains(priority))
                {
                    throw new InvalidOperationException($"Art camera layer [{_layers[index].Layer}] is outside its provider range.");
                }
                for (var otherIndex = index + 1; otherIndex < _layers.Length; ++otherIndex)
                {
                    if (_layers[index].Layer == _layers[otherIndex].Layer || _layers[index].PriorityOffset == _layers[otherIndex].PriorityOffset)
                    {
                        throw new InvalidOperationException($"Art camera layer configuration conflicts at indices [{index}] and [{otherIndex}].");
                    }
                }
            }
            for (var layerIndex = 0; layerIndex <= (int)ArtCameraLayer.CriticalCutscene; ++layerIndex)
            {
                var found = false;
                for (var index = 0; index < _layers.Length; ++index)
                {
                    if ((int)_layers[index].Layer == layerIndex)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    throw new InvalidOperationException($"Art camera layer [{(ArtCameraLayer)layerIndex}] is not configured.");
                }
            }
            for (var index = 0; index < _blends.Length; ++index)
            {
                for (var otherIndex = index + 1; otherIndex < _blends.Length; ++otherIndex)
                {
                    if (_blends[index].From == _blends[otherIndex].From && _blends[index].To == _blends[otherIndex].To)
                    {
                        throw new InvalidOperationException($"Art camera blend [{_blends[index].From} -> {_blends[index].To}] is configured more than once.");
                    }
                }
            }
        }
    }
}
