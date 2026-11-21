using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ActionResolver(typeof(SetGameSwitchActionDef))]
    public static class SetGameSwitchActionResolver
    {
        public static bool Execute(SetGameSwitchActionDef action, ICommandContext context)
        {
            return context.ExecuteCommand<SetGameSwitchCommand, bool>(new SetGameSwitchCommand(action.SwitchId, action.Value));
        }
    }
}
