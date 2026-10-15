using Aoiro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Editor
{
    public sealed class SceneMovementRouteEditorSystem : SystemBase, IEditorController
    {
        private SceneMovementPath _source;
        private bool _isDirty = true;

        public SceneMovementPath Source => _source;

        protected override void OnInitialize()
        {
            Undo.undoRedoPerformed += Invalidate;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        protected override void OnShutdown()
        {
            Undo.undoRedoPerformed -= Invalidate;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            _source = null;
            _isDirty = true;
        }

        public void Invalidate()
        {
            _isDirty = true;
        }

        public bool EnsureReady()
        {
            var source = Object.FindFirstObjectByType<SceneMovementPath>();
            if (!_isDirty && source == _source)
            {
                return source != null;
            }

            _source = source;
            _isDirty = false;
            this.ExecuteCommand(new BuildMovementRoutesCommand(source));
            return source != null;
        }

        public void Clear()
        {
            _source = null;
            _isDirty = true;
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Clear();
        }
    }
}
