using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ConditionResolver(typeof(GlobalIntVariableConditionDef), ConditionSource.GlobalIntVariables)]
    public static class GlobalIntVariableConditionResolver
    {
        public static bool Evaluate(GlobalIntVariableConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGlobalIntVariableQuery, int>(new GetGlobalIntVariableQuery(condition.VariableId));
            return IntVariableComparison.Evaluate(value, condition.Comparison, condition.Value);
        }
    }
}
