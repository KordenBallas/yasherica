using System.Collections.Generic;

namespace Narrative.Casting.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free combat fragment: an enemy id plus the semantic tags that match it
    /// into a story's combat slot (W2-6). The concrete enemy stats live in the Combat system's
    /// <c>EnemyDefinition</c>, looked up by id at combat time; only id + tags cross into the narrative
    /// Core for matching.
    /// </summary>
    public sealed class EnemyFragment
    {
        public string EnemyId { get; }
        public IReadOnlyList<string> Tags { get; }

        public EnemyFragment(string enemyId, IReadOnlyList<string> tags)
        {
            EnemyId = enemyId ?? string.Empty;
            Tags = tags ?? System.Array.Empty<string>();
        }
    }
}
