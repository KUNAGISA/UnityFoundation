namespace Gameplay.UI.Editor
{
    internal readonly struct UIBindingField
    {
        public string FieldName { get; }
        public string FieldTypeName { get; }
        public string BindingName { get; }
        public UIBindingEvent Event { get; }
        public UIBindingElement[] Elements { get; }
        public int ElementCount => Elements.Length;
        public bool IsCollection { get; }
        public bool HasEvent => Event != null;
        public bool IsComponent => AccessKind == UIBindingAccessKind.Component;
        public bool IsTransform => AccessKind == UIBindingAccessKind.Transform;
        public bool IsGameObject => AccessKind == UIBindingAccessKind.GameObject;

        internal UIBindingAccessKind AccessKind { get; }

        internal UIBindingField(string fieldName, string fieldTypeName, string bindingName, UIBindingEvent bindingEvent, UIBindingAccessKind accessKind, bool isCollection, UIBindingElement[] elements)
        {
            FieldName = fieldName;
            FieldTypeName = fieldTypeName;
            BindingName = bindingName;
            Event = bindingEvent;
            AccessKind = accessKind;
            IsCollection = isCollection;
            Elements = elements;
        }
    }
}
