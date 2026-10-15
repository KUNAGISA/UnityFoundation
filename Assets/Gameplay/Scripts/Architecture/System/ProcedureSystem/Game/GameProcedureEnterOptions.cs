namespace Gameplay
{
    public class GameProcedureEnterOptions : IProcedureEnterOptions
    {
        public GameSaveData SaveData { get; }
        public int? InitialSceneId { get; }

        public GameProcedureEnterOptions(GameSaveData saveData = null, int? initialSceneId = null)
        {
            SaveData = saveData;
            InitialSceneId = initialSceneId;
        }
    }
}
