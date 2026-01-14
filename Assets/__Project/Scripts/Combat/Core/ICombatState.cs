using System.Collections.Generic;
using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// Represents the complete game state. 
    /// IMMUTABLE - single source of truth for the combat system.
    /// </summary>
    public interface ICombatState
    {
        /// <summary>
        /// All units in the game.
        /// </summary>
        IReadOnlyList<IUnit> Units { get; }
        
        /// <summary>
        /// All players in the game.
        /// </summary>
        IReadOnlyList<IPlayer> Players { get; }
        
        /// <summary>
        /// The player whose turn it currently is.
        /// </summary>
        IPlayer CurrentPlayer { get; }
        
        /// <summary>
        /// Current turn number.
        /// </summary>
        int TurnNumber { get; }
        
        /// <summary>
        /// Current phase of the game.
        /// </summary>
        CombatPhase Phase { get; }
        
        /// <summary>
        /// Gets a unit by its ID.
        /// </summary>
        IUnit GetUnit(int unitId);
        
        /// <summary>
        /// Gets the unit at a specific position, or null if none exists.
        /// </summary>
        IUnit GetUnitAt(HexCoordinates position);
        
        /// <summary>
        /// Gets all units owned by a specific player.
        /// </summary>
        IReadOnlyList<IUnit> GetUnitsByPlayer(IPlayer player);
        
        /// <summary>
        /// Gets all units owned by a player that can still act this turn.
        /// </summary>
        IReadOnlyList<IUnit> GetActiveUnitsByPlayer(IPlayer player);

        /// <summary>
        /// Checks if a hex position is valid (within battlefield boundaries).
        /// </summary>
        bool IsPositionValid(HexCoordinates position);

        /// <summary>
        /// Gets all valid positions within range of a center point.
        /// Filters out positions outside battlefield boundaries and occupied cells.
        /// </summary>
        IReadOnlyList<HexCoordinates> GetValidPositionsInRange(HexCoordinates center, int range);

        /// <summary>
        /// Calculates hex distance between two positions.
        /// </summary>
        int CalculateDistance(HexCoordinates from, HexCoordinates to);
    }
}

