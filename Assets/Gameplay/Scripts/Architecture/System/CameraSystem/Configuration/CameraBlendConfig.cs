using System;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public struct CameraBlendConfig
    {
        [SerializeField, HorizontalGroup, LabelText("样式")]
        private CinemachineBlendDefinition.Styles _style;
        public CinemachineBlendDefinition.Styles Style => _style;

        [SerializeField, HorizontalGroup, MinValue(0f), LabelText("时长")]
        private float _duration;
        public float Duration => _duration;

        public CameraBlendConfig(CinemachineBlendDefinition.Styles style, float duration)
        {
            _style = style;
            _duration = Mathf.Max(0f, duration);
        }

        public CinemachineBlendDefinition ToDefinition()
        {
            return new CinemachineBlendDefinition(_style, Mathf.Max(0f, _duration));
        }
    }
}
