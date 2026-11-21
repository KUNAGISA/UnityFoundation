using Aoiro;
using Gameplay.Tables;
using QFSW.QC;
using UnityEngine.Scripting;

namespace Gameplay.GM
{
    [CommandPrefix("variable.game.")]
    internal static class GameVariableCommands
    {
        [Preserve]
        [Command("get", "读取变量当前值和默认值")]
        private static string GetVariable(int id)
        {
            var value = GmContext.Shared.ExecuteQuery<GetGameVariableQuery, int>(new GetGameVariableQuery(id));
            var entry = GmContext.Shared.GetModel<TablesModel>().GameVariableTable.Get(id);
            return $"{id} ({entry.Desc}): {value}, 默认值: {entry.DefaultValue}";
        }

        [Preserve]
        [Command("set", "设置变量")]
        private static bool SetVariable(int id, int value)
        {
            return GmContext.Shared.ExecuteCommand<SetGameVariableCommand, bool>(new SetGameVariableCommand(id, value));
        }

        [Preserve]
        [Command("reset", "清除变量覆盖值，下次读取使用配表默认值")]
        private static bool ResetVariable(int id)
        {
            var model = GmContext.Shared.GetModel<GameValueModel>();
            model.ClearVariable(id);
            return true;
        }

        [Preserve]
        [Command("reset-all", "清除全部游戏变量覆盖值")]
        private static bool ResetAll()
        {
            var model = GmContext.Shared.GetModel<GameValueModel>();
            model.ClearVariables();
            return true;
        }
    }
}
