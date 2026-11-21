using Aoiro;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Editor
{
    [CustomEditor(typeof(SceneMovementPath))]
    public sealed class SceneMovementPathEditor : OdinEditor, IEditorController
    {
        private const float NodePickSize = 0.12f;
        private const float NodeDrawSize = 0.08f;
        private const float InfoMessageFontScale = 1.2f;
        private const int BezierPreviewSteps = 32;

        private static GUIStyle _largeInfoMessageStyle;

        private static GUIStyle LargeInfoMessageStyle
        {
            get
            {
                if (_largeInfoMessageStyle != null)
                {
                    return _largeInfoMessageStyle;
                }

                var fontSize = EditorStyles.label.fontSize > 0 ? EditorStyles.label.fontSize : 12;
                _largeInfoMessageStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = Mathf.RoundToInt(fontSize * InfoMessageFontScale),
                    wordWrap = true
                };
                return _largeInfoMessageStyle;
            }
        }

        private int _routeIndex;
        private int _selectedNodeIndex = -1;

        private SceneMovementPath Path => (SceneMovementPath)target;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            EditorGUILayout.Space(4f);
            DrawRouteToolbar();
            EditorGUILayout.Space(4f);
            DrawSelectedNodeInspector();
            if (serializedObject.ApplyModifiedProperties())
            {
                InvalidateRouteCache();
            }
        }

        private void OnSceneGUI()
        {
            var path = Path;
            ClampSelection(path);
            DrawRoutes(path);
            HandleSceneInput(path);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(path);
            }
        }

        private void DrawRouteToolbar()
        {
            var path = Path;
            var routes = path.EditorRoutes;

            var options = new string[routes.Count];
            for (var i = 0; i < routes.Count; i++)
            {
                options[i] = $"Route {routes[i].RouteId}";
            }

            SirenixEditorGUI.BeginHorizontalToolbar(22f, 4);
            try
            {
                using (new EditorGUI.DisabledScope(routes.Count == 0))
                {
                    _routeIndex = SirenixEditorFields.Dropdown("编辑路径", Mathf.Clamp(_routeIndex, 0, Mathf.Max(0, routes.Count - 1)), options);
                }

                if (SirenixEditorGUI.ToolbarButton("Add", true))
                {
                    Undo.RecordObject(path, "Add Movement Route");
                    routes.Add(new MovementRouteData
                    {
                        RouteId = GetNextRouteId(routes),
                        Color = Color.white
                    });
                    _routeIndex = routes.Count - 1;
                    _selectedNodeIndex = -1;
                    InvalidateRouteCache();
                    EditorUtility.SetDirty(path);
                }

                using (new EditorGUI.DisabledScope(routes.Count == 0))
                {
                    if (SirenixEditorGUI.ToolbarButton("Delete", true))
                    {
                        Undo.RecordObject(path, "Delete Movement Route");
                        routes.RemoveAt(_routeIndex);
                        _routeIndex = Mathf.Clamp(_routeIndex, 0, Mathf.Max(0, routes.Count - 1));
                        _selectedNodeIndex = -1;
                        InvalidateRouteCache();
                        EditorUtility.SetDirty(path);
                    }
                }
            }
            finally
            {
                SirenixEditorGUI.EndHorizontalToolbar();
            }
        }

        private void DrawSelectedNodeInspector()
        {
            if (!TryGetSelectedRoute(out var route) || _selectedNodeIndex < 0 || _selectedNodeIndex >= route.Nodes.Count)
            {
                DrawLargeInfoMessageBox("SceneView 中 Ctrl + 左键追加节点；点击节点选中，拖动节点或控制柄编辑。");
                return;
            }

            var path = Path;
            var node = route.Nodes[_selectedNodeIndex];
            EditorGUILayout.LabelField($"选中节点 {_selectedNodeIndex}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var position = SirenixEditorFields.Vector2Field("坐标", node.Position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Move Movement Node");
                MoveNodeTo(route, _selectedNodeIndex, position);
                InvalidateRouteCache();
                EditorUtility.SetDirty(path);
                node = route.Nodes[_selectedNodeIndex];
            }

            EditorGUI.BeginChangeCheck();
            var inTangent = SirenixEditorFields.Vector2Field("入切线", node.InTangent);
            var outTangent = SirenixEditorFields.Vector2Field("出切线", node.OutTangent);
            if (_selectedNodeIndex < route.Nodes.Count - 1)
            {
                node.NextSegmentType = (PathSegmentType)EditorGUILayout.EnumPopup("下一段", node.NextSegmentType);
            }

            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(path, "Edit Movement Node");
            node.InTangent = inTangent;
            node.OutTangent = outTangent;
            route.Nodes[_selectedNodeIndex] = node;
            InvalidateRouteCache();
            EditorUtility.SetDirty(path);
        }

        private void DrawRoutes(SceneMovementPath path)
        {
            for (var routeIndex = 0; routeIndex < path.EditorRoutes.Count; routeIndex++)
            {
                var route = path.EditorRoutes[routeIndex];
                if (route?.Nodes == null)
                {
                    continue;
                }

                var routeColor = route.Color;
                var isActiveRoute = routeIndex == _routeIndex;
                var z = path.transform.position.z;
                Handles.color = isActiveRoute ? routeColor : new Color(routeColor.r, routeColor.g, routeColor.b, 0.35f);

                DrawSegments(route, z);
                DrawNodes(path, route, routeIndex, isActiveRoute, z);
                if (isActiveRoute)
                {
                    DrawBezierHandles(path, route, z);
                }
            }
        }

        private void DrawNodes(SceneMovementPath path, MovementRouteData route, int routeIndex, bool isActiveRoute, float z)
        {
            for (var nodeIndex = 0; nodeIndex < route.Nodes.Count; nodeIndex++)
            {
                var node = route.Nodes[nodeIndex];
                var nodePosition = ToHandlePosition(node.Position, z);
                var size = HandleUtility.GetHandleSize(nodePosition) * NodeDrawSize;
                Handles.color = isActiveRoute && nodeIndex == _selectedNodeIndex ? Color.yellow : route.Color;
                if (Handles.Button(nodePosition, Quaternion.identity, size, HandleUtility.GetHandleSize(nodePosition) * NodePickSize, Handles.CubeHandleCap))
                {
                    _routeIndex = routeIndex;
                    _selectedNodeIndex = nodeIndex;
                    GUI.changed = true;
                }

                if (!isActiveRoute || nodeIndex != _selectedNodeIndex)
                {
                    continue;
                }

                EditorGUI.BeginChangeCheck();
                var handleSize = HandleUtility.GetHandleSize(nodePosition) * NodePickSize;
                var newPosition = ToVector2(Handles.FreeMoveHandle(nodePosition, handleSize, Vector3.zero, Handles.CubeHandleCap));
                if (Event.current.shift && nodeIndex > 0)
                {
                    newPosition = LockToDominantAxis(route.Nodes[nodeIndex - 1].Position, newPosition);
                }

                if (!EditorGUI.EndChangeCheck())
                {
                    continue;
                }

                Undo.RecordObject(path, "Move Movement Node");
                MoveNodeTo(route, nodeIndex, newPosition);
                InvalidateRouteCache();
                GUI.changed = true;
            }
        }

        private void DrawBezierHandles(SceneMovementPath path, MovementRouteData route, float z)
        {
            if (_selectedNodeIndex < 0 || _selectedNodeIndex >= route.Nodes.Count)
            {
                return;
            }

            var node = route.Nodes[_selectedNodeIndex];
            if (_selectedNodeIndex < route.Nodes.Count - 1 && node.NextSegmentType == PathSegmentType.Bezier)
            {
                var nodePosition = ToHandlePosition(node.Position, z);
                var outPosition = ToHandlePosition(node.OutTangent, z);
                Handles.color = Color.cyan;
                Handles.DrawLine(nodePosition, outPosition);
                EditorGUI.BeginChangeCheck();
                var outSize = HandleUtility.GetHandleSize(outPosition) * NodePickSize;
                var outTangent = ToVector2(Handles.FreeMoveHandle(outPosition, outSize, Vector3.zero, Handles.CubeHandleCap));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(path, "Move Bezier Out Tangent");
                    node.OutTangent = outTangent;
                    route.Nodes[_selectedNodeIndex] = node;
                    InvalidateRouteCache();
                    GUI.changed = true;
                }
            }

            if (_selectedNodeIndex <= 0 || route.Nodes[_selectedNodeIndex - 1].NextSegmentType != PathSegmentType.Bezier)
            {
                return;
            }

            node = route.Nodes[_selectedNodeIndex];
            var inNodePosition = ToHandlePosition(node.Position, z);
            var inPosition = ToHandlePosition(node.InTangent, z);
            Handles.color = Color.magenta;
            Handles.DrawLine(inNodePosition, inPosition);
            EditorGUI.BeginChangeCheck();
            var inSize = HandleUtility.GetHandleSize(inPosition) * NodePickSize;
            var inTangent = ToVector2(Handles.FreeMoveHandle(inPosition, inSize, Vector3.zero, Handles.CubeHandleCap));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Move Bezier In Tangent");
                node.InTangent = inTangent;
                route.Nodes[_selectedNodeIndex] = node;
                InvalidateRouteCache();
                GUI.changed = true;
            }
        }

        private void HandleSceneInput(SceneMovementPath path)
        {
            var evt = Event.current;
            if (evt == null)
            {
                return;
            }

            if ((evt.type == EventType.KeyDown) && (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace))
            {
                DeleteSelectedNode(path);
                evt.Use();
                return;
            }

            if (evt.type != EventType.MouseDown || evt.button != 0 || !evt.control || !TryGetSelectedRoute(out var route))
            {
                return;
            }

            if (!TryGetMouseWorldPosition(path, route, evt.mousePosition, out var position))
            {
                return;
            }

            Undo.RecordObject(path, "Add Movement Node");
            route.Nodes.Add(CreateNode(position, route));
            _selectedNodeIndex = route.Nodes.Count - 1;
            InvalidateRouteCache();
            EditorUtility.SetDirty(path);
            GUI.changed = true;
            evt.Use();
        }

        private void DeleteSelectedNode(SceneMovementPath path)
        {
            if (!TryGetSelectedRoute(out var route) || _selectedNodeIndex < 0 || _selectedNodeIndex >= route.Nodes.Count)
            {
                return;
            }

            Undo.RecordObject(path, "Delete Movement Node");
            var removedIndex = _selectedNodeIndex;
            route.Nodes.RemoveAt(removedIndex);
            if (removedIndex > 0 && removedIndex - 1 < route.Nodes.Count)
            {
                var previous = route.Nodes[removedIndex - 1];
                previous.NextSegmentType = PathSegmentType.Line;
                route.Nodes[removedIndex - 1] = previous;
            }

            _selectedNodeIndex = -1;
            InvalidateRouteCache();
            EditorUtility.SetDirty(path);
            GUI.changed = true;
        }

        private bool TryGetMouseWorldPosition(SceneMovementPath path, MovementRouteData route, Vector2 mousePosition, out Vector2 position)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var z = path.transform.position.z;
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, z));
            if (!plane.Raycast(ray, out var enter))
            {
                position = default;
                return false;
            }

            position = ToVector2(ray.GetPoint(enter));
            return true;
        }

        private bool TryGetSelectedRoute(out MovementRouteData route)
        {
            var routes = Path.EditorRoutes;
            if (routes.Count == 0)
            {
                route = null;
                return false;
            }

            _routeIndex = Mathf.Clamp(_routeIndex, 0, routes.Count - 1);
            route = routes[_routeIndex];
            return route != null;
        }

        private void ClampSelection(SceneMovementPath path)
        {
            _routeIndex = Mathf.Clamp(_routeIndex, 0, Mathf.Max(0, path.EditorRoutes.Count - 1));
            if (!TryGetSelectedRoute(out var route) || _selectedNodeIndex >= route.Nodes.Count)
            {
                _selectedNodeIndex = -1;
            }
        }

        private static void DrawLargeInfoMessageBox(string message)
        {
            SirenixEditorGUI.MessageBox(message, MessageType.Info, LargeInfoMessageStyle, true);
        }

        private void InvalidateRouteCache()
        {
            this.GetSystem<SceneMovementRouteEditorSystem>().Invalidate();
        }

        [DrawGizmo(GizmoType.NonSelected)]
        private static void DrawMovementPathPreview(SceneMovementPath path, GizmoType gizmoType)
        {
            var z = path.transform.position.z;
            foreach (var route in path.EditorRoutes)
            {
                if (route?.Nodes == null)
                {
                    continue;
                }

                Handles.color = route.Color;
                DrawSegments(route, z);
            }
        }

        [MenuItem("GameObject/场景/移动路径", false, 1000)]
        private static void SelectOrCreateSceneMovementPath(MenuCommand menuCommand)
        {
            var movementPath = Object.FindFirstObjectByType<SceneMovementPath>();
            if (movementPath != null)
            {
                Selection.activeGameObject = movementPath.gameObject;
                EditorGUIUtility.PingObject(movementPath.gameObject);
                return;
            }

            var gameObject = new GameObject("移动路径");
            GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);
            gameObject.AddComponent<SceneMovementPath>();
            GameObjectUtility.EnsureUniqueNameForSibling(gameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Movement Path");
            Selection.activeGameObject = gameObject;
        }

        private static void DrawSegments(MovementRouteData route, float z)
        {
            for (var i = 0; i < route.Nodes.Count - 1; i++)
            {
                var current = route.Nodes[i];
                var next = route.Nodes[i + 1];
                if (current.NextSegmentType == PathSegmentType.Line)
                {
                    Handles.DrawLine(ToHandlePosition(current.Position, z), ToHandlePosition(next.Position, z));
                    continue;
                }

                var previous = current.Position;
                for (var step = 1; step <= BezierPreviewSteps; step++)
                {
                    var t = (float)step / BezierPreviewSteps;
                    var position = PathMath.EvaluateBezier(current.Position, current.OutTangent, next.InTangent, next.Position, t);
                    Handles.DrawLine(ToHandlePosition(previous, z), ToHandlePosition(position, z));
                    previous = position;
                }
            }
        }

        private static PathNode CreateNode(Vector2 position, MovementRouteData route)
        {
            var tangentOffset = Vector2.right;
            if (route.Nodes.Count > 0)
            {
                var previous = route.Nodes[^1].Position;
                var direction = position - previous;
                if (direction.sqrMagnitude > Mathf.Epsilon)
                {
                    tangentOffset = direction.normalized * Mathf.Max(direction.magnitude * 0.33f, 0.1f);
                }
            }

            return new PathNode
            {
                Position = position,
                NextSegmentType = PathSegmentType.Line,
                InTangent = position - tangentOffset,
                OutTangent = position + tangentOffset
            };
        }

        private static void MoveNodeTo(MovementRouteData route, int nodeIndex, Vector2 position)
        {
            var node = route.Nodes[nodeIndex];
            var delta = position - node.Position;
            node.Position = position;
            node.InTangent += delta;
            node.OutTangent += delta;
            route.Nodes[nodeIndex] = node;
        }

        private static Vector2 LockToDominantAxis(Vector2 origin, Vector2 position)
        {
            var delta = position - origin;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                position.y = origin.y;
            }
            else
            {
                position.x = origin.x;
            }

            return position;
        }

        private static Vector3 ToHandlePosition(Vector2 position, float z)
        {
            return new Vector3(position.x, position.y, z);
        }

        private static Vector2 ToVector2(Vector3 position)
        {
            return new Vector2(position.x, position.y);
        }

        private static int GetNextRouteId(System.Collections.Generic.List<MovementRouteData> routes)
        {
            var routeId = 1001;
            for (var i = 0; i < routes.Count; i++)
            {
                routeId = Mathf.Max(routeId, routes[i].RouteId + 1);
            }

            return routeId;
        }
    }
}
