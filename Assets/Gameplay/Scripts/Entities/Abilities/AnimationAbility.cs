using System;
using Gameplay.Entities;
using UnityEngine;

namespace Gameplay
{
    public sealed class AnimationAbility : Ability
    {
        private const int AnimatorLayerIndex = 0;
        private const float DefaultTransitionDuration = 0.2f;

        private Animator _animator;
        private string _baseAnimation;
        private float _baseTransitionDuration = DefaultTransitionDuration;
        private bool _baseLoop = true;
        private string _appliedAnimation;
        private bool _appliedLoop = true;
        private bool _hasAppliedAnimation;
        private int _overrideId;
        private bool _hasOverride;

        protected override void OnInitialize()
        {
            _animator = Owner.Root.GetComponentInChildren<Animator>(true);
        }

        protected override void OnDestroy()
        {
            _animator = null;
            _baseAnimation = null;
            _appliedAnimation = null;
            _hasAppliedAnimation = false;
            _hasOverride = false;
        }

        public void SetBaseAnimation(string animationName, float transitionDuration = DefaultTransitionDuration, bool loop = true)
        {
            ValidateTransitionDuration(transitionDuration);
            var previousAnimation = _baseAnimation;
            var previousTransitionDuration = _baseTransitionDuration;
            var previousLoop = _baseLoop;
            _baseAnimation = animationName;
            _baseTransitionDuration = transitionDuration;
            _baseLoop = loop;
            if (_hasAppliedAnimation && previousAnimation == animationName && (animationName == null || previousLoop == loop))
            {
                return;
            }

            if (_hasOverride)
            {
                return;
            }

            try
            {
                ApplyAnimation(animationName, transitionDuration, loop);
            }
            catch
            {
                _baseAnimation = previousAnimation;
                _baseTransitionDuration = previousTransitionDuration;
                _baseLoop = previousLoop;
                throw;
            }
        }

        public AnimationOverrideToken BeginAnimationOverride(string animationName, float transitionDuration = DefaultTransitionDuration, bool loop = true)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                throw new ArgumentException("Override animation name must not be empty.", nameof(animationName));
            }
            ValidateTransitionDuration(transitionDuration);
            if (_hasOverride)
            {
                throw new InvalidOperationException("An animation override is already active.");
            }

            unchecked
            {
                ++_overrideId;
                if (_overrideId == 0)
                {
                    ++_overrideId;
                }
            }

            _hasOverride = true;
            try
            {
                ApplyAnimation(animationName, transitionDuration, loop);
            }
            catch
            {
                _hasOverride = false;
                throw;
            }

            return new AnimationOverrideToken(this, _overrideId);
        }

        internal void CancelOverride(int id)
        {
            if (!_hasOverride || id != _overrideId)
            {
                return;
            }

            _hasOverride = false;
            ApplyAnimation(_baseAnimation, _baseTransitionDuration, _baseLoop);
        }

        private void ApplyAnimation(string animationName, float transitionDuration, bool loop)
        {
            if (_hasAppliedAnimation && _appliedAnimation == animationName && (animationName == null || _appliedLoop == loop))
            {
                return;
            }

            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                var stateName = animationName ?? CharacterAnimationNames.Idle;
                if (transitionDuration > 0f)
                {
                    _animator.CrossFadeInFixedTime(stateName, transitionDuration, AnimatorLayerIndex);
                }
                else
                {
                    _animator.Play(stateName, AnimatorLayerIndex, 0f);
                }
            }

            _appliedAnimation = animationName;
            _appliedLoop = loop;
            _hasAppliedAnimation = true;
        }

        private static void ValidateTransitionDuration(float transitionDuration)
        {
            if (transitionDuration < 0f || float.IsNaN(transitionDuration) || float.IsInfinity(transitionDuration))
            {
                throw new ArgumentOutOfRangeException(nameof(transitionDuration), transitionDuration, "Transition duration must be finite and non-negative.");
            }
        }
    }
}
