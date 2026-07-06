using UnityEngine;

namespace UI.AbilityPreview
{
    /// <summary>
    /// The popover's 3D half: a hidden stage with the hero performing the previewed ability
    /// (cast pose + looping cell sweep; idle for a passive), rendered to a texture.
    /// </summary>
    public interface IAbilityPreviewStage
    {
        bool TryShow(AbilityPreviewData data, out Texture texture);
        void Hide();
    }
}
