namespace Gameplay
{
    public enum ProcedureState : byte
    {
        Idle = 0,
        Running = 1,
        Transitioning = 2,
        Failed = 3,
        ShuttingDown = 4,
    }
}
