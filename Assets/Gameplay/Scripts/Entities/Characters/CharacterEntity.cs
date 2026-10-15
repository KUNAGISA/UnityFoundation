using System;
using Gameplay.Entities;

namespace Gameplay
{
    public abstract class CharacterEntity : Entity
    {
        public CharacterState State { get; }
        public SpatialState Spatial { get; }

        private CharacterMotorAbility _motor;
        private AnimationAbility _animation;

        protected CharacterEntity(CharacterState state, SpatialState spatial)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Spatial = spatial ?? throw new ArgumentNullException(nameof(spatial));
        }

        protected sealed override void BuildAbilities(AbilityContainer abilities)
        {
            _motor = abilities.Add(new CharacterMotorAbility(Spatial));
            _animation = abilities.Add(new AnimationAbility());
            BuildCharacterAbilities(abilities);
        }

        protected override void OnFrameTick(float deltaTime)
        {
            if (_motor.IsEnabled)
            {
                _motor.ResolveMotion();
            }

            _animation.SetBaseAnimation(_motor.IsEnabled && _motor.ActualDelta.sqrMagnitude > 1e-8f ? CharacterAnimationNames.Move : null);
        }

        protected virtual void BuildCharacterAbilities(AbilityContainer abilities) { }
    }
}
