namespace Narrative.Interaction
{
    /// <summary>
    /// Abstraction over the interact (F) button, so the proximity presenter stays free of raw input
    /// handling (CLAUDE.md §6). The MonoBehaviour adapter reads the Unity Input System and answers this.
    /// </summary>
    public interface IInteractionInput
    {
        /// <summary>True on the single frame the interact button went down.</summary>
        bool WasInteractPressedThisFrame();
    }
}
