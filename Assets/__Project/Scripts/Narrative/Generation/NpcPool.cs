using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Manages a pool of NpcDefinitions with filtering and assignment tracking.
    /// </summary>
    public class NpcPool : INpcPool
    {
        private readonly IReadOnlyList<NpcDefinition> _npcs;
        private readonly HashSet<string> _assigned = new();

        public int Count => _npcs.Count;

        public NpcPool(IReadOnlyList<NpcDefinition> npcs)
        {
            _npcs = npcs ?? System.Array.Empty<NpcDefinition>();
        }

        public IReadOnlyList<NpcDefinition> Filter(
            IReadOnlyList<string> requiredTags,
            IReadOnlyList<string> excludedNpcIds)
        {
            var results = new List<NpcDefinition>();

            for (int i = 0; i < _npcs.Count; i++)
            {
                var npc = _npcs[i];

                if (string.IsNullOrEmpty(npc.NpcId))
                    continue;

                if (_assigned.Contains(npc.NpcId))
                    continue;

                if (IsExcluded(npc.NpcId, excludedNpcIds))
                    continue;

                if (!MatchesRequiredTags(npc, requiredTags))
                    continue;

                results.Add(npc);
            }

            return results;
        }

        public void MarkAssigned(string npcId)
        {
            if (!string.IsNullOrEmpty(npcId))
                _assigned.Add(npcId);
        }

        public void ResetAssignments()
        {
            _assigned.Clear();
        }

        private static bool IsExcluded(string npcId, IReadOnlyList<string> excludedNpcIds)
        {
            if (excludedNpcIds == null || excludedNpcIds.Count == 0)
                return false;

            for (int i = 0; i < excludedNpcIds.Count; i++)
            {
                if (excludedNpcIds[i] == npcId)
                    return true;
            }
            return false;
        }

        private static bool MatchesRequiredTags(NpcDefinition npc, IReadOnlyList<string> requiredTags)
        {
            if (requiredTags == null || requiredTags.Count == 0)
                return true;

            for (int i = 0; i < requiredTags.Count; i++)
            {
                if (!npc.HasTag(requiredTags[i]))
                    return false;
            }
            return true;
        }
    }
}
