using System;
using Gameplay.Tables;

namespace Gameplay
{
    public static class VariableComparison
    {
        public static bool Evaluate(int value, VariableComparisonOperator comparison, int expected)
        {
            return comparison switch
            {
                VariableComparisonOperator.Equal => value == expected,
                VariableComparisonOperator.NotEqual => value != expected,
                VariableComparisonOperator.Greater => value > expected,
                VariableComparisonOperator.GreaterOrEqual => value >= expected,
                VariableComparisonOperator.Less => value < expected,
                VariableComparisonOperator.LessOrEqual => value <= expected,
                _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null),
            };
        }
    }
}
