using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public readonly struct SetGlobalVariableCommand : ICommand<bool>
    {
        private readonly int _id;
        private readonly int _value;

        public SetGlobalVariableCommand(int id, int value)
        {
            _id = id;
            _value = value;
        }

        bool ICommand<bool>.Execute(ICommandContext context)
        {
            if (!context.GetModel<TablesModel>().GlobalVariableTable.TryGet(_id, out _))
            {
                Debug.LogError($"[Variable] Global 变量 [{_id}] 未配置。");
                return false;
            }
            var current = context.ExecuteQuery<GetGlobalVariableQuery, int>(new GetGlobalVariableQuery(_id));
            var model = context.GetModel<GlobalValueModel>();
            if (current != _value)
            {
                model.SetVariable(_id, _value);
            }
            model.SaveIfDirty();
            return true;
        }
    }
}
