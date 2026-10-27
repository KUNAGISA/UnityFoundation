using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [DefinitionResolver(typeof(SetGameIntVariableActionDef))]
    public static class SetGameIntVariableActionResolver
    {
        public static bool Execute(SetGameIntVariableActionDef action, ICommandContext context)
        {
            return context.ExecuteCommand<SetGameIntVariableCommand, bool>(new SetGameIntVariableCommand(action.VariableId, action.Value));
        }
    }
}
