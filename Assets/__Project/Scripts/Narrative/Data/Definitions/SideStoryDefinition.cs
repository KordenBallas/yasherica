using System;
using System.Collections.Generic;
using LevelGeneration;
using UnityEngine;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// Defines a side story with prerequisites and configuration.
    /// Contains ONLY configuration data - NO logic.
    /// Drop assets into Resources/SideStories for auto-discovery.
    /// </summary>
    [CreateAssetMenu(fileName = "SideStoryDefinition", menuName = "Narrative/Story/Side Story")]
    public class SideStoryDefinition : BaseStoryDefinition
    {
        [Header("Side Story Specific")]
        [Tooltip("Type of platform for this story")]
        [SerializeField] private StoryPlatformType _platformType = StoryPlatformType.Dialogue;

        [Header("NPC Association")]
        [Tooltip("NPC definition associated with this side story")]
        [SerializeField] private NpcDefinition _associatedNpc;

        [Tooltip("Whether the NPC can become an enemy")]
        [SerializeField] private bool _npcCanBecomeEnemy;

        [Tooltip("Enemy ID if NPC transitions to combat")]
        [SerializeField] private string _enemyId;

        [Header("Repeatable Settings")]
        [Tooltip("Whether this side story can be replayed")]
        [SerializeField] private bool _isRepeatable;

        [Tooltip("Minimum platforms between repeats")]
        [SerializeField] private int _cooldownPlatforms = 10;

        [Header("Legacy Tags (Deprecated - Use Attributes Instead)")]
        [Tooltip("Tags for filtering and categorization (migrated to Attributes)")]
        [SerializeField] private List<string> _legacyTags = new();

        // Public read-only accessors - base properties inherited from BaseStoryDefinition
        // _storyId, _displayName, _description, _inkJsonAsset, _startingKnot are inherited
        // _prerequisites, _platformConfig, _relationships, _attributes are inherited

        // Side story specific accessors
        public StoryPlatformType PlatformType => _platformType;
        public NpcDefinition AssociatedNpc => _associatedNpc;
        public bool NpcCanBecomeEnemy => _npcCanBecomeEnemy;
        public string EnemyId => _enemyId;
        public bool IsRepeatable => _isRepeatable;
        public int CooldownPlatforms => _cooldownPlatforms;
        public IReadOnlyList<string> LegacyTags => _legacyTags;

        /// <summary>
        /// Checks if this side story has an associated NPC.
        /// </summary>
        public bool HasNpc => _associatedNpc != null;

        /// <summary>
        /// Gets the NPC ID if associated.
        /// </summary>
        public string NpcId => _associatedNpc != null ? _associatedNpc.NpcId : null;

        /// <summary>
        /// Returns the configured platform type.
        /// </summary>
        public override StoryPlatformType GetPlatformType()
        {
            return _platformType;
        }

        /// <summary>
        /// Side stories can be repeated based on configuration.
        /// </summary>
        public override bool CanRepeat()
        {
            return _isRepeatable;
        }

        private void OnValidate()
        {
            // Auto-set StoryType for side stories
            _storyType = StoryType.SideStory;

            // Migrate legacy tags to attributes if they exist
            if (_legacyTags.Count > 0 && _attributes.Count == 0)
            {
                MigrateLegacyTagsToAttributes();
            }

            // Auto-add NPC attribute if NPC is associated
            if (_associatedNpc != null)
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
                        _associatedNpc.NpcId,
                        Data.AttributeMatchType.Exact,
                        0.7f
                    ));
                }
            }
        }

        private void MigrateLegacyTagsToAttributes()
        {
            foreach (var tag in _legacyTags)
            {
                _attributes.Add(new Data.StoryAttribute(
                    Data.StoryAttributeKeys.Tag,
                    tag,
                    Data.AttributeMatchType.Exact,
                    0.5f
                ));
            }
        }
    }
}
