using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// Represents unit position on the battlefield.
    /// Part of interface segregation for IUnit.
    /// </summary>
    public interface IUnitPosition
    {
        /// <summary>
        /// Current position on the battlefield in hex coordinates.
        /// </summary>
        HexCoordinates Position { get; }

        /// <summary>
        /// Direction the unit is facing as a hex offset (e.g., (1,0) for East).
        /// Represents one of the six hex neighbor directions.
        /// </summary>
        HexCoordinates FacingDirection { get; }
    }
}
