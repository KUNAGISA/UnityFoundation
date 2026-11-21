using Cysharp.Threading.Tasks;
using Aoiro;

namespace Gameplay
{
    public interface IProcedureCapacitySystem : ISystem
    {
        protected internal void Attach(IProcedureContext context);
        protected internal void Detach(IProcedureContext context);

        protected internal UniTask ReadyAsync(IProcedureContext context);
        protected internal UniTask TeardownAsync(IProcedureContext context);
    }
    
    public abstract class ProcedureCapacitySystem : SystemBase, IProcedureCapacitySystem
    {
        void IProcedureCapacitySystem.Attach(IProcedureContext context)
        {
            //上层Procedure确保只会有一次
            OnProcedureAttach(context);
        }

        void IProcedureCapacitySystem.Detach(IProcedureContext context)
        {
            //上层Procedure确保只会有一次
            OnProcedureDetach(context);
        }

        UniTask IProcedureCapacitySystem.ReadyAsync(IProcedureContext context)
        {
            return OnProcedureReadyAsync(context);
        }

        UniTask IProcedureCapacitySystem.TeardownAsync(IProcedureContext context)
        {
            return OnProcedureTeardownAsync(context);
        }

        protected virtual void OnProcedureAttach(IProcedureContext context) { }
        protected virtual void OnProcedureDetach(IProcedureContext context) { }

        protected virtual UniTask OnProcedureReadyAsync(IProcedureContext context) => UniTask.CompletedTask;
        protected virtual UniTask OnProcedureTeardownAsync(IProcedureContext context) => UniTask.CompletedTask;
    }
}
