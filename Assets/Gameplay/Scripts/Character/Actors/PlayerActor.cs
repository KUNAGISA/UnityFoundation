using Gameplay.Tags;
using UnityEngine;

namespace Gameplay
{
    public sealed class PlayerActor : CharacterActor
    {
        public new PlayerCharacterState State => (PlayerCharacterState)base.State;
        public PlayerInputState InputState { get; } = new();
        public GameplayTagContainer GameplayTags { get; private set; }
        public PerformanceRunner Performance { get; private set; }

        [SerializeField, Min(0f)] private float _moveSpeed = 2.5f;

        public PerformanceToken MoveTo(Vector2 targetPosition) => Performance.MoveTo(targetPosition);

        protected override void InitializeCharacter(MovementRouteModel movementRoutes)
        {
            GameplayTags = new GameplayTagContainer(Tables.GameplayTags.Catalog);
            var context = new PerformanceContext(EntityId, Motor, movementRoutes, Spatial);
            Performance = new PerformanceRunner(GameplayTags, context);
        }

        protected override void CollectMovementRequests(float deltaTime)
        {
            var inputDirection = InputState.InputDir;
            if (inputDirection != Vector2.zero && !GameplayTags.Has(Tables.GameplayTags.Control.Block.Movement.Tag))
            {
                Motor.SubmitDelta(inputDirection.normalized * (_moveSpeed * deltaTime), MotionMode.Override);
            }

            Performance.Tick(deltaTime);
        }

        protected override void ReleaseCharacter()
        {
            Performance?.Release();
            InputState.Reset();
            GameplayTags?.Dispose();
        }
    }
}
