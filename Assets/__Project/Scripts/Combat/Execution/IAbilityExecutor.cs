using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Executes individual abilities.
    /// </summary>
    public interface IAbilityExecutor
    {
        /// <summary>
        /// Executes a single scheduled ability. Cells are computed at execution time from the
        /// caster's current position and facing (live path).
        /// </summary>
        /// <returns>New game state after ability execution.</returns>
        ICombatState ExecuteAbility(ICombatState gameState, IUnit caster, ScheduledAbility scheduledAbility);

        /// <summary>
        /// Applies an ability to an explicit cell set (committed path — enemy intents fire at
        /// the cells snapshotted at plan time). Same damage/status/trigger behavior as the
        /// live path; lineDirection carries the committed facing for direction-dependent effects.
        /// </summary>
        /// <returns>New game state after ability execution.</returns>
        ICombatState ExecuteAbilityAtCells(
            ICombatState gameState,
            IUnit caster,
            IAbility ability,
            IReadOnlyList<HexCoordinates> cells,
            HexDirection? lineDirection);
    }
}
