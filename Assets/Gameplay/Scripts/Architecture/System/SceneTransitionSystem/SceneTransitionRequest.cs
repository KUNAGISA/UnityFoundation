namespace Gameplay
{
    public readonly struct SceneTransitionRequest
    {
        public readonly int SceneId;
        public readonly SceneTransitionReason Reason;
        public readonly int? SpawnPointId;

        public SceneTransitionRequest(int sceneId, SceneTransitionReason reason = SceneTransitionReason.Unknow, int? spawnPointId = null)
        {
            SceneId = sceneId;
            Reason = reason;
            SpawnPointId = spawnPointId;
        }
        
        public static SceneTransitionRequest Trigger(int sceneId, int? spawnPointId = null) => new SceneTransitionRequest(sceneId, SceneTransitionReason.Trigger, spawnPointId);
        public static SceneTransitionRequest System(int sceneId, int? spawnPointId = null) => new SceneTransitionRequest(sceneId, SceneTransitionReason.System, spawnPointId);
    }
}
