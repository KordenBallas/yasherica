using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// The readable *direction* of a blank's socketed reagents: the post-grammar
    /// trait ids and tier of the combined profile. This is the hint channel the
    /// cauldron voice will speak from ("turning heavy and venomous, are we...") -
    /// it telegraphs the trend, never the exact variant menu.
    /// </summary>
    public sealed class SocketingTrend
    {
        public int BlankInstanceId { get; }
        public IReadOnlyList<string> TraitIds { get; }
        public int Tier { get; }

        public SocketingTrend(int blankInstanceId, IReadOnlyList<string> traitIds, int tier)
        {
            BlankInstanceId = blankInstanceId;
            TraitIds = traitIds;
            Tier = tier;
        }
    }
}
