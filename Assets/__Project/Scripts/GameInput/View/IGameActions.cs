using GameInput.Core;
using UnityEngine.InputSystem;

namespace GameInput.View
{
    /// <summary>
    /// Access to the runtime <see cref="InputAction"/> behind each named <see cref="GameAction"/>
    /// (Input Foundation R1). Input adapters poll these instead of raw devices, so every action
    /// automatically listens to all its per-source bindings at once — keyboard, gamepad, and the
    /// touch overlay's virtual controls all merge into the same action.
    /// </summary>
    public interface IGameActions
    {
        InputAction Get(GameAction action);
    }
}
