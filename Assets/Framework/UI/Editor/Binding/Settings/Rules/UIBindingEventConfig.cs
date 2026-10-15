using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.UI.Editor
{
    [Serializable]
    internal sealed class UIBindingEventConfig
    {
        [SerializeField]
        private string _memberName;

        [SerializeField]
        private string _handlerSuffix;

        internal bool Enabled => !string.IsNullOrWhiteSpace(_memberName) || !string.IsNullOrWhiteSpace(_handlerSuffix);

        internal static UIBindingEventConfig Create(string memberName, string handlerSuffix)
        {
            return new UIBindingEventConfig
            {
                _memberName = memberName,
                _handlerSuffix = handlerSuffix
            };
        }

        internal UIBindingEvent CreateEvent(Type componentType)
        {
            var memberType = GetEventMemberType(componentType, _memberName);
            var parameterTypes = GetEventParameterTypes(memberType);
            var parameters = new UIBindingEvent.Parameter[parameterTypes.Length];
            for (var index = 0; index < parameterTypes.Length; index++)
            {
                parameters[index] = new UIBindingEvent.Parameter(GetCSharpTypeName(parameterTypes[index]), GetParameterName(index, parameterTypes.Length));
            }
            return new UIBindingEvent(_memberName, _handlerSuffix, parameters);
        }

        internal void Validate(int ruleIndex, Type componentType, List<string> errors)
        {
            if (!UIBindingRuleConfig.IsValidIdentifier(_memberName) || !UIBindingRuleConfig.IsValidIdentifier(_handlerSuffix))
            {
                errors.Add($"Binding rule [{ruleIndex}] has an invalid event member or handler suffix.");
                return;
            }

            if (componentType == null)
            {
                return;
            }

            var memberType = GetEventMemberType(componentType, _memberName);
            if (memberType == null || !typeof(UnityEventBase).IsAssignableFrom(memberType))
            {
                errors.Add($"Binding rule [{ruleIndex}] cannot find UnityEvent member [{_memberName}] on [{componentType.FullName}].");
                return;
            }

            var parameterTypes = GetEventParameterTypes(memberType);
            if (parameterTypes == null)
            {
                errors.Add($"Binding rule [{ruleIndex}] uses unsupported UnityEvent type [{memberType.FullName}].");
            }
        }

        private static Type GetEventMemberType(Type componentType, string memberName)
        {
            var property = componentType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (property != null)
            {
                return property.PropertyType;
            }
            return componentType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public)?.FieldType;
        }

        private static Type[] GetEventParameterTypes(Type eventType)
        {
            for (var type = eventType; type != null; type = type.BaseType)
            {
                if (type == typeof(UnityEvent))
                {
                    return Type.EmptyTypes;
                }
                if (type.IsGenericType)
                {
                    var genericDefinition = type.GetGenericTypeDefinition();
                    if (genericDefinition == typeof(UnityEvent<>)
                        || genericDefinition == typeof(UnityEvent<,>)
                        || genericDefinition == typeof(UnityEvent<,,>)
                        || genericDefinition == typeof(UnityEvent<,,,>))
                    {
                        return type.GetGenericArguments();
                    }
                }
            }
            return null;
        }

        private static string GetParameterName(int index, int parameterCount)
        {
            return parameterCount == 1 ? "value" : $"value{index}";
        }

        private static string GetCSharpTypeName(Type type)
        {
            if (type == typeof(bool))
            {
                return "bool";
            }
            if (type == typeof(int))
            {
                return "int";
            }
            if (type == typeof(float))
            {
                return "float";
            }
            if (type == typeof(string))
            {
                return "string";
            }
            return type.FullName;
        }
    }
}
