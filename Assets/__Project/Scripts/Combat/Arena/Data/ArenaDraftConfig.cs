using System.Collections.Generic;
using CharacterSystem.Data.Definitions;
using UnityEngine;

namespace Combat.Arena.Data
{
    /// <summary>
    /// The parts draft's authored surface (P4-5): the fixed Arena slot loadout, the common
    /// floor (duplicates allowed — the floor guarantees every seat a complete body), the
    /// catalog sample size, and the pick pacing. Configuration only; the mapper converts it to
    /// Core settings at install time.
    /// </summary>
    [CreateAssetMenu(fileName = "ArenaDraftConfig", menuName = "Yasherica/Arena/Draft Config")]
    public class ArenaDraftConfig : ScriptableObject
    {
        [Header("The fixed Arena slot loadout (draft completes when every seat fills each)")]
        [SerializeField] private List<SlotDefinition> _slotLoadout = new List<SlotDefinition>();

        [Header("Common floor — stocked in full; list a part N times for N copies")]
        [SerializeField] private List<PartDefinition> _floorParts = new List<PartDefinition>();

        [Header("Board composition")]
        [Tooltip("How many distinct tasted-catalog parts are sampled onto the board (single-copy).")]
        [SerializeField] private int _catalogSampleSize = 8;

        [Header("Pick pacing (seconds)")]
        [Tooltip("Generous soft limit per human pick; on expiry the host auto-picks (G4 req 13).")]
        [SerializeField] private float _pickTimerSeconds = 45f;
        [Tooltip("Short pacing delay before an offline AI dummy's pick lands.")]
        [SerializeField] private float _aiPickDelaySeconds = 1.5f;
        [Tooltip("How long the \"your monster\" beat holds before the fight starts.")]
        [SerializeField] private float _beatSeconds = 4f;

        [Header("The base body every drafted monster assembles onto")]
        [SerializeField] private CharacterAssemblyDefinition _baseAssembly;

        public IReadOnlyList<SlotDefinition> SlotLoadout => _slotLoadout;
        public IReadOnlyList<PartDefinition> FloorParts => _floorParts;
        public int CatalogSampleSize => _catalogSampleSize;
        public float PickTimerSeconds => _pickTimerSeconds;
        public float AiPickDelaySeconds => _aiPickDelaySeconds;
        public float BeatSeconds => _beatSeconds;
        public CharacterAssemblyDefinition BaseAssembly => _baseAssembly;
    }
}
