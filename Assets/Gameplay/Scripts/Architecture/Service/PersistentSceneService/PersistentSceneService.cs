using Aoiro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay
{
    internal class PersistentSceneService : ServiceBase, IPersistentSceneService
    {
        private Scene _scene = default;
        
        protected override void OnInitialize()
        {
            _scene = SceneManager.CreateScene("PersistentScene");
        }

        protected override void OnShutdown()
        {
            SceneManager.UnloadSceneAsync(_scene);
            _scene = default;
        }

        public void MoveToPersistent(GameObject target)
        {
            SceneManager.MoveGameObjectToScene(target, _scene);
        }
    }
}