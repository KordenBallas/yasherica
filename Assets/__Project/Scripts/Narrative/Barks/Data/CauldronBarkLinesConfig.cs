using System.Collections.Generic;
using UnityEngine;

namespace Narrative.Barks.Data
{
    /// <summary>
    /// Data-authored line pools for the cauldron's live bark channel (P1-10,
    /// cauldron-voice-barks.md). Contains ONLY configuration data — NO logic (CLAUDE.md §7);
    /// selection lives in <c>CauldronBarkService</c>. Per slot the designer authors BOTH registers
    /// (indulgent = bolder/proprietary, restrained = sour/clipped) so the tone can track the run's
    /// path lean; a missing register falls back to the other, an empty slot stays quiet. Adding or
    /// retuning a line = editing this asset (<c>Resources/Narrative/CauldronBarkLines</c>), never
    /// code. The dark-belonging list is the data-side vocabulary of which offer belongings read as
    /// Monster-lean (fires the dark-offer slot).
    /// </summary>
    [CreateAssetMenu(fileName = "CauldronBarkLines", menuName = "Narrative/Cauldron Bark Lines")]
    public class CauldronBarkLinesConfig : ScriptableObject
    {
        [Header("Temptation — a strong/monstrous mutation is on offer")]
        [SerializeField] private List<string> _temptationIndulgent = new List<string>();
        [SerializeField] private List<string> _temptationRestrained = new List<string>();

        [Header("Dark offer / attack — the Monster verb is on the table")]
        [SerializeField] private List<string> _darkOfferIndulgent = new List<string>();
        [SerializeField] private List<string> _darkOfferRestrained = new List<string>();

        [Header("Restraint — a modest / marker (passport) part taken")]
        [SerializeField] private List<string> _restraintIndulgent = new List<string>();
        [SerializeField] private List<string> _restraintRestrained = new List<string>();

        [Header("Socketing trend — a consistent reagent pattern forms")]
        [SerializeField] private List<string> _socketingTrendIndulgent = new List<string>();
        [SerializeField] private List<string> _socketingTrendRestrained = new List<string>();

        [Header("Dark belonging vocabulary")]
        [Tooltip("Reward-belonging ids that read as the Monster-lean currency (e.g. 'power'); an offer card carrying one fires the dark-offer slot.")]
        [SerializeField] private List<string> _darkBelongingIds = new List<string>();

        public IReadOnlyList<string> TemptationIndulgent => _temptationIndulgent;
        public IReadOnlyList<string> TemptationRestrained => _temptationRestrained;
        public IReadOnlyList<string> DarkOfferIndulgent => _darkOfferIndulgent;
        public IReadOnlyList<string> DarkOfferRestrained => _darkOfferRestrained;
        public IReadOnlyList<string> RestraintIndulgent => _restraintIndulgent;
        public IReadOnlyList<string> RestraintRestrained => _restraintRestrained;
        public IReadOnlyList<string> SocketingTrendIndulgent => _socketingTrendIndulgent;
        public IReadOnlyList<string> SocketingTrendRestrained => _socketingTrendRestrained;
        public IReadOnlyList<string> DarkBelongingIds => _darkBelongingIds;
    }
}
