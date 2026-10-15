namespace Gameplay.UI.Editor
{
    internal sealed class UIBindingTemplateModel
    {
        public string Namespace { get; init; }
        public string TypeName { get; init; }
        public UIBindingField[] Bindings { get; init; }
        public bool HasBindings { get; init; }
        public bool HasAwakeHandler { get; init; }
        public bool HasEvents { get; init; }
    }
}
