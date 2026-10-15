namespace Gameplay
{
    public readonly struct LoadResult
    {
        public SaveOperationStatus Status { get; }
        public GameSaveData SaveData { get; }
        public string Error { get; }

        private LoadResult(SaveOperationStatus status, GameSaveData saveData, string error)
        {
            Status = status;
            SaveData = saveData;
            Error = error;
        }

        internal static LoadResult Succeeded(GameSaveData saveData)
        {
            return new LoadResult(SaveOperationStatus.Succeeded, saveData, null);
        }

        internal static LoadResult FromCancellation()
        {
            return new LoadResult(SaveOperationStatus.Canceled, null, null);
        }

        internal static LoadResult Failed(string error)
        {
            return new LoadResult(SaveOperationStatus.Failed, null, error);
        }
    }
}
