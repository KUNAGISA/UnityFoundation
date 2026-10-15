using Gameplay.Entities;
using Gameplay.Tags;

namespace Gameplay
{
    public sealed class PlayerEntity : CharacterEntity
    {
        public new PlayerCharacterState State => (PlayerCharacterState)base.State;
        public PlayerInputState InputState { get; }
        public GameplayTagContainer GameplayTags { get; }

        public PlayerEntity(PlayerCharacterState state) : base(state ?? throw new System.ArgumentNullException(nameof(state)), state.Spatial)
        {
            InputState = new PlayerInputState();
            GameplayTags = new GameplayTagContainer(Tables.GameplayTags.Catalog);
        }

        protected override void OnDestroy()
        {
            InputState.Reset();
            GameplayTags.Dispose();
        }

        protected override void BuildCharacterAbilities(AbilityContainer abilities)
        {
            abilities.Add(new PlayerInputMovementAbility(InputState, GameplayTags));
            abilities.Add(new PerformanceAbility(GameplayTags, Spatial));
        }
    }
}
