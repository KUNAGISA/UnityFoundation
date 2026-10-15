using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aoiro;

namespace Gameplay
{
    public class GameStateModel : ModelBase
    {
        private readonly HashSet<int> _completedSceneEvents = new();
        public HashSet<int> CompletedSceneEvents => _completedSceneEvents;
        
        public int CurrentSceneId { get; private set; }
        
        public bool IsSceneEventCompleted(int eventId)
        {
            return _completedSceneEvents.Contains(eventId);
        }

        internal void SetCurrentScene(int sceneId)
        {
            CurrentSceneId = sceneId;
        }

        internal void ClearCurrentScene()
        {
            CurrentSceneId = default;
        }

        internal void Reset()
        {
            CurrentSceneId = default;
            _completedSceneEvents.Clear();
        }

        internal bool MarkSceneEventCompleted(int eventId)
        {
            return _completedSceneEvents.Add(eventId);
        }

        internal void LoadCompletedSceneEvents(IEnumerable<int> eventIds)
        {
            _completedSceneEvents.Clear();
            if (eventIds != null)
            {
                foreach (var eventId in eventIds)
                {
                    _completedSceneEvents.Add(eventId);
                }
            }
        }
    }
}
