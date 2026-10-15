using Aoiro;

namespace Gameplay.Entities
{
    public sealed class EntityContext : ArchitectureContext, ISystemAccessor, IModelAccessor, IServiceAccessor, ICommandExecutor, IQueryExecutor, IEventPublisher, IEventSubscriber
    {
    }
}
