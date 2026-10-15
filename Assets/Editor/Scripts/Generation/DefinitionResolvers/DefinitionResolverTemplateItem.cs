namespace Gameplay.Editor
{
    internal sealed class DefinitionResolverTemplateItem
    {
        public DefinitionResolverTemplateItem(string definitionTypeName, string resolverTypeName, string methodName, bool hasOptionalMethod)
        {
            DefinitionTypeName = definitionTypeName;
            ResolverTypeName = resolverTypeName;
            TypeIdName = definitionTypeName + ".__ID__";
            MethodName = methodName;
            HasOptionalMethod = hasOptionalMethod;
        }

        public string DefinitionTypeName { get; }
        public string ResolverTypeName { get; }
        public string TypeIdName { get; }
        public string MethodName { get; }
        public bool HasOptionalMethod { get; }
    }
}
