using System.Linq;
using Aoiro;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Editor
{
    public sealed class SceneRouteIdAttributeDrawer : OdinAttributeDrawer<SceneRouteIdAttribute, int>, IEditorController
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var routeSystem = this.GetSystem<SceneMovementRouteEditorSystem>();
            var routeModel = this.GetModel<MovementRouteModel>();

            if (!routeSystem.EnsureReady())
            {
                SirenixEditorGUI.MessageBox("当前场景没有移动路径。", MessageType.Warning);
                CallNextDrawer(label);
                return;
            }

            var routes = routeSystem.Source.EditorRoutes;
            if (routes.Count == 0)
            {
                SirenixEditorGUI.MessageBox("当前场景没有可用路线。", MessageType.Warning);
                CallNextDrawer(label);
                return;
            }

            var currentId = ValueEntry.SmartValue;
            var display = routeModel.Contains(currentId) ? FormatOption(currentId) : "-";
            var selectedIds = OdinSelector<int>.DrawSelectorDropdown(label, new GUIContent(display), rect =>
            {
                var orderedRoutes = routes.OrderBy(route => route.RouteId).ToArray();
                var items = new GenericSelectorItem<int>[orderedRoutes.Length];
                for (var i = 0; i < orderedRoutes.Length; i++)
                {
                    items[i] = new GenericSelectorItem<int>(FormatOption(orderedRoutes[i].RouteId), orderedRoutes[i].RouteId);
                }

                var selector = new GenericSelector<int>("选择路线", false, items);
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

            if (!routeModel.Contains(ValueEntry.SmartValue))
            {
                SirenixEditorGUI.MessageBox($"路线 [{ValueEntry.SmartValue}] 不存在。", MessageType.Warning);
            }
        }

        private static string FormatOption(int routeId)
        {
            return routeId.ToString();
        }
    }
}
