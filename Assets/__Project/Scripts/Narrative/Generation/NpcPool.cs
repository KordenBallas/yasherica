using System;
using System.Collections.Generic;
using System.Linq;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Generation
{
    /// <summary>
    /// Manages available NPCs for binding to story templates.
    /// Tracks availability, assignments, and cooldowns.
    /// Pure C# class - no Unity dependencies except logging.
    /// </summary>
    public class NpcPool : INpcPool
    {
        private readonly Dictionary<string, NpcInstance> _npcsByInstanceId;
        private readonly Dictionary<string, NpcInstance> _npcsByDefinitionId;
        private readonly List<NpcInstance> _allNpcs;

        public IReadOnlyList<NpcInstance> AllNpcs => _allNpcs;

        public IReadOnlyList<NpcInstance> AvailableNpcs
        {
            get
            {
                var available = new List<NpcInstance>();
                foreach (var npc in _allNpcs)
                {
                    if (npc.IsAvailable)
                        available.Add(npc);
                }
                return available;
            }
        }

        public NpcPool()
        {
            _npcsByInstanceId = new Dictionary<string, NpcInstance>();
            _npcsByDefinitionId = new Dictionary<string, NpcInstance>();
            _allNpcs = new List<NpcInstance>();
        }

        public void PopulatePool(IReadOnlyList<NpcDefinition> definitions, INarrativeContext context)
        {
            if (definitions == null || definitions.Count == 0)
            {
                Debug.Log("[NpcPool] No NPC definitions provided");
                return;
            }

            ClearPool();

            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.NpcId))
                    continue;

                // Check if NPC should be available based on context
                if (!ShouldIncludeNpc(definition, context))
                    continue;

                var instance = new NpcInstance(definition);
                _npcsByInstanceId[instance.InstanceId] = instance;
                _npcsByDefinitionId[definition.NpcId] = instance;
                _allNpcs.Add(instance);
            }

            Debug.Log($"[NpcPool] Populated pool with {_allNpcs.Count} NPCs from {definitions.Count} definitions");
        }

        public void ClearPool()
        {
            _npcsByInstanceId.Clear();
            _npcsByDefinitionId.Clear();
            _allNpcs.Clear();
        }

        public bool ReserveNpc(string npcId, NpcRole role, string storyId)
        {
            var npc = GetNpcByAnyId(npcId);
            if (npc == null)
            {
                Debug.LogWarning($"[NpcPool] Cannot reserve NPC '{npcId}' - not found");
                return false;
            }

            if (!npc.IsAvailable)
            {
                Debug.LogWarning($"[NpcPool] Cannot reserve NPC '{npcId}' - not available (state: {npc.State})");
                return false;
            }

            npc.Assign(storyId, role);
            Debug.Log($"[NpcPool] Reserved NPC '{npc.DisplayName}' as {role} for story '{storyId}'");
            return true;
        }

        public void ReleaseNpc(string npcId)
        {
            var npc = GetNpcByAnyId(npcId);
            if (npc == null)
            {
                Debug.LogWarning($"[NpcPool] Cannot release NPC '{npcId}' - not found");
                return;
            }

            if (npc.IsAssigned)
            {
                npc.Release();
                Debug.Log($"[NpcPool] Released NPC '{npc.DisplayName}'");
            }
        }

        public IReadOnlyList<NpcInstance> FindMatchingNpcs(NpcSearchCriteria criteria)
        {
            if (criteria == null)
                return AvailableNpcs;

            var results = new List<NpcInstance>();

            foreach (var npc in _allNpcs)
            {
                if (MatchesCriteria(npc, criteria))
                {
                    results.Add(npc);
                    if (results.Count >= criteria.MaxResults)
                        break;
                }
            }

            // Sort by suitability (relationship score, etc.)
            results.Sort((a, b) => b.RelationshipScore.CompareTo(a.RelationshipScore));

            return results;
        }

        public NpcInstance GetNpc(string npcId)
        {
            return GetNpcByAnyId(npcId);
        }

        public void SetCooldown(string npcId, int cooldownPlatforms)
        {
            var npc = GetNpcByAnyId(npcId);
            if (npc == null)
                return;

            npc.SetCooldown(cooldownPlatforms);
            Debug.Log($"[NpcPool] Set cooldown of {cooldownPlatforms} platforms for NPC '{npc.DisplayName}'");
        }

        public void DecrementCooldowns()
        {
            foreach (var npc in _allNpcs)
            {
                npc.DecrementCooldown();
            }
        }

        /// <summary>
        /// Adds an NPC instance directly to the pool.
        /// </summary>
        public void AddNpc(NpcInstance npc)
        {
            if (npc == null)
                return;

            if (!_npcsByInstanceId.ContainsKey(npc.InstanceId))
            {
                _npcsByInstanceId[npc.InstanceId] = npc;
                _npcsByDefinitionId[npc.NpcId] = npc;
                _allNpcs.Add(npc);
            }
        }

        /// <summary>
        /// Gets NPCs by faction.
        /// </summary>
        public IReadOnlyList<NpcInstance> GetNpcsByFaction(NpcFaction faction)
        {
            var results = new List<NpcInstance>();
            foreach (var npc in _allNpcs)
            {
                if (npc.Faction == faction)
                    results.Add(npc);
            }
            return results;
        }

        /// <summary>
        /// Gets the count of available NPCs.
        /// </summary>
        public int AvailableCount
        {
            get
            {
                int count = 0;
                foreach (var npc in _allNpcs)
                {
                    if (npc.IsAvailable)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the count of assigned NPCs.
        /// </summary>
        public int AssignedCount
        {
            get
            {
                int count = 0;
                foreach (var npc in _allNpcs)
                {
                    if (npc.IsAssigned)
                        count++;
                }
                return count;
            }
        }

        private NpcInstance GetNpcByAnyId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            // Try instance ID first
            if (_npcsByInstanceId.TryGetValue(id, out var byInstance))
                return byInstance;

            // Try definition ID
            if (_npcsByDefinitionId.TryGetValue(id, out var byDef))
                return byDef;

            return null;
        }

        private bool ShouldIncludeNpc(NpcDefinition definition, INarrativeContext context)
        {
            if (context?.WorldState == null)
                return true;

            // Could add additional filtering based on:
            // - Chapter requirements
            // - Quest completion status
            // - Faction relationships
            // For now, include all NPCs

            return true;
        }

        private bool MatchesCriteria(NpcInstance npc, NpcSearchCriteria criteria)
        {
            // Check availability
            if (!criteria.IncludeAssigned && npc.IsAssigned)
                return false;

            if (!criteria.IncludeCooldown && npc.State == NpcInstanceState.OnCooldown)
                return false;

            // Check exclusions
            if (criteria.ExcludedNpcIds != null && criteria.ExcludedNpcIds.Contains(npc.NpcId))
                return false;

            // Check faction
            if (criteria.RequiredFaction.HasValue && npc.Faction != criteria.RequiredFaction.Value)
                return false;

            // Check traits (any match)
            if (criteria.RequiredTraits != null && criteria.RequiredTraits.Count > 0)
            {
                // This would require traits on NpcDefinition
                // For now, skip trait matching if not implemented
            }

            return true;
        }
    }
}
