using Aoiro;
using Gameplay.Tables;
using QFSW.QC;
using UnityEngine.Scripting;

namespace Gameplay.GM
{
    [CommandPrefix("variable.global.")]
    internal static class GlobalVariableCommands
    {
        [Preserve]
        [Command("get-int", "读取变量当前值和默认值")]
        private static string GetInt(int id)
        {
            var value = GmContext.Shared.ExecuteQuery<GetGlobalIntVariableQuery, int>(new GetGlobalIntVariableQuery(id));
            var entry = GmContext.Shared.GetModel<TablesModel>().GlobalIntVariableTable.Get(id);
            return $"{id} ({entry.Desc}): {value}, 默认值: {entry.DefaultValue}";
        }

        [Preserve]
        [Command("set-int", "设置变量")]
        private static bool SetInt(int id, int value)
        {
            return GmContext.Shared.ExecuteCommand<SetGlobalIntVariableCommand, bool>(new SetGlobalIntVariableCommand(id, value));
        }

        [Preserve]
        [Command("reset-int", "清除变量覆盖值，下次读取使用配表默认值")]
        private static bool ResetInt(int id)
        {
            var model = GmContext.Shared.GetModel<GlobalVariableModel>();
            model.ClearInt(id);
            model.SaveIfDirty();
            return true;
        }

        [Preserve]
        [Command("get-bool", "读取变量当前值和默认值")]
        private static string GetBool(int id)
        {
            var value = GmContext.Shared.ExecuteQuery<GetGlobalBoolVariableQuery, bool>(new GetGlobalBoolVariableQuery(id));
            var entry = GmContext.Shared.GetModel<TablesModel>().GlobalBoolVariableTable.Get(id);
            return $"{id} ({entry.Desc}): {value}, 默认值: {entry.DefaultValue}";
        }

        [Preserve]
        [Command("set-bool", "设置变量")]
        private static bool SetBool(int id, bool value)
        {
            return GmContext.Shared.ExecuteCommand<SetGlobalBoolVariableCommand, bool>(new SetGlobalBoolVariableCommand(id, value));
        }

        [Preserve]
        [Command("reset-bool", "清除变量覆盖值，下次读取使用配表默认值")]
        private static bool ResetBool(int id)
        {
            var model = GmContext.Shared.GetModel<GlobalVariableModel>();
            model.ClearBool(id);
            model.SaveIfDirty();
            return true;
        }

        [Preserve]
        [Command("reset-all", "清除全部全局变量覆盖值")]
        private static bool ResetAll()
        {
            var model = GmContext.Shared.GetModel<GlobalVariableModel>();
            model.Clear();
            model.SaveIfDirty();
            return true;
        }
    }
}
