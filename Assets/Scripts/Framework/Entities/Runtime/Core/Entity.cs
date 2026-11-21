using System;
using UnityEngine;

namespace Gameplay.Entities
{
    public abstract class Entity : MonoBehaviour
    {
        private enum Lifecycle : byte
        {
            New,
            Initializing,
            Ready,
            PendingRemoval,
            Releasing,
            Released,
        }

        public bool IsPendingRemoval => _lifecycle == Lifecycle.PendingRemoval;
        public bool IsValid => _lifecycle == Lifecycle.Ready;
        public long EntityId { get; private set; }
        public Transform Root => transform;

        private Lifecycle _lifecycle;

        internal bool CanInitialize => _lifecycle == Lifecycle.New;

        internal void Initialize(long entityId)
        {
            if (!CanInitialize)
            {
                throw new InvalidOperationException($"Entity [{GetType().Name}] cannot initialize from state [{_lifecycle}].");
            }

            _lifecycle = Lifecycle.Initializing;
            EntityId = entityId;
            OnInitialize();
            _lifecycle = Lifecycle.Ready;
        }

        internal void Release()
        {
            if (_lifecycle is Lifecycle.New or Lifecycle.Releasing or Lifecycle.Released)
            {
                return;
            }

            _lifecycle = Lifecycle.Releasing;
            try
            {
                OnRelease();
            }
            finally
            {
                EntityId = 0;
                _lifecycle = Lifecycle.Released;
            }
        }

        internal bool TryMarkPendingRemoval()
        {
            if (!IsValid)
            {
                return false;
            }

            _lifecycle = Lifecycle.PendingRemoval;
            return true;
        }

        internal void FrameTick(float deltaTime) => OnFrameTick(deltaTime);
        internal void LateTick(float deltaTime) => OnLateTick(deltaTime);

        protected virtual void OnInitialize() { }
        protected virtual void OnFrameTick(float deltaTime) { }
        protected virtual void OnLateTick(float deltaTime) { }
        protected virtual void OnRelease() { }
    }
}
