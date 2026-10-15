using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gameplay.Editor
{
    internal sealed class DefinitionResolverBinding
    {
        public DefinitionResolverBinding(Type definitionType, Type resolverType, IReadOnlyList<MethodInfo> methods)
        {
            DefinitionType = definitionType;
            ResolverType = resolverType;
            Methods = methods;
        }

        public Type DefinitionType { get; }
        public Type ResolverType { get; }
        public IReadOnlyList<MethodInfo> Methods { get; }
    }
}
