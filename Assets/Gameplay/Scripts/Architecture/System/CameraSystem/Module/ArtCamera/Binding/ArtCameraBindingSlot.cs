using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [Serializable]
    public sealed class ArtCameraBindingSlot
    {
        [SerializeField, LabelText("槽位")]
        private ArtCameraBindingSlotId _slotId;
        public ArtCameraBindingSlotId SlotId => _slotId;

        [SerializeField, LabelText("是否必需")]
        private bool _required;
        public bool Required => _required;

        [SerializeField, LabelText("目标丢失策略")]
        private ArtCameraLostTargetPolicy _lostTargetPolicy;
        public ArtCameraLostTargetPolicy LostTargetPolicy => _lostTargetPolicy;

        [SerializeField, LabelText("绑定代理")]
        private Transform _bindingProxy;
        public Transform BindingProxy => _bindingProxy;

        [SerializeField, LabelText("动画偏移")]
        private Transform _animatedOffset;
        public Transform AnimatedOffset => _animatedOffset;

        [NonSerialized]
        public Transform Target;

        [NonSerialized]
        public bool WasBound;

        public void Validate(string rigName)
        {
            if (_bindingProxy == null || _animatedOffset == null || _animatedOffset.parent != _bindingProxy)
            {
                throw new InvalidOperationException($"Art camera rig [{rigName}] binding slot [{_slotId}] requires BindingProxy -> AnimatedOffset hierarchy.");
            }
        }

        public void Sync()
        {
            if (Target != null)
            {
                _bindingProxy.SetPositionAndRotation(Target.position, Target.rotation);
            }
        }

        public ArtCameraLostTargetPolicy? SyncOrResolveLostTarget()
        {
            if (Target != null)
            {
                WasBound = true;
                Sync();
                return null;
            }
            if (!WasBound)
            {
                return null;
            }

            WasBound = false;
            if (_lostTargetPolicy == ArtCameraLostTargetPolicy.ClearBinding)
            {
                _bindingProxy.localPosition = Vector3.zero;
                _bindingProxy.localRotation = Quaternion.identity;
            }
            return _lostTargetPolicy;
        }
    }
}
