using System.Collections.Generic;
using UnityEngine;
using Combat.Data.Definitions;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for NPC configurations.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "NpcDefinition", menuName = "Narrative/NPCs/NPC")]
    public class NpcDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this NPC")]
        [SerializeField] private string _npcId;

        [Tooltip("Display name of the NPC")]
        [SerializeField] private string _displayName;

        [TextArea(2, 4)]
        [Tooltip("Description of the NPC")]
        [SerializeField] private string _description;

        [Header("Visual")]
        [Tooltip("Prefab for NPC GameObject")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Portrait sprite for dialogue UI")]
        [SerializeField] private Sprite _portrait;

        [Header("Dialogue")]
        [Tooltip("Default Ink knot for initial dialogue")]
        [SerializeField] private string _defaultDialogueKnot;

        [Header("Story Associations")]
        [Tooltip("Stories this NPC is involved in with their roles")]
        [SerializeField] private List<Data.NpcStoryAssociation> _associatedStories = new();

        [Header("Legacy Dialogue Sessions (Deprecated)")]
        [Tooltip("Old dialogue session system - use Story Associations instead")]
        [SerializeField] private List<DialogueSessionDefinition> _legacyDialogueSessions = new();

        [Header("Faction & Behavior")]
        [Tooltip("NPC's faction alignment")]
        [SerializeField] private NpcFaction _faction = NpcFaction.Neutral;

        [Header("Combat Integration")]
        [Tooltip("Whether this NPC can transition to an enemy in combat")]
        [SerializeField] private bool _canBecomeEnemy;

        [Tooltip("Enemy definition to use when NPC becomes hostile")]
        [SerializeField] private EnemyDefinition _enemyDefinition;

        // Public read-only accessors
        public string NpcId => _npcId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public GameObject Prefab => _prefab;
        public Sprite Portrait => _portrait;
        public string DefaultDialogueKnot => _defaultDialogueKnot;
        public IReadOnlyList<Data.NpcStoryAssociation> AssociatedStories => _associatedStories;
        public IReadOnlyList<DialogueSessionDefinition> LegacyDialogueSessions => _legacyDialogueSessions;
        public NpcFaction Faction => _faction;
        public bool CanBecomeEnemy => _canBecomeEnemy;
        public EnemyDefinition EnemyDefinition => _enemyDefinition;

        /// <summary>
        /// Checks if NPC has a default dialogue entry point.
        /// </summary>
        public bool HasDefaultDialogue => !string.IsNullOrEmpty(_defaultDialogueKnot);

        /// <summary>
        /// Checks if NPC has any story associations.
        /// </summary>
        public bool HasStoryAssociations => _associatedStories.Count > 0;

        /// <summary>
        /// Gets stories where this NPC has a specific role.
        /// </summary>
        public IEnumerable<Data.NpcStoryAssociation> GetStoriesByRole(Data.NpcStoryRole role)
        {
            foreach (var association in _associatedStories)
            {
                if (association.Role == role)
                    yield return association;
            }
        }
    }

    /// <summary>
    /// NPC faction alignment.
    /// </summary>
    public enum NpcFaction
    {
        Neutral,
        Friendly,
        Hostile,
        Merchant,
        QuestGiver
    }
}
