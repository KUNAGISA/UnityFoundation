using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Scriban;
using UnityEditor;
using UnityEditor.Build;

namespace Gameplay.Editor
{
    internal static class ResolverGenerationUtility
    {
        public static void ValidateBinding(Type definition, Type resolver, Type baseType, string methodName, Type contextType, ISet<Type> definitions)
        {
            if (!resolver.IsAbstract || !resolver.IsSealed || resolver.ContainsGenericParameters || !baseType.IsAssignableFrom(definition) || definition.IsAbstract || definition.ContainsGenericParameters)
            {
                throw new InvalidOperationException($"Invalid {baseType.Name} resolver [{resolver.FullName}] for [{definition.FullName}].");
            }
            if (!definitions.Add(definition))
            {
                throw new InvalidOperationException($"Duplicate resolver for [{definition.FullName}].");
            }
            var method = resolver.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { definition, contextType }, null);
            if (method == null || method.ReturnType != typeof(bool) || method.ContainsGenericParameters)
            {
                throw new InvalidOperationException($"[{resolver.FullName}] requires public static bool {methodName}({definition.Name}, {contextType.Name}).");
            }
        }

        public static void ValidateCoverage(Type baseType, ISet<Type> definitions)
        {
            foreach (var type in baseType.Assembly.GetTypes())
            {
                if (type.IsAbstract || !baseType.IsAssignableFrom(type))
                {
                    continue;
                }
                if (!definitions.Contains(type))
                {
                    throw new InvalidOperationException($"Missing resolver for [{type.FullName}].");
                }
            }
        }

        public static string Render(string name, object model)
        {
            var path = "Assets/Editor/Scripts/Generation/DefinitionResolvers/Templates/" + name + ".sbn";
            var template = Template.Parse(File.ReadAllText(path, Encoding.UTF8), path);
            if (template.HasErrors)
            {
                throw new InvalidOperationException(string.Join(", ", template.Messages));
            }
            return template.Render(model, member => member.Name).Replace("\r\n", "\n");
        }

        public static void Write(string path, string content, string marker)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!File.Exists(path) || File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n") != content)
            {
                File.WriteAllText(path, content, new UTF8Encoding(false));
            }
            foreach (var stale in FindStale(path, marker))
            {
                AssetDatabase.DeleteAsset(stale);
            }
            AssetDatabase.Refresh();
        }

        public static void Check(string path, string content, string marker, string menu)
        {
            if (!File.Exists(path) || File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n") != content || FindStale(path, marker).Count != 0)
            {
                throw new BuildFailedException($"Resolver output is stale. Run {menu}.");
            }
        }

        private static List<string> FindStale(string path, string marker)
        {
            var stale = new List<string>();
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                return stale;
            }
            foreach (var candidate in Directory.GetFiles(directory, "*.g.cs", SearchOption.TopDirectoryOnly))
            {
                if (Path.GetFullPath(candidate) != Path.GetFullPath(path) && File.ReadAllText(candidate, Encoding.UTF8).Contains(marker))
                {
                    stale.Add(candidate.Replace('\\', '/'));
                }
            }
            return stale;
        }

        public static string GetTypeName(Type type)
        {
            if (type == typeof(void))
            {
                return "void";
            }

            if (type.IsArray)
            {
                return GetTypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            }

            if (type.IsByRef || type.IsPointer || type.ContainsGenericParameters || type.FullName == null)
            {
                throw new InvalidOperationException($"Unsupported resolver signature type [{type}].");
            }

            if (!type.IsGenericType)
            {
                return "global::" + type.FullName.Replace('+', '.');
            }

            var segments = type.GetGenericTypeDefinition().FullName.Split('+');
            var arguments = type.GetGenericArguments();
            var argumentIndex = 0;
            var name = new StringBuilder("global::");
            for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
            {
                if (segmentIndex > 0)
                {
                    name.Append('.');
                }

                var segment = segments[segmentIndex];
                var marker = segment.LastIndexOf('`');
                if (marker < 0)
                {
                    name.Append(segment);
                    continue;
                }

                name.Append(segment.Substring(0, marker));
                var count = int.Parse(segment.Substring(marker + 1));
                name.Append('<');
                for (var index = 0; index < count; index++)
                {
                    if (index > 0)
                    {
                        name.Append(", ");
                    }

                    name.Append(GetTypeName(arguments[argumentIndex++]));
                }

                name.Append('>');
            }

            return name.ToString();
        }

    }
}
