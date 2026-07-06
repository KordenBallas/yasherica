using System.Collections.Generic;
using CharacterSystem.Data.Definitions;
using CharacterSystem.Runtime;
using UI.AbilityPreview;
using Zenject;

namespace Mutation.Infrastructure
{
    /// <summary>
    /// The mutation surface's hero for the shared ability-preview popover: the live hero's
    /// assembly + equipped parts, so the cast demonstration shows the body the player
    /// actually has. Resolves lazily — before the rig assembles, the popover degrades to
    /// text-only.
    /// </summary>
    public class MutationAbilityPreviewHeroSource : IAbilityPreviewHeroSource
    {
        [Inject] private ModularCharacterVisual _heroVisual;

        public bool TryGetHero(
            out CharacterAssemblyDefinition assembly,
            out IReadOnlyDictionary<string, string> partOverrides)
        {
            assembly = null;
            partOverrides = null;
            if (_heroVisual == null || _heroVisual.Character == null)
            {
                return false;
            }

            assembly = _heroVisual.Assembly;
            partOverrides = _heroVisual.Character.EquippedParts;
            return assembly != null;
        }
    }
}
