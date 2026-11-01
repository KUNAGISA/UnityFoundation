using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ActionResolver(typeof(SetGlobalSwitchActionDef))]
    public static class SetGlobalSwitchActionResolver
    {
        public static bool Execute(SetGlobalSwitchActionDef action, ICommandContext context)
        {
            return context.ExecuteCommand<SetGlobalSwitchCommand, bool>(new SetGlobalSwitchCommand(action.SwitchId, action.Value));
        }
    }
}
