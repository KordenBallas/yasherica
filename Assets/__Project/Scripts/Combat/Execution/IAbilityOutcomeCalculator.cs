using Combat.Config;
using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Computes an ability's predicted outcome against the current board without applying
    /// it — the data source for the ghost telegraph (submit ghost + hover replay).
    /// </summary>
    public interface IAbilityOutcomeCalculator
    {
        /// <summary>
        /// Predicted outcome of the caster firing the ability along the given facing
        /// (ignored for ring shapes), from its current position.
        /// </summary>
        AbilityOutcome ComputeForFacing(ICombatState state, IUnit caster, IAbility ability, HexDirection facing);

        /// <summary>
        /// Predicted outcome of a committed enemy intent, over its snapshotted cells.
        /// Returns an empty outcome for non-ability intents.
        /// </summary>
        AbilityOutcome ComputeCommitted(ICombatState state, EnemyIntent intent);
    }
}
