using System.Collections.Generic;

namespace Narrative.Director.Core
{
    /// <summary>
    /// One biome-pool enemy with its authored tags (from <c>EnemyDefinition.EnemyTags</c>), so a site
    /// combat beat can draw a flavor-matching enemy (Combat·bandit/guard/den-monster) from the same
    /// authored pool the plain ambient draw uses.
    /// </summary>
    public readonly struct MonsterPoolEntry
    {
        public MonsterPoolEntry(int id, IReadOnlyList<string> tags, RunTierBand band = default)
        {
            Id = id;
            Tags = tags ?? System.Array.Empty<string>();
            Band = band;
        }

        public int Id { get; }
        public IReadOnlyList<string> Tags { get; }

        /// <summary>The run-escalation band where this creature belongs (D19); defaults to every tier.</summary>
        public RunTierBand Band { get; }
    }
}
