namespace Gameplay
{
    public readonly struct EnterProcedureSuccessEvent
    {
        public readonly ProcedureType ProcedureType;
        public readonly IProcedureContext Context;

        public EnterProcedureSuccessEvent(ProcedureType procedureType, IProcedureContext context)
        {
            ProcedureType = procedureType;
            Context = context;
        }
    }
}
