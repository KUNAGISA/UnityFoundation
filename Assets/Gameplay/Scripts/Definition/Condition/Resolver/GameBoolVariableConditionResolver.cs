using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [DefinitionResolver(typeof(GameBoolVariableConditionDef))]
    public static class GameBoolVariableConditionResolver
    {
        public static bool Evaluate(GameBoolVariableConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGameBoolVariableQuery, bool>(new GetGameBoolVariableQuery(condition.VariableId));
            return value == condition.Value;
        }

        public static void CollectDependencies(GameBoolVariableConditionDef condition, ConditionDependencyCollector collector)
        {
            collector.Add(ConditionChangeType.GameBoolVariable);
        }
    }
}
