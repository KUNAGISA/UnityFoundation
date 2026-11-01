using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ConditionResolver(typeof(GameVariableConditionDef), ConditionSource.GameVariables)]
    public static class GameVariableConditionResolver
    {
        public static bool Evaluate(GameVariableConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGameVariableQuery, int>(new GetGameVariableQuery(condition.VariableId));
            return VariableComparison.Evaluate(value, condition.Comparison, condition.Value);
        }
    }
}
