using System.Collections.Generic;

namespace Combat.Core.StatusEffects
{
    /// <summary>
    /// Pure per-turn duration bookkeeping for status effects, kept separate from the
    /// combat controller so the rule is unit-testable without Unity.
    /// </summary>
    public static class StatusEffectDurations
    {
        /// <summary>
        /// Advances all effects by one turn. Duration semantics:
        /// <list type="bullet">
        /// <item>&lt; 0 means "infinite" (e.g. a part's passive standing modifier): never decremented, never removed.</item>
        /// <item>&gt; 0 ticks down by one and is removed once it reaches 0.</item>
        /// <item>== 0 is treated as expired and removed.</item>
        /// </list>
        /// </summary>
        public static IReadOnlyList<IStatusEffect> Tick(IReadOnlyList<IStatusEffect> effects)
        {
            var result = new List<IStatusEffect>(effects?.Count ?? 0);
            if (effects == null)
            {
                return result;
            }

            foreach (var effect in effects)
            {
                var concrete = effect as StatusEffect;
                var next = effect.Duration > 0 && concrete != null
                    ? (IStatusEffect)concrete.DecrementDuration()
                    : effect;

                if (next.Duration != 0)
                {
                    result.Add(next);
                }
            }

            return result;
        }
    }
}
