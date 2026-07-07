using Combat.Data.Definitions;

namespace Combat.Data.Providers
{
    /// <summary>
    /// Id-indexed lookup over every authored StatusEffectDefinition — the single source of a
    /// status's glyph and display data for both UI surfaces (on-unit row + card effect slot).
    /// </summary>
    public interface IStatusEffectDefinitionCatalog
    {
        bool TryGet(int statusId, out StatusEffectDefinition definition);
    }
}
