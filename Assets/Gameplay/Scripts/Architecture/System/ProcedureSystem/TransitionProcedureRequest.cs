namespace Gameplay
{
    public enum TransitionProcedureReason
    {
        Unknow = -1,
        Startup = 0,
        Initialize = 1,
        Quit = 2,
        Load = 3,
    }
    
    public readonly struct TransitionProcedureRequest
    {
        public ProcedureType TargetProcedure { get; }
        public int Priority { get; }
        public TransitionProcedureReason Reason { get; init; }
#nullable enable
        public IProcedureEnterOptions? Options { get; init; }
#nullable disable
        
        public TransitionProcedureRequest(ProcedureType target, int priority = 0)
        {
            TargetProcedure = target;
            Priority = priority;
            Reason = TransitionProcedureReason.Unknow;
            Options = null;
        }
    }
}
