using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [DefinitionResolver(typeof(GameIntVariableConditionDef))]
    public static class GameIntVariableConditionResolver
    {
        public static bool Evaluate(GameIntVariableConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGameIntVariableQuery, int>(new GetGameIntVariableQuery(condition.VariableId));
            return IntVariableComparison.Evaluate(value, condition.Comparison, condition.Value);
        }

        public static void CollectDependencies(GameIntVariableConditionDef condition, ConditionDependencyCollector collector)
        {
            collector.Add(ConditionChangeType.GameIntVariable);
        }
    }
}
