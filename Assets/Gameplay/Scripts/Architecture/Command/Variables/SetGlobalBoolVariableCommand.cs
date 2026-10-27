using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public readonly struct SetGlobalBoolVariableCommand : ICommand<bool>
    {
        private readonly int _id;
        private readonly bool _value;

        public SetGlobalBoolVariableCommand(int id, bool value)
        {
            _id = id;
            _value = value;
        }

        bool ICommand<bool>.Execute(ICommandContext context)
        {
            if (!context.GetModel<TablesModel>().GlobalBoolVariableTable.TryGet(_id, out _))
            {
                Debug.LogError($"[Variable] Global Bool 变量 [{_id}] 未配置。");
                return false;
            }
            var current = context.ExecuteQuery<GetGlobalBoolVariableQuery, bool>(new GetGlobalBoolVariableQuery(_id));
            var model = context.GetModel<GlobalVariableModel>();
            if (current != _value)
            {
                model.SetBool(_id, _value);
            }
            model.SaveIfDirty();
            return true;
        }
    }
}
