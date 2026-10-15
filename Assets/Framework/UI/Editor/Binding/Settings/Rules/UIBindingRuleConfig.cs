using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.UI.Editor
{
    [Serializable]
    internal abstract class UIBindingRuleConfig
    {
        [SerializeField]
        private string _key;

        [SerializeField]
        private string _displayName;

        [SerializeField, LabelText("Auto Resolve")]
        [Tooltip("Include this rule when resolving Auto bindings. Rules are evaluated from top to bottom.")]
        private bool _includeInAutoResolve = true;

        [SerializeField]
        private string _fieldPrefix;

        internal string Key => _key;
        internal int Id => UIBindingIdUtility.FromStableKey(_key);
        internal string DisplayName => _displayName;
        internal bool IncludeInAutoResolve => _includeInAutoResolve;
        protected string FieldPrefix => _fieldPrefix;

        protected UIBindingRuleConfig(string key, string displayName, bool includeInAutoResolve, string fieldPrefix)
        {
            _key = key;
            _displayName = displayName;
            _includeInAutoResolve = includeInAutoResolve;
            _fieldPrefix = fieldPrefix;
        }

        internal abstract bool TryResolve(GameObject target, out UIBindingDefinition definition);

        internal virtual void Validate(int index, List<string> errors)
        {
            if (!IsValidKey(_key))
            {
                errors.Add($"Binding rule [{index}] must use a lowercase namespaced key such as [gameplay.localized-text].");
            }
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Binding rule [{index}] must have a display name.");
            }
            if (!IsValidIdentifier(_fieldPrefix))
            {
                errors.Add($"Binding rule [{index}] has invalid field prefix [{_fieldPrefix}].");
            }
        }

        internal static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || (!char.IsLetter(value[0]) && value[0] != '_'))
            {
                return false;
            }

            for (var index = 1; index < value.Length; index++)
            {
                if (!char.IsLetterOrDigit(value[index]) && value[index] != '_')
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsValidKey(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] == '.' || value[^1] == '.')
            {
                return false;
            }

            var hasNamespaceSeparator = false;
            var isSegmentStart = true;
            for (var index = 0; index < value.Length; index++)
            {
                var ch = value[index];
                if (ch == '.')
                {
                    if (isSegmentStart)
                    {
                        return false;
                    }
                    hasNamespaceSeparator = true;
                    isSegmentStart = true;
                    continue;
                }
                if (isSegmentStart && (ch < 'a' || ch > 'z'))
                {
                    return false;
                }
                if (!isSegmentStart && (ch < 'a' || ch > 'z') && (ch < '0' || ch > '9') && ch != '-')
                {
                    return false;
                }
                isSegmentStart = false;
            }
            return hasNamespaceSeparator && !isSegmentStart;
        }
    }
}
