using Combat.Data.Definitions;

namespace Combat.Data.Providers
{
    /// <summary>
    /// Lookup of authored ability definitions by ability id — the presentation layer's
    /// source for icons and other authored visuals of a runtime ability.
    /// </summary>
    public interface IAbilityDefinitionCatalog
    {
        bool TryGet(int abilityId, out AbilityDefinition definition);
    }
}
