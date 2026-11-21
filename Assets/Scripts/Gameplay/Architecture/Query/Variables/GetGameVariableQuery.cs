using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct GetGameVariableQuery : IQuery<int>
    {
        private readonly int _id;

        public GetGameVariableQuery(int id)
        {
            _id = id;
        }

        int IQuery<int>.Execute(IQueryContext context)
        {
            if (!context.GetModel<TablesModel>().GameVariableTable.TryGet(_id, out var entry))
            {
                throw new InvalidOperationException($"[Variable] Game 变量 [{_id}] 未配置。");
            }
            return context.GetModel<GameValueModel>().GetVariable(_id, entry.DefaultValue);
        }
    }
}
