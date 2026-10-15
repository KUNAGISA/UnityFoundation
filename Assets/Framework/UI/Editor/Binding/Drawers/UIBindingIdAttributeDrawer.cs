using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Gameplay.UI.Editor
{
    internal sealed class UIBindingIdAttributeDrawer : OdinAttributeDrawer<UIBindingIdAttribute, int>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var settings = UIBindingProjectSettings.GetOrCreate();
            var hasMixedValues = ValueEntry.ValueState == PropertyValueState.PrimitiveValueConflict;
            var currentId = ValueEntry.SmartValue;
            var isMissing = false;
            var display = hasMixedValues ? "—" : ResolveDisplay(settings, currentId, out isMissing);

            var bindingRuleLabel = new GUIContent("Binding Rule", label?.tooltip);
            var selectedIds = OdinSelector<int>.DrawSelectorDropdown(bindingRuleLabel, new GUIContent(display), rect =>
            {
                var items = BuildSelectorItems(settings, isMissing, currentId);
                var selector = new GenericSelector<int>("Select Binding Rule", false, items);
                if (!hasMixedValues)
                {
                    selector.SetSelection(currentId);
                }
                selector.SelectionTree.Config.DrawSearchToolbar = true;
                selector.ShowInPopup(rect);
                selector.EnableSingleClickToSelect();
                return selector;
            });

            if (TryGetSelectedId(selectedIds, out var selectedId) && (hasMixedValues || selectedId != currentId))
            {
                ValueEntry.SmartValue = selectedId;
            }

            if (isMissing)
            {
                SirenixEditorGUI.ErrorMessageBox($"Binding rule [{currentId}] does not exist in Project/Gameplay/UI Binding.");
            }
        }

        private static string ResolveDisplay(UIBindingProjectSettings settings, int bindingId, out bool isMissing)
        {
            if (bindingId == UIBindingProjectSettings.AutoBindingId)
            {
                isMissing = false;
                return "Auto";
            }

            var rule = settings.FindRule(bindingId);
            isMissing = rule == null;
            return isMissing ? $"Missing Binding Rule ({bindingId})" : rule.DisplayName;
        }

        private static List<GenericSelectorItem<int>> BuildSelectorItems(UIBindingProjectSettings settings, bool includeMissing, int currentId)
        {
            var items = new List<GenericSelectorItem<int>>(settings.BindingRules.Count + 2)
            {
                new("Auto", UIBindingProjectSettings.AutoBindingId)
            };
            for (var index = 0; index < settings.BindingRules.Count; index++)
            {
                var rule = settings.BindingRules[index];
                if (rule != null)
                {
                    items.Add(new GenericSelectorItem<int>(rule.DisplayName, rule.Id));
                }
            }
            if (includeMissing)
            {
                items.Add(new GenericSelectorItem<int>($"Missing Binding Rule ({currentId})", currentId));
            }
            return items;
        }

        private static bool TryGetSelectedId(IEnumerable<int> selectedIds, out int selectedId)
        {
            if (selectedIds != null)
            {
                using var enumerator = selectedIds.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    selectedId = enumerator.Current;
                    return true;
                }
            }

            selectedId = default;
            return false;
        }
    }
}
