using Aoiro;
using Gameplay;
using Gameplay.Tables;

[assembly: GenerateDefinitionResolver("", typeof(ConditionDef), "ConditionResolver.sbn", typeof(bool), new[] { typeof(IQueryContext) })]
[assembly: GenerateDefinitionResolver("", typeof(ActionDef), "ActionResolver.sbn", typeof(bool), new[] { typeof(ICommandContext) })]
