using UnityEngine;

namespace UI.AbilityPreview
{
    /// <summary>
    /// The missing-prefab fallback: hovers do nothing, callers never null-check. Keeps a scene
    /// alive when the popover prefab is absent (mirrors the installer guard convention).
    /// </summary>
    public class NullAbilityPreviewPopover : IAbilityPreviewPopover
    {
        public void Show(AbilityPreviewData data, Vector2 screenPosition)
        {
        }

        public void Hide()
        {
        }
    }
}
