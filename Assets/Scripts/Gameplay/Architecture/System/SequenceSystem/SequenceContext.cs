using Aoiro;

namespace Gameplay.Sequence
{
    public interface ISequenceContext : ISystemAccessor, IModelAccessor, IServiceAccessor, ICommandExecutor, IQueryExecutor
    {
    }

    internal sealed class SequenceContext : ArchitectureContext, ISequenceContext
    {
    }
}
