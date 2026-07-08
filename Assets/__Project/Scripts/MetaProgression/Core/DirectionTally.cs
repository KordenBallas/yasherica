using System.Collections.Generic;
using Core.Persistence;

namespace MetaProgression.Core
{
    /// <summary>
    /// The sliding-window direction tally (meta-progression FR8): reads the persisted run ledger's
    /// last <c>DirectionWindowRuns</c> recorded runs (anchored at the newest recorded run, so the
    /// direction shifts as the player changes what they build — never a lifetime average) and
    /// aggregates on the two existing data axes — the installed part's race marker (weighted by
    /// <c>RaceAxisWeight</c>) and the socketed reagent's function traits (<c>ArtifactAxisWeight</c>).
    /// Ids the catalogs no longer know are skipped (robust to content edits). Pure and
    /// deterministic: same ledger + maps + settings ⇒ same profile.
    /// </summary>
    public static class DirectionTally
    {
        public static DirectionProfile Compute(
            MetaRunLedgerSnapshot ledger,
            MetaProgressionSettings settings,
            IReadOnlyDictionary<string, string> raceByPartId,
            IReadOnlyDictionary<string, IReadOnlyList<string>> traitsByArtifactId)
        {
            settings = settings ?? MetaProgressionSettings.Defaults;
            if (ledger?.Runs == null || ledger.Runs.Count == 0)
            {
                return DirectionProfile.Neutral;
            }

            int anchor = 0;
            foreach (var run in ledger.Runs)
            {
                if (run != null && run.RunIndex > anchor)
                {
                    anchor = run.RunIndex;
                }
            }

            if (anchor == 0)
            {
                return DirectionProfile.Neutral;
            }

            int windowFloor = anchor - settings.DirectionWindowRuns; // exclusive lower bound
            var raceWeights = new Dictionary<string, float>(System.StringComparer.Ordinal);
            var traitWeights = new Dictionary<string, float>(System.StringComparer.Ordinal);

            foreach (var run in ledger.Runs)
            {
                if (run == null || run.RunIndex <= windowFloor)
                {
                    continue;
                }

                TallyRaces(run.InstalledPartIds, raceByPartId, settings.RaceAxisWeight, raceWeights);
                TallyTraits(run.SocketedArtifactIds, traitsByArtifactId, settings.ArtifactAxisWeight, traitWeights);
            }

            return new DirectionProfile(raceWeights, traitWeights);
        }

        private static void TallyRaces(
            List<string> partIds,
            IReadOnlyDictionary<string, string> raceByPartId,
            float weight,
            Dictionary<string, float> raceWeights)
        {
            if (partIds == null || raceByPartId == null || weight <= 0f)
            {
                return;
            }

            foreach (var partId in partIds)
            {
                if (string.IsNullOrEmpty(partId)
                    || !raceByPartId.TryGetValue(partId, out var raceId)
                    || string.IsNullOrEmpty(raceId))
                {
                    continue;
                }

                raceWeights.TryGetValue(raceId, out float existing);
                raceWeights[raceId] = existing + weight;
            }
        }

        private static void TallyTraits(
            List<string> artifactIds,
            IReadOnlyDictionary<string, IReadOnlyList<string>> traitsByArtifactId,
            float weight,
            Dictionary<string, float> traitWeights)
        {
            if (artifactIds == null || traitsByArtifactId == null || weight <= 0f)
            {
                return;
            }

            foreach (var artifactId in artifactIds)
            {
                if (string.IsNullOrEmpty(artifactId)
                    || !traitsByArtifactId.TryGetValue(artifactId, out var traits)
                    || traits == null)
                {
                    continue;
                }

                foreach (var traitId in traits)
                {
                    if (string.IsNullOrEmpty(traitId))
                    {
                        continue;
                    }

                    traitWeights.TryGetValue(traitId, out float existing);
                    traitWeights[traitId] = existing + weight;
                }
            }
        }
    }
}
