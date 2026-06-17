using CharacterSystem.Runtime;
using Core.Logging;
using Mutation.Core;

namespace Mutation.Infrastructure
{
    /// <summary>
    /// Adapts the scene's <see cref="ModularCharacterVisual"/> to the UnityEngine-free
    /// <see cref="IMutationCharacter"/> port so the stage-up mutation choice can swap a body part
    /// without the presenter touching a MonoBehaviour. The live character is assembled lazily
    /// (after <c>Start</c>), so both the swap and the equipped-part query are resolved at call time:
    /// an unassembled rig logs/returns false rather than throwing. The equipped-part query reads the
    /// character's live <see cref="IModularCharacter.EquippedParts"/> snapshot, so the option builder
    /// excludes every currently equipped part - including the character's starting parts.
    /// </summary>
    public sealed class ModularCharacterMutationAdapter : IMutationCharacter
    {
        private readonly ModularCharacterVisual _visual;
        private readonly IGameLogger _logger;

        public ModularCharacterMutationAdapter(ModularCharacterVisual visual, IGameLogger logger)
        {
            _visual = visual;
            _logger = logger;
        }

        public bool SwapPart(string slotId, string partId)
        {
            var character = _visual != null ? _visual.Character : null;
            if (character == null)
            {
                _logger.Warning(
                    $"[ModularCharacterMutationAdapter] No assembled character yet; cannot swap " +
                    $"'{partId}' into '{slotId}'.");
                return false;
            }

            return character.SwapPart(slotId, partId);
        }

        public bool TryGetEquippedPartId(string slotId, out string partId)
        {
            partId = null;
            if (string.IsNullOrEmpty(slotId))
            {
                return false;
            }

            // Read the live equipped-parts snapshot so starting parts (never swapped this run) are
            // excluded too. An unassembled rig has no equipped parts yet -> unknown (false).
            var character = _visual != null ? _visual.Character : null;
            return character != null && character.EquippedParts.TryGetValue(slotId, out partId);
        }
    }
}
