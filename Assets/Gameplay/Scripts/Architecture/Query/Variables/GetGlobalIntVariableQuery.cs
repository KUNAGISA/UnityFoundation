using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct GetGlobalIntVariableQuery : IQuery<int>
    {
        private readonly int _id;

        public GetGlobalIntVariableQuery(int id)
        {
            _id = id;
        }

        int IQuery<int>.Execute(IQueryContext context)
        {
            if (!context.GetModel<TablesModel>().GlobalIntVariableTable.TryGet(_id, out var entry))
            {
                throw new InvalidOperationException($"[Variable] Global Int 变量 [{_id}] 未配置。");
            }
            return context.GetModel<GlobalVariableModel>().GetInt(_id, entry.DefaultValue);
        }
    }
}
