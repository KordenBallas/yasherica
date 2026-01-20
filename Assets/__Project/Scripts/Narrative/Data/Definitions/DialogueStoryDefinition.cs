using System.Collections.Generic;
using UnityEngine;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// Defines a dialogue-based story for NPC interactions.
    /// Replaces DialogueSessionDefinition with a story graph compatible approach.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueStoryDefinition", menuName = "Narrative/Story/Dialogue")]
    public class DialogueStoryDefinition : BaseStoryDefinition
    {
        [Header("NPC Reference")]
        [Tooltip("The NPC this dialogue is associated with")]
        [SerializeField] private NpcDefinition _npc;

        [Header("Dialogue Configuration")]
        [Tooltip("Optional stitch within the starting knot")]
        [SerializeField] private string _inkStitch;

        [Tooltip("Can the player exit dialogue at any time?")]
        [SerializeField] private bool _allowEarlyExit = true;

        [Tooltip("Should dialogue auto-advance without player input?")]
        [SerializeField] private bool _autoAdvance;

        [Tooltip("Delay between auto-advance lines (seconds)")]
        [SerializeField] private float _autoAdvanceDelay = 2f;

        [Header("Possible Outcomes")]
        [Tooltip("List of possible outcomes from this dialogue")]
        [SerializeField] private List<DialogueOutcome> _possibleOutcomes = new();

        [Header("Repeatable Settings")]
        [Tooltip("Whether this dialogue can be replayed")]
        [SerializeField] private bool _isRepeatable = true;

        [Tooltip("Minimum encounters between repeats")]
        [SerializeField] private int _cooldownEncounters = 1;

        // Public read-only accessors
        public NpcDefinition Npc => _npc;
        public string InkStitch => _inkStitch;
        public bool AllowEarlyExit => _allowEarlyExit;
        public bool AutoAdvance => _autoAdvance;
        public float AutoAdvanceDelay => _autoAdvanceDelay;
        public IReadOnlyList<DialogueOutcome> PossibleOutcomes => _possibleOutcomes;
        public bool IsRepeatable => _isRepeatable;
        public int CooldownEncounters => _cooldownEncounters;

        /// <summary>
        /// Gets the full Ink path (knot.stitch if stitch is specified).
        /// </summary>
        public string FullInkPath
        {
            get
            {
                if (string.IsNullOrEmpty(_inkStitch))
                    return _startingKnot;
                return $"{_startingKnot}.{_inkStitch}";
            }
        }

        /// <summary>
        /// Checks if this dialogue has an associated NPC.
        /// </summary>
        public bool HasNpc => _npc != null;

        /// <summary>
        /// Gets the NPC ID if associated.
        /// </summary>
        public string NpcId => _npc != null ? _npc.NpcId : null;

        /// <summary>
        /// Dialogue stories always use the Dialogue platform type.
        /// </summary>
        public override StoryPlatformType GetPlatformType()
        {
            return StoryPlatformType.Dialogue;
        }

        /// <summary>
        /// Dialogue can be repeated based on configuration.
        /// </summary>
        public override bool CanRepeat()
        {
            return _isRepeatable;
        }

        private void OnValidate()
        {
            // Auto-set StoryType for dialogue stories
            _storyType = StoryType.Dialogue;

            // Auto-add NPC attribute if NPC is associated
            if (_npc != null)
            {
                bool hasNpcAttribute = false;
                foreach (var attr in _attributes)
                {
                    if (attr.AttributeKey == Data.StoryAttributeKeys.Npc)
                    {
                        hasNpcAttribute = true;
                        break;
                    }
                }

                if (!hasNpcAttribute)
                {
                    _attributes.Add(new Data.StoryAttribute(
                        Data.StoryAttributeKeys.Npc,
                        _npc.NpcId,
                        Data.AttributeMatchType.Exact,
                        1.0f  // High weight for NPC-specific dialogues
                    ));
                }
            }
        }
    }
}
