using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ConditionResolver(typeof(GameSwitchConditionDef), ConditionSource.GameSwitches)]
    public static class GameSwitchConditionResolver
    {
        public static bool Evaluate(GameSwitchConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGameSwitchQuery, bool>(new GetGameSwitchQuery(condition.SwitchId));
            return value == condition.Value;
        }
    }
}
