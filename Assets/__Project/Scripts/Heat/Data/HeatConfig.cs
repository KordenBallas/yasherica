using System;
using System.Collections.Generic;
using Heat.Core;
using UnityEngine;

namespace Heat.Data
{
    /// <summary>
    /// The one Heat tuning asset (heat-ascension "Tuning surface"): the modifier menu with its ranks
    /// and Heat values, plus the dials mapping total Heat onto R's pacing and dig bias. The menu is
    /// embedded here rather than split into per-modifier SOs — it is deliberately small (a handful of
    /// modifiers, FR3), and one asset is the whole balance surface. Configuration data only;
    /// consumers read the mapped Core <c>HeatSettings</c>, never this SO. Loaded from
    /// <c>Resources/Configs/HeatConfig</c>; a missing asset degrades to Core defaults (Heat absent).
    /// </summary>
    [CreateAssetMenu(fileName = "HeatConfig", menuName = "Heat/Config")]
    public class HeatConfig : ScriptableObject
    {
        [Serializable]
        public class HeatRankAuthoring
        {
            [Tooltip("Heat this rank step adds to the pact total (per step, summed 1..k at rank k).")]
            [Min(0)]
            [SerializeField] private int _heatValue = 1;

            [Tooltip("Effect magnitude this step adds (per step; meaning depends on the modifier's kind).")]
            [Min(0)]
            [SerializeField] private int _magnitude = 1;

            [Tooltip("The rule text the player reads for this step.")]
            [SerializeField] private string _description = string.Empty;

            public int HeatValue => _heatValue;
            public int Magnitude => _magnitude;
            public string Description => _description;
        }

        [Serializable]
        public class HeatModifierAuthoring
        {
            [Tooltip("Stable id — persisted in the run pact; renaming orphans saved pacts (they degrade to a cooler pact).")]
            [SerializeField] private string _id = string.Empty;

            [Tooltip("Display name on the pact card.")]
            [SerializeField] private string _displayName = string.Empty;

            [Tooltip("The code seam this modifier pulls. Adding a new KIND is a code change; new instances/ranks over existing kinds are data-only (FR13).")]
            [SerializeField] private HeatEffectKind _kind;

            [Tooltip("The rank steps (1–N). Each step is a pain the player can feel and name.")]
            [SerializeField] private List<HeatRankAuthoring> _ranks = new List<HeatRankAuthoring>();

            public string Id => _id;
            public string DisplayName => _displayName;
            public HeatEffectKind Kind => _kind;
            public IReadOnlyList<HeatRankAuthoring> Ranks => _ranks;
        }

        [Header("The modifier menu (FR1/FR3)")]
        [SerializeField] private List<HeatModifierAuthoring> _modifiers = new List<HeatModifierAuthoring>();

        [Header("Reward mapping onto R (FR6)")]
        [Tooltip("Runs of tier run-floor relief per point of total Heat (the pacing acceleration).")]
        [Min(0f)]
        [SerializeField] private float _floorReliefRunsPerHeat = 0.5f;

        [Tooltip("Additive lift to R's dig BiasStrength per point of total Heat. The bias CEILING is untouchable by design — Heat raises odds, never certainty.")]
        [Min(0f)]
        [SerializeField] private float _biasStrengthLiftPerHeat = 0.15f;

        [Tooltip("Total Heat at which the dig reserves a slot for the pursued direction. 0 = Heat never enables it.")]
        [Min(0)]
        [SerializeField] private int _reserveDirectionSlotMinHeat = 4;

        [Header("Pact shape")]
        [Tooltip("Soft cap on total Heat the hub pact refuses to exceed. 0 = uncapped.")]
        [Min(0)]
        [SerializeField] private int _softCapTotalHeat = 9;

        [Tooltip("Window index a hot run must reach at a savepoint to count as 'cleared' for the high-water record (MVP criterion until the Track Z apex exists).")]
        [Min(0)]
        [SerializeField] private int _clearWindowFloor = 2;

        [Header("Presentation")]
        [Tooltip("Frame tint of the pact cards on the hub panel (the ember read).")]
        [SerializeField] private Color _pactCardTint = new Color(0.85f, 0.45f, 0.2f, 1f);

        public IReadOnlyList<HeatModifierAuthoring> Modifiers => _modifiers;
        public float FloorReliefRunsPerHeat => _floorReliefRunsPerHeat;
        public float BiasStrengthLiftPerHeat => _biasStrengthLiftPerHeat;
        public int ReserveDirectionSlotMinHeat => _reserveDirectionSlotMinHeat;
        public int SoftCapTotalHeat => _softCapTotalHeat;
        public int ClearWindowFloor => _clearWindowFloor;
        public Color PactCardTint => _pactCardTint;
    }
}
