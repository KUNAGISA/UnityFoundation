using System;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Gameplay
{
    public sealed class ArtCameraRig : MonoBehaviour
    {
        [SerializeField, LabelText("输出镜头")]
        private CinemachineVirtualCameraBase _outputCamera;
        public CinemachineVirtualCameraBase OutputCamera => _outputCamera;

        [SerializeField, LabelText("动画器")]
        private Animator _animator;

        [SerializeField, LabelText("主动画片段")]
        private AnimationClip _clip;
        public AnimationClip MainClip => _clip;

        [SerializeField, LabelText("绑定槽位"), TableList(AlwaysExpanded = true)]
        private ArtCameraBindingSlot[] _bindingSlots = Array.Empty<ArtCameraBindingSlot>();

        private PlayableGraph _graph;
        private AnimationClipPlayable _clipPlayable;
        private float _elapsed;

        public float NormalizedTime => _clip == null || _clip.length <= 0f ? 1f : Mathf.Clamp01(_elapsed / _clip.length);

        public void ValidateConfiguration()
        {
            if (_outputCamera == null || _animator == null || _clip == null)
            {
                throw new InvalidOperationException($"Art camera rig [{name}] is missing OutputCamera, Animator, or AnimationClip.");
            }
            var cameras = GetComponentsInChildren<CinemachineVirtualCameraBase>(true);
            for (var index = 0; index < cameras.Length; ++index)
            {
                if (cameras[index] != _outputCamera && cameras[index].ParentCamera == null)
                {
                    throw new InvalidOperationException($"Art camera rig [{name}] contains more than one top-level output camera.");
                }
            }
            for (var index = 0; index < _bindingSlots.Length; ++index)
            {
                _bindingSlots[index].Validate(name);
                for (var otherIndex = index + 1; otherIndex < _bindingSlots.Length; ++otherIndex)
                {
                    if (_bindingSlots[index].SlotId == _bindingSlots[otherIndex].SlotId)
                    {
                        throw new InvalidOperationException($"Art camera rig [{name}] has duplicate binding slot [{_bindingSlots[index].SlotId}].");
                    }
                }
            }
        }

        public void ValidateAndPrepare()
        {
            ValidateConfiguration();
            _outputCamera.enabled = false;
        }

        public void ApplyBindings(ArtCameraBinding[] bindings)
        {
            for (var slotIndex = 0; slotIndex < _bindingSlots.Length; ++slotIndex)
            {
                var slot = _bindingSlots[slotIndex];
                slot.Target = null;
                if (bindings != null)
                {
                    for (var bindingIndex = 0; bindingIndex < bindings.Length; ++bindingIndex)
                    {
                        if (bindings[bindingIndex].SlotId == slot.SlotId)
                        {
                            slot.Target = bindings[bindingIndex].Target;
                            break;
                        }
                    }
                }
                slot.WasBound = slot.Target != null;
                if (slot.Required && slot.Target == null)
                {
                    throw new InvalidOperationException($"Art camera rig [{name}] requires binding slot [{slot.SlotId}].");
                }
                slot.Sync();
            }
        }

        public ArtCameraLostTargetPolicy? SyncBindings()
        {
            for (var index = 0; index < _bindingSlots.Length; ++index)
            {
                var slot = _bindingSlots[index];
                var lostTargetPolicy = slot.SyncOrResolveLostTarget();
                if (lostTargetPolicy == ArtCameraLostTargetPolicy.CancelSession)
                {
                    return lostTargetPolicy;
                }
            }
            return null;
        }

        public void CreatePlayback()
        {
            _graph = PlayableGraph.Create($"ArtCamera [{name}]");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _clipPlayable = AnimationClipPlayable.Create(_graph, _clip);
            _clipPlayable.SetApplyFootIK(false);
            _clipPlayable.SetApplyPlayableIK(false);
            var output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
            output.SetSourcePlayable(_clipPlayable);
            _graph.Play();
            _graph.Evaluate(0f);
            _elapsed = 0f;
        }

        public bool Evaluate(float deltaTime, ArtCameraCompletionPolicy completionPolicy)
        {
            if (!_graph.IsValid())
            {
                return false;
            }
            var duration = Mathf.Max(0f, _clip.length);
            if (completionPolicy != ArtCameraCompletionPolicy.LoopUntilCancelled && duration <= 0f)
            {
                return true;
            }
            if (deltaTime <= 0f)
            {
                return false;
            }
            if (completionPolicy == ArtCameraCompletionPolicy.LoopUntilCancelled)
            {
                if (duration <= 0f)
                {
                    return false;
                }
                var totalElapsed = _elapsed + deltaTime;
                if (totalElapsed >= duration)
                {
                    _elapsed = totalElapsed % duration;
                    _clipPlayable.SetTime(_elapsed);
                    _graph.Evaluate(0f);
                    return false;
                }
                _graph.Evaluate(deltaTime);
                _elapsed = totalElapsed;
                return false;
            }
            deltaTime = Mathf.Min(deltaTime, Mathf.Max(0f, duration - _elapsed));
            if (deltaTime > 0f)
            {
                _graph.Evaluate(deltaTime);
                _elapsed += deltaTime;
            }
            return _elapsed >= duration;
        }

        public void SetOutputEnabled(bool enabled)
        {
            _outputCamera.enabled = enabled;
            if (enabled)
            {
                _outputCamera.PreviousStateIsValid = false;
            }
        }

        public void DisposePlayback()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }
    }
}
