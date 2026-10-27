using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [DefinitionResolver(typeof(SetGlobalIntVariableActionDef))]
    public static class SetGlobalIntVariableActionResolver
    {
        public static bool Execute(SetGlobalIntVariableActionDef action, ICommandContext context)
        {
            return context.ExecuteCommand<SetGlobalIntVariableCommand, bool>(new SetGlobalIntVariableCommand(action.VariableId, action.Value));
        }
    }
}
