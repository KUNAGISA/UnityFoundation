using System;
using System.Diagnostics;

namespace Gameplay
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ActionResolverAttribute : Attribute
    {
        public Type DefinitionType { get; }

        public ActionResolverAttribute(Type definitionType)
        {
            DefinitionType = definitionType ?? throw new ArgumentNullException(nameof(definitionType));
        }
    }
}
