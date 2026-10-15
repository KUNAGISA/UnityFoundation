using Aoiro;

namespace Gameplay
{
    public interface IGameplayController : ISystemAccessor, IModelAccessor, IServiceAccessor, IEventSubscriber, ICommandExecutor, IQueryExecutor, IEventPublisher
    {
        IArchitecture IArchitectureProvider.GetArchitecture() => GameplayArchitecture.Instance;
    }
}
