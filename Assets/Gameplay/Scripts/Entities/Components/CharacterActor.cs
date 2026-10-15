using Gameplay.Entities;
using UnityEngine;

namespace Gameplay
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Gameplay/Character Actor")]
    public sealed class CharacterActor : MonoBehaviour
    {
        public CharacterEntity Entity { get; private set; }
        public long EntityId { get; private set; }
        public CharacterKind Kind => Entity?.State.Kind ?? CharacterKind.Unknown;

        public bool IsBound => Entity != null;
        public bool HasValidEntity => Entity != null && Entity.IsValid && Entity.EntityId == EntityId;
        public bool IsPlayer => HasValidEntity && Kind == CharacterKind.Player;
        public bool IsNpc => HasValidEntity && Kind == CharacterKind.Npc;

        public void Bind(CharacterEntity entity)
        {
            if (entity == null)
            {
                Unbind();
                return;
            }

            Entity = entity;
            EntityId = entity.EntityId;
        }

        public void Unbind()
        {
            Entity = null;
            EntityId = 0;
        }

        public static bool TryGet(Collider2D collider, out CharacterActor actor)
        {
            actor = null;
            if (collider == null)
            {
                return false;
            }

            actor = collider.GetComponentInParent<CharacterActor>();
            if (actor == null || !actor.HasValidEntity)
            {
                actor = null;
                return false;
            }

            return true;
        }
    }
}
