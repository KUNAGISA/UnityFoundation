using System;

namespace Gameplay.Entities
{
    public abstract class Ability
    {
        private enum State : byte
        {
            New,
            Initializing,
            Initialized,
            Prepared,
            Ready,
            Destroying,
            Destroyed,
        }

        public Entity Owner { get; private set; }
        public bool IsEnabled { get; private set; } = true;

        protected EntityContext Context { get; private set; }

        private State _state = State.New;

        internal void Initialize(Entity owner, EntityContext context)
        {
            if (_state != State.New)
            {
                throw new InvalidOperationException($"Ability [{GetType().Name}] cannot be initialized from state [{_state}].");
            }

            Owner = owner;
            Context = context;
            IsEnabled = true;
            _state = State.Initializing;
            OnInitialize();
            _state = State.Initialized;
        }

        internal void PrepareDestroy()
        {
            if (_state == State.Destroyed || _state == State.Destroying || _state == State.New)
            {
                return;
            }

            var wasEnabled = _state == State.Ready && IsEnabled;
            _state = State.Destroying;
            if (wasEnabled)
            {
                OnDisabled();
            }
        }

        internal void Destroy()
        {
            if (_state == State.Destroyed || _state == State.New)
            {
                return;
            }

            PrepareDestroy();
            try
            {
                OnDestroy();
            }
            finally
            {
                _state = State.Destroyed;
                IsEnabled = false;
                Owner = null;
                Context = null;
            }
        }

        internal void PrepareReady()
        {
            if (_state == State.Prepared || _state == State.Ready)
            {
                return;
            }
            if (_state != State.Initialized)
            {
                throw new InvalidOperationException($"Ability [{GetType().Name}] cannot prepare for ready from state [{_state}].");
            }

            OnReady();
            _state = State.Prepared;
        }

        internal void CompleteReady()
        {
            if (_state != State.Prepared)
            {
                throw new InvalidOperationException($"Ability [{GetType().Name}] cannot complete ready from state [{_state}].");
            }

            _state = State.Ready;
            if (IsEnabled)
            {
                Enable();
            }
        }

        public void SetEnabled(bool enabled)
        {
            if (IsEnabled == enabled)
            {
                return;
            }

            IsEnabled = enabled;
            if (_state != State.Ready)
            {
                return;
            }

            if (enabled)
            {
                Enable();
            }
            else
            {
                OnDisabled();
            }
        }

        private void Enable()
        {
            try
            {
                OnEnabled();
            }
            catch
            {
                IsEnabled = false;
                throw;
            }
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnReady() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnEnabled() { }
        protected virtual void OnDisabled() { }
    }
}
