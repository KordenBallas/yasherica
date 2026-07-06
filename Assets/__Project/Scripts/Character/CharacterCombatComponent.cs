using System.Collections.Generic;
using Combat.Animation;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Integration;
using Core.Logging;

namespace Character
{
    /// <summary>
    /// Thin adapter that integrates the player character with the combat system: builds the
    /// hero's internal Unit (abilities from equipped parts, standing passive modifiers) and
    /// hands sync to <see cref="CombatUnitComponentBase"/> — the one visual path all units share.
    /// </summary>
    public class CharacterCombatComponent : CombatUnitComponentBase
    {
        protected override LogCategory LogCategory => LogCategory.Character;

        /// <summary>
        /// Initializes the character for combat.
        /// Creates internal Unit instance with basic stats and subscribes to state changes.
        /// </summary>
        public void InitializeForCombat(
            int unitId,
            IPlayer owner,
            HexCoordinates startPosition,
            ICombatController combatController,
            int maxHP = 100)
        {
            var emptyAbilities = new List<IAbilityInstance>();
            InitializeForCombat(unitId, owner, startPosition, combatController, maxHP, emptyAbilities);
        }

        /// <summary>
        /// Initializes the character for combat with specific abilities and standing passive
        /// modifiers granted by equipped parts (applied for the whole combat). The optional
        /// movement config + animator enable the shared cell-to-cell glide (D8).
        /// </summary>
        public void InitializeForCombat(
            int unitId,
            IPlayer owner,
            HexCoordinates startPosition,
            ICombatController combatController,
            int maxHP,
            IReadOnlyList<IAbilityInstance> abilities,
            IReadOnlyList<IStatusEffect> passiveEffects = null,
            CombatMovementConfig movementConfig = null,
            ICharacterMovementAnimator moveAnimator = null,
            HexDirectionConfig hexConfig = null)
        {
            var internalUnit = new Unit(
                id: unitId,
                owner: owner,
                position: startPosition,
                currentHP: maxHP,
                maxHP: maxHP,
                abilities: abilities,
                statusEffects: passiveEffects);

            BeginCombat(internalUnit, combatController, movementConfig, moveAnimator, hexConfig);

            Logger?.Info(LogCategory, $"[CharacterCombatComponent] Initialized for combat: ID={unitId}, Position={startPosition}, Owner={owner.Name}, Abilities={abilities.Count}, Passives={passiveEffects?.Count ?? 0}");
        }

        /// <summary>
        /// The hero stays visible when it falls — its death ends the combat and the defeat
        /// presentation owns that moment, so the board-clearing rule (D5) does not hide it.
        /// </summary>
        protected override void OnUnitDied()
        {
        }
    }
}
