using System.Collections.Generic;

namespace MetaProgression.Core
{
    /// <summary>
    /// Pure image of the master meta-progression tuning asset (meta-progression FR8–FR12, the
    /// "one config the owner slides generous↔minimal"). Values are validated here so no
    /// configuration — however extreme — can break the never-guarantee invariant: the direction-bias
    /// ceiling is hard-clamped below certainty (FR9) and the ledger retention never shrinks under
    /// the direction window.
    /// </summary>
    public sealed class MetaProgressionSettings
    {
        /// <summary>The FR9 hard cap: no authored ceiling may reach 100%.</summary>
        public const float MaxBiasCeiling = 0.99f;

        private static readonly int[] DefaultUnlockTierRunFloors = { 1, 1, 5, 12 };

        public static readonly MetaProgressionSettings Defaults = new MetaProgressionSettings(
            unlockTierRunFloors: null,
            directionWindowRuns: 4,
            raceAxisWeight: 1f,
            artifactAxisWeight: 1f,
            biasStrength: 1f,
            biasCeiling: 0.75f,
            digOfferSize: 3,
            reserveDirectionSlot: false,
            dilutionExponent: 1f,
            ledgerRetentionRuns: 8);

        /// <summary>Earliest run (world.run_count) at which each unlock tier may open (FR11).</summary>
        public IReadOnlyList<int> UnlockTierRunFloors { get; }

        /// <summary>How many recent runs the direction tally reads (FR8's sliding window).</summary>
        public int DirectionWindowRuns { get; }

        public float RaceAxisWeight { get; }
        public float ArtifactAxisWeight { get; }

        /// <summary>How strongly the pursued direction raises a matching candidate's draw weight.</summary>
        public float BiasStrength { get; }

        /// <summary>Max share of a draw any single candidate can reach — always &lt; 1 (FR9).</summary>
        public float BiasCeiling { get; }

        /// <summary>Cards the hub dig offers (the "N offered" dig-shape dial).</summary>
        public int DigOfferSize { get; }

        /// <summary>Dig-shape floor rule: reserve one offer slot for the leading direction.</summary>
        public bool ReserveDirectionSlot { get; }

        /// <summary>Exponent over the final draw weights — the global dilution slope dial (FR10).</summary>
        public float DilutionExponent { get; }

        /// <summary>How many past runs the meta ledger keeps (≥ the direction window).</summary>
        public int LedgerRetentionRuns { get; }

        public MetaProgressionSettings(
            IReadOnlyList<int> unlockTierRunFloors,
            int directionWindowRuns,
            float raceAxisWeight,
            float artifactAxisWeight,
            float biasStrength,
            float biasCeiling,
            int digOfferSize,
            bool reserveDirectionSlot,
            float dilutionExponent,
            int ledgerRetentionRuns)
        {
            UnlockTierRunFloors = unlockTierRunFloors == null || unlockTierRunFloors.Count == 0
                ? DefaultUnlockTierRunFloors
                : unlockTierRunFloors;
            DirectionWindowRuns = directionWindowRuns < 1 ? 1 : directionWindowRuns;
            RaceAxisWeight = raceAxisWeight < 0f ? 0f : raceAxisWeight;
            ArtifactAxisWeight = artifactAxisWeight < 0f ? 0f : artifactAxisWeight;
            BiasStrength = biasStrength < 0f ? 0f : biasStrength;
            BiasCeiling = biasCeiling < 0f ? 0f : (biasCeiling > MaxBiasCeiling ? MaxBiasCeiling : biasCeiling);
            DigOfferSize = digOfferSize < 0 ? 0 : digOfferSize;
            ReserveDirectionSlot = reserveDirectionSlot;
            DilutionExponent = dilutionExponent <= 0f ? 1f : dilutionExponent;
            int minRetention = DirectionWindowRuns;
            LedgerRetentionRuns = ledgerRetentionRuns < minRetention ? minRetention : ledgerRetentionRuns;
        }

        /// <summary>
        /// The pacing run-floor for an unlock tier: tiers beyond the authored list share the last
        /// floor (the runway's tail), so an out-of-range tier is never a free pass.
        /// </summary>
        public int FloorForTier(int tier)
        {
            if (UnlockTierRunFloors.Count == 0)
            {
                return 0;
            }

            if (tier < 0)
            {
                tier = 0;
            }

            int index = tier >= UnlockTierRunFloors.Count ? UnlockTierRunFloors.Count - 1 : tier;
            return UnlockTierRunFloors[index];
        }
    }
}
