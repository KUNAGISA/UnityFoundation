using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public readonly struct SetGameVariableCommand : ICommand<bool>
    {
        private readonly int _id;
        private readonly int _value;

        public SetGameVariableCommand(int id, int value)
        {
            _id = id;
            _value = value;
        }

        bool ICommand<bool>.Execute(ICommandContext context)
        {
            if (!context.GetModel<TablesModel>().GameVariableTable.TryGet(_id, out _))
            {
                Debug.LogError($"[Variable] Game 变量 [{_id}] 未配置。");
                return false;
            }
            var current = context.ExecuteQuery<GetGameVariableQuery, int>(new GetGameVariableQuery(_id));
            if (current != _value)
            {
                context.GetModel<GameValueModel>().SetVariable(_id, _value);
            }
            return true;
        }
    }
}
