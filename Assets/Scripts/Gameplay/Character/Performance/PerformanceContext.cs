namespace Gameplay
{
    public readonly struct PerformanceContext
    {
        public long EntityId { get; }
        public CharacterMotor Motor { get; }
        public MovementRouteModel MovementRoutes { get; }
        public SpatialState Spatial { get; }

        public PerformanceContext(long entityId, CharacterMotor motor, MovementRouteModel movementRoutes, SpatialState spatial)
        {
            EntityId = entityId;
            Motor = motor;
            MovementRoutes = movementRoutes;
            Spatial = spatial;
        }
    }
}
