using System.Collections;
using Core.Logging;
using UnityEngine;

namespace Combat.Animation
{
    /// <summary>
    /// Instant teleport animator for testing or fast gameplay.
    /// Immediately moves character to target position without animation.
    /// </summary>
    public class InstantTeleportAnimator : ICharacterMovementAnimator
    {
        private readonly IGameLogger _logger;

        public InstantTeleportAnimator(IGameLogger logger = null)
        {
            _logger = logger;
        }

        public IEnumerator AnimateMovement(Transform character, Vector3 from, Vector3 to)
        {
            if (character == null)
            {
                _logger?.Warning(LogCategory.Combat, "[InstantTeleportAnimator] Character transform is null");
                yield break;
            }
            
            character.position = to;
            yield return null; // Yield at least one frame for coroutine consistency
        }
    }
}

