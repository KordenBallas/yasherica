using UnityEngine;
using UnityEngine.InputSystem;

namespace Narrative.Interaction.View
{
    /// <summary>
    /// Input System adapter for the interact (F) button. A thin MonoBehaviour that answers the
    /// <see cref="IInteractionInput"/> the proximity presenter polls, keeping raw input out of the
    /// presenter (CLAUDE.md §6).
    /// </summary>
    public sealed class NpcInteractionInput : MonoBehaviour, IInteractionInput
    {
        public bool WasInteractPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.fKey.wasPressedThisFrame;
        }
    }
}
