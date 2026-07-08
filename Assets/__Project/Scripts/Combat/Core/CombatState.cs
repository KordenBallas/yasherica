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
        public RoundPhase RoundPhase { get; }
        public IReadOnlyList<EnemyIntent> EnemyIntents { get; }

        private readonly IBattlefield _battlefield;

        public CombatState(
            IReadOnlyList<IUnit> units,
            IReadOnlyList<IPlayer> players,
            IPlayer currentPlayer,
            int turnNumber = 1,
            CombatPhase phase = CombatPhase.Setup,
            IBattlefield battlefield = null,
            RoundPhase roundPhase = RoundPhase.EnemyPlan,
            IReadOnlyList<EnemyIntent> enemyIntents = null)
        {
            Units = units ?? new List<IUnit>();
            Players = players ?? new List<IPlayer>();
            CurrentPlayer = currentPlayer;
            TurnNumber = turnNumber;
            Phase = phase;
            RoundPhase = roundPhase;
            EnemyIntents = enemyIntents ?? new List<EnemyIntent>();
            _battlefield = battlefield;
        }
        
        public IUnit GetUnit(int unitId)
        {
            return Units.FirstOrDefault(u => u.Id == unitId);
        }
        
        public IUnit GetUnitAt(HexCoordinates position)
        {
            // Occupancy reads alive units only (D5): a unit that dies frees its cell the moment
            // it dies — movement validation, enemy move fizzle, push blocking, and targeting all
            // stop treating the corpse as a blocker. Dead units stay in Units (win conditions,
            // HP sync, and the turn strip still see them); they just no longer hold the board.
            return Units.FirstOrDefault(u => u.IsAlive && u.Position.Equals(position));
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
            return new CombatState(newUnits, Players, CurrentPlayer, TurnNumber, Phase, _battlefield, RoundPhase, EnemyIntents);
        }

        /// <summary>
        /// Creates a new game state with a single unit updated.
        /// </summary>
        public CombatState WithUpdatedUnit(IUnit updatedUnit)
        {
            var newUnits = Units.Select(u => u.Id == updatedUnit.Id ? updatedUnit : u).ToList();
            return new CombatState(newUnits, Players, CurrentPlayer, TurnNumber, Phase, _battlefield, RoundPhase, EnemyIntents);
        }

        /// <summary>
        /// Creates a new game state with updated current player.
        /// </summary>
        public CombatState WithCurrentPlayer(IPlayer newCurrentPlayer)
        {
            return new CombatState(Units, Players, newCurrentPlayer, TurnNumber, Phase, _battlefield, RoundPhase, EnemyIntents);
        }

        /// <summary>
        /// Creates a new game state with incremented turn number.
        /// </summary>
        public CombatState WithNextTurn()
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber + 1, Phase, _battlefield, RoundPhase, EnemyIntents);
        }

        /// <summary>
        /// Creates a new game state at an explicit turn number (arena state transfer: a rejoiner
        /// adopts the authoritative round mid-match).
        /// </summary>
        public CombatState WithTurnNumber(int turnNumber)
        {
            return new CombatState(Units, Players, CurrentPlayer, turnNumber, Phase, _battlefield, RoundPhase, EnemyIntents);
        }

        /// <summary>
        /// Creates a new game state with updated phase.
        /// </summary>
        public CombatState WithPhase(CombatPhase newPhase)
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber, newPhase, _battlefield, RoundPhase, EnemyIntents);
        }

        /// <summary>
        /// Creates a new game state with updated round phase.
        /// </summary>
        public CombatState WithRoundPhase(RoundPhase newRoundPhase)
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber, Phase, _battlefield, newRoundPhase, EnemyIntents);
        }

        /// <summary>
        /// Creates a new game state with the round's committed enemy intents.
        /// </summary>
        public CombatState WithEnemyIntents(IReadOnlyList<EnemyIntent> intents)
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber, Phase, _battlefield, RoundPhase, intents);
        }

        /// <summary>
        /// Creates a new game state with battlefield reference injected.
        /// Used after battlefield is initialized.
        /// </summary>
        public CombatState WithBattlefield(IBattlefield battlefield)
        {
            return new CombatState(Units, Players, CurrentPlayer, TurnNumber, Phase, battlefield, RoundPhase, EnemyIntents);
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

