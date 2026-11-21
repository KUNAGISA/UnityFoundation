using Aoiro;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Editor
{
    [CustomEditor(typeof(SceneEventPoint))]
    public sealed class SceneEventPointEditor : OdinEditor, IEditorController
    {
        private const string RootName = "场景事件";

        private MovementRouteModel _routeModel;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            foreach (var editorTarget in targets)
            {
                if (editorTarget is SceneEventPoint point)
                {
                    SyncTriggerMode(point);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _routeModel = this.GetModel<MovementRouteModel>();
        }

        private void OnSceneGUI()
        {
            SnapToBoundRoute();
        }

        [MenuItem("GameObject/场景/场景事件", false, 10003)]
        private static void CreateSceneEvent(MenuCommand menuCommand)
        {
            var context = menuCommand.context as GameObject;
            var scene = context != null ? context.scene : SceneManager.GetActiveScene();
            var root = FindOrCreateRoot(scene);
            var eventId = GetNextEventId(scene);
            var gameObject = new GameObject($"场景事件 {eventId}");

            GameObjectUtility.SetParentAndAlign(gameObject, root);
            var point = gameObject.AddComponent<SceneEventPoint>();
            var serializedObject = new SerializedObject(point);
            serializedObject.FindProperty("_eventId").intValue = eventId;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Scene Event");
            Selection.activeGameObject = gameObject;
        }

        private static void SyncTriggerMode(SceneEventPoint point)
        {
            if (point.TriggerMode == SceneEventTriggerMode.EnterTrigger)
            {
                EnsureEnterTriggerComponents(point);
                return;
            }

            RemoveEnterTriggerComponents(point);
        }

        private static void EnsureEnterTriggerComponents(SceneEventPoint point)
        {
            var gameObject = point.gameObject;
            if (!gameObject.TryGetComponent<BoxCollider2D>(out var boxCollider))
            {
                boxCollider = Undo.AddComponent<BoxCollider2D>(gameObject);
            }

            if (!boxCollider.isTrigger)
            {
                Undo.RecordObject(boxCollider, "Configure Scene Event Trigger Collider");
                boxCollider.isTrigger = true;
                EditorUtility.SetDirty(boxCollider);
            }

            if (!gameObject.TryGetComponent<SceneEventTrigger>(out _))
            {
                Undo.AddComponent<SceneEventTrigger>(gameObject);
            }
        }

        private static void RemoveEnterTriggerComponents(SceneEventPoint point)
        {
            var gameObject = point.gameObject;
            if (gameObject.TryGetComponent<SceneEventTrigger>(out var trigger))
            {
                Undo.DestroyObjectImmediate(trigger);
            }

            if (gameObject.TryGetComponent<BoxCollider2D>(out var boxCollider))
            {
                Undo.DestroyObjectImmediate(boxCollider);
            }
        }

        private void SnapToBoundRoute()
        {
            var routeSystem = this.GetSystem<SceneMovementRouteEditorSystem>();
            if (!routeSystem.EnsureReady())
            {
                return;
            }

            var point = (SceneEventPoint)target;
            var route = point.Route;
            if (!route.Enabled)
            {
                return;
            }

            if (!_routeModel.Contains(route.RouteId))
            {
                return;
            }

            if (SceneRouteSnapEditorUtility.SnapToRoute(point.transform, route.RouteId, _routeModel, "Snap Scene Event To Route"))
            {
                EditorUtility.SetDirty(point.transform);
                EditorUtility.SetDirty(point);
            }
        }

        private static GameObject FindOrCreateRoot(Scene scene)
        {
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.name == RootName)
                {
                    return gameObject;
                }
            }

            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create Scene Event Root");
            return root;
        }

        private static int GetNextEventId(Scene scene)
        {
            var maxId = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var point in root.GetComponentsInChildren<SceneEventPoint>(true))
                {
                    maxId = Mathf.Max(maxId, point.EventId);
                }
            }

            return maxId + 1;
        }
    }
}
