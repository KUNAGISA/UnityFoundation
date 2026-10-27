using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public readonly struct SetGlobalIntVariableCommand : ICommand<bool>
    {
        private readonly int _id;
        private readonly int _value;

        public SetGlobalIntVariableCommand(int id, int value)
        {
            _id = id;
            _value = value;
        }

        bool ICommand<bool>.Execute(ICommandContext context)
        {
            if (!context.GetModel<TablesModel>().GlobalIntVariableTable.TryGet(_id, out _))
            {
                Debug.LogError($"[Variable] Global Int 变量 [{_id}] 未配置。");
                return false;
            }
            var current = context.ExecuteQuery<GetGlobalIntVariableQuery, int>(new GetGlobalIntVariableQuery(_id));
            var model = context.GetModel<GlobalVariableModel>();
            if (current != _value)
            {
                model.SetInt(_id, _value);
            }
            model.SaveIfDirty();
            return true;
        }
    }
}
