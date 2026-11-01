using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public readonly struct SetGameSwitchCommand : ICommand<bool>
    {
        private readonly int _id;
        private readonly bool _value;

        public SetGameSwitchCommand(int id, bool value)
        {
            _id = id;
            _value = value;
        }

        bool ICommand<bool>.Execute(ICommandContext context)
        {
            if (!context.GetModel<TablesModel>().GameSwitchTable.TryGet(_id, out _))
            {
                Debug.LogError($"[Switch] Game 开关 [{_id}] 未配置。");
                return false;
            }
            var current = context.ExecuteQuery<GetGameSwitchQuery, bool>(new GetGameSwitchQuery(_id));
            if (current != _value)
            {
                context.GetModel<GameValueModel>().SetSwitch(_id, _value);
            }
            return true;
        }
    }
}
