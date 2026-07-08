using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;

namespace MetaProgression.Core
{
    /// <summary>
    /// Computes "the run this vocabulary serves" from the meta store on disk: the stored
    /// <c>world.run_count</c> holds the runs already started, so a fresh run (the Hub staging a
    /// launch, an Area boot without a restore) serves stored + 1 — the same number
    /// <c>RunCounterService</c> will publish — while a continue serves the stored count itself.
    /// </summary>
    public static class EffectiveRunCount
    {
        public static int From(FactStoreSnapshot metaFacts, bool servesFreshRun)
        {
            int stored = ReadStoredRunCount(metaFacts);
            return servesFreshRun ? stored + 1 : stored;
        }

        private static int ReadStoredRunCount(FactStoreSnapshot metaFacts)
        {
            if (metaFacts?.Entries == null)
            {
                return 0;
            }

            var runCountKey = WorldFacts.RunCount.ToKey();
            foreach (var entry in metaFacts.Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                var key = new FactKey(entry.Namespace, entry.Subject, entry.Key);
                if (key.Equals(runCountKey))
                {
                    return (int)FactStoreSnapshotMapper.ToValue(entry).AsInt();
                }
            }

            return 0;
        }
    }
}
