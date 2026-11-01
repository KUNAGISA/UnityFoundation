using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ConditionResolver(typeof(GlobalSwitchConditionDef), ConditionSource.GlobalSwitches)]
    public static class GlobalSwitchConditionResolver
    {
        public static bool Evaluate(GlobalSwitchConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGlobalSwitchQuery, bool>(new GetGlobalSwitchQuery(condition.SwitchId));
            return value == condition.Value;
        }
    }
}
