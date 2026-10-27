using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [DefinitionResolver(typeof(SetGlobalBoolVariableActionDef))]
    public static class SetGlobalBoolVariableActionResolver
    {
        public static bool Execute(SetGlobalBoolVariableActionDef action, ICommandContext context)
        {
            return context.ExecuteCommand<SetGlobalBoolVariableCommand, bool>(new SetGlobalBoolVariableCommand(action.VariableId, action.Value));
        }
    }
}
