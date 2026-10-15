using Aoiro;

namespace Gameplay.Editor
{
    public interface IEditorController : ISystemAccessor, IModelAccessor, IServiceAccessor, IEventSubscriber, ICommandExecutor, IQueryExecutor, IEventPublisher
    {
        IArchitecture IArchitectureProvider.GetArchitecture() => EditorArchitecture.Instance;
    }
}