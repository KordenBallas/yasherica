using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// Concrete implementation of game state.
    /// IMMUTABLE - all modifications create new instances.
    /// </summary>
    public class CombatState : ICombatState
    {
        public IReadOnlyList<IUnit> Units { get; }
        public IReadOnlyList<IPlayer> Players { get; }
        public IPlayer CurrentPlayer { get; }
        public int TurnNumber { get; }
        public CombatPhase Phase { get; }
        
        public CombatState(
            IReadOnlyList<IUnit> units,
            IReadOnlyList<IPlayer> players,
            IPlayer currentPlayer,
            int turnNumber = 1,
            CombatPhase phase = CombatPhase.Setup)
        {
            Units = units ?? new List<IUnit>();
            Players = players ?? new List<IPlayer>();
            CurrentPlayer = currentPlayer;
            TurnNumber = turnNumber;
            Phase = phase;
        }
        
        public IUnit GetUnit(int unitId)
        {
            return Units.FirstOrDefault(u => u.Id == unitId);
        }
        
        public IUnit GetUnitAt(HexCoordinates position)
        {
            return Units.FirstOrDefault(u => u.Position.Equals(position));
        }
        
        public IReadOnlyList<IUnit> GetUnitsByPlayer(IPlayer player)
        {
            return Units.Where(u => u.Owner.Id == player.Id).ToList();
        }
        
        public IReadOnlyList<IUnit> GetActiveUnitsByPlayer(IPlayer player)
        {
            return Units
                .Where(u => u.Owner.Id == player.Id && u.IsAlive && u.CanAct)
                .ToList();
        }
        
        /// <summary>
        /// Creates a new game state with updated units.
        /// </summary>
        public CombatState WithUnits(IReadOnlyList<IUnit> newUnits)
        {
            return new CombatState(newUnits, Players, CurrentPlayer, TurnNumber, Phase);
        }
        
        /// <summary>
        /// Creates a new game state with a single unit updated.
        /// </summary>
        public CombatState WithUpdatedUnit(IUnit updatedUnit)
        {
            var newUnits = Units.Select(u => u.Id == updatedUnit.Id ? updatedUnit : u).ToList();
            return new CombatState(newUnits, Players, CurrentPlayer, TurnNumber, Phase);
        }
        
        /// <summary>
        /// Creates a new game state with updated current player.
        /// </summary>
        public CombatState WithCurrentPlayer(IPlayer newCurrentPlayer)
        {
            return new CombatState(Units, Players, newCurrentPlayer, TurnNumber, Phase);
        }
        
        /// <summary>
        /// Creates a new game state with incremented turn number.
        /// </summary>
        public CombatState WithNextTurn()
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber + 1, Phase);
        }
        
        /// <summary>
        /// Creates a new game state with updated phase.
        /// </summary>
        public CombatState WithPhase(CombatPhase newPhase)
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber, newPhase);
        }
    }
}

