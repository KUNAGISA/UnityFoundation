using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    [DefinitionResolver(typeof(TrueConditionDef))]
    public static class TrueConditionResolver
    {
        public static bool Evaluate(TrueConditionDef condition, IQueryContext context)
        {
            return true;
        }
    }
}
