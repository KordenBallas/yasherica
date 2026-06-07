using System.Collections.Generic;

namespace Narrative.Generation
{
    /// <summary>
    /// Result of level narrative generation.
    /// Contains all NPC assignments for placement on platforms.
    /// </summary>
    public class LevelNarrative
    {
        public IReadOnlyList<NpcAssignment> Assignments { get; }

        public LevelNarrative(IReadOnlyList<NpcAssignment> assignments)
        {
            Assignments = assignments ?? System.Array.Empty<NpcAssignment>();
        }
    }
}
