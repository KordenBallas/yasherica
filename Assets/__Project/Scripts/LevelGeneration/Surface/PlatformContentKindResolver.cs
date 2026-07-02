using Platform;

namespace LevelGeneration.Surface
{
    /// <summary>
    /// Derives the size/shape profile key from what the graph node already carries — no new field on
    /// <see cref="GraphNode"/> or the planner output. Combat wins first because the streaming
    /// coordinator sets <see cref="GraphNode.Type"/> from <c>PlannedPlatform.IsCombat</c>, which
    /// covers both ambient monsters and story encounters with a required combat slot (those must
    /// meet the battlefield minimum regardless of also carrying an NPC).
    /// </summary>
    public static class PlatformContentKindResolver
    {
        public static PlatformContentKind Resolve(GraphNode node)
        {
            if (node == null)
            {
                return PlatformContentKind.Empty;
            }

            if (node.Type == PlatformType.Combat)
            {
                return PlatformContentKind.Combat;
            }

            if (node.ContentTypes != null)
            {
                if (node.ContentTypes.Contains(PlatformContentType.Npc))
                {
                    return PlatformContentKind.Npc;
                }

                if (node.ContentTypes.Contains(PlatformContentType.Enemy))
                {
                    return PlatformContentKind.Combat;
                }

                if (node.ContentTypes.Contains(PlatformContentType.Loot))
                {
                    return PlatformContentKind.Loot;
                }
            }

            return PlatformContentKind.Empty;
        }
    }
}
