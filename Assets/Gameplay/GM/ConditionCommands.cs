using Aoiro;
using QFSW.QC;
using UnityEngine.Scripting;

namespace Gameplay.GM
{
    [CommandPrefix("condition.")]
    internal static class ConditionCommands
    {
        [Preserve]
        [Command("refresh", "重建已观察条件的依赖并刷新结果缓存，不重新加载配表")]
        private static void Refresh()
        {
            GmContext.Shared.GetSystem<ConditionSystem>().RefreshAll();
        }
    }
}
