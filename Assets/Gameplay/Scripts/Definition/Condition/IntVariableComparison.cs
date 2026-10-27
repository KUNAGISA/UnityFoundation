using System;
using Gameplay.Tables;

namespace Gameplay
{
    public static class IntVariableComparison
    {
        public static bool Evaluate(int value, IntVariableComparisonOperator comparison, int expected)
        {
            return comparison switch
            {
                IntVariableComparisonOperator.Equal => value == expected,
                IntVariableComparisonOperator.NotEqual => value != expected,
                IntVariableComparisonOperator.Greater => value > expected,
                IntVariableComparisonOperator.GreaterOrEqual => value >= expected,
                IntVariableComparisonOperator.Less => value < expected,
                IntVariableComparisonOperator.LessOrEqual => value <= expected,
                _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null),
            };
        }
    }
}
