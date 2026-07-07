using GameInput.Core;
using GameInput.View;

namespace Narrative.Interaction.View
{
    /// <summary>
    /// Adapter answering <see cref="IInteractionInput"/> from the shared Interact action (Input
    /// Foundation R1), so the proximity presenter stays free of raw input handling (CLAUDE.md §6).
    /// Polling the action instead of a raw key is what puts world interaction on every source at
    /// once — F, the gamepad's north button, and the touch overlay's interact button all land here.
    /// </summary>
    public sealed class NpcInteractionInput : IInteractionInput
    {
        private readonly IGameActions _actions;

        public NpcInteractionInput(IGameActions actions)
        {
            _actions = actions;
        }

        public bool WasInteractPressedThisFrame()
        {
            var interact = _actions.Get(GameAction.Interact);
            return interact != null && interact.WasPressedThisFrame();
        }
    }
}
