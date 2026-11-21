using System.Collections.Generic;
using Aoiro;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Editor
{
    public sealed class LocalizationKeyAttributeDrawer : OdinAttributeDrawer<LocalizationKeyAttribute, string>, IEditorController
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var model = this.GetModel<LocalizationKeyEditorModel>();
            var options = model.Options;
            if (options.Count == 0)
            {
                SirenixEditorGUI.MessageBox("Localization table has no available keys.", MessageType.Warning);
                CallNextDrawer(label);
                return;
            }

            var currentKey = ValueEntry.SmartValue;
            var display = FormatCurrentValue(model, currentKey);
            var selectedKeys = OdinSelector<string>.DrawSelectorDropdown(label, new GUIContent(display), rect =>
            {
                var items = new GenericSelectorItem<string>[options.Count + 1];
                items[0] = new GenericSelectorItem<string>("-", string.Empty);
                for (var index = 0; index < options.Count; index++)
                {
                    var option = options[index];
                    items[index + 1] = new GenericSelectorItem<string>(option.DisplayName, option.Key);
                }

                var selector = new GenericSelector<string>("Select Localization Key", false, items);
                selector.SetSelection(string.IsNullOrEmpty(currentKey) ? string.Empty : currentKey);
                selector.SelectionTree.Config.DrawSearchToolbar = true;
                selector.ShowInPopup(rect);
                selector.EnableSingleClickToSelect();
                return selector;
            });

            if (TryGetSelectedValue(selectedKeys, currentKey, out var selectedKey) && selectedKey != currentKey)
            {
                ValueEntry.SmartValue = selectedKey;
            }

            var value = ValueEntry.SmartValue;
            if (!string.IsNullOrEmpty(value) && !model.TryGetText(value, out _))
            {
                SirenixEditorGUI.MessageBox($"Localization key [{value}] does not exist.", MessageType.Warning);
            }
        }

        private static string FormatCurrentValue(LocalizationKeyEditorModel model, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "-";
            }

            return model.TryGetText(key, out var text) ? $"{key} - {text}" : key;
        }

        private static bool TryGetSelectedValue(IEnumerable<string> values, string fallback, out string selected)
        {
            if (values == null)
            {
                selected = fallback;
                return false;
            }

            using (var enumerator = values.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    selected = enumerator.Current;
                    return true;
                }
            }

            selected = fallback;
            return false;
        }
    }
}
