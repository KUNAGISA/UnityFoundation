using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [ConditionResolver(typeof(TrueConditionDef), ConditionSource.None)]
    public static class TrueConditionResolver
    {
        public static bool Evaluate(TrueConditionDef condition, IQueryContext context)
        {
            return true;
        }
    }
}
