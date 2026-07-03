using System.Collections.Generic;
using Character;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Data.Definitions;
using Combat.Data.Factories;
using Combat.Input;
using Combat.Player;
using Combat.View;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Combat.Arena.View
{
    /// <summary>
    /// Spawns the same default hero for every roster slot (brief R6): each seat gets its own
    /// Hero.prefab instance with the HeroDefinition ability set, a HOST-ASSIGNED unit id (never
    /// random — the ids are part of the deterministic lockstep contract), and its planned spawn
    /// cell. The local seat additionally gets the planning input stack (combat coordinator +
    /// input transform); AI seats are driven by <see cref="ArenaAICommitSource"/> instead.
    /// </summary>
    public class ArenaHeroSpawner
    {
        private const string HeroPrefabPath = "Prefabs/Hero";

        private readonly DiContainer _container;
        private readonly IAbilityFactory _abilityFactory;
        private readonly HeroDefinition _heroDefinition;
        private readonly ICombatUnitViewRegistry _unitViewRegistry;
        private readonly HexDirectionConfig _hexDirectionConfig;
        private readonly IInputController _inputController;
        private readonly IGameLogger _logger;

        public ArenaHeroSpawner(
            DiContainer container,
            IAbilityFactory abilityFactory,
            HeroDefinition heroDefinition,
            ICombatUnitViewRegistry unitViewRegistry,
            HexDirectionConfig hexDirectionConfig,
            IInputController inputController,
            IGameLogger logger)
        {
            _container = container;
            _abilityFactory = abilityFactory;
            _heroDefinition = heroDefinition;
            _unitViewRegistry = unitViewRegistry;
            _hexDirectionConfig = hexDirectionConfig;
            _inputController = inputController;
            _logger = logger;
        }

        public void SpawnAll(
            IReadOnlyList<ArenaSpawnSlot> slots,
            IBattlefield battlefield,
            ICombatController controller)
        {
            var prefab = Resources.Load<GameObject>(HeroPrefabPath);
            if (prefab == null)
            {
                _logger.Error(LogCategory.Combat,
                    $"[ArenaHeroSpawner] Hero prefab not found at Resources/{HeroPrefabPath} — cannot spawn the match");
                return;
            }

            foreach (var slot in slots)
            {
                Spawn(slot, prefab, battlefield, controller);
            }
        }

        private void Spawn(
            ArenaSpawnSlot slot, GameObject prefab, IBattlefield battlefield, ICombatController controller)
        {
            var worldPosition = battlefield.HexToWorld(slot.SpawnCell);
            var hero = _container.InstantiatePrefab(prefab, worldPosition, Quaternion.identity, null);
            hero.name = slot.IsLocal ? $"ArenaHero_Local_P{slot.Owner.Id}" : $"ArenaHero_P{slot.Owner.Id}";

            // Exploration locomotion never runs in the arena; combat sync owns the transform.
            var movement = hero.GetComponent<CharacterMovementController>();
            if (movement != null)
            {
                movement.enabled = false;
            }

            var combatComponent = hero.GetComponent<CharacterCombatComponent>();
            if (combatComponent == null)
            {
                combatComponent = _container.InstantiateComponent<CharacterCombatComponent>(hero);
            }

            // Fresh ability instances per unit — instances carry per-unit cooldown state.
            var abilities = new List<IAbilityInstance>();
            var abilityDefinitions = new List<AbilityDefinition>();
            foreach (var definition in _heroDefinition.Abilities)
            {
                abilities.Add(_abilityFactory.CreateAbilityInstance(definition));
                abilityDefinitions.Add(definition);
            }

            combatComponent.InitializeForCombat(
                slot.UnitId, slot.Owner, slot.SpawnCell, controller, _heroDefinition.MaxHP, abilities);
            controller.AddUnit(combatComponent.InternalUnit);

            var facingRotator = hero.GetComponent<UnitFacingRotator>();
            if (facingRotator == null)
            {
                facingRotator = hero.AddComponent<UnitFacingRotator>();
            }

            facingRotator.Initialize(controller, battlefield, _hexDirectionConfig, slot.UnitId);
            _unitViewRegistry.Register(slot.UnitId, hero.transform);

            if (slot.IsLocal)
            {
                var coordinator = _container.InstantiateComponent<CharacterCombatCoordinator>(hero);
                coordinator.Initialize(combatComponent, controller, battlefield, abilityDefinitions);

                if (_inputController is PCInputController pcInput)
                {
                    pcInput.SetCharacterTransform(hero.transform);
                }
            }

            _logger.Info(LogCategory.Combat,
                $"[ArenaHeroSpawner] Spawned hero for player {slot.Owner.Id} (unit {slot.UnitId}) at {slot.SpawnCell.Q},{slot.SpawnCell.R}" +
                (slot.IsLocal ? " [local]" : string.Empty));
        }
    }
}
