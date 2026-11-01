using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ActionResolver(typeof(SetGameBoolVariableActionDef))]
    public static class SetGameBoolVariableActionResolver
    {
        public static bool Execute(SetGameBoolVariableActionDef action, ICommandContext context)
        {
            return context.ExecuteCommand<SetGameBoolVariableCommand, bool>(new SetGameBoolVariableCommand(action.VariableId, action.Value));
        }
    }
}
