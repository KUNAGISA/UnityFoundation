using System.Collections.Generic;

namespace Gameplay.Editor
{
    internal sealed class DefinitionResolverTemplateModel
    {
        public DefinitionResolverTemplateModel(string resolverTypeName, string definitionTypeName, string returnTypeName, IReadOnlyList<string> requiredParameterTypeNames, string optionalParameterTypeName, IReadOnlyList<DefinitionResolverTemplateItem> items)
        {
            ResolverTypeName = resolverTypeName;
            DefinitionTypeName = definitionTypeName;
            ReturnTypeName = returnTypeName;
            RequiredParameterTypeNames = requiredParameterTypeNames;
            OptionalParameterTypeName = optionalParameterTypeName;
            Items = items;
        }

        public string ResolverTypeName { get; }
        public string DefinitionTypeName { get; }
        public string ReturnTypeName { get; }
        public IReadOnlyList<string> RequiredParameterTypeNames { get; }
        public string OptionalParameterTypeName { get; }
        public IReadOnlyList<DefinitionResolverTemplateItem> Items { get; }
    }
}
