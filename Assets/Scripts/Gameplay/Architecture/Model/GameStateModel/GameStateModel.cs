using Aoiro;

namespace Gameplay
{
    public class GameStateModel : ModelBase
    {
        public int CurrentSceneId { get; private set; }

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
        }
    }
}
