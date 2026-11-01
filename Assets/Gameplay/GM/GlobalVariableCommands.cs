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
        [Command("get", "读取变量当前值和默认值")]
        private static string GetVariable(int id)
        {
            var value = GmContext.Shared.ExecuteQuery<GetGlobalVariableQuery, int>(new GetGlobalVariableQuery(id));
            var entry = GmContext.Shared.GetModel<TablesModel>().GlobalVariableTable.Get(id);
            return $"{id} ({entry.Desc}): {value}, 默认值: {entry.DefaultValue}";
        }

        [Preserve]
        [Command("set", "设置变量")]
        private static bool SetVariable(int id, int value)
        {
            return GmContext.Shared.ExecuteCommand<SetGlobalVariableCommand, bool>(new SetGlobalVariableCommand(id, value));
        }

        [Preserve]
        [Command("reset", "清除变量覆盖值，下次读取使用配表默认值")]
        private static bool ResetVariable(int id)
        {
            var model = GmContext.Shared.GetModel<GlobalValueModel>();
            model.ClearVariable(id);
            model.SaveIfDirty();
            return true;
        }

        [Preserve]
        [Command("reset-all", "清除全部全局变量覆盖值")]
        private static bool ResetAll()
        {
            var model = GmContext.Shared.GetModel<GlobalValueModel>();
            model.ClearVariables();
            model.SaveIfDirty();
            return true;
        }
    }
}
