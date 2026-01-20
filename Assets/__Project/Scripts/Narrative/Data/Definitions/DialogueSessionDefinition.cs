using System.Collections.Generic;
using UnityEngine;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// Links an NPC to a specific Ink dialogue session with possible outcomes.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueSessionDefinition", menuName = "Narrative/Dialogue/Session")]
    public class DialogueSessionDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this dialogue session")]
        [SerializeField] private string _sessionId;

        [Tooltip("Display name for editor reference")]
        [SerializeField] private string _displayName;

        [Header("Dialogue Reference")]
        [Tooltip("The NPC this dialogue is associated with")]
        [SerializeField] private NpcDefinition _npc;

        [Tooltip("The Ink knot to start this dialogue session")]
        [SerializeField] private string _inkKnot;

        [Tooltip("Optional stitch within the knot")]
        [SerializeField] private string _inkStitch;

        [Header("Session Configuration")]
        [Tooltip("Can the player exit dialogue at any time?")]
        [SerializeField] private bool _allowEarlyExit = true;

        [Tooltip("Should dialogue auto-advance without player input?")]
        [SerializeField] private bool _autoAdvance;

        [Tooltip("Delay between auto-advance lines (seconds)")]
        [SerializeField] private float _autoAdvanceDelay = 2f;

        [Header("Possible Outcomes")]
        [Tooltip("List of possible outcomes from this dialogue")]
        [SerializeField] private List<DialogueOutcome> _possibleOutcomes = new();

        [Header("Prerequisites")]
        [Tooltip("Quest IDs that must be active for this session")]
        [SerializeField] private List<string> _requiredActiveQuests = new();

        [Tooltip("Quest IDs that must be completed for this session")]
        [SerializeField] private List<string> _requiredCompletedQuests = new();

        [Tooltip("NPC IDs that must have been encountered")]
        [SerializeField] private List<string> _requiredEncounteredNpcs = new();

        // Public read-only accessors
        public string SessionId => _sessionId;
        public string DisplayName => _displayName;
        public NpcDefinition Npc => _npc;
        public string InkKnot => _inkKnot;
        public string InkStitch => _inkStitch;
        public bool AllowEarlyExit => _allowEarlyExit;
        public bool AutoAdvance => _autoAdvance;
        public float AutoAdvanceDelay => _autoAdvanceDelay;
        public IReadOnlyList<DialogueOutcome> PossibleOutcomes => _possibleOutcomes;
        public IReadOnlyList<string> RequiredActiveQuests => _requiredActiveQuests;
        public IReadOnlyList<string> RequiredCompletedQuests => _requiredCompletedQuests;
        public IReadOnlyList<string> RequiredEncounteredNpcs => _requiredEncounteredNpcs;

        /// <summary>
        /// Gets the full Ink path (knot.stitch if stitch is specified).
        /// </summary>
        public string FullInkPath
        {
            get
            {
                if (string.IsNullOrEmpty(_inkStitch))
                    return _inkKnot;
                return $"{_inkKnot}.{_inkStitch}";
            }
        }

        /// <summary>
        /// Checks if this session has any prerequisites.
        /// </summary>
        public bool HasPrerequisites =>
            _requiredActiveQuests.Count > 0 ||
            _requiredCompletedQuests.Count > 0 ||
            _requiredEncounteredNpcs.Count > 0;
    }

    /// <summary>
    /// Defines a possible outcome from a dialogue session.
    /// </summary>
    [System.Serializable]
    public class DialogueOutcome
    {
        [Tooltip("Identifier for this outcome (used in Ink tags)")]
        public string outcomeId;

        [Tooltip("Type of outcome")]
        public DialogueOutcomeType outcomeType;

        [Tooltip("Ink variable to check for this outcome")]
        public string inkVariable;

        [Tooltip("Expected value of the Ink variable")]
        public string expectedValue;

        [Tooltip("Quest ID to start if outcome type is Quest")]
        public string questId;

        [TextArea(1, 2)]
        [Tooltip("Description of this outcome for debugging")]
        public string description;
    }
}
