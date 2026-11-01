namespace Gameplay.Editor
{
    internal sealed class ConditionResolverTemplateItem : ResolverTemplateItem
    {
        public string SourcesExpression { get; }

        public ConditionResolverTemplateItem(System.Type definitionType, System.Type resolverType, ConditionSource sources) : base(definitionType, resolverType)
        {
            SourcesExpression = "ConditionSource." + sources.ToString().Replace(", ", " | ConditionSource.");
        }
    }
}
