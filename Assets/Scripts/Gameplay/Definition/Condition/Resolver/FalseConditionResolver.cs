using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ConditionResolver(typeof(FalseConditionDef), ConditionSource.None)]
    public static class FalseConditionResolver
    {
        public static bool Evaluate(FalseConditionDef condition, IQueryContext context)
        {
            return false;
        }
    }
}
