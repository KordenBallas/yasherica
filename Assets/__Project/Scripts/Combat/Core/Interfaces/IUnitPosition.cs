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
    }
}
