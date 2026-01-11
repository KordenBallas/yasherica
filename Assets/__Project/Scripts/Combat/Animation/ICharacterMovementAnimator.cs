using System.Collections;
using UnityEngine;

namespace Combat.Animation
{
    /// <summary>
    /// Strategy interface for character movement animation.
    /// Allows different animation implementations to be swapped easily.
    /// </summary>
    public interface ICharacterMovementAnimator
    {
        /// <summary>
        /// Animates character movement from one position to another.
        /// </summary>
        /// <param name="character">The character transform to animate</param>
        /// <param name="from">Starting world position</param>
        /// <param name="to">Target world position</param>
        /// <returns>Coroutine that completes when animation finishes</returns>
        IEnumerator AnimateMovement(Transform character, Vector3 from, Vector3 to);
    }
}
