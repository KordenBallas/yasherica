using System.Collections.Generic;
using Combat.Data.Definitions;
using Core.Logging;
using UnityEngine;

namespace Combat.Data.Providers
{
    /// <summary>
    /// Loads every authored AbilityDefinition once from Resources and indexes it by id.
    /// </summary>
    public class AbilityDefinitionCatalog : IAbilityDefinitionCatalog
    {
        private const string ResourcesPath = "Abilities/Data";

        private readonly Dictionary<int, AbilityDefinition> _byId = new Dictionary<int, AbilityDefinition>();

        public AbilityDefinitionCatalog(IGameLogger logger)
        {
            foreach (var definition in Resources.LoadAll<AbilityDefinition>(ResourcesPath))
            {
                if (_byId.ContainsKey(definition.Id))
                {
                    logger.Warning(LogCategory.Combat,
                        $"[AbilityDefinitionCatalog] Duplicate ability id {definition.Id} ('{definition.Name}') — keeping the first");
                    continue;
                }

                _byId.Add(definition.Id, definition);
            }

            logger.Info(LogCategory.Combat,
                $"[AbilityDefinitionCatalog] Indexed {_byId.Count} ability definition(s) from Resources/{ResourcesPath}");
        }

        public bool TryGet(int abilityId, out AbilityDefinition definition)
        {
            return _byId.TryGetValue(abilityId, out definition);
        }
    }
}
