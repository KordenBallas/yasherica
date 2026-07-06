using System.Collections.Generic;
using CharacterSystem.Data.Definitions;

namespace UI.AbilityPreview
{
    /// <summary>
    /// Which hero the preview stage shows — the per-scene seam of the shared popover: the
    /// mutation surface supplies the live hero (assembly + equipped parts), the arena draft
    /// its base assembly + currently drafted parts. May report false while no hero exists yet
    /// (the popover then degrades to text-only).
    /// </summary>
    public interface IAbilityPreviewHeroSource
    {
        bool TryGetHero(
            out CharacterAssemblyDefinition assembly,
            out IReadOnlyDictionary<string, string> partOverrides);
    }
}
