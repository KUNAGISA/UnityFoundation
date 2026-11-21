namespace Gameplay
{
    public enum GameSaveDataCheckResult : byte
    {
        Success,
        EmptyData,
        MissingPlayer,
        InvalidPlayerId,
        MissingPlayerSpatial,
        MissingPlayerRoute,
        InvalidScene,
    }
}
