using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Gameplay.UI.Editor
{
    [Serializable]
    internal sealed class ComponentBindingRuleConfig : UIBindingRuleConfig
    {
        [SerializeField]
        private MonoScript _componentScript;

        [SerializeField, BoxGroup]
        private UIBindingEventConfig _event;

        public ComponentBindingRuleConfig() : base(string.Empty, "Component", true, string.Empty)
        {
        }

        internal ComponentBindingRuleConfig(string key, string displayName, Type componentType, string fieldPrefix, UIBindingEventConfig bindingEvent = null)
            : base(key, displayName, true, fieldPrefix)
        {
            _componentScript = FindMonoScript(componentType);
            _event = bindingEvent;
        }

        internal override bool TryResolve(GameObject target, out UIBindingDefinition definition)
        {
            var componentType = GetComponentType();
            if (componentType == null || target.GetComponent(componentType) == null)
            {
                definition = default;
                return false;
            }

            definition = new UIBindingDefinition(FieldPrefix, componentType.FullName, UIBindingAccessKind.Component, _event != null && _event.Enabled ? _event.CreateEvent(componentType) : null);
            return true;
        }

        internal override void Validate(int index, List<string> errors)
        {
            base.Validate(index, errors);
            var componentType = GetComponentType();
            if (componentType == null)
            {
                errors.Add($"Binding rule [{index}] must reference a Component script.");
            }
            if (_event != null && _event.Enabled)
            {
                _event.Validate(index, componentType, errors);
            }
        }

        private Type GetComponentType()
        {
            var type = _componentScript == null ? null : _componentScript.GetClass();
            return type != null && typeof(Component).IsAssignableFrom(type) ? type : null;
        }

        private static MonoScript FindMonoScript(Type componentType)
        {
            var scripts = MonoImporter.GetAllRuntimeMonoScripts();
            for (var index = 0; index < scripts.Length; index++)
            {
                if (scripts[index].GetClass() == componentType)
                {
                    return scripts[index];
                }
            }
            return null;
        }
    }
}
