using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Gameplay
{
    public sealed class SceneSystem : ProcedureCapacitySystem, ISceneBeforeEnter, ISceneBeforeLeave
    {
        UniTask ISceneBeforeEnter.BeforeEnterSceneAsync(SceneTransitionContext context, CancellationToken cancellationToken)
        {
            ClearScene();

            var movementPath = Object.FindFirstObjectByType<SceneMovementPath>();
            this.ExecuteCommand(new BuildMovementRoutesCommand(movementPath));

            var routeModel = this.GetModel<MovementRouteModel>();
            var spawnPointModel = this.GetModel<SpawnPointModel>();
            foreach (var spawnPoint in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
            {
                if (!routeModel.Contains(spawnPoint.RouteId))
                {
                    Debug.LogError($"[Scene] 出生点 [{spawnPoint.SpawnPointId}] 引用了不存在的路线 [{spawnPoint.RouteId}]，已跳过");
                    continue;
                }

                spawnPointModel.TryAdd(new SpawnPointData(spawnPoint.SpawnPointId, spawnPoint.RouteId, spawnPoint.transform.position, spawnPoint.transform.rotation), spawnPoint.IsDefault);
            }

            return UniTask.CompletedTask;
        }

        UniTask ISceneBeforeLeave.BeforeLeaveSceneAsync(CancellationToken cancellationToken)
        {
            ClearScene();
            return UniTask.CompletedTask;
        }

        private void ClearScene()
        {
            this.GetModel<SpawnPointModel>().Clear();
            this.GetModel<MovementRouteModel>().Clear();
        }
    }
}
