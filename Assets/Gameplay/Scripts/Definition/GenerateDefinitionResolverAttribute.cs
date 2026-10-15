using System;
using System.Diagnostics;

namespace Gameplay
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class GenerateDefinitionResolverAttribute : Attribute
    {
        public string GeneratedType { get; }
        public Type DefinitionBaseType { get; }
        public string TemplateName { get; }
        public Type ReturnType { get; }
        public Type[] RequiredParameterTypes { get; }

        public Type OptionalParameterType { get; set; }
        public string OutputDirectory { get; set; }

        public GenerateDefinitionResolverAttribute(string generatedType, Type definitionBaseType, string templateName, Type returnType, Type[] requiredParameterTypes)
        {
            if (generatedType == null || (generatedType.Length > 0 && string.IsNullOrWhiteSpace(generatedType)))
            {
                throw new ArgumentException("Generated type cannot be null or whitespace.", nameof(generatedType));
            }

            if (string.IsNullOrWhiteSpace(templateName))
            {
                throw new ArgumentException("Template name cannot be empty.", nameof(templateName));
            }

            GeneratedType = generatedType;
            DefinitionBaseType = definitionBaseType ?? throw new ArgumentNullException(nameof(definitionBaseType));
            TemplateName = templateName;
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            RequiredParameterTypes = requiredParameterTypes ?? throw new ArgumentNullException(nameof(requiredParameterTypes));
        }
    }
}
