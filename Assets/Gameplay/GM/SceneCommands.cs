using System;
using Aoiro;
using QFSW.QC;
using UnityEngine;
using UnityEngine.Scripting;

namespace Gameplay.GM
{
    [CommandPrefix("scene.")]
    internal static class SceneCommands
    {
        [Preserve]
        [Command("transition", "切换当前场景")]
        private static void Transition([CommandParameterDescription("场景Id")] int sceneId)
        {
            GmContext.Shared.GetSystem<SceneTransitionSystem>().RequestTransition(new SceneTransitionRequest(sceneId, SceneTransitionReason.System));
        }

        [Preserve]
        [Command("teleport-spawn", "传送主角到当前场景的指定出生点")]
        private static void TeleportSpawn([CommandParameterDescription("出生点 ID")] int spawnPointId)
        {
            var player = GetCurrentPlayer();
            var context = GmContext.Shared;
            if (!context.GetModel<SpawnPointModel>().TryGet(spawnPointId, out var spawnPoint))
            {
                throw new ArgumentException($"当前场景没有出生点 [{spawnPointId}]。", nameof(spawnPointId));
            }

            if (!context.GetModel<MovementRouteModel>().TryProject(spawnPoint.RouteId, spawnPoint.Position, out var location))
            {
                throw new InvalidOperationException($"出生点 [{spawnPointId}] 无法投影到绑定路线 [{spawnPoint.RouteId}]。");
            }

            Teleport(player, location, spawnPoint.Facing);
        }

        [Preserve]
        [Command("teleport-position", "将主角传送到当前场景中距指定世界坐标最近的路线位置")]
        private static void TeleportPosition([CommandParameterDescription("世界坐标 X")] float x, [CommandParameterDescription("世界坐标 Y")] float y)
        {
            if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))
            {
                throw new ArgumentException("世界坐标 X 和 Y 必须为有限数值。");
            }

            var player = GetCurrentPlayer();
            if (!GmContext.Shared.GetModel<MovementRouteModel>().TryProjectToClosestRoute(new Vector2(x, y), out var location))
            {
                throw new InvalidOperationException("当前场景没有可投影的运动路线。");
            }

            Teleport(player, location, player.Motor.Facing);
        }

        private static CharacterActor GetCurrentPlayer()
        {
            var context = GmContext.Shared;
            var procedure = context.GetSystem<ProcedureSystem>();
            if (procedure.State != ProcedureState.Running || procedure.HasPauseReason(ProcedurePauseReason.SceneTransition) ||
                context.GetModel<GameStateModel>().CurrentSceneId <= 0 ||
                !context.GetSystem<EntitySystem>().TryGetEntityByCharacterId(PlayerCharacterState.PlayerId, out var player))
            {
                throw new InvalidOperationException("请等待游戏场景加载完成后再传送。");
            }

            return player;
        }

        private static void Teleport(CharacterActor player, in RouteLocation location, FacingDirection facing)
        {
            player.Motor.WarpToRoute(location, facing);
            GmContext.Shared.GetSystem<CameraSystem>().GetModule<PlayerCamera>().Snap();
        }
    }
}
