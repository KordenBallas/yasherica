using UnityEngine;

namespace Combat.Config
{
    /// <summary>
    /// Combat input feel settings. Key/button assignments no longer live here — they are carried by
    /// the shared <c>GameActions.inputactions</c> asset (declared in <c>InputBindingCatalog</c>), so
    /// this config keeps only the device-agnostic gesture thresholds.
    /// </summary>
    [CreateAssetMenu(fileName = "InputConfig", menuName = "Combat/Input Config")]
    public class InputConfig : ScriptableObject
    {
        [Tooltip("Holding the volley fire input at least this long enters volley aim mode; a shorter " +
                 "tap fires the queue immediately along the current facing (D7)")]
        public float volleyAimHoldThresholdSeconds = 0.25f;

        [Tooltip("Aim deadzone: minimum cursor distance from the hero (world units) and minimum " +
                 "stick deflection for an aim direction to register")]
        public float inputDeadzone = 0.1f;
    }
}
