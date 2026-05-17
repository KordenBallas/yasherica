using System.Collections.Generic;
using UnityEngine;

namespace Narrative.Data.Definitions
{
    /// <summary>
    /// ScriptableObject definition for reward configurations.
    /// Contains ONLY configuration data - NO logic.
    /// </summary>
    [CreateAssetMenu(fileName = "RewardDefinition", menuName = "Narrative/Rewards/Reward")]
    public class RewardDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this reward")]
        [SerializeField] private string _rewardId;

        [Tooltip("Display name of the reward")]
        [SerializeField] private string _displayName;

        [TextArea(2, 4)]
        [Tooltip("Description of the reward")]
        [SerializeField] private string _description;

        [Header("Type & Value")]
        [Tooltip("Type of reward")]
        [SerializeField] private RewardType _rewardType = RewardType.Currency;

        [Tooltip("Base value of the reward")]
        [SerializeField] private int _baseValue = 10;

        [Tooltip("Value scaling per chapter")]
        [SerializeField] private float _chapterScaling = 1.1f;

        [Header("Visual")]
        [Tooltip("Icon for UI display")]
        [SerializeField] private Sprite _icon;

        [Header("Item Reference")]
        [Tooltip("Item ID if this is an item reward")]
        [SerializeField] private string _itemId;

        [Tooltip("Quantity range for item rewards")]
        [SerializeField] private Vector2Int _quantityRange = new(1, 1);

        [Header("Rarity")]
        [Tooltip("Rarity tier of this reward")]
        [SerializeField] private RewardRarity _rarity = RewardRarity.Common;

        [Tooltip("Drop weight within rarity tier")]
        [SerializeField] private float _dropWeight = 1f;

        [Header("Conditions")]
        [Tooltip("Minimum chapter for this reward to appear")]
        [SerializeField] private int _minimumChapter;

        [Tooltip("Required quest completion")]
        [SerializeField] private string _requiredQuestId;

        // Public read-only accessors
        public string RewardId => _rewardId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public RewardType RewardType => _rewardType;
        public int BaseValue => _baseValue;
        public float ChapterScaling => _chapterScaling;
        public Sprite Icon => _icon;
        public string ItemId => _itemId;
        public Vector2Int QuantityRange => _quantityRange;
        public RewardRarity Rarity => _rarity;
        public float DropWeight => _dropWeight;
        public int MinimumChapter => _minimumChapter;
        public string RequiredQuestId => _requiredQuestId;

        /// <summary>
        /// Checks if this reward has an associated item.
        /// </summary>
        public bool HasItem => !string.IsNullOrEmpty(_itemId);

        /// <summary>
        /// Checks if this reward has a prerequisite quest.
        /// </summary>
        public bool HasPrerequisite => !string.IsNullOrEmpty(_requiredQuestId);

        /// <summary>
        /// Calculates the scaled value for a given chapter.
        /// </summary>
        public int GetScaledValue(int chapterNumber)
        {
            if (chapterNumber <= 1)
                return _baseValue;

            float multiplier = Mathf.Pow(_chapterScaling, chapterNumber - 1);
            return Mathf.RoundToInt(_baseValue * multiplier);
        }

        private void OnValidate()
        {
            // Ensure values are reasonable
            if (_baseValue < 0)
                _baseValue = 0;

            if (_chapterScaling < 1f)
                _chapterScaling = 1f;

            if (_dropWeight < 0f)
                _dropWeight = 0f;

            // Ensure quantity range is valid
            if (_quantityRange.x < 1)
                _quantityRange.x = 1;
            if (_quantityRange.y < _quantityRange.x)
                _quantityRange.y = _quantityRange.x;
        }
    }

    /// <summary>
    /// Type of reward.
    /// </summary>
    public enum RewardType
    {
        Currency,
        Experience,
        Item,
        Equipment,
        Ability,
        Reputation,
        Unlock
    }

    /// <summary>
    /// Rarity tier of rewards.
    /// </summary>
    public enum RewardRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
