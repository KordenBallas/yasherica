using System.Collections.Generic;
using System.Linq;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Data.Providers
{
    /// <summary>
    /// Provides NPC data from ScriptableObject definitions.
    /// Pure C# class - receives definitions via constructor injection.
    /// </summary>
    public class ScriptableObjectNpcDataProvider : INpcDataProvider
    {
        private readonly Dictionary<string, NpcDefinition> _npcLookup;
        private readonly List<NpcDefinition> _allNpcs;

        public ScriptableObjectNpcDataProvider(IReadOnlyList<NpcDefinition> npcDefinitions)
        {
            _allNpcs = npcDefinitions?.ToList() ?? new List<NpcDefinition>();
            _npcLookup = new Dictionary<string, NpcDefinition>();

            foreach (var npc in _allNpcs)
            {
                if (npc == null)
                {
                    Debug.LogWarning("[ScriptableObjectNpcDataProvider] Null NPC definition in list");
                    continue;
                }

                if (string.IsNullOrEmpty(npc.NpcId))
                {
                    Debug.LogWarning($"[ScriptableObjectNpcDataProvider] NPC '{npc.DisplayName}' has no ID");
                    continue;
                }

                if (_npcLookup.ContainsKey(npc.NpcId))
                {
                    Debug.LogWarning($"[ScriptableObjectNpcDataProvider] Duplicate NPC ID: {npc.NpcId}");
                    continue;
                }

                _npcLookup[npc.NpcId] = npc;
            }

            Debug.Log($"[ScriptableObjectNpcDataProvider] Loaded {_npcLookup.Count} NPC definitions");
        }

        public NpcDefinition GetNpcById(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                Debug.LogWarning("[ScriptableObjectNpcDataProvider] GetNpcById called with null/empty ID");
                return null;
            }

            if (_npcLookup.TryGetValue(npcId, out var npc))
            {
                return npc;
            }

            Debug.LogWarning($"[ScriptableObjectNpcDataProvider] NPC not found: {npcId}");
            return null;
        }

        public IReadOnlyList<NpcDefinition> GetAllNpcs()
        {
            return _allNpcs;
        }

        public IReadOnlyList<NpcDefinition> GetNpcsByFaction(NpcFaction faction)
        {
            return _allNpcs.Where(n => n.Faction == faction).ToList();
        }

        public bool HasNpc(string npcId)
        {
            return !string.IsNullOrEmpty(npcId) && _npcLookup.ContainsKey(npcId);
        }
    }
}
