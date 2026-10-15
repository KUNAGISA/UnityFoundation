using UnityEngine;

namespace Gameplay.Entities
{
    public readonly ref struct EntityReleaseContext
    {
        public readonly Entity Entity;
        public readonly Transform Root;

        internal EntityReleaseContext(Entity entity, Transform root)
        {
            Entity = entity;
            Root = root;
        }

        internal static EntityReleaseContext Create(Entity entity)
        {
            return new EntityReleaseContext(entity, entity.Root);
        }
    }
}
