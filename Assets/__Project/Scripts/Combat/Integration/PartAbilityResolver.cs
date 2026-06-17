using System.Collections.Generic;
using CharacterSystem.Data;
using Combat.Data.Definitions;
using Core.Logging;

namespace Combat.Integration
{
    /// <summary>
    /// Resolves part-granted abilities by looking each equipped part id up in the part
    /// catalog and collecting its declared active and passive abilities. Pure C#: depends
    /// only on the part catalog and the logger, so it is unit-testable without Unity.
    /// </summary>
    public class PartAbilityResolver : IPartAbilityResolver
    {
        private readonly IPartCatalog _partCatalog;
        private readonly IGameLogger _logger;

        public PartAbilityResolver(IPartCatalog partCatalog, IGameLogger logger)
        {
            _partCatalog = partCatalog;
            _logger = logger;
        }

        public PartAbilitySet Resolve(IEnumerable<string> equippedPartIds)
        {
            if (equippedPartIds == null)
            {
                return PartAbilitySet.Empty;
            }

            var active = new List<AbilityDefinition>();
            var passive = new List<PassiveAbilityDefinition>();
            // Dedupe by asset reference: the same ability granted by two parts is offered once.
            var seenActive = new HashSet<AbilityDefinition>();
            var seenPassive = new HashSet<PassiveAbilityDefinition>();

            foreach (var partId in equippedPartIds)
            {
                if (string.IsNullOrEmpty(partId))
                {
                    continue;
                }

                if (!_partCatalog.TryGet(partId, out var part))
                {
                    _logger?.Warning($"[PartAbilityResolver] Unknown part id '{partId}'; no abilities granted.");
                    continue;
                }

                foreach (var ability in part.ActiveAbilities)
                {
                    if (ability != null && seenActive.Add(ability))
                    {
                        active.Add(ability);
                    }
                }

                foreach (var passiveAbility in part.PassiveAbilities)
                {
                    if (passiveAbility != null && seenPassive.Add(passiveAbility))
                    {
                        passive.Add(passiveAbility);
                    }
                }
            }

            return new PartAbilitySet(active, passive);
        }
    }
}
