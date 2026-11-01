using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct GetGlobalVariableQuery : IQuery<int>
    {
        private readonly int _id;

        public GetGlobalVariableQuery(int id)
        {
            _id = id;
        }

        int IQuery<int>.Execute(IQueryContext context)
        {
            if (!context.GetModel<TablesModel>().GlobalVariableTable.TryGet(_id, out var entry))
            {
                throw new InvalidOperationException($"[Variable] Global 变量 [{_id}] 未配置。");
            }
            return context.GetModel<GlobalValueModel>().GetVariable(_id, entry.DefaultValue);
        }
    }
}
