using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ActionResolver(typeof(SetGameVariableActionDef))]
    public static class SetGameVariableActionResolver
    {
        public static bool Execute(SetGameVariableActionDef action, ICommandContext context)
        {
            return context.ExecuteCommand<SetGameVariableCommand, bool>(new SetGameVariableCommand(action.VariableId, action.Value));
        }
    }
}
