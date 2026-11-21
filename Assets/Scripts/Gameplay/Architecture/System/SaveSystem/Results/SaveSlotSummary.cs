namespace Gameplay
{
    public enum SaveSlotState : byte
    {
        Empty,
        Available,
        Invalid,
    }

    public readonly struct SaveSlotSummary
    {
        public SaveSlotState State { get; }
        public int SceneId { get; }
        public long SaveTimeUtcTicks { get; }

        private SaveSlotSummary(SaveSlotState state, int sceneId, long saveTimeUtcTicks)
        {
            State = state;
            SceneId = sceneId;
            SaveTimeUtcTicks = saveTimeUtcTicks;
        }

        internal static SaveSlotSummary Empty()
        {
            return new SaveSlotSummary(SaveSlotState.Empty, default, default);
        }

        internal static SaveSlotSummary Available(int sceneId, long saveTimeUtcTicks)
        {
            return new SaveSlotSummary(SaveSlotState.Available, sceneId, saveTimeUtcTicks);
        }

        internal static SaveSlotSummary Invalid()
        {
            return new SaveSlotSummary(SaveSlotState.Invalid, default, default);
        }
    }
}
