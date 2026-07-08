using System.Collections.Generic;
using System.Linq;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The migration election rule, isolated because CROSS-PEER determinism is the point: every
    /// survivor runs it over the same mirror view and must name the same seat. Lowest PlayerId
    /// wins — no randomness, no negotiation.
    /// </summary>
    public static class ArenaHostElection
    {
        /// <summary>The elected seat, or 0 when nobody is eligible.</summary>
        public static int Elect(IReadOnlyList<int> candidatePlayerIds)
        {
            return candidatePlayerIds == null || candidatePlayerIds.Count == 0
                ? 0
                : candidatePlayerIds.Min();
        }
    }
}
