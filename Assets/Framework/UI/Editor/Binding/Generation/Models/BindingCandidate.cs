using System;

namespace Gameplay.UI.Editor
{
    internal readonly struct BindingCandidate
    {
        internal string FieldName { get; }
        internal string FieldTypeName { get; }
        internal string BindingName { get; }
        internal UIBindingAccessKind AccessKind { get; }
        internal UIBindingEvent Event { get; }
        internal int CollectionIndex { get; }
        internal int CollectionCount { get; }
        internal string CollectionRootPath { get; }
        internal string SourceNodeName { get; }
        internal UIBindingElement Element { get; }

        internal BindingCandidate(string fieldName, string fieldTypeName, string bindingName, UIBindingAccessKind accessKind, UIBindingEvent bindingEvent, int collectionIndex, int collectionCount, string collectionRootPath, string sourceNodeName, UIBindingElement element)
        {
            FieldName = fieldName;
            FieldTypeName = fieldTypeName;
            BindingName = bindingName;
            AccessKind = accessKind;
            Event = bindingEvent;
            CollectionIndex = collectionIndex;
            CollectionCount = collectionCount;
            CollectionRootPath = collectionRootPath;
            SourceNodeName = sourceNodeName;
            Element = element;
        }

        internal bool IsCompatibleWith(in BindingCandidate other)
        {
            return string.Equals(FieldTypeName, other.FieldTypeName, StringComparison.Ordinal)
                && string.Equals(BindingName, other.BindingName, StringComparison.Ordinal)
                && AccessKind == other.AccessKind
                && AreEventsCompatible(Event, other.Event);
        }

        internal UIBindingField CreateField(bool isCollection, UIBindingElement[] elements)
        {
            return new UIBindingField(FieldName, FieldTypeName, BindingName, Event, AccessKind, isCollection, elements);
        }

        private static bool AreEventsCompatible(UIBindingEvent left, UIBindingEvent right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }
            return left.IsCompatibleWith(right);
        }
    }
}
