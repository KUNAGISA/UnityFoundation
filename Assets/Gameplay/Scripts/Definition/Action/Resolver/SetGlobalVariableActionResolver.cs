using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ActionResolver(typeof(SetGlobalVariableActionDef))]
    public static class SetGlobalVariableActionResolver
    {
        public static bool Execute(SetGlobalVariableActionDef action, ICommandContext context)
        {
            return context.ExecuteCommand<SetGlobalVariableCommand, bool>(new SetGlobalVariableCommand(action.VariableId, action.Value));
        }
    }
}
