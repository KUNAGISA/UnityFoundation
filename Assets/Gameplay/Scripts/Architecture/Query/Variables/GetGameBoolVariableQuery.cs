using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct GetGameBoolVariableQuery : IQuery<bool>
    {
        private readonly int _id;

        public GetGameBoolVariableQuery(int id)
        {
            _id = id;
        }

        bool IQuery<bool>.Execute(IQueryContext context)
        {
            if (!context.GetModel<TablesModel>().GameBoolVariableTable.TryGet(_id, out var entry))
            {
                throw new InvalidOperationException($"[Variable] Game Bool 变量 [{_id}] 未配置。");
            }
            return context.GetModel<GameVariableModel>().GetBool(_id, entry.DefaultValue);
        }
    }
}
