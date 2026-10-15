using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [DefinitionResolver(typeof(PlaySequenceActionDef))]
    public static class PlaySequenceActionResolver
    {
        public static bool Execute(PlaySequenceActionDef action, ICommandContext context)
        {
            var playback = context.GetSystem<SequenceSystem>().Play(action.SequenceId);
            return playback.Status != SequencePlaybackStatus.Failed;
        }
    }
}
