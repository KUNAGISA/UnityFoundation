using System;
using UnityEngine;

namespace Gameplay.UI.Editor
{
    [Serializable]
    internal sealed class UIWidgetBindingRuleConfig : UIBindingRuleConfig
    {
        public UIWidgetBindingRuleConfig() : base(string.Empty, "UI Widget", true, "widget")
        {
        }

        internal UIWidgetBindingRuleConfig(string key) : base(key, "UI Widget", true, "widget")
        {
        }

        internal override bool TryResolve(GameObject target, out UIBindingDefinition definition)
        {
            if (!target.TryGetComponent<UIWidget>(out var widget))
            {
                definition = default;
                return false;
            }

            definition = new UIBindingDefinition(FieldPrefix, widget.GetType().FullName, UIBindingAccessKind.Component);
            return true;
        }
    }
}
