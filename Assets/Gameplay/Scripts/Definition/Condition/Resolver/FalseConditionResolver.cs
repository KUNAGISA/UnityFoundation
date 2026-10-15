using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [DefinitionResolver(typeof(FalseConditionDef))]
    public static class FalseConditionResolver
    {
        public static bool Evaluate(FalseConditionDef condition, IQueryContext context)
        {
            return false;
        }

        public static void CollectDependencies(FalseConditionDef condition, ConditionDependencyCollector collector)
        {
        }
    }
}
