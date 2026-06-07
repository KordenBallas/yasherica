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
        [SerializeField] private string _rewardId;
        [SerializeField] private string _displayName;

        [Header("Type & Value")]
        [SerializeField] private RewardType _rewardType = RewardType.Currency;
        [SerializeField] private int _baseValue = 10;

        [Header("Visual")]
        [SerializeField] private Sprite _icon;

        [Header("Item Reference")]
        [SerializeField] private string _itemId;
        [SerializeField] private Vector2Int _quantityRange = new(1, 1);

        [Header("Selection")]
        [SerializeField] private RewardRarity _rarity = RewardRarity.Common;
        [SerializeField] private float _dropWeight = 1f;

        [Header("Filters")]
        [SerializeField] private int _minDifficulty;
        [SerializeField] private int _maxDifficulty;

        public string RewardId => _rewardId;
        public string DisplayName => _displayName;
        public RewardType RewardType => _rewardType;
        public int BaseValue => _baseValue;
        public Sprite Icon => _icon;
        public string ItemId => _itemId;
        public Vector2Int QuantityRange => _quantityRange;
        public RewardRarity Rarity => _rarity;
        public float DropWeight => _dropWeight;
        public int MinDifficulty => _minDifficulty;
        public int MaxDifficulty => _maxDifficulty;

        public bool HasItem => !string.IsNullOrEmpty(_itemId);

        public bool MatchesDifficulty(int difficulty)
        {
            if (_minDifficulty == 0 && _maxDifficulty == 0)
                return true;

            return difficulty >= _minDifficulty
                && (_maxDifficulty == 0 || difficulty <= _maxDifficulty);
        }

        private void OnValidate()
        {
            if (_baseValue < 0)
                _baseValue = 0;
            if (_dropWeight < 0f)
                _dropWeight = 0f;
            if (_quantityRange.x < 1)
                _quantityRange.x = 1;
            if (_quantityRange.y < _quantityRange.x)
                _quantityRange.y = _quantityRange.x;
            if (_minDifficulty < 0)
                _minDifficulty = 0;
            if (_maxDifficulty < 0)
                _maxDifficulty = 0;
            if (_maxDifficulty > 0 && _minDifficulty > _maxDifficulty)
                _minDifficulty = _maxDifficulty;
        }
    }

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

    public enum RewardRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
