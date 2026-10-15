using System.Linq;
using Aoiro;
using Gameplay.Tables;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Editor
{
    public sealed class SceneEventIdAttributeDrawer : OdinAttributeDrawer<SceneEventIdAttribute, int>, IEditorController
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var sceneEventTable = this.GetModel<TablesModel>().SceneEventTable;
            if (!sceneEventTable.DataMap.Any())
            {
                SirenixEditorGUI.MessageBox("SceneEventTable 没有可用场景事件。", MessageType.Warning);
                CallNextDrawer(label);
                return;
            }

            var currentId = ValueEntry.SmartValue;
            var display = sceneEventTable.TryGet(currentId, out var currentEntry) ? FormatOption(currentEntry) : "-";
            var selectedIds = OdinSelector<int>.DrawSelectorDropdown(label, new GUIContent(display), rect =>
            {
                var entries = sceneEventTable.DataMap.Values.OrderBy(entry => entry.EventId).ToArray();
                var items = new GenericSelectorItem<int>[entries.Length];
                for (var i = 0; i < entries.Length; i++)
                {
                    items[i] = new GenericSelectorItem<int>(FormatOption(entries[i]), entries[i].EventId);
                }

                var selector = new GenericSelector<int>("选择场景事件", false, items);
                selector.SetSelection(currentId);
                selector.ShowInPopup(rect);
                selector.EnableSingleClickToSelect();
                return selector;
            });

            var selectedId = selectedIds?.DefaultIfEmpty(currentId).First() ?? currentId;
            if (selectedId != currentId)
            {
                ValueEntry.SmartValue = selectedId;
            }

            if (!sceneEventTable.TryGet(ValueEntry.SmartValue, out _))
            {
                SirenixEditorGUI.MessageBox($"场景事件 [{ValueEntry.SmartValue}] 不存在。", MessageType.Warning);
            }
        }

        private static string FormatOption(SceneEventEntry entry)
        {
            return $"[{entry.EventId}] {entry.Desc}";
        }
    }
}
