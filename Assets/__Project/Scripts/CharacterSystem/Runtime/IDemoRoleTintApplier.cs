using UnityEngine;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Applies a demo role tint over an assembled character rig (green villager / maroon bandit).
    /// A placeholder readability affordance until real per-faction art lands; an alpha of 0 means
    /// "no tint" so unedited assets keep their authored materials.
    /// </summary>
    public interface IDemoRoleTintApplier
    {
        /// <summary>Tints every renderer under <paramref name="root"/>; no-op when the tint's alpha is 0.</summary>
        void Apply(GameObject root, Color tint);
    }
}
