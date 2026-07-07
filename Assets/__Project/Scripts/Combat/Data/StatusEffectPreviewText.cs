using Combat.Data.Definitions;

namespace Combat.Data
{
    /// <summary>
    /// Shared formatter for the "which status does this apply" line shown in the ability
    /// preview popover and on the mutation card's ability description (combat-status-effects
    /// FR9) — one grammar for every surface, no per-view string building.
    /// </summary>
    public static class StatusEffectPreviewText
    {
        /// <summary>
        /// Describes the status an ability applies, e.g. "Applies: Burn (2 turns)".
        /// False when the ability applies no status (pure untyped damage/heal).
        /// </summary>
        public static bool TryDescribeApplied(AbilityDefinition ability, out string text)
        {
            var status = GetAppliedStatus(ability, out int durationOverride);
            if (status == null)
            {
                text = null;
                return false;
            }

            int duration = durationOverride >= 0 ? durationOverride : status.Duration;
            text = duration >= 0
                ? $"Applies: {status.Name} ({duration} turn{(duration == 1 ? "" : "s")})"
                : $"Applies: {status.Name} (permanent)";
            return true;
        }

        /// <summary>
        /// Describes a part passive's standing modifier, e.g. "Standing: Hardened".
        /// </summary>
        public static bool TryDescribeStanding(PassiveAbilityDefinition passive, out string text)
        {
            if (passive == null || passive.Modifier == null)
            {
                text = null;
                return false;
            }

            text = $"Standing: {passive.Modifier.Name}";
            return true;
        }

        /// <summary>
        /// The status a (potentially status-applying) ability definition references, or null.
        /// </summary>
        public static StatusEffectDefinition GetAppliedStatus(AbilityDefinition ability, out int durationOverride)
        {
            switch (ability)
            {
                case HybridAbilityDefinition hybrid when hybrid.StatusEffect != null:
                    durationOverride = hybrid.DurationOverride;
                    return hybrid.StatusEffect;
                case StatusEffectAbilityDefinition status when status.StatusEffect != null:
                    durationOverride = status.DurationOverride;
                    return status.StatusEffect;
                default:
                    durationOverride = -1;
                    return null;
            }
        }
    }
}
