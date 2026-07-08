using UnityEngine;

namespace MetaProgression.Data
{
    /// <summary>
    /// The master meta-progression tuning asset (meta-progression brief, "Tuning surface"):
    /// pacing run-floors, the direction-bias dials and its hard ceiling, the dig shape, and the
    /// dilution slope — one asset the designer slides from generous toward minimal without a
    /// rebuild. Configuration data only; consumers read the mapped Core
    /// <c>MetaProgressionSettings</c>, never this SO. Loaded from
    /// <c>Resources/Configs/MetaProgressionConfig</c>; a missing asset degrades to Core defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "MetaProgressionConfig", menuName = "MetaProgression/Config")]
    public class MetaProgressionConfig : ScriptableObject
    {
        [Header("Pacing (FR11/FR12)")]
        [Tooltip("Earliest run (world.run_count) at which each unlock tier may open; index = a token's unlock tier. Tiers past the end share the last floor. Front-load by keeping early entries low.")]
        [SerializeField] private int[] _unlockTierRunFloors = { 1, 1, 5, 12 };

        [Header("Direction bias (FR8/FR9)")]
        [Tooltip("How many recent runs the pursued-direction tally reads (small, so direction can shift).")]
        [Min(1)]
        [SerializeField] private int _directionWindowRuns = 4;
        [Tooltip("Relative weight of the part race-marker axis in the direction tally.")]
        [Min(0f)]
        [SerializeField] private float _raceAxisWeight = 1f;
        [Tooltip("Relative weight of the socketed-artifact function-family axis in the direction tally.")]
        [Min(0f)]
        [SerializeField] private float _artifactAxisWeight = 1f;
        [Tooltip("How strongly the pursued direction raises a matching candidate's draw weight. 0 = bias off.")]
        [Min(0f)]
        [SerializeField] private float _biasStrength = 1f;
        [Tooltip("Hard never-guarantee ceiling: the max share of a draw any single candidate can reach. Re-clamped below 100% in the mapper — no value can make a draw deterministic.")]
        [Range(0f, 0.95f)]
        [SerializeField] private float _biasCeiling = 0.75f;

        [Header("Dig shape (FR7)")]
        [Tooltip("How many cards the hub dig offers.")]
        [Min(0)]
        [SerializeField] private int _digOfferSize = 3;
        [Tooltip("Floor rule: reserve one offered card for the leading direction when a match exists in the pool.")]
        [SerializeField] private bool _reserveDirectionSlot;

        [Header("Dilution (FR10)")]
        [Tooltip("Exponent over the final draw weights — >1 sharpens differences, <1 flattens them.")]
        [Min(0.01f)]
        [SerializeField] private float _dilutionExponent = 1f;

        [Header("Ledger")]
        [Tooltip("How many past runs the cross-run ledger keeps (never below the direction window).")]
        [Min(1)]
        [SerializeField] private int _ledgerRetentionRuns = 8;

        public int[] UnlockTierRunFloors => _unlockTierRunFloors;
        public int DirectionWindowRuns => _directionWindowRuns;
        public float RaceAxisWeight => _raceAxisWeight;
        public float ArtifactAxisWeight => _artifactAxisWeight;
        public float BiasStrength => _biasStrength;
        public float BiasCeiling => _biasCeiling;
        public int DigOfferSize => _digOfferSize;
        public bool ReserveDirectionSlot => _reserveDirectionSlot;
        public float DilutionExponent => _dilutionExponent;
        public int LedgerRetentionRuns => _ledgerRetentionRuns;
    }
}
