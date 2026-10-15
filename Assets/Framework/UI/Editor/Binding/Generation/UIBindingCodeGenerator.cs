using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Scriban;
using UnityEditor;
using UnityEngine;

namespace Gameplay.UI.Editor
{
    internal sealed class UIBindingCodeGenerator
    {
        private const string BeginBindCodeTag = "#region Auto Generate UI Bind";
        private const string EndBindCodeTag = "#endregion Auto Generate UI Bind";
        private readonly UIBindingProjectSettings _settings;

        internal UIBindingCodeGenerator()
        {
            _settings = UIBindingProjectSettings.GetOrCreate();
        }

        internal void Generate(MonoBehaviour target)
        {
            _settings.ValidateOrThrow();
            var filePath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(target));
            var originalContent = File.ReadAllText(filePath, Encoding.UTF8);
            var clearedContent = RemoveGeneratedRegion(originalContent);
            var bindings = CollectBindings(target.gameObject);
            var generatedCode = RenderTemplate(target.GetType(), bindings);
            var content = string.IsNullOrEmpty(generatedCode) ? clearedContent.TrimEnd() + Environment.NewLine : clearedContent + generatedCode;
            File.WriteAllText(filePath, content, Encoding.UTF8);
        }

        private UIBindingField[] CollectBindings(GameObject root)
        {
            var candidates = new List<BindingCandidate>(20);
            CollectBindings(root.transform, root.transform, null, null, -1, -1, candidates);
            return BuildBindings(candidates);
        }

