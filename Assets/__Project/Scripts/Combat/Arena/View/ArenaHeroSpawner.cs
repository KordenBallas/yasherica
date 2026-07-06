using System.Collections.Generic;
using System.Linq;
using Character;
using CharacterSystem.Runtime;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Data.Definitions;
using Combat.Data.Factories;
using Combat.Input;
using Combat.Integration;
using Combat.Player;
using Combat.View;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Combat.Arena.View
{
    /// <summary>
    /// Spawns one hero per roster slot with its DRAFTED body (P4-5/G4): the seat's slot→part
    /// loadout drives both the combat ability set (via <see cref="IPartAbilityResolver"/> — the
    /// same part→combat mapping PvE uses) and the visual (part swaps on the modular rig once it
    /// assembles). Unit ids stay HOST-ASSIGNED (the deterministic lockstep contract); identical
    /// loadouts on every client ⇒ identical ability sets ⇒ determinism holds. A seat without a
    /// loadout falls back to the HeroDefinition kit — loudly, that is a draft wiring bug.
    /// </summary>
    public class ArenaHeroSpawner
    {
        private const string HeroPrefabPath = "Prefabs/Hero";
        private const int PermanentEffectDuration = -1;

        private readonly DiContainer _container;
        private readonly IAbilityFactory _abilityFactory;
        private readonly IPartAbilityResolver _partAbilityResolver;
        private readonly IStatusEffectFactory _statusEffectFactory;
        private readonly HeroDefinition _heroDefinition;
        private readonly ICombatUnitViewRegistry _unitViewRegistry;
        private readonly HexDirectionConfig _hexDirectionConfig;
        private readonly IInputController _inputController;
        private readonly IGameLogger _logger;

        public ArenaHeroSpawner(
            DiContainer container,
            IAbilityFactory abilityFactory,
            IPartAbilityResolver partAbilityResolver,
            IStatusEffectFactory statusEffectFactory,
            HeroDefinition heroDefinition,
            ICombatUnitViewRegistry unitViewRegistry,
            HexDirectionConfig hexDirectionConfig,
            IInputController inputController,
            IGameLogger logger)
        {
            _container = container;
            _abilityFactory = abilityFactory;
            _partAbilityResolver = partAbilityResolver;
            _statusEffectFactory = statusEffectFactory;
            _heroDefinition = heroDefinition;
            _unitViewRegistry = unitViewRegistry;
            _hexDirectionConfig = hexDirectionConfig;
            _inputController = inputController;
            _logger = logger;
        }

        public void SpawnAll(
            IReadOnlyList<ArenaSpawnSlot> slots,
            IBattlefield battlefield,
            ICombatController controller,
            IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> loadoutByPlayerId)
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
                IReadOnlyDictionary<string, string> loadout = null;
                loadoutByPlayerId?.TryGetValue(slot.Owner.Id, out loadout);
                Spawn(slot, prefab, battlefield, controller, loadout);
            }
        }

        private void Spawn(
            ArenaSpawnSlot slot,
            GameObject prefab,
            IBattlefield battlefield,
            ICombatController controller,
            IReadOnlyDictionary<string, string> loadout)
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

            BuildAbilitySet(slot, loadout,
                out var abilities, out var abilityDefinitions, out var passiveEffects);
            ApplyDraftedBody(hero, slot, loadout);

            combatComponent.InitializeForCombat(
                slot.UnitId, slot.Owner, slot.SpawnCell, controller, _heroDefinition.MaxHP,
                abilities, passiveEffects);
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
                (loadout != null ? $" with {loadout.Count} drafted parts" : " [HeroDefinition fallback]") +
                (slot.IsLocal ? " [local]" : string.Empty));
        }

        /// <summary>
        /// Fresh ability instances per unit (instances carry per-unit cooldown state): drafted
        /// parts are the source of truth; the shared HeroDefinition kit is the loud fallback.
        /// </summary>
        private void BuildAbilitySet(
            ArenaSpawnSlot slot,
            IReadOnlyDictionary<string, string> loadout,
            out List<IAbilityInstance> abilities,
            out List<AbilityDefinition> abilityDefinitions,
            out List<IStatusEffect> passiveEffects)
        {
            abilities = new List<IAbilityInstance>();
            abilityDefinitions = new List<AbilityDefinition>();
            passiveEffects = new List<IStatusEffect>();

            if (loadout != null && loadout.Count > 0)
            {
                var partAbilities = _partAbilityResolver.Resolve(loadout.Values);
                foreach (var definition in partAbilities.ActiveAbilities)
                {
                    abilities.Add(_abilityFactory.CreateAbilityInstance(definition));
                    abilityDefinitions.Add(definition);
                }

                foreach (var passive in partAbilities.PassiveAbilities)
                {
                    if (passive != null && passive.Modifier != null)
                    {
                        passiveEffects.Add(
                            _statusEffectFactory.CreateStatusEffect(passive.Modifier, PermanentEffectDuration));
                    }
                }
            }

            if (abilities.Count == 0)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaHeroSpawner] Player {slot.Owner.Id} has no drafted active abilities " +
                    "(missing loadout or ability-less parts) — falling back to the HeroDefinition kit.");
                foreach (var definition in _heroDefinition.Abilities)
                {
                    abilities.Add(_abilityFactory.CreateAbilityInstance(definition));
                    abilityDefinitions.Add(definition);
                }
            }
        }

        /// <summary>
        /// Rebuilds the hero's modular rig to the drafted body: one part swap per drafted slot,
        /// applied as soon as the rig reports assembled (the prefab assembles in Start, which
        /// may run after this spawn call).
        /// </summary>
        private void ApplyDraftedBody(
            GameObject hero, ArenaSpawnSlot slot, IReadOnlyDictionary<string, string> loadout)
        {
            if (loadout == null || loadout.Count == 0)
            {
                return;
            }

            var visual = hero.GetComponentInChildren<ModularCharacterVisual>();
            if (visual == null)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaHeroSpawner] Player {slot.Owner.Id}'s hero has no ModularCharacterVisual — " +
                    "the drafted body cannot be shown (abilities still apply).");
                return;
            }

            var pending = loadout.ToList();
            void Apply(IModularCharacter character)
            {
                foreach (var slotToPart in pending)
                {
                    character.SwapPart(slotToPart.Key, slotToPart.Value);
                }
            }

            if (visual.Character != null)
            {
                Apply(visual.Character);
            }
            else
            {
                void OnAssembled(IModularCharacter character)
                {
                    visual.CharacterAssembled -= OnAssembled;
                    Apply(character);
                }

                visual.CharacterAssembled += OnAssembled;
            }
        }
    }
}
