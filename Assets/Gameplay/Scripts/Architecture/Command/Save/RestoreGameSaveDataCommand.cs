using System;
using System.Collections.Generic;
using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public readonly struct RestoreGameSaveDataCommand : ICommand
    {
        private readonly GameSaveData _saveData;

        public RestoreGameSaveDataCommand(GameSaveData saveData)
        {
            _saveData = saveData;
        }

        void ICommand.Execute(ICommandContext context)
        {
            var checkResult = context.ExecuteQuery<CheckGameSaveDataQuery, GameSaveDataCheckResult>(new CheckGameSaveDataQuery(_saveData));
            if (checkResult != GameSaveDataCheckResult.Success)
            {
                throw new InvalidOperationException($"Game save data check failed. Result: {checkResult}");
            }

            if (context.GetSystem<EntitySystem>().HasActiveEntities)
            {
                throw new InvalidOperationException("Game save data cannot be restored while active entities exist.");
            }

            Commit(context);
        }

        private void Commit(ICommandContext context)
        {
            var characterModel = context.GetModel<CharacterModel>();
            ApplyPlayerCharacter(_saveData.PlayerCharacter, characterModel.PlayerCharacter);

            if (_saveData.Npcs != null)
            {
                var npcIds = new HashSet<int>();
                foreach (var npcSaveData in _saveData.Npcs)
                {
                    if (npcSaveData == null)
                    {
                        Debug.LogError("[Save] Npc save data is empty and has been skipped.");
                        continue;
                    }

                    if (!npcIds.Add(npcSaveData.Id))
                    {
                        Debug.LogError($"[Save] Duplicate npc save data will be applied in order. NpcId: {npcSaveData.Id}");
                    }

                    if (!characterModel.TryGetNpc(npcSaveData.Id, out var npcData))
                    {
                        Debug.LogError($"[Save] Npc save data references an unknown npc and has been skipped. NpcId: {npcSaveData.Id}");
                        continue;
                    }
                    ApplyNpc(npcSaveData, npcData);
                }
            }

            context.GetModel<GameVariableModel>().Restore(_saveData.Variables);
        }

        private static void ApplyPlayerCharacter(PlayerCharacterSaveData saveData, PlayerCharacterState state)
        {
            if (saveData.Stats != null)
            {
                ApplyStats(saveData.Stats, state.Stats);
            }

            if (saveData.Emotion != null)
            {
                ApplyEmotion(saveData.Emotion, state.Emotion);
            }

            ApplySpatial(saveData.Spatial, state.Spatial);
        }

        private static void ApplyNpc(NpcSaveData saveData, NpcCharacterState state)
        {
            if (saveData.Emotion != null)
            {
                ApplyEmotion(saveData.Emotion, state.Emotion);
            }

            state.Favorability.Value = saveData.Favorability;
        }

        private static void ApplyStats(StatsSaveData saveData, StatsState data)
        {
            if (saveData.Attributes != null)
            {
                var count = Math.Min(saveData.Attributes.Length, (int)AttributeId.Count);
                for (var i = 0; i < count; i++)
                {
                    data.SetAttribute((AttributeId)i, saveData.Attributes[i]);
                }
            }

            if (saveData.Vitals != null)
            {
                var count = Math.Min(saveData.Vitals.Length, (int)VitalId.Count);
                for (var i = 0; i < count; i++)
                {
                    data.SetVital((VitalId)i, saveData.Vitals[i]);
                }
            }
        }

        private static void ApplyEmotion(EmotionSaveData saveData, EmotionState data)
        {
            if (saveData.Values != null)
            {
                var count = Math.Min(saveData.Values.Length, (int)EmotionId.Count);
                for (var i = 0; i < count; i++)
                {
                    data.SetValue((EmotionId)i, saveData.Values[i]);
                }
            }

            if (saveData.Thresholds != null)
            {
                var count = Math.Min(saveData.Thresholds.Length, (int)EmotionId.Count);
                for (var i = 0; i < count; i++)
                {
                    data.SetThreshold((EmotionId)i, saveData.Thresholds[i]);
                }
            }
        }

        private static void ApplySpatial(SpatialSaveData saveData, SpatialState data)
        {
            data.SetPose(saveData.RouteId, saveData.Position, saveData.Rotation);
        }
    }
}
