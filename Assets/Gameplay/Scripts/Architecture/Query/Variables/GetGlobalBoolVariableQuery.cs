using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct GetGlobalBoolVariableQuery : IQuery<bool>
    {
        private readonly int _id;

        public GetGlobalBoolVariableQuery(int id)
        {
            _id = id;
        }

        bool IQuery<bool>.Execute(IQueryContext context)
        {
            if (!context.GetModel<TablesModel>().GlobalBoolVariableTable.TryGet(_id, out var entry))
            {
                throw new InvalidOperationException($"[Variable] Global Bool 变量 [{_id}] 未配置。");
            }
            return context.GetModel<GlobalVariableModel>().GetBool(_id, entry.DefaultValue);
        }
    }
}
