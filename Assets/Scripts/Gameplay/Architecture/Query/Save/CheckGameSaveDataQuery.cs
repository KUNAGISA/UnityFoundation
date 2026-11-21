using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct CheckGameSaveDataQuery : IQuery<GameSaveDataCheckResult>
    {
        private readonly GameSaveData _saveData;

        public CheckGameSaveDataQuery(GameSaveData saveData)
        {
            _saveData = saveData;
        }

        GameSaveDataCheckResult IQuery<GameSaveDataCheckResult>.Execute(IQueryContext context)
        {
            if (_saveData == null)
            {
                return GameSaveDataCheckResult.EmptyData;
            }
            if (_saveData.PlayerCharacter == null)
            {
                return GameSaveDataCheckResult.MissingPlayer;
            }
            if (_saveData.PlayerCharacter.Id != PlayerCharacterState.PlayerId)
            {
                return GameSaveDataCheckResult.InvalidPlayerId;
            }
            if (_saveData.PlayerCharacter.Spatial == null)
            {
                return GameSaveDataCheckResult.MissingPlayerSpatial;
            }
            if (!_saveData.PlayerCharacter.Spatial.RouteId.HasValue)
            {
                return GameSaveDataCheckResult.MissingPlayerRoute;
            }
            if (_saveData.SceneId <= 0 || !context.GetModel<TablesModel>().SceneTable.TryGet(_saveData.SceneId, out _))
            {
                return GameSaveDataCheckResult.InvalidScene;
            }

            return GameSaveDataCheckResult.Success;
        }
    }
}
