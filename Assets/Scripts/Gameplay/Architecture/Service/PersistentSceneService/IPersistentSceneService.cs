using Aoiro;
using UnityEngine;

namespace Gameplay
{
    public interface IPersistentSceneService : IService
    {
        void MoveToPersistent(GameObject target);
    }
}