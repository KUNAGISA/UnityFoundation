using Aoiro;
using Gameplay.Tables;
using QFSW.QC;
using UnityEngine.Scripting;

namespace Gameplay.GM
{
    [CommandPrefix("switch.game.")]
    internal static class GameSwitchCommands
    {
        [Preserve]
        [Command("get", "读取开关当前值和默认值")]
        private static string GetSwitch(int id)
        {
            var value = GmContext.Shared.ExecuteQuery<GetGameSwitchQuery, bool>(new GetGameSwitchQuery(id));
            var entry = GmContext.Shared.GetModel<TablesModel>().GameSwitchTable.Get(id);
            return $"{id} ({entry.Desc}): {value}, 默认值: {entry.DefaultValue}";
        }

        [Preserve]
        [Command("set", "设置开关")]
        private static bool SetSwitch(int id, bool value)
        {
            return GmContext.Shared.ExecuteCommand<SetGameSwitchCommand, bool>(new SetGameSwitchCommand(id, value));
        }

        [Preserve]
        [Command("reset", "清除开关覆盖值，下次读取使用配表默认值")]
        private static bool ResetSwitch(int id)
        {
            var model = GmContext.Shared.GetModel<GameValueModel>();
            model.ClearSwitch(id);
            return true;
        }

        [Preserve]
        [Command("reset-all", "清除全部游戏开关覆盖值")]
        private static bool ResetAll()
        {
            var model = GmContext.Shared.GetModel<GameValueModel>();
            model.ClearSwitches();
            return true;
        }
    }
}
