using Gameplay.Tables;

namespace Gameplay
{
    internal sealed class SceneEventState
    {
        public int EventId;
        public SceneEventEntry Config;
        public SceneEventPoint Point;
        public bool IsTriggerable;
        public bool ConditionResult;
        public ConditionObservationToken Observation;
    }
}
