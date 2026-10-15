using System;
using Gameplay.Entities;
using Gameplay.Tags;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class PlayerInputMovementAbility : Ability, IAbilityFrameTickable
    {
        public float Speed { get; set; } = 10f;

        private const int MotionPriority = 0;

        private readonly PlayerInputState _input;
        private readonly GameplayTagContainer _gameplayTags;
        private CharacterMotorAbility _motor;

        public PlayerInputMovementAbility(PlayerInputState input, GameplayTagContainer gameplayTags)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _gameplayTags = gameplayTags ?? throw new ArgumentNullException(nameof(gameplayTags));
        }

        protected override void OnReady()
        {
            _motor = Owner.RequireAbility<CharacterMotorAbility>();
        }

        protected override void OnDestroy()
        {
            _motor = null;
        }

        void IAbilityFrameTickable.FrameTick(float deltaTime)
        {
            if (_gameplayTags.Has(GameplayTags.Control.Block.Movement.Tag))
            {
                return;
            }

            var inputDir = _input.InputDir;
            if (inputDir == Vector2.zero)
            {
                return;
            }

            var desiredDelta = inputDir.normalized * (Speed * deltaTime);
            _motor.SubmitDelta(desiredDelta, MotionMode.Override, MotionPriority);
        }
    }
}
