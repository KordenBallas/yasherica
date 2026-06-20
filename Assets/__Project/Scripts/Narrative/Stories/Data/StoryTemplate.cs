using System.Collections.Generic;
using Narrative.Facts.Data;
using UnityEngine;

namespace Narrative.Stories.Data
{
    /// <summary>
    /// ScriptableObject for a story template (R5/R7): a skeleton of typed slots, fact preconditions
    /// gating eligibility, any story-level direct effects, and director metadata. References NO other
    /// template (R7). The advisory effect footprint is derived later by the planning component, not
    /// here (W3-2). Configuration data only.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryTemplate", menuName = "Narrative/Stories/Story Template")]
    public class StoryTemplate : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _storyId;

        [Header("Structure")]
        [SerializeField] private List<StorySlotDefinition> _slots = new List<StorySlotDefinition>();

        [Header("Eligibility & Effects")]
        [Tooltip("Predicates over facts that gate eligibility (AND-composed)")]
        [SerializeField] private List<FactPredicateSerial> _preconditions = new List<FactPredicateSerial>();
        [Tooltip("Optional story-level direct fact writes (rare); feed the derived planning footprint")]
        [SerializeField] private List<FactEffectSerial> _ownEffects = new List<FactEffectSerial>();

        [Header("Director Metadata")]
        [SerializeField] private List<string> _storyTags = new List<string>();
        [Tooltip("Thread/arc label this story advances (first-class threads deferred)")]
        [SerializeField] private string _threadId;
        [Tooltip("Optional authored backbone beat (R13)")]
        [SerializeField] private bool _isSpine;

        public string StoryId => _storyId;
        public IReadOnlyList<StorySlotDefinition> Slots => _slots;
        public IReadOnlyList<FactPredicateSerial> Preconditions => _preconditions;
        public IReadOnlyList<FactEffectSerial> OwnEffects => _ownEffects;
        public IReadOnlyList<string> StoryTags => _storyTags;
        public string ThreadId => _threadId;
        public bool IsSpine => _isSpine;
    }
}
