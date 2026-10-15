using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.UI.Editor
{
    [Serializable]
    internal sealed class GameObjectBindingRuleConfig : UIBindingRuleConfig
    {
        public GameObjectBindingRuleConfig() : base(string.Empty, "Game Object", false, "go")
        {
        }

        internal GameObjectBindingRuleConfig(string key) : base(key, "Game Object", false, "go")
        {
        }

        internal override bool TryResolve(GameObject target, out UIBindingDefinition definition)
        {
            definition = new UIBindingDefinition(FieldPrefix, typeof(GameObject).FullName, UIBindingAccessKind.GameObject);
            return true;
        }

        internal override void Validate(int index, List<string> errors)
        {
            base.Validate(index, errors);
            if (IncludeInAutoResolve)
            {
                errors.Add($"Binding rule [{index}] for GameObject cannot participate in automatic resolution.");
            }
        }
    }
}
