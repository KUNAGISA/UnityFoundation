using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct GetGameSwitchQuery : IQuery<bool>
    {
        private readonly int _id;

        public GetGameSwitchQuery(int id)
        {
            _id = id;
        }

        bool IQuery<bool>.Execute(IQueryContext context)
        {
            if (!context.GetModel<TablesModel>().GameSwitchTable.TryGet(_id, out var entry))
            {
                throw new InvalidOperationException($"[Switch] Game 开关 [{_id}] 未配置。");
            }
            return context.GetModel<GameValueModel>().GetSwitch(_id, entry.DefaultValue);
        }
    }
}
