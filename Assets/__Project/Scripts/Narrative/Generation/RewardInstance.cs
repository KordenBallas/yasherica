using System;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Runtime instance of a reward with calculated values.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class RewardInstance
    {
        private string _instanceId;
        private RewardDefinition _definition;
        private int _calculatedValue;
        private int _quantity;
        private bool _isClaimed;
        private RewardCondition _condition;

        /// <summary>
        /// Unique instance identifier.
        /// </summary>
        public string InstanceId => _instanceId;

        /// <summary>
        /// The underlying reward definition.
        /// </summary>
        public RewardDefinition Definition => _definition;

        /// <summary>
        /// Reward ID from definition.
        /// </summary>
        public string RewardId => _definition?.RewardId ?? string.Empty;

        /// <summary>
        /// Display name of the reward.
        /// </summary>
        public string DisplayName => _definition?.DisplayName ?? "Unknown Reward";

        /// <summary>
        /// Description of the reward.
        /// </summary>
        public string Description => _definition?.Description ?? string.Empty;

        /// <summary>
        /// Type of reward.
        /// </summary>
        public RewardType RewardType => _definition?.RewardType ?? RewardType.Currency;

        /// <summary>
        /// Calculated value for this instance (scaled by chapter, etc.).
        /// </summary>
        public int CalculatedValue => _calculatedValue;

        /// <summary>
        /// Quantity of items/rewards.
        /// </summary>
        public int Quantity => _quantity;

        /// <summary>
        /// Whether this reward has been claimed.
        /// </summary>
        public bool IsClaimed => _isClaimed;

        /// <summary>
        /// Condition required for this reward.
        /// </summary>
        public RewardCondition Condition => _condition;

        /// <summary>
        /// Item ID if this is an item reward.
        /// </summary>
        public string ItemId => _definition?.ItemId ?? string.Empty;

        /// <summary>
        /// Rarity of the reward.
        /// </summary>
        public RewardRarity Rarity => _definition?.Rarity ?? RewardRarity.Common;

        /// <summary>
        /// Creates a new reward instance.
        /// </summary>
        public RewardInstance(
            RewardDefinition definition,
            int chapterNumber = 1,
            float valueMultiplier = 1f,
            RewardCondition condition = RewardCondition.Always)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _instanceId = Guid.NewGuid().ToString("N").Substring(0, 8);
            _condition = condition;
            _isClaimed = false;

            // Calculate scaled value
            int baseScaled = definition.GetScaledValue(chapterNumber);
            _calculatedValue = (int)Math.Round(baseScaled * valueMultiplier);

            // Calculate quantity for item rewards
            if (definition.RewardType == RewardType.Item || definition.RewardType == RewardType.Equipment)
            {
                var range = definition.QuantityRange;
                _quantity = UnityEngine.Random.Range(range.x, range.y + 1);
            }
            else
            {
                _quantity = 1;
            }
        }

        /// <summary>
        /// Creates a reward instance with explicit values.
        /// </summary>
        public RewardInstance(
            RewardDefinition definition,
            int calculatedValue,
            int quantity,
            RewardCondition condition = RewardCondition.Always)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _instanceId = Guid.NewGuid().ToString("N").Substring(0, 8);
            _calculatedValue = Math.Max(0, calculatedValue);
            _quantity = Math.Max(1, quantity);
            _condition = condition;
            _isClaimed = false;
        }

        /// <summary>
        /// Checks if this reward should be granted given the quest outcome.
        /// </summary>
        public bool ShouldGrant(QuestOutcome outcome)
        {
            switch (_condition)
            {
                case RewardCondition.Always:
                    return outcome == QuestOutcome.Success || outcome == QuestOutcome.PartialSuccess;

                case RewardCondition.OnSuccess:
                    return outcome == QuestOutcome.Success;

                case RewardCondition.OnPartialSuccess:
                    return outcome == QuestOutcome.Success || outcome == QuestOutcome.PartialSuccess;

                case RewardCondition.OnPeacefulResolution:
                    // This would need additional context about how quest was resolved
                    return outcome == QuestOutcome.Success;

                case RewardCondition.OnCombatVictory:
                    // This would need additional context about how quest was resolved
                    return outcome == QuestOutcome.Success;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Marks this reward as claimed.
        /// </summary>
        public void Claim()
        {
            _isClaimed = true;
        }

        /// <summary>
        /// Gets a display string for this reward.
        /// </summary>
        public string GetDisplayString()
        {
            if (_definition.RewardType == RewardType.Item || _definition.RewardType == RewardType.Equipment)
            {
                return _quantity > 1
                    ? $"{DisplayName} x{_quantity}"
                    : DisplayName;
            }

            return $"{_calculatedValue} {DisplayName}";
        }

        /// <summary>
        /// Creates a serializable snapshot.
        /// </summary>
        public RewardInstanceSnapshot CreateSnapshot()
        {
            return new RewardInstanceSnapshot
            {
                InstanceId = _instanceId,
                RewardId = RewardId,
                CalculatedValue = _calculatedValue,
                Quantity = _quantity,
                IsClaimed = _isClaimed,
                Condition = _condition
            };
        }
    }

    /// <summary>
    /// Serializable snapshot of reward instance state.
    /// </summary>
    [Serializable]
    public class RewardInstanceSnapshot
    {
        public string InstanceId;
        public string RewardId;
        public int CalculatedValue;
        public int Quantity;
        public bool IsClaimed;
        public RewardCondition Condition;
    }
}
