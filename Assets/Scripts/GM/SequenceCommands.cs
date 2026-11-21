using Aoiro;
using QFSW.QC;
using UnityEngine.Scripting;

namespace Gameplay.GM
{
    [CommandPrefix("sequence.")]
    internal static class SequenceCommands
    {
        [Preserve]
        [Command("play", "播放剧情脚本")]
        [CloseConsoleAfterInvoke]
        private static void Play([CommandParameterDescription("演出Id")] int sequenceId)
        {
            GmContext.Shared.GetSystem<SequenceSystem>().Play(sequenceId);
        }

        [Preserve]
        [Command("stop", "停止播放剧情")]
        private static void Stop()
        {
            GmContext.Shared.GetSystem<SequenceSystem>().CancelCurrent();
        }
    }
}
