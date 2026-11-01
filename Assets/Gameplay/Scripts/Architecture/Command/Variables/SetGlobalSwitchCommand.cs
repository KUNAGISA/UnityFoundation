using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public readonly struct SetGlobalSwitchCommand : ICommand<bool>
    {
        private readonly int _id;
        private readonly bool _value;

        public SetGlobalSwitchCommand(int id, bool value)
        {
            _id = id;
            _value = value;
        }

        bool ICommand<bool>.Execute(ICommandContext context)
        {
            if (!context.GetModel<TablesModel>().GlobalSwitchTable.TryGet(_id, out _))
            {
                Debug.LogError($"[Switch] Global 开关 [{_id}] 未配置。");
                return false;
            }
            var current = context.ExecuteQuery<GetGlobalSwitchQuery, bool>(new GetGlobalSwitchQuery(_id));
            var model = context.GetModel<GlobalValueModel>();
            if (current != _value)
            {
                model.SetSwitch(_id, _value);
            }
            model.SaveIfDirty();
            return true;
        }
    }
}
