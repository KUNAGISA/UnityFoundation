using System.Collections.Generic;
using Aoiro;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Gameplay/Interaction Area")]
    public sealed class InteractionArea : MonoBehaviour, IGameplayController
    {
        [SerializeField, LabelText("交互来源")]
        private MonoBehaviour _source;

        [SerializeField, LabelText("交互范围")]
        private Collider2D _collider;

        private readonly HashSet<int> _playerColliderIds = new();
        private InteractionSystem _interactionSystem;
        private InteractionToken _interaction;

        private void Reset()
        {
            _collider = GetComponent<Collider2D>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            var components = GetComponents<MonoBehaviour>();
            for (var index = 0; index < components.Length; index++)
            {
                if (components[index] is IInteractionSource)
                {
                    _source = components[index];
                    break;
                }
            }
        }

        private void Awake()
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
        }

        private void OnDisable()
        {
            _playerColliderIds.Clear();
            _interactionSystem?.Unregister(_interaction);
            _interaction = default;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CharacterActor.TryGet(other, out var actor) || !actor.IsPlayer)
            {
                return;
            }

            if (!_playerColliderIds.Add(other.GetInstanceID()) || _playerColliderIds.Count != 1)
            {
                return;
            }

            if (_source is not IInteractionSource source)
            {
                Debug.LogError($"[Interaction] Interaction area [{name}] has no valid interaction source.");
                return;
            }

            _interactionSystem ??= this.GetSystem<InteractionSystem>();
            _interaction = _interactionSystem.Register(source);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_playerColliderIds.Remove(other.GetInstanceID()) || _playerColliderIds.Count != 0)
            {
                return;
            }

            _interactionSystem?.Unregister(_interaction);
            _interaction = default;
        }

        public bool Refresh()
        {
            return _interactionSystem != null && _interactionSystem.Refresh(_interaction);
        }
    }
}
