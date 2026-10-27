using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public readonly struct SetGameIntVariableCommand : ICommand<bool>
    {
        private readonly int _id;
        private readonly int _value;

        public SetGameIntVariableCommand(int id, int value)
        {
            _id = id;
            _value = value;
        }

        bool ICommand<bool>.Execute(ICommandContext context)
        {
            if (!context.GetModel<TablesModel>().GameIntVariableTable.TryGet(_id, out _))
            {
                Debug.LogError($"[Variable] Game Int 变量 [{_id}] 未配置。");
                return false;
            }
            var current = context.ExecuteQuery<GetGameIntVariableQuery, int>(new GetGameIntVariableQuery(_id));
            if (current != _value)
            {
                context.GetModel<GameVariableModel>().SetInt(_id, _value);
            }
            return true;
        }
    }
}
