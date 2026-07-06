using UnityEngine;

namespace World.Dressing.Data
{
    /// <summary>
    /// The dressing-kit contract's shared base (dressing-kit brief FR1): a kit is a named,
    /// data-only bundle of visual-asset references a dressing seam binds as a whole — swapping the
    /// look is repointing the one kit reference on the consuming config, never scene or code
    /// surgery. Store-pack assets are reachable ONLY through kit assets (FR4); demo kits live in
    /// <c>Resources/World/Dressing/Demo/</c>. A future backdrop-kit kind (Track E4) is one more
    /// subclass — the binding/swap/tone rules stay this contract's.
    /// </summary>
    public abstract class DressingKitDefinition : ScriptableObject
    {
        [Tooltip("Stable kit id for logs and registries (e.g. demo-desert-features)")]
        [SerializeField] private string _kitId = string.Empty;

        public string KitId => _kitId;
    }
}
