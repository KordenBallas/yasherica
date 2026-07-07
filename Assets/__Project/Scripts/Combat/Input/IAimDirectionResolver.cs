using UnityEngine;

namespace Combat.Input
{
    /// <summary>
    /// Resolves the combat aim direction for the active gesture (movement aim, ability aim, volley
    /// aim) from whatever pointing device the player is using right now: pointer position raycast to
    /// the ground on keyboard+mouse, left-stick vector in camera space on gamepad. Keeps the input
    /// controller's gesture state machine device-agnostic.
    /// </summary>
    public interface IAimDirectionResolver
    {
        /// <summary>Planar world-space aim direction from the character, or null when the device
        /// points nowhere useful (no ground hit, stick in deadzone, device missing).</summary>
        Vector3? ResolveAimDirection(Transform characterTransform);
    }
}
