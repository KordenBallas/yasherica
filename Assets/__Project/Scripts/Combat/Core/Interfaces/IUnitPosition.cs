using Combat.Battlefield;
using Combat.Config;

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
        /// Direction the unit is facing. Directional (Line) abilities fire along it;
        /// there is one facing for the whole ability queue.
        /// </summary>
        HexDirection FacingDirection { get; }
    }
}
