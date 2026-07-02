using System.Collections.Generic;
using System.Linq;
using Combat.Core;
using Combat.Data.Definitions;
using Combat.Data.Factories;
using Core.Logging;
using UnityEngine;

namespace Combat.Data.Providers
{
    /// <summary>
    /// IAbilityDataProvider implementation using ScriptableObject definitions.
    /// </summary>
    public class ScriptableObjectAbilityProvider : IAbilityDataProvider
    {
        private readonly Dictionary<int, IAbility> _abilities;
        private readonly List<IAbility> _allAbilities;
        private readonly IGameLogger _logger;

        public ScriptableObjectAbilityProvider(
            IReadOnlyList<AbilityDefinition> abilityDefinitions,
            IAbilityFactory abilityFactory,
            IGameLogger logger)
        {
            _logger = logger;
            _abilities = new Dictionary<int, IAbility>();
            _allAbilities = new List<IAbility>();

            if (abilityDefinitions == null || abilityDefinitions.Count == 0)
            {
                _logger.Warning(LogCategory.Combat,"[ScriptableObjectAbilityProvider] No ability definitions provided");
                return;
            }

            foreach (var definition in abilityDefinitions)
            {
                if (definition == null)
                    continue;

                var ability = abilityFactory.CreateAbility(definition);

                if (_abilities.ContainsKey(ability.Id))
                {
                    _logger.Warning(LogCategory.Combat,
                        $"[ScriptableObjectAbilityProvider] Duplicate ability ID {ability.Id}: " +
                        $"'{ability.Name}' conflicts with existing ability");
                    continue;
                }

                _abilities[ability.Id] = ability;
                _allAbilities.Add(ability);
            }
        }

        public IAbility GetAbility(int abilityId)
        {
            if (_abilities.TryGetValue(abilityId, out var ability))
                return ability;

            _logger.Warning(LogCategory.Combat,$"[ScriptableObjectAbilityProvider] Ability {abilityId} not found");
            return null;
        }

        public IReadOnlyList<IAbility> GetAllAbilities()
        {
            return _allAbilities;
        }

        public IReadOnlyList<IAbility> GetAbilitiesByEffectType(AbilityEffectType effectType)
        {
            return _allAbilities.Where(a => a.EffectType == effectType).ToList();
        }
    }
}
