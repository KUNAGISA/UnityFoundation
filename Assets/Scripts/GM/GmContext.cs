using Aoiro;

namespace Gameplay.GM
{
    internal sealed class GmContext : ISystemAccessor, IModelAccessor, IServiceAccessor, ICommandExecutor, IQueryExecutor, IEventPublisher
    {
        internal static readonly GmContext Shared = new();

        private GmContext() { }

        IArchitecture IArchitectureProvider.GetArchitecture() => GameplayArchitecture.Instance;
    }
}
