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

        private readonly IBattlefield _battlefield;

        public CombatState(
            IReadOnlyList<IUnit> units,
            IReadOnlyList<IPlayer> players,
            IPlayer currentPlayer,
            int turnNumber = 1,
            CombatPhase phase = CombatPhase.Setup,
            IBattlefield battlefield = null)
        {
            Units = units ?? new List<IUnit>();
            Players = players ?? new List<IPlayer>();
            CurrentPlayer = currentPlayer;
            TurnNumber = turnNumber;
            Phase = phase;
            _battlefield = battlefield;
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
            return new CombatState(newUnits, Players, CurrentPlayer, TurnNumber, Phase, _battlefield);
        }
        
        /// <summary>
        /// Creates a new game state with a single unit updated.
        /// </summary>
        public CombatState WithUpdatedUnit(IUnit updatedUnit)
        {
            var newUnits = Units.Select(u => u.Id == updatedUnit.Id ? updatedUnit : u).ToList();
            return new CombatState(newUnits, Players, CurrentPlayer, TurnNumber, Phase, _battlefield);
        }
        
        /// <summary>
        /// Creates a new game state with updated current player.
        /// </summary>
        public CombatState WithCurrentPlayer(IPlayer newCurrentPlayer)
        {
            return new CombatState(Units, Players, newCurrentPlayer, TurnNumber, Phase, _battlefield);
        }
        
        /// <summary>
        /// Creates a new game state with incremented turn number.
        /// </summary>
        public CombatState WithNextTurn()
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber + 1, Phase, _battlefield);
        }
        
        /// <summary>
        /// Creates a new game state with updated phase.
        /// </summary>
        public CombatState WithPhase(CombatPhase newPhase)
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber, newPhase, _battlefield);
        }

        /// <summary>
        /// Creates a new game state with battlefield reference injected.
        /// Used after battlefield is initialized.
        /// </summary>
        public CombatState WithBattlefield(IBattlefield battlefield)
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber, Phase, battlefield);
        }

        public bool IsPositionValid(HexCoordinates position)
        {
            if (_battlefield == null)
                return true; // If no battlefield, assume valid (early initialization)

            return _battlefield.IsCellInBoundary(position);
        }

        public IReadOnlyList<HexCoordinates> GetValidPositionsInRange(HexCoordinates center, int range)
        {
            if (_battlefield == null)
                return new List<HexCoordinates>(); // No battlefield, no valid positions

            var validPositions = new List<HexCoordinates>();
            var cellsInRange = _battlefield.GetCellsInRange(center, range);

            foreach (var cell in cellsInRange)
            {
                // Skip if occupied
                if (GetUnitAt(cell.Coordinates) != null)
                    continue;

                validPositions.Add(cell.Coordinates);
            }

            return validPositions;
        }

        public int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            var dq = System.Math.Abs(from.Q - to.Q);
            var dr = System.Math.Abs(from.R - to.R);
            var ds = System.Math.Abs((from.Q + from.R) - (to.Q + to.R));

            return (dq + dr + ds) / 2;
        }
    }
}

