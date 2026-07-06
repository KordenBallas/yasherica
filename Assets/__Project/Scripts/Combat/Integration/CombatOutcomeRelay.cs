using Combat.Core;

namespace Combat.Integration
{
    /// <summary>Default <see cref="ICombatOutcomeRelay"/>: a plain re-broadcast, no state.</summary>
    public sealed class CombatOutcomeRelay : ICombatOutcomeRelay
    {
        public event System.Action<CombatPhase> CombatEnded;

        public void Notify(CombatPhase phase) => CombatEnded?.Invoke(phase);
    }
}
