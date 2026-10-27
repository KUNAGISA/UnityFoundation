using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct GetGameIntVariableQuery : IQuery<int>
    {
        private readonly int _id;

        public GetGameIntVariableQuery(int id)
        {
            _id = id;
        }

        int IQuery<int>.Execute(IQueryContext context)
        {
            if (!context.GetModel<TablesModel>().GameIntVariableTable.TryGet(_id, out var entry))
            {
                throw new InvalidOperationException($"[Variable] Game Int 变量 [{_id}] 未配置。");
            }
            return context.GetModel<GameVariableModel>().GetInt(_id, entry.DefaultValue);
        }
    }
}
