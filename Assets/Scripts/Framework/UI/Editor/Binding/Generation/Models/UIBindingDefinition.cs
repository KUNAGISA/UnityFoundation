namespace Gameplay.UI.Editor
{
    internal readonly struct UIBindingDefinition
    {
        internal string Prefix { get; }
        internal string FieldTypeName { get; }
        internal UIBindingAccessKind AccessKind { get; }
        internal UIBindingEvent Event { get; }

        internal UIBindingDefinition(string prefix, string fieldTypeName, UIBindingAccessKind accessKind, UIBindingEvent bindingEvent = null)
        {
            Prefix = prefix;
            FieldTypeName = fieldTypeName;
            AccessKind = accessKind;
            Event = bindingEvent;
        }
    }
}