        private void CollectBindings(Transform transform, Transform root, Transform collectionRoot, Transform collectionItem, int collectionIndex, int collectionCount, List<BindingCandidate> candidates)
        {
            var isCollectionRoot = transform.TryGetComponent<UIBindCollectionTag>(out _);
            if (isCollectionRoot && collectionRoot != null)
            {
                throw new InvalidOperationException($"Collection binding root [{transform.name}] cannot be nested under collection root [{collectionRoot.name}].");
            }

            var target = transform.gameObject;
            var tags = target.GetComponents<UIBindTag>();
            var nodeName = transform.name;
            var collectionRootPath = collectionRoot == null ? null : EscapeString(AnimationUtility.CalculateTransformPath(collectionRoot, root));
            var nodePath = EscapeString(AnimationUtility.CalculateTransformPath(transform, collectionItem == null ? root : collectionItem));
            for (var index = 0; index < tags.Length; index++)
            {
                var tag = tags[index];
                var definition = Resolve(target, tag.BindingId);
                candidates.Add(new BindingCandidate(
                    GetFieldName(nodeName, definition.Prefix),
                    definition.FieldTypeName,
                    nodeName,
                    definition.AccessKind,
                    tag.DisableEvent ? null : definition.Event,
                    collectionIndex,
                    collectionCount,
                    collectionRootPath,
                    nodeName,
                    new UIBindingElement(nodePath, collectionRootPath, collectionIndex)));
            }

            if (isCollectionRoot)
            {
                if (transform.childCount == 0)
                {
                    throw new InvalidOperationException($"Collection binding root [{transform.name}] must contain at least one direct child item.");
                }

                var candidateStart = candidates.Count;
                for (var index = transform.childCount - 1; index >= 0; index--)
                {
                    var child = transform.GetChild(index);
                    if (CanTraverse(child))
                    {
                        CollectBindings(child, root, transform, child, index, transform.childCount, candidates);
                    }
                }

                if (candidates.Count == candidateStart)
                {
                    throw new InvalidOperationException($"Collection binding root [{transform.name}] does not contain any UI binding tags.");
                }
                return;
            }

            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                if (CanTraverse(child))
                {
                    CollectBindings(child, root, collectionRoot, collectionItem, collectionIndex, collectionCount, candidates);
                }
            }
        }

        private static bool CanTraverse(Transform transform)
        {
            return !PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject) || transform.TryGetComponent<PrefabDepthTag>(out _);
        }

        private UIBindingDefinition Resolve(GameObject target, int bindingId)
        {
            if (bindingId == UIBindingProjectSettings.AutoBindingId)
            {
                var rules = _settings.BindingRules;
                for (var index = 0; index < rules.Count; index++)
                {
                    var rule = rules[index];
                    if (rule != null && rule.IncludeInAutoResolve && rule.TryResolve(target, out var definition))
                    {
                        return definition;
                    }
                }

                throw new InvalidOperationException($"Node [{target.name}] does not match any automatic UI binding rule.");
            }

            var explicitRule = _settings.FindRule(bindingId);
            if (explicitRule == null)
            {
                throw new InvalidOperationException($"Node [{target.name}] references missing UI binding rule [{bindingId}].");
            }

            if (explicitRule.TryResolve(target, out var explicitDefinition))
            {
                return explicitDefinition;
            }
            throw new InvalidOperationException($"Node [{target.name}] cannot bind with UI binding rule [{explicitRule.DisplayName}] ({bindingId}).");
        }

        private static UIBindingField[] BuildBindings(List<BindingCandidate> candidates)
        {
            var bindings = new List<UIBindingField>(candidates.Count);
            var fieldNames = new HashSet<string>(StringComparer.Ordinal);
            var collections = new Dictionary<string, List<BindingCandidate>>(StringComparer.Ordinal);
            var collectionOrder = new List<string>();
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (candidate.CollectionIndex < 0)
                {
                    AddBinding(bindings, fieldNames, candidate.FieldName, candidate.SourceNodeName, candidate.CreateField(false, new[] { candidate.Element }));
                    continue;
                }

                if (!collections.TryGetValue(candidate.FieldName, out var collection))
                {
                    collection = new List<BindingCandidate>();
                    collections.Add(candidate.FieldName, collection);
                    collectionOrder.Add(candidate.FieldName);
                }
                collection.Add(candidate);
            }

            for (var index = 0; index < collectionOrder.Count; index++)
            {
                var fieldName = collectionOrder[index];
                var collection = collections[fieldName];
                collection.Sort((left, right) => left.CollectionIndex.CompareTo(right.CollectionIndex));
                var first = collection[0];
                for (var itemIndex = 0; itemIndex < collection.Count; itemIndex++)
                {
                    var item = collection[itemIndex];
                    if (!string.Equals(item.CollectionRootPath, first.CollectionRootPath, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Duplicate generated field name [{fieldName}] across collection roots [{first.CollectionRootPath}] and [{item.CollectionRootPath}].");
                    }
                }

                if (collection.Count != first.CollectionCount)
                {
                    throw new InvalidOperationException($"Collection binding [{fieldName}] under root [{first.CollectionRootPath}] must appear exactly once in each direct child item.");
                }
                for (var itemIndex = 0; itemIndex < collection.Count; itemIndex++)
                {
                    var item = collection[itemIndex];
                    if (item.CollectionIndex != itemIndex)
                    {
                        throw new InvalidOperationException(
                            $"Collection binding [{fieldName}] under root [{first.CollectionRootPath}] must appear exactly once in direct child item [{itemIndex}].");
                    }
                    if (!item.IsCompatibleWith(first))
                    {
                        throw new InvalidOperationException($"Collection binding [{fieldName}] contains incompatible nodes.");
                    }
                }

                var elements = new UIBindingElement[collection.Count];
                for (var itemIndex = 0; itemIndex < collection.Count; itemIndex++)
                {
                    elements[itemIndex] = collection[itemIndex].Element;
                }
                AddBinding(bindings, fieldNames, fieldName, first.SourceNodeName, first.CreateField(true, elements));
            }

            return bindings.ToArray();
        }

        private static void AddBinding(List<UIBindingField> bindings, HashSet<string> fieldNames, string fieldName, string sourceNodeName, in UIBindingField binding)
        {
            if (!fieldNames.Add(fieldName))
            {
                throw new InvalidOperationException($"Duplicate generated field name [{fieldName}] on node [{sourceNodeName}].");
            }
            bindings.Add(binding);
        }

        private static string GetFieldName(string nodeName, string prefix)
        {
            if (string.IsNullOrWhiteSpace(nodeName))
            {
                throw new InvalidOperationException("Bindable node name cannot be empty.");
            }

            if (!char.IsLetter(nodeName[0]) && nodeName[0] != '_')
            {
                throw new InvalidOperationException($"Bindable node name [{nodeName}] must start with a letter or underscore.");
            }

            for (var index = 1; index < nodeName.Length; ++index)
            {
                var ch = nodeName[index];
                if (!char.IsLetterOrDigit(ch) && ch != '_')
                {
                    throw new InvalidOperationException($"Bindable node name [{nodeName}] contains invalid character [{ch}].");
                }
            }

            var pascalNodeName = char.ToUpperInvariant(nodeName[0]) + nodeName[1..];
            if (pascalNodeName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var suffix = pascalNodeName[prefix.Length..];
                if (!string.IsNullOrEmpty(suffix))
                {
                    pascalNodeName = suffix;
                }
            }
            return "_" + prefix + pascalNodeName;
        }

        private static string RemoveGeneratedRegion(string content)
        {
            var reader = new StringReader(content);
            var builder = new StringBuilder();
            var inGeneratedRegion = false;
            while (reader.ReadLine() is { } line)
            {
                if (line.IndexOf(BeginBindCodeTag, StringComparison.Ordinal) >= 0)
                {
                    inGeneratedRegion = true;
                }

                if (!inGeneratedRegion)
                {
                    builder.AppendLine(line);
                }

                if (inGeneratedRegion && line.IndexOf(EndBindCodeTag, StringComparison.Ordinal) >= 0)
                {
                    inGeneratedRegion = false;
                }
            }

            return string.Concat(builder.ToString().TrimEnd(), Environment.NewLine, Environment.NewLine);
        }

        private static string EscapeString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string RenderTemplate(Type targetType, UIBindingField[] bindings)
        {
            var hasAwakeHandler = targetType.GetMethod("OnAwake", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) != null;
            if (bindings.Length == 0 && !hasAwakeHandler)
            {
                return string.Empty;
            }

            var templateFilePath = UIBindingProjectSettings.GetOrCreate().GetBindingTemplatePath();
            var templateText = File.ReadAllText(templateFilePath, Encoding.UTF8);
            var template = Template.Parse(templateText, templateFilePath);
            if (template.HasErrors)
            {
                throw new InvalidOperationException($"Failed to parse UI bind template [{templateFilePath}]: {string.Join(", ", template.Messages)}");
            }

            var hasEvents = false;
            for (var index = 0; index < bindings.Length; index++)
            {
                if (bindings[index].HasEvent)
                {
                    hasEvents = true;
                    break;
                }
            }

            var model = new UIBindingTemplateModel
            {
                Namespace = targetType.Namespace ?? string.Empty,
                TypeName = targetType.Name,
                Bindings = bindings,
                HasBindings = bindings.Length > 0,
                HasAwakeHandler = hasAwakeHandler,
                HasEvents = hasEvents
            };
            return template.Render(model, member => member.Name);
        }

    }
}
