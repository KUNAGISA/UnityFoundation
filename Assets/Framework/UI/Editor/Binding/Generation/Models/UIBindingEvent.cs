using System;

namespace Gameplay.UI.Editor
{
    internal sealed class UIBindingEvent
    {
        internal readonly struct Parameter
        {
            public string TypeName { get; }
            public string Name { get; }

            internal Parameter(string typeName, string name)
            {
                TypeName = typeName;
                Name = name;
            }
        }

        public string EventMemberName { get; }
        public string HandlerSuffix { get; }
        public Parameter[] Parameters { get; }
        public bool HasParameters => Parameters.Length > 0;

        internal UIBindingEvent(string eventMemberName, string handlerSuffix, Parameter[] parameters)
        {
            EventMemberName = eventMemberName;
            HandlerSuffix = handlerSuffix;
            Parameters = parameters;
        }

        internal bool IsCompatibleWith(UIBindingEvent other)
        {
            if (other == null
                || !string.Equals(EventMemberName, other.EventMemberName, StringComparison.Ordinal)
                || !string.Equals(HandlerSuffix, other.HandlerSuffix, StringComparison.Ordinal)
                || Parameters.Length != other.Parameters.Length)
            {
                return false;
            }

            for (var index = 0; index < Parameters.Length; index++)
            {
                if (!string.Equals(Parameters[index].TypeName, other.Parameters[index].TypeName, StringComparison.Ordinal) || !string.Equals(Parameters[index].Name, other.Parameters[index].Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
