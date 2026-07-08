using Combat.Arena.Core;
using Combat.Core;
using Combat.Data.Factories;
using Combat.Data.Providers;

namespace Combat.Arena
{
    /// <summary>
    /// The production <see cref="IArenaStatusReconstructor"/>: a snapshot's status triple
    /// re-derives its behaviour from the authored <c>StatusEffectDefinition</c> by id (the same
    /// catalog the status-icon views read) and rebuilds mid-life through the factory. The one
    /// Data-layer bridge on the arena state-transfer path.
    /// </summary>
    public class CatalogStatusReconstructor : IArenaStatusReconstructor
    {
        private readonly IStatusEffectDefinitionCatalog _catalog;
        private readonly IStatusEffectFactory _factory;

        public CatalogStatusReconstructor(
            IStatusEffectDefinitionCatalog catalog, IStatusEffectFactory factory)
        {
            _catalog = catalog;
            _factory = factory;
        }

        public bool TryRebuild(int effectId, int duration, int stackCount, out IStatusEffect effect)
        {
            if (!_catalog.TryGet(effectId, out var definition))
            {
                effect = null;
                return false;
            }

            effect = _factory.CreateStatusEffect(definition, duration, stackCount);
            return true;
        }
    }
}
