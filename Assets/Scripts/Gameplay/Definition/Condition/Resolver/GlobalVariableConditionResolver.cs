using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ConditionResolver(typeof(GlobalVariableConditionDef), ConditionSource.GlobalVariables)]
    public static class GlobalVariableConditionResolver
    {
        public static bool Evaluate(GlobalVariableConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGlobalVariableQuery, int>(new GetGlobalVariableQuery(condition.VariableId));
            return VariableComparison.Evaluate(value, condition.Comparison, condition.Value);
        }
    }
}
