using System;
using System.Runtime.CompilerServices;
using Gameplay.Common;
using UnityEngine;

namespace Gameplay.Entities
{
    public abstract class Entity
    {
        private enum State : byte
        {
            New,
            Initializing,
            Ready,
            PendingRemoval,
            Destroying,
            Destroyed,
        }

        public bool IsDestroyed => _state == State.Destroyed;
        public bool IsPendingRemoval => _state == State.PendingRemoval;
        public bool IsValid => _state == State.Ready;

        public long EntityId { get; private set; }
        public Transform Root { get; private set; }
        protected EntityContext Context { get; private set; }

        private readonly AbilityContainer _abilities;
        private State _state = State.New;

        internal bool CanInitialize => _state == State.New;

        protected Entity()
        {
            _abilities = new AbilityContainer(this);
        }

        internal void Initialize(EntityContext context, long entityId, Transform root)
        {
            if (_state != State.New)
            {
                throw new InvalidOperationException($"Entity [{GetType().Name}] cannot be initialized from state [{_state}].");
            }

            _state = State.Initializing;

            Context = context;
            EntityId = entityId;
            Root = root;

            _abilities.Initialize(context);
            BuildAbilities(_abilities);
            OnInitialize();
            _abilities.EnterReady();
            _state = State.Ready;
        }

        internal void Destroy()
        {
            if (_state == State.Destroyed || _state == State.Destroying || _state == State.New)
            {
                return;
            }

            _state = State.Destroying;
            var exceptions = new CleanupExceptionCollector();
            try
            {
                _abilities.Destroy();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }

            try
            {
                OnDestroy();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }

            _state = State.Destroyed;
            EntityId = 0;
            Root = null;
            Context = null;
            exceptions.ThrowIfAny();
        }

        internal bool TryMarkPendingRemoval()
        {
            if (_state != State.Ready)
            {
                return false;
            }

            _state = State.PendingRemoval;
            return true;
        }

        internal void FrameTick(float deltaTime)
        {
            _abilities.FrameTick(deltaTime);
            OnFrameTick(deltaTime);
        }

        internal void LateTick(float deltaTime)
        {
            _abilities.LateTick(deltaTime);
            OnLateTick(deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TAbility RequireAbility<TAbility>() where TAbility : Ability
        {
            return _abilities.Require<TAbility>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetAbility<TAbility>(out TAbility ability) where TAbility : Ability
        {
            return _abilities.TryGet(out ability);
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnFrameTick(float deltaTime) { }
        protected virtual void OnLateTick(float deltaTime) { }
        protected virtual void OnDestroy() { }
        protected abstract void BuildAbilities(AbilityContainer abilities);
    }
}
