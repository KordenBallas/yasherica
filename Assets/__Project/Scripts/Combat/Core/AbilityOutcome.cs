using System.Collections.Generic;
using Combat.Battlefield;

namespace Combat.Core
{
    /// <summary>
    /// The predicted result of one ability against the current board — what the ghost
    /// telegraph shows. Computed without mutating any state.
    /// </summary>
    public class AbilityOutcome
    {
        public IReadOnlyList<HexCoordinates> AffectedCells { get; }
        public IReadOnlyList<UnitOutcome> Units { get; }

        public AbilityOutcome(
            IReadOnlyList<HexCoordinates> affectedCells,
            IReadOnlyList<UnitOutcome> units)
        {
            AffectedCells = affectedCells ?? new List<HexCoordinates>();
            Units = units ?? new List<UnitOutcome>();
        }
    }

    /// <summary>
    /// Predicted effect on one affected unit: damage/heal numbers and the cell it would be
    /// displaced to (From equals To when the unit stays in place).
    /// </summary>
    public readonly struct UnitOutcome
    {
        public int UnitId { get; }
        public int Damage { get; }
        public int Heal { get; }
        public HexCoordinates From { get; }
        public HexCoordinates To { get; }
        public bool IsDisplaced => !From.Equals(To);

        public UnitOutcome(int unitId, int damage, int heal, HexCoordinates from, HexCoordinates to)
        {
            UnitId = unitId;
            Damage = damage;
            Heal = heal;
            From = from;
            To = to;
        }
    }
}
