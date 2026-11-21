using System;
using System.Diagnostics;

namespace Gameplay
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ConditionResolverAttribute : Attribute
    {
        public Type DefinitionType { get; }

        public ConditionSource Sources { get; }

        public ConditionResolverAttribute(Type definitionType, ConditionSource sources)
        {
            DefinitionType = definitionType ?? throw new ArgumentNullException(nameof(definitionType));
            Sources = sources;
        }
    }
}
