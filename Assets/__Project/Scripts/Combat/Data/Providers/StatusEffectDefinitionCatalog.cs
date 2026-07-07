using System.Collections.Generic;
using Combat.Data.Definitions;
using Core.Logging;
using UnityEngine;

namespace Combat.Data.Providers
{
    /// <summary>
    /// Loads every authored StatusEffectDefinition once from Resources and indexes it by id.
    /// </summary>
    public class StatusEffectDefinitionCatalog : IStatusEffectDefinitionCatalog
    {
        private const string ResourcesPath = "Combat/StatusEffects";

        private readonly Dictionary<int, StatusEffectDefinition> _byId =
            new Dictionary<int, StatusEffectDefinition>();

        public StatusEffectDefinitionCatalog(IGameLogger logger)
        {
            foreach (var definition in Resources.LoadAll<StatusEffectDefinition>(ResourcesPath))
            {
                if (_byId.ContainsKey(definition.Id))
                {
                    logger.Warning(LogCategory.Combat,
                        $"[StatusEffectDefinitionCatalog] Duplicate status id {definition.Id} ('{definition.Name}') — keeping the first");
                    continue;
                }

                _byId.Add(definition.Id, definition);
            }

            logger.Info(LogCategory.Combat,
                $"[StatusEffectDefinitionCatalog] Indexed {_byId.Count} status definition(s) from Resources/{ResourcesPath}");
        }

        public bool TryGet(int statusId, out StatusEffectDefinition definition)
        {
            return _byId.TryGetValue(statusId, out definition);
        }
    }
}
