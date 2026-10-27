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
        [Command("get-int", "读取变量当前值和默认值")]
        private static string GetInt(int id)
        {
            var value = GmContext.Shared.ExecuteQuery<GetGameIntVariableQuery, int>(new GetGameIntVariableQuery(id));
            var entry = GmContext.Shared.GetModel<TablesModel>().GameIntVariableTable.Get(id);
            return $"{id} ({entry.Desc}): {value}, 默认值: {entry.DefaultValue}";
        }

        [Preserve]
        [Command("set-int", "设置变量")]
        private static bool SetInt(int id, int value)
        {
            return GmContext.Shared.ExecuteCommand<SetGameIntVariableCommand, bool>(new SetGameIntVariableCommand(id, value));
        }

        [Preserve]
        [Command("reset-int", "清除变量覆盖值，下次读取使用配表默认值")]
        private static bool ResetInt(int id)
        {
            var model = GmContext.Shared.GetModel<GameVariableModel>();
            model.ClearInt(id);
            return true;
        }

        [Preserve]
        [Command("get-bool", "读取变量当前值和默认值")]
        private static string GetBool(int id)
        {
            var value = GmContext.Shared.ExecuteQuery<GetGameBoolVariableQuery, bool>(new GetGameBoolVariableQuery(id));
            var entry = GmContext.Shared.GetModel<TablesModel>().GameBoolVariableTable.Get(id);
            return $"{id} ({entry.Desc}): {value}, 默认值: {entry.DefaultValue}";
        }

        [Preserve]
        [Command("set-bool", "设置变量")]
        private static bool SetBool(int id, bool value)
        {
            return GmContext.Shared.ExecuteCommand<SetGameBoolVariableCommand, bool>(new SetGameBoolVariableCommand(id, value));
        }

        [Preserve]
        [Command("reset-bool", "清除变量覆盖值，下次读取使用配表默认值")]
        private static bool ResetBool(int id)
        {
            var model = GmContext.Shared.GetModel<GameVariableModel>();
            model.ClearBool(id);
            return true;
        }

        [Preserve]
        [Command("reset-all", "清除全部游戏变量覆盖值")]
        private static bool ResetAll()
        {
            var model = GmContext.Shared.GetModel<GameVariableModel>();
            model.Clear();
            return true;
        }
    }
}
