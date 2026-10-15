using System;
using System.Collections.Generic;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct CaptureGameSaveDataQuery : IQuery<GameSaveData>
    {
        GameSaveData IQuery<GameSaveData>.Execute(IQueryContext context)
        {
            var characterModel = context.GetModel<CharacterModel>();
            var gameStateModel = context.GetModel<GameStateModel>();
            if (gameStateModel.CurrentSceneId <= 0)
            {
                throw new InvalidOperationException("Game save data cannot be captured without an active scene.");
            }

            var npcTable = context.GetModel<TablesModel>().NpcTable;
            var npcs = new List<NpcSaveData>();
            foreach (var (npcId, npc) in characterModel.Npcs)
            {
                if (npcTable.TryGet(npcId, out var npcEntry) && !HasNpcChanged(npc, npcEntry))
                {
                    continue;
                }

                var npcSaveData = CaptureNpc(npc);
                if (npcSaveData != null)
                {
                    npcs.Add(npcSaveData);
                }
            }

            return new GameSaveData
            {
                SceneId = gameStateModel.CurrentSceneId,
                SaveTimeUtcTicks = DateTime.UtcNow.Ticks,
                PlayerCharacter = CapturePlayerCharacter(characterModel.PlayerCharacter),
                Npcs = npcs,
                CompletedSceneEvents = new List<int>(gameStateModel.CompletedSceneEvents),
            };
        }

        private static bool HasNpcChanged(NpcCharacterState state, NpcEntry entry)
        {
            if (state.Favorability.Value != 0)
            {
                return true;
            }

            var emotion = state.Emotion;
            Span<int> initialValues = stackalloc int[(int)EmotionId.Count];
            initialValues.Clear();
            foreach (var initialEmotion in entry.InitialEmotions)
            {
                initialValues[(int)initialEmotion.Id] = initialEmotion.Value;
            }

            for (var i = 0; i < initialValues.Length; i++)
            {
                var id = (EmotionId)i;
                if (emotion.GetValue(id) != initialValues[i] || emotion.GetThreshold(id) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static PlayerCharacterSaveData CapturePlayerCharacter(PlayerCharacterState state)
        {
            if (state == null)
            {
                return null;
            }

            return new PlayerCharacterSaveData
            {
                Id = state.CharacterId,
                Stats = CaptureStats(state.Stats),
                Emotion = CaptureEmotion(state.Emotion),
                Spatial = CaptureSpatial(state.Spatial),
            };
        }

        private static NpcSaveData CaptureNpc(NpcCharacterState state)
        {
            if (state == null)
            {
                return null;
            }

            return new NpcSaveData
            {
                Id = state.CharacterId,
                Emotion = CaptureEmotion(state.Emotion),
                Favorability = state.Favorability.Value,
            };
        }

        private static StatsSaveData CaptureStats(StatsState data)
        {
            var attributes = new int[(int)AttributeId.Count];
            for (var i = 0; i < attributes.Length; i++)
            {
                attributes[i] = data.GetAttribute((AttributeId)i);
            }

            var vitals = new RangedValue[(int)VitalId.Count];
            for (var i = 0; i < vitals.Length; i++)
            {
                vitals[i] = data.GetVital((VitalId)i);
            }

            return new StatsSaveData
            {
                Attributes = attributes,
                Vitals = vitals,
            };
        }

        private static EmotionSaveData CaptureEmotion(EmotionState data)
        {
            var values = new int[(int)EmotionId.Count];
            var thresholds = new int[(int)EmotionId.Count];
            for (var i = 0; i < values.Length; i++)
            {
                var id = (EmotionId)i;
                values[i] = data.GetValue(id);
                thresholds[i] = data.GetThreshold(id);
            }

            return new EmotionSaveData
            {
                Values = values,
                Thresholds = thresholds,
            };
        }

        private static SpatialSaveData CaptureSpatial(SpatialState data)
        {
            if (!data.RouteId.HasValue)
            {
                throw new InvalidOperationException("Player route id is empty.");
            }

            return new SpatialSaveData
            {
                RouteId = data.RouteId,
                Position = data.Position,
                Rotation = data.Rotation,
            };
        }
    }
}
