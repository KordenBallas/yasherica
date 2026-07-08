using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hub.Data
{
    /// <summary>
    /// Data-authored cauldron-voice lines for the Hub's staging moments (O1). Contains ONLY
    /// configuration data — NO logic (CLAUDE.md §7); selection lives in
    /// <see cref="Hub.Core.CauldronVoiceSelector"/>. Adding or retuning a line = editing this
    /// asset (<c>Resources/Hub/HubVoiceLines</c>), never code. Shaped as moment-keyed pools with
    /// race-keyed part-pick sub-pools so the later full bark channel (P1-10) can absorb it as its
    /// hub-presence slots.
    /// </summary>
    [CreateAssetMenu(fileName = "HubVoiceLines", menuName = "Hub/Cauldron Voice Lines")]
    public class HubVoiceLinesConfig : ScriptableObject
    {
        [Serializable]
        public class RaceLinePool
        {
            [Tooltip("The race id the picked part carries (e.g. 'fox'); must match a RaceDefinition.")]
            [SerializeField] private string _raceId;

            [Tooltip("Lines for picking a part of this race; one is chosen deterministically.")]
            [SerializeField] private List<string> _lines = new List<string>();

            public string RaceId => _raceId;
            public IReadOnlyList<string> Lines => _lines;
        }

        [Header("Part pick")]
        [Tooltip("Race-keyed pools for picking a race-tagged starting organ.")]
        [SerializeField] private List<RaceLinePool> _partPickedByRace = new List<RaceLinePool>();

        [Tooltip("Fallback pool for any part pick without a race pool (incl. kindless parts).")]
        [SerializeField] private List<string> _partPickedGeneric = new List<string>();

        [Header("Other staging moments")]
        [Tooltip("Spoken when the offer is empty (the first, bare launch).")]
        [SerializeField] private List<string> _noPartAvailable = new List<string>();

        [Tooltip("Spoken at the launch/descent.")]
        [SerializeField] private List<string> _launch = new List<string>();

        [Tooltip("Spoken when the player returns from a death (the junkyard reforms him).")]
        [SerializeField] private List<string> _deathReturn = new List<string>();

        [Header("The Heat pact (Track Y)")]
        [Tooltip("Spoken when the pact panel opens — the cauldron's dare, in the tempter register.")]
        [SerializeField] private List<string> _heatDare = new List<string>();

        [Tooltip("Spoken when the pact closes hot (total Heat > 0) — accepting reads as a bargain.")]
        [SerializeField] private List<string> _heatSealed = new List<string>();

        [Tooltip("Spoken when the pact closes cold (total Heat 0) — declining reads as prudishness.")]
        [SerializeField] private List<string> _heatDeclined = new List<string>();

        public IReadOnlyList<RaceLinePool> PartPickedByRace => _partPickedByRace;
        public IReadOnlyList<string> PartPickedGeneric => _partPickedGeneric;
        public IReadOnlyList<string> NoPartAvailable => _noPartAvailable;
        public IReadOnlyList<string> Launch => _launch;
        public IReadOnlyList<string> DeathReturn => _deathReturn;
        public IReadOnlyList<string> HeatDare => _heatDare;
        public IReadOnlyList<string> HeatSealed => _heatSealed;
        public IReadOnlyList<string> HeatDeclined => _heatDeclined;
    }
}
