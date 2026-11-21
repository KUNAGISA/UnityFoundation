using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct GetGlobalSwitchQuery : IQuery<bool>
    {
        private readonly int _id;

        public GetGlobalSwitchQuery(int id)
        {
            _id = id;
        }

        bool IQuery<bool>.Execute(IQueryContext context)
        {
            if (!context.GetModel<TablesModel>().GlobalSwitchTable.TryGet(_id, out var entry))
            {
                throw new InvalidOperationException($"[Switch] Global 开关 [{_id}] 未配置。");
            }
            return context.GetModel<GlobalValueModel>().GetSwitch(_id, entry.DefaultValue);
        }
    }
}
