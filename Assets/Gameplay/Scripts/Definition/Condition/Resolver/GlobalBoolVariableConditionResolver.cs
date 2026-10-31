using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [DefinitionResolver(typeof(GlobalBoolVariableConditionDef))]
    public static class GlobalBoolVariableConditionResolver
    {
        public static bool Evaluate(GlobalBoolVariableConditionDef condition, IQueryContext context)
        {
            var value = context.ExecuteQuery<GetGlobalBoolVariableQuery, bool>(new GetGlobalBoolVariableQuery(condition.VariableId));
            return value == condition.Value;
        }
    }
}
