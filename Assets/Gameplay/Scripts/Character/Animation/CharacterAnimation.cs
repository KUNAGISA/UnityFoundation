using UnityEngine;

namespace Gameplay
{
    public enum CharacterAnimationAction : byte
    {
        Idle,
        Walk,
        Talk,
        Interact,
        StairsUp,
        StairsDown,
    }

    [DisallowMultipleComponent]
    public sealed class CharacterAnimation : MonoBehaviour
    {
        private const int AnimatorLayer = 0;
        private const float TransitionSeconds = 0.2f;
        private static readonly string[] ActionNames = { "idle", "walk", "talk", "interact", "stairs_up", "stairs_down" };
        private static readonly string[] FacingNames = { "left", "right", "front", "back" };

        private Animator _animator;
        private int[,] _stateHashes;
        private int _legacyIdle;
        private int _legacyMove;
        private CharacterAnimationAction _baseAction;
        private FacingDirection _baseFacing;
        private FacingDirection _appliedFacing;
        private int _appliedState;
        private int _overrideId;
        private bool _hasBaseRequest;
        private bool _hasAppliedState;
        private bool _hasOverride;

        internal void Initialize()
        {
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null || _animator.runtimeAnimatorController == null || _animator.layerCount == 0)
            {
                return;
            }

            var layer = _animator.GetLayerName(AnimatorLayer);
            _stateHashes = new int[ActionNames.Length, FacingNames.Length];
            for (var action = 0; action < ActionNames.Length; action++)
            {
                for (var facing = 0; facing < FacingNames.Length; facing++)
                {
                    _stateHashes[action, facing] = Animator.StringToHash($"{layer}.{ActionNames[action]}_{FacingNames[facing]}");
                }
            }

            _legacyIdle = Animator.StringToHash($"{layer}.Idle");
            _legacyMove = Animator.StringToHash($"{layer}.Move");
        }

        internal void Release()
        {
            _animator = null;
            _stateHashes = null;
            _hasBaseRequest = false;
            _hasAppliedState = false;
            _hasOverride = false;
        }

        public void SetBaseAnimation(CharacterAnimationAction action, FacingDirection facing)
        {
            if (_hasBaseRequest && _baseAction == action && _baseFacing == facing)
            {
                return;
            }

            _baseAction = action;
            _baseFacing = facing;
            _hasBaseRequest = true;
            if (!_hasOverride)
            {
                PlayBaseAnimation();
            }
        }

        public AnimationOverrideToken BeginOverride(CharacterAnimationAction action, FacingDirection facing)
        {
            if (_hasOverride)
            {
                Debug.LogError("[CharacterAnimation] An animation override is already active.", this);
                return AnimationOverrideToken.None;
            }

            if (!TryGetDirectionalState(action, facing, out var state))
            {
                Debug.LogError($"[CharacterAnimation] No Animator state for {action} facing {facing}.", this);
                return AnimationOverrideToken.None;
            }

            Play(state, facing);
            _hasOverride = true;
            return new AnimationOverrideToken(this, ++_overrideId);
        }

        internal bool IsOverrideActive(int id) => _hasOverride && id == _overrideId;

        internal void CancelOverride(int id)
        {
            if (!IsOverrideActive(id))
            {
                return;
            }

            _hasOverride = false;
            PlayBaseAnimation();
        }

        private void PlayBaseAnimation()
        {
            if (TryGetDirectionalState(_baseAction, _baseFacing, out var state))
            {
                Play(state, _baseFacing);
                return;
            }

            var legacyState = _baseAction == CharacterAnimationAction.Walk ? _legacyMove : _legacyIdle;
            if (_stateHashes != null && _animator.HasState(AnimatorLayer, legacyState))
            {
                Play(legacyState, _baseFacing);
            }
        }

        private bool TryGetDirectionalState(CharacterAnimationAction action, FacingDirection facing, out int state)
        {
            state = 0;
            if (_stateHashes == null || (int)action >= ActionNames.Length || (int)facing >= FacingNames.Length)
            {
                return false;
            }

            state = _stateHashes[(int)action, (int)facing];
            return _animator.HasState(AnimatorLayer, state);
        }

        private void Play(int state, FacingDirection facing)
        {
            var sameFacing = _hasAppliedState && _appliedFacing == facing;
            _appliedFacing = facing;
            if (_hasAppliedState && _appliedState == state)
            {
                return;
            }

            if (sameFacing)
            {
                _animator.CrossFadeInFixedTime(state, TransitionSeconds, AnimatorLayer);
            }
            else
            {
                _animator.Play(state, AnimatorLayer, 0f);
            }

            _appliedState = state;
            _hasAppliedState = true;
        }
    }
}
