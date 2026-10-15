using Gameplay.Entities;

namespace Gameplay
{
    public readonly struct PerformanceContext
    {
        public Entity Owner { get; }
        public EntityContext EntityContext { get; }
        public SpatialState Spatial { get; }

        public PerformanceContext(Entity owner, EntityContext entityContext, SpatialState spatial)
        {
            Owner = owner;
            EntityContext = entityContext;
            Spatial = spatial;
        }
    }
}
