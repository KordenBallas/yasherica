using System.Collections.Generic;

namespace Narrative.Actors.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free identity of an NPC archetype (R1): id, candidate names, faction
    /// membership, a personality seed, and semantic tags for slot matching. Carries NO dialogue,
    /// quest, combat, or hostility data — those belong to the casting (R3). Visual assets
    /// (assembly/portrait) stay on the SO and are consumed directly by the spawn layer.
    /// </summary>
    public sealed class NpcArchetypeData
    {
        public string ArchetypeId { get; }
        public IReadOnlyList<string> DisplayNamePool { get; }
        public string FactionId { get; }
        public int BaseDisposition { get; }
        public IReadOnlyList<string> ArchetypeTags { get; }

        public NpcArchetypeData(string archetypeId, IReadOnlyList<string> displayNamePool, string factionId,
            int baseDisposition, IReadOnlyList<string> archetypeTags)
        {
            ArchetypeId = archetypeId ?? string.Empty;
            DisplayNamePool = displayNamePool ?? System.Array.Empty<string>();
            FactionId = factionId ?? string.Empty;
            BaseDisposition = baseDisposition;
            ArchetypeTags = archetypeTags ?? System.Array.Empty<string>();
        }
    }
}
