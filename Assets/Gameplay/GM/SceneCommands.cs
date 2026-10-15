using Aoiro;
using QFSW.QC;
using UnityEngine.Scripting;

namespace Gameplay.GM
{
    [CommandPrefix("scene.")]
    internal static class SceneCommands
    {
        [Preserve]
        [Command("transition", "切换当前场景")]
        private static void Transition([CommandParameterDescription("场景Id")] int sceneId)
        {
            GmContext.Shared.GetSystem<SceneTransitionSystem>().RequestTransition(new SceneTransitionRequest(sceneId, SceneTransitionReason.System));
        }
    }
}
