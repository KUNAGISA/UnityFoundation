using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Editor
{
    internal static class SceneRouteSnapEditorUtility
    {
        private const float SnapEpsilon = 0.000001f;
        
        public static bool SnapToRoute(Transform targetTransform, int routeId, MovementRouteModel routeModel, string undoName)
        {
            if (!routeModel.TryProject(routeId, targetTransform.position, out var location))
            {
                Debug.LogError($"无法投影到路线 [{routeId}]。", targetTransform);
                return false;
            }

            var position = new Vector3(location.Position.x, location.Position.y, targetTransform.position.z);
            if ((targetTransform.position - position).sqrMagnitude <= SnapEpsilon)
            {
                return false;
            }

            Undo.RecordObject(targetTransform, undoName);
            targetTransform.position = position;
            EditorUtility.SetDirty(targetTransform);
            return true;
        }
    }
}
