using UnityEngine;

namespace UI.AbilityPreview
{
    /// <summary>
    /// The one ability-preview mechanism, two surfaces (mutation cards + arena draft): name +
    /// description always, plus a 3D hero demonstrating the cast when the stage can build one.
    /// </summary>
    public interface IAbilityPreviewPopover
    {
        void Show(AbilityPreviewData data, Vector2 screenPosition);
        void Hide();
    }
}
