using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Data;
using Combat.Integration;
using Core.Logging;

namespace Combat.Enemy
{
    /// <summary>
    /// Thin adapter that integrates an enemy GameObject with the combat system: builds the
    /// enemy's internal Unit from its EnemyData and hands sync to
    /// <see cref="CombatUnitComponentBase"/> — the one visual path all units share. On death the
    /// base clears the model from the board (D5); the GameObject survives until platform cleanup
    /// so the loot drop still reads the death position.
    /// </summary>
    public class EnemyCombatComponent : CombatUnitComponentBase
    {
        /// <summary>
        /// Initializes the enemy for combat. Creates the internal Unit with enemy stats and
        /// abilities from EnemyData. The optional movement config + animator enable the shared
        /// cell-to-cell glide (D8).
        /// </summary>
        public void InitializeForCombat(
            int unitId,
            IPlayer owner,
            HexCoordinates startPosition,
            ICombatController combatController,
            EnemyData enemyData,
            CombatMovementConfig movementConfig = null,
            ICharacterMovementAnimator moveAnimator = null,
            HexDirectionConfig hexConfig = null)
        {
            var internalUnit = new Unit(
                id: unitId,
                owner: owner,
                position: startPosition,
                currentHP: enemyData.MaxHP,
                maxHP: enemyData.MaxHP,
                abilities: enemyData.Abilities);

            BeginCombat(internalUnit, combatController, movementConfig, moveAnimator, hexConfig);

            Logger?.Info(LogCategory, $"[EnemyCombatComponent] Initialized for combat: ID={unitId}, Name={enemyData.Name}, Position={startPosition}, HP={enemyData.MaxHP}");
            Logger?.Info(LogCategory, $"[EnemyCombatComponent] Abilities: {enemyData.Abilities.Count}, Owner={owner.Name}");
        }
    }
}
