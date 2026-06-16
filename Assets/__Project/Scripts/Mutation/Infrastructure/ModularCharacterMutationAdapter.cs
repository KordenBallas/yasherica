using System;
using System.Collections.Generic;
using CharacterSystem.Runtime;
using Core.Logging;
using Mutation.Core;

namespace Mutation.Infrastructure
{
    /// <summary>
    /// Adapts the scene's <see cref="ModularCharacterVisual"/> to the UnityEngine-free
    /// <see cref="IMutationCharacter"/> port so the stage-up mutation choice can swap a body part
    /// without the presenter touching a MonoBehaviour. The live character is assembled lazily
    /// (after <c>Start</c>), so the swap is resolved at call time: an unassembled rig logs and
    /// returns false rather than throwing. A write-through cache remembers parts swapped in this run
    /// so the option builder will not re-offer them (starting parts are unknown - accepted for M1).
    /// </summary>
    public sealed class ModularCharacterMutationAdapter : IMutationCharacter
    {
        private readonly ModularCharacterVisual _visual;
        private readonly IGameLogger _logger;
        private readonly Dictionary<string, string> _equippedBySlot =
            new Dictionary<string, string>(StringComparer.Ordinal);

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

            if (!character.SwapPart(slotId, partId))
            {
                return false;
            }

            _equippedBySlot[slotId] = partId;
            return true;
        }

        public bool TryGetEquippedPartId(string slotId, out string partId)
        {
            if (!string.IsNullOrEmpty(slotId))
            {
                return _equippedBySlot.TryGetValue(slotId, out partId);
            }

            partId = null;
            return false;
        }
    }
}
