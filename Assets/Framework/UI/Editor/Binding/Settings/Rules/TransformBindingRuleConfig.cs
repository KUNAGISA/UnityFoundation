using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.UI.Editor
{
    [Serializable]
    internal sealed class TransformBindingRuleConfig : UIBindingRuleConfig
    {
        public TransformBindingRuleConfig() : base(string.Empty, "Transform", false, "tf")
        {
        }

        internal TransformBindingRuleConfig(string key) : base(key, "Transform", false, "tf")
        {
        }

        internal override bool TryResolve(GameObject target, out UIBindingDefinition definition)
        {
            definition = new UIBindingDefinition(FieldPrefix, target.transform.GetType().FullName, UIBindingAccessKind.Transform);
            return true;
        }

        internal override void Validate(int index, List<string> errors)
        {
            base.Validate(index, errors);
            if (IncludeInAutoResolve)
            {
                errors.Add($"Binding rule [{index}] for Transform cannot participate in automatic resolution.");
            }
        }
    }
}
