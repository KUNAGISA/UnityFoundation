namespace Gameplay
{
    public readonly struct ConditionResultChangedEvent
    {
        public readonly int ConditionId;
        public readonly bool Result;

        public ConditionResultChangedEvent(int conditionId, bool result)
        {
            ConditionId = conditionId;
            Result = result;
        }
    }
}
