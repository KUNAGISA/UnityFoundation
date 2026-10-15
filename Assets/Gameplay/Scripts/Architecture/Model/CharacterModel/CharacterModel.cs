using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aoiro;

namespace Gameplay
{
    public class CharacterModel : ModelBase
    {
        private readonly Dictionary<int, NpcCharacterState> _npcs = new Dictionary<int, NpcCharacterState>();

        public PlayerCharacterState PlayerCharacter { get; private set; }
        public ReadOnlyDictionary<int, NpcCharacterState> Npcs { get; }

        public CharacterModel()
        {
            Npcs = new ReadOnlyDictionary<int, NpcCharacterState>(_npcs);
        }

        protected override void OnInitialize()
        {
            Cleanup();
        }

        protected override void OnShutdown()
        {
            Cleanup();
        }

        public bool TryGetNpc(int npcId, out NpcCharacterState result)
        {
            return _npcs.TryGetValue(npcId, out result);
        }

        public NpcCharacterState RequireNpc(int npcId)
        {
            if (TryGetNpc(npcId, out var result))
            {
                return result;
            }

            throw new InvalidOperationException($"Npc state was not initialized. NpcId: {npcId}");
        }

        public PlayerCharacterState InitializePlayerCharacter()
        {
            PlayerCharacter = new PlayerCharacterState();
            return PlayerCharacter;
        }

        public bool AddNpc(NpcCharacterState state)
        {
            return state != null && _npcs.TryAdd(state.CharacterId, state);
        }

        public void Cleanup()
        {
            _npcs.Clear();
            PlayerCharacter = null;
        }
    }
}
