using Combat.Core;
using UnityEngine;

namespace Combat.Battlefield
{
    /// <summary>
    /// Represents a cell occupied by a unit.
    /// Stores reference to the occupying unit and prevents certain interactions.
    /// </summary>
    public class HexCellOccupiedState : HexCellStateBase
    {
        private readonly IUnit occupyingUnit;
        private readonly Color baseColor;

        public override HexCellStateType StateType => HexCellStateType.Occupied;

        /// <summary>
        /// Gets the unit currently occupying this cell.
        /// </summary>
        public IUnit OccupyingUnit => occupyingUnit;

        /// <summary>
        /// Creates an occupied state.
        /// </summary>
        /// <param name="unit">The unit occupying this cell</param>
        /// <param name="color">Optional color override, defaults to light blue tint</param>
        public HexCellOccupiedState(IUnit unit, Color? color = null)
        {
            occupyingUnit = unit;
            baseColor = color ?? new Color(0.8f, 0.8f, 1f, 0.3f); // Light blue tint
        }

        public override void OnEnter(IHexCell cell)
        {
            cell.IsActive = true;
            Debug.Log($"[HexCellOccupiedState] Cell {cell.Coordinates} occupied by unit {occupyingUnit?.Id}");
        }

        public override void OnExit(IHexCell cell)
        {
            Debug.Log($"[HexCellOccupiedState] Cell {cell.Coordinates} no longer occupied");
        }

        public override Color GetColor()
        {
            return baseColor;
        }

        public override bool CanTransitionTo(IHexCellState targetState)
        {
            // Can transition to highlighted (to show hover over occupied cell)
            // Can transition to idle (when unit leaves)
            // Cannot transition to another occupied state directly
            if (targetState.StateType == HexCellStateType.Occupied)
            {
                return false; // Must clear occupation first
            }
            return true;
        }
    }
}
