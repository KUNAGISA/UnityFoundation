using UnityEngine;

namespace Gameplay
{
    public class GameProcedureEnterOptions : IProcedureEnterOptions
    {
        public GameSaveData SaveData { get; }
        public int? InitialSceneId { get; }
        public Vector2? PreviewPosition { get; }

        public GameProcedureEnterOptions(GameSaveData saveData = null, int? initialSceneId = null, Vector2? previewPosition = null)
        {
            SaveData = saveData;
            InitialSceneId = initialSceneId;
            PreviewPosition = previewPosition;
        }
    }
}
