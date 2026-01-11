using Combat.Battlefield;
using Combat.Config;
using Combat.Input;

namespace Combat.Player
{
    /// <summary>
    /// Handles combat movement input processing.
    /// Single responsibility: reading input and converting to hex coordinates.
    /// Pure C# class, no Unity dependencies.
    /// </summary>
    public class CombatMovementInputHandler
    {
        private readonly IInputController _inputController;
        private readonly HexDirectionConfig _hexConfig;
        
        public CombatMovementInputHandler(
            IInputController inputController,
            HexDirectionConfig hexConfig)
        {
            _inputController = inputController;
            _hexConfig = hexConfig;
        }
        
        /// <summary>
        /// Returns true if movement mode is currently active.
        /// </summary>
        public bool IsActive => _inputController.IsMovementModeActive;
        
        /// <summary>
        /// Gets the target cell based on current input direction.
        /// </summary>
        /// <param name="currentPosition">The unit's current position</param>
        /// <returns>Target hex coordinates, or null if no valid direction</returns>
        public HexCoordinates? GetTargetCell(HexCoordinates currentPosition)
        {
            var direction = _inputController.GetMovementDirection();
            if (!direction.HasValue)
                return null;
            
            return DirectionToHexConverter.GetNeighborInDirection(
                currentPosition, 
                direction.Value, 
                _hexConfig);
        }
        
        /// <summary>
        /// Returns true if the confirm action was pressed this frame.
        /// </summary>
        public bool IsConfirmed => _inputController.IsConfirmPressed;
        
        /// <summary>
        /// Returns true if the cancel action was pressed this frame.
        /// </summary>
        public bool IsCancelled => _inputController.IsCancelPressed;
    }
}
