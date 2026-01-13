using UnityEngine;

namespace Combat.Input.Commands
{
    /// <summary>
    /// Command emitted when the movement direction changes.
    /// Only emitted when movement mode is active.
    /// </summary>
    public readonly struct MovementDirectionChangedCommand : IInputCommand
    {
        public float Timestamp { get; }
        public InputCommandType Type => InputCommandType.MovementDirectionChanged;

        /// <summary>
        /// Normalized world direction vector (XZ plane).
        /// Null indicates no direction (e.g., player stopped pointing).
        /// </summary>
        public Vector3? WorldDirection { get; }

        public MovementDirectionChangedCommand(Vector3? worldDirection, float timestamp)
        {
            WorldDirection = worldDirection;
            Timestamp = timestamp;
        }
    }
}
