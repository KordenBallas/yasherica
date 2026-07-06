using System.Collections.Generic;
using CharacterSystem.Data.Definitions;
using Combat.Arena.Data;
using UI.AbilityPreview;
using Zenject;

namespace Combat.Arena.View
{
    /// <summary>
    /// The arena surface's hero for the shared ability-preview popover: the draft's base
    /// assembly + whatever the local player has drafted so far, so the cast demonstration
    /// shows the monster being built. NEVER injects the scene's ModularCharacterVisual —
    /// with several spawned heroes that binding is ambiguous in the Arena scene.
    /// </summary>
    public class ArenaDraftHeroSource : IAbilityPreviewHeroSource
    {
        [Inject] private ArenaDraftConfig _draftConfig;
        [Inject] private ArenaDraftFlow _draftFlow;

        public bool TryGetHero(
            out CharacterAssemblyDefinition assembly,
            out IReadOnlyDictionary<string, string> partOverrides)
        {
            assembly = _draftConfig.BaseAssembly;
            partOverrides = _draftFlow.Model != null
                ? _draftFlow.Model.LoadoutOf(_draftFlow.LocalPlayerId)
                : null;
            return assembly != null;
        }
    }
}
