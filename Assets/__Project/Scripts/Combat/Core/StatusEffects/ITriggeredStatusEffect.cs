using Combat.Data.Definitions;

namespace Combat.Core.StatusEffects
{
    /// <summary>
    /// Extended status effect interface with trigger support.
    /// Runtime effects implementing this interface can respond to specific game events.
    /// </summary>
    public interface ITriggeredStatusEffect : IStatusEffect
    {
        /// <summary>
        /// When this effect triggers its behavior.
        /// </summary>
        StatusEffectTriggerType TriggerType { get; }

        /// <summary>
        /// Damage dealt each time the effect triggers.
        /// </summary>
        int DamagePerTrigger { get; }

        /// <summary>
        /// Healing applied each time the effect triggers.
        /// </summary>
        int HealPerTrigger { get; }

        /// <summary>
        /// HP percentage threshold for OnThreshold triggers.
        /// </summary>
        float HpThreshold { get; }

        /// <summary>
        /// Direction of threshold comparison (below or above).
        /// </summary>
        ThresholdDirection ThresholdDirection { get; }
    }
}
