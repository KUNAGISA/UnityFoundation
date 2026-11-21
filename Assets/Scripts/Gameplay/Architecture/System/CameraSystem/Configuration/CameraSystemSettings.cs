using System;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public struct CameraProviderRangeConfig
    {
        [SerializeField, LabelText("镜头模块")]
        private CameraProviderId _providerId;
        public CameraProviderId ProviderId => _providerId;

        [SerializeField, InlineProperty, LabelText("优先级范围")]
        private CameraPriorityRange _range;
        public CameraPriorityRange Range => _range;
    }

    [Serializable]
    public struct CameraProviderBlendConfig
    {
        [SerializeField, LabelText("来源模块")]
        private CameraProviderId _from;
        public CameraProviderId From => _from;

        [SerializeField, LabelText("目标模块")]
        private CameraProviderId _to;
        public CameraProviderId To => _to;

        [SerializeField, InlineProperty]
        private CameraBlendConfig _blend;
        public CameraBlendConfig Blend => _blend;
    }

    [CreateAssetMenu(menuName = "Gameplay/Camera/Camera System Settings")]
    public sealed class CameraSystemSettings : ScriptableObject
    {
        [SerializeField, LabelText("主相机地址")]
        private string _mainCameraLocation = "Global/MainCamera";
        public string MainCameraLocation => _mainCameraLocation;

        [SerializeField, LabelText("输出通道")]
        private OutputChannels _outputChannel = OutputChannels.Default;
        public OutputChannels OutputChannel => _outputChannel;

        [SerializeField, InlineProperty, LabelText("默认混合")]
        private CameraBlendConfig _defaultBlend = new CameraBlendConfig(CinemachineBlendDefinition.Styles.EaseInOut, 0.5f);
        public CameraBlendConfig DefaultBlend => _defaultBlend;

        [SerializeField, TableList(AlwaysExpanded = true), LabelText("模块优先级")]
        private CameraProviderRangeConfig[] _providerRanges = Array.Empty<CameraProviderRangeConfig>();
        internal CameraProviderRangeConfig[] ProviderRanges => _providerRanges;

        [SerializeField, TableList(AlwaysExpanded = true), LabelText("跨模块混合")]
        private CameraProviderBlendConfig[] _providerBlends = Array.Empty<CameraProviderBlendConfig>();
        internal CameraProviderBlendConfig[] ProviderBlends => _providerBlends;

        public bool TryGetPriorityRange(CameraProviderId providerId, out CameraPriorityRange range)
        {
            for (var index = 0; index < _providerRanges.Length; ++index)
            {
                if (_providerRanges[index].ProviderId == providerId)
                {
                    range = _providerRanges[index].Range;
                    return true;
                }
            }

            range = default;
            return false;
        }

        public CinemachineBlendDefinition ResolveBlend(CameraProviderId from, CameraProviderId to)
        {
            if (from == CameraProviderId.None || to == CameraProviderId.None)
            {
                return new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }

            for (var index = 0; index < _providerBlends.Length; ++index)
            {
                ref readonly var entry = ref _providerBlends[index];
                if (entry.From == from && entry.To == to)
                {
                    return entry.Blend.ToDefinition();
                }
            }

            return _defaultBlend.ToDefinition();
        }
    }
}
