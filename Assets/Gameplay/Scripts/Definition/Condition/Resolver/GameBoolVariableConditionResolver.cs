using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ConditionResolver(typeof(GameBoolVariableConditionDef), ConditionSource.GameBoolVariables)]
    public static class GameBoolVariableConditionResolver
    {
        public static bool Evaluate(GameBoolVariableConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGameBoolVariableQuery, bool>(new GetGameBoolVariableQuery(condition.VariableId));
            return value == condition.Value;
        }
    }
}
