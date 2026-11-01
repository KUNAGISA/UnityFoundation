namespace Gameplay.Editor
{
    internal class ResolverTemplateItem
    {
        public string DefinitionTypeName { get; }
        public string ResolverTypeName { get; }
        public string TypeIdName => DefinitionTypeName + ".__ID__";

        public ResolverTemplateItem(System.Type definitionType, System.Type resolverType)
        {
            DefinitionTypeName = ResolverGenerationUtility.GetTypeName(definitionType);
            ResolverTypeName = ResolverGenerationUtility.GetTypeName(resolverType);
        }
    }
}
