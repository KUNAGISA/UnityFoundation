using System;
using Aoiro;

namespace Gameplay
{
    public readonly struct EnterGameCommand : ICommand
    {
        private readonly TransitionProcedureReason _reason;
        private readonly GameSaveData _saveData;

        public EnterGameCommand(TransitionProcedureReason reason = TransitionProcedureReason.Unknow)
        {
            _reason = reason;
            _saveData = null;
        }

        public EnterGameCommand(GameSaveData saveData)
        {
            _saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
            _reason = TransitionProcedureReason.Load;
        }

        void ICommand.Execute(ICommandContext context)
        {
            context.GetSystem<ProcedureSystem>().Transition(new TransitionProcedureRequest(ProcedureType.Game)
            {
                Reason = _reason,
                Options = new GameProcedureEnterOptions(_saveData),
            });
        }
    }
}
