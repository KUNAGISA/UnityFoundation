using System;
using System.Collections.Generic;
using Gameplay.Common;

namespace Gameplay.Entities
{
    public sealed class AbilityContainer
    {
        private readonly Entity _owner;
        private EntityContext _context;
        private readonly Dictionary<Type, Ability> _abilities = new();
        private readonly List<Ability> _orderedAbilities = new();
        private readonly List<IAbilityFrameTickable> _frameTickAbilities = new();
        private readonly List<IAbilityLateTickable> _lateTickAbilities = new();
        private uint _version = uint.MinValue;

        internal AbilityContainer(Entity owner)
        {
            _owner = owner;
        }

        internal void Initialize(EntityContext context)
        {
            _context = context;
        }

        internal void Destroy()
        {
            ++_version;
            _frameTickAbilities.Clear();
            _lateTickAbilities.Clear();
            var exceptions = new CleanupExceptionCollector();
            for (var index = _orderedAbilities.Count - 1; index >= 0; --index)
            {
                try
                {
                    _orderedAbilities[index].PrepareDestroy();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }
            for (var index = _orderedAbilities.Count - 1; index >= 0; --index)
            {
                try
                {
                    _orderedAbilities[index].Destroy();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }
            _orderedAbilities.Clear();
            _abilities.Clear();
            _context = null;
            exceptions.ThrowIfAny();
        }

        internal void EnterReady()
        {
            for (var index = 0; index < _orderedAbilities.Count; ++index)
            {
                _orderedAbilities[index].PrepareReady();
            }
            for (var index = 0; index < _orderedAbilities.Count; ++index)
            {
                _orderedAbilities[index].CompleteReady();
            }
        }

        public TAbility Add<TAbility>(TAbility ability) where TAbility : Ability
        {
            if (!_abilities.TryAdd(typeof(TAbility), ability))
            {
                throw new InvalidOperationException($"重复添加Ability[{typeof(TAbility)}] EntityId[{_owner.EntityId}]");
            }
            try
            {
                ability.Initialize(_owner, _context);
                _orderedAbilities.Add(ability);
                RegisterTickables(ability);
                return ability;
            }
            catch (Exception initializeException)
            {
                _abilities.Remove(typeof(TAbility));
                var exceptions = new CleanupExceptionCollector();
                exceptions.Add(initializeException);
                try
                {
                    ability.Destroy();
                }
                catch (Exception rollbackException)
                {
                    exceptions.Add(rollbackException);
                }
                exceptions.ThrowIfAny();
                return null;
            }
        }

        public TAbility Get<TAbility>() where TAbility : Ability
        {
            return _abilities.TryGetValue(typeof(TAbility), out var ability) ? (TAbility)ability : null;
        }

        public TAbility Require<TAbility>() where TAbility : Ability
        {
            if (_abilities.TryGetValue(typeof(TAbility), out var ability))
            {
                return (TAbility)ability;
            }
            throw new InvalidOperationException($"Entity {_owner.EntityId} requires ability {typeof(TAbility).Name}, but it was not found.");
        }

        public bool TryGet<TAbility>(out TAbility ability) where TAbility : Ability
        {
            ability = null;
            if (_abilities.TryGetValue(typeof(TAbility), out var value))
            {
                ability = (TAbility)value;
            }
            return ability != null;
        }

        internal void FrameTick(float deltaTime)
        {
            var version = _version;
            var count = _frameTickAbilities.Count;
            for (var index = 0; index < count && version == _version; ++index)
            {
                var frameTickAbility = _frameTickAbilities[index];
                if (frameTickAbility.IsEnabled)
                {
                    frameTickAbility.FrameTick(deltaTime);
                }
            }
        }

        internal void LateTick(float deltaTime)
        {
            var version = _version;
            var count = _lateTickAbilities.Count;
            for (var index = 0; index < count && version == _version; ++index)
            {
                var lateTickAbility = _lateTickAbilities[index];
                if (lateTickAbility.IsEnabled)
                {
                    lateTickAbility.LateTick(deltaTime);
                }
            }
        }

        private void RegisterTickables(Ability ability)
        {
            if (ability is IAbilityFrameTickable frameTickAbility)
            {
                _frameTickAbilities.Add(frameTickAbility);
            }
            if (ability is IAbilityLateTickable lateTickAbility)
            {
                _lateTickAbilities.Add(lateTickAbility);
            }
        }
    }
}
