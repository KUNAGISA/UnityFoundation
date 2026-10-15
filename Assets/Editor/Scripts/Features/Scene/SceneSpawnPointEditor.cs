using Aoiro;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Editor
{
    [CustomEditor(typeof(SceneSpawnPoint))]
    public class SceneSpawnPointEditor : OdinEditor, IEditorController
    {
        private const string SpawnPointRootName = "出生点";

        private MovementRouteModel _routeModel;

        protected override void OnEnable()
        {
            base.OnEnable();
            _routeModel = this.GetModel<MovementRouteModel>();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        private void OnSceneGUI()
        {
            SnapToBoundRoute();
        }

        [MenuItem("GameObject/场景/出生点", false, 10001)]
        private static void CreateSceneSpawnPoint(MenuCommand menuCommand)
        {
            var context = menuCommand.context as GameObject;
            var scene = context != null ? context.scene : SceneManager.GetActiveScene();
            var root = FindOrCreateRoot(scene);
            var spawnPointId = GetNextSpawnPointId(scene);
            var gameObject = new GameObject($"出生点 {spawnPointId}");

            GameObjectUtility.SetParentAndAlign(gameObject, root);
            var spawnPoint = gameObject.AddComponent<SceneSpawnPoint>();
            var serializedObject = new SerializedObject(spawnPoint);
            serializedObject.FindProperty("_spawnPointId").intValue = spawnPointId;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Scene Spawn Point");
            Selection.activeGameObject = gameObject;
        }
        
        private void SnapToBoundRoute()
        {
            var routeSystem = this.GetSystem<SceneMovementRouteEditorSystem>();
            if (!routeSystem.EnsureReady())
            {
                return;
            }
            
            var spawnPoint = (SceneSpawnPoint)target;
            if (!_routeModel.Contains(spawnPoint.RouteId))
            {
                return;
            }
            
            if (SceneRouteSnapEditorUtility.SnapToRoute(spawnPoint.transform, spawnPoint.RouteId, _routeModel, "Snap Spawn Point To Route"))
            {
                EditorUtility.SetDirty(spawnPoint);
            }
        }
        
        private static GameObject FindOrCreateRoot(Scene scene)
        {
            foreach (var gameObject in scene.GetRootGameObjects())
            {
                if (gameObject.name == SpawnPointRootName)
                {
                    return gameObject;
                }
            }

            var root = new GameObject(SpawnPointRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create Scene Spawn Point Root");
            return root;
        }

        private static int GetNextSpawnPointId(Scene scene)
        {
            var maxId = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var spawnPoint in root.GetComponentsInChildren<SceneSpawnPoint>(true))
                {
                    maxId = Mathf.Max(maxId, spawnPoint.SpawnPointId);
                }
            }

            return maxId + 1;
        }
    }
}
