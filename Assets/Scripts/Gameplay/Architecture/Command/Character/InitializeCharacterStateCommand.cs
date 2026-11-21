using System;
using Aoiro;
using Gameplay.Tables;

namespace Gameplay
{
    public readonly struct InitializeCharacterStateCommand : ICommand
    {
        void ICommand.Execute(ICommandContext context)
        {
            var characterModel = context.GetModel<CharacterModel>();
            var tablesModel = context.GetModel<TablesModel>();

            characterModel.Cleanup();
            InitializePlayer(characterModel.InitializePlayerCharacter(), tablesModel.PlayerCharacterTable);

            foreach (var (_, npcEntry) in tablesModel.NpcTable.DataMap)
            {
                var npcState = new NpcCharacterState(npcEntry.Id);
                foreach (var emotion in npcEntry.InitialEmotions)
                {
                    npcState.Emotion.SetValue(emotion.Id, emotion.Value);
                }

                if (!characterModel.AddNpc(npcState))
                {
                    throw new InvalidOperationException($"Failed to initialize npc state. NpcId: {npcEntry.Id}");
                }
            }
        }

        private static void InitializePlayer(PlayerCharacterState playerState, PlayerCharacterTable playerCharacterTable)
        {
            playerState.Stats.SetVital(VitalId.Stamina, new RangedValue(playerCharacterTable.InitialMaxHp, playerCharacterTable.InitialMaxHp));
            playerState.Stats.SetVital(VitalId.Sanity, new RangedValue(playerCharacterTable.InitialMaxSan, playerCharacterTable.InitialMaxSan));

            foreach (var attribute in playerCharacterTable.InitialAttributes)
            {
                playerState.Stats.SetAttribute(attribute.Id, attribute.Value);
            }

            foreach (var emotion in playerCharacterTable.InitialEmotions)
            {
                playerState.Emotion.SetValue(emotion.Id, emotion.Value);
            }

            foreach (var emotionThreshold in playerCharacterTable.InitialEmotionThresholds)
            {
                playerState.Emotion.SetThreshold(emotionThreshold.Id, emotionThreshold.Value);
            }
        }
    }
}
