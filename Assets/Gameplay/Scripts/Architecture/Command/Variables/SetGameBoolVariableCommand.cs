using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public readonly struct SetGameBoolVariableCommand : ICommand<bool>
    {
        private readonly int _id;
        private readonly bool _value;

        public SetGameBoolVariableCommand(int id, bool value)
        {
            _id = id;
            _value = value;
        }

        bool ICommand<bool>.Execute(ICommandContext context)
        {
            if (!context.GetModel<TablesModel>().GameBoolVariableTable.TryGet(_id, out _))
            {
                Debug.LogError($"[Variable] Game Bool 变量 [{_id}] 未配置。");
                return false;
            }
            var current = context.ExecuteQuery<GetGameBoolVariableQuery, bool>(new GetGameBoolVariableQuery(_id));
            if (current != _value)
            {
                context.GetModel<GameVariableModel>().SetBool(_id, _value);
            }
            return true;
        }
    }
}
