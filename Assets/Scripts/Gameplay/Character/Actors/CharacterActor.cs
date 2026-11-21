using Gameplay.Entities;
using UnityEngine;

namespace Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterAnimation))]
    public abstract class CharacterActor : Entity
    {
        public CharacterState State { get; private set; }
        public SpatialState Spatial { get; private set; }
        public CharacterMotor Motor { get; private set; }
        public CharacterAnimation Animation => _animation;
        public bool IsPlayer => IsValid && State.Kind == CharacterKind.Player;
        public bool IsNpc => IsValid && State.Kind == CharacterKind.Npc;

        [SerializeField] private CharacterAnimation _animation;

        private MovementRouteModel _movementRoutes;

        internal void Prepare(CharacterState state, SpatialState spatial, MovementRouteModel movementRoutes, RouteLocation? routeLocation)
        {
            State = state;
            Spatial = spatial;
            _movementRoutes = movementRoutes;
            Motor = new CharacterMotor(spatial, Root, movementRoutes, routeLocation);
        }

        protected sealed override void OnInitialize()
        {
            _animation.Initialize();
            _animation.SetBaseAnimation(CharacterAnimationAction.Idle, Spatial.Facing);
            InitializeCharacter(_movementRoutes);
        }

        protected sealed override void OnFrameTick(float deltaTime)
        {
            CollectMovementRequests(deltaTime);
            Motor.ResolveMotion();
            var action = Motor.ActualDelta.sqrMagnitude > 1e-8f ? CharacterAnimationAction.Walk : CharacterAnimationAction.Idle;
            _animation.SetBaseAnimation(action, Spatial.Facing);
        }

        protected sealed override void OnRelease()
        {
            try
            {
                ReleaseCharacter();
            }
            finally
            {
                if (_animation != null)
                {
                    _animation.Release();
                }
            }
        }

        protected virtual void InitializeCharacter(MovementRouteModel movementRoutes) { }
        protected virtual void CollectMovementRequests(float deltaTime) { }
        protected virtual void ReleaseCharacter() { }

        public static bool TryGet(Collider2D collider, out CharacterActor actor)
        {
            actor = collider != null ? collider.GetComponentInParent<CharacterActor>() : null;
            if (actor != null && actor.IsValid)
            {
                return true;
            }

            actor = null;
            return false;
        }
    }
}
