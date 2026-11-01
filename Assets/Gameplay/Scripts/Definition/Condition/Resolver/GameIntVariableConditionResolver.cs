using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ConditionResolver(typeof(GameIntVariableConditionDef), ConditionSource.GameIntVariables)]
    public static class GameIntVariableConditionResolver
    {
        public static bool Evaluate(GameIntVariableConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGameIntVariableQuery, int>(new GetGameIntVariableQuery(condition.VariableId));
            return IntVariableComparison.Evaluate(value, condition.Comparison, condition.Value);
        }
    }
}
