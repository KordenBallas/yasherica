using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;

namespace Heat.Core
{
    /// <summary>
    /// Pure read of the Heat high-water mark straight from a persisted meta-fact snapshot (the
    /// <c>HubMetaReader.ExtractRunCount</c> precedent) — used by scenes that need the record before
    /// (or without) a bootstrapped fact store. 0 when absent: no hot run has ever cleared.
    /// </summary>
    public static class HeatFactReader
    {
        public static int ExtractHighWater(FactStoreSnapshot snapshot)
        {
            if (snapshot?.Entries == null)
            {
                return 0;
            }

            foreach (var entry in snapshot.Entries)
            {
                if (entry.Namespace == WorldFacts.HeatHighWater.Namespace
                    && entry.Key == WorldFacts.HeatHighWater.Key
                    && entry.Type == FactValueType.Int)
                {
                    return (int)entry.IntValue;
                }
            }

            return 0;
        }
    }
}
