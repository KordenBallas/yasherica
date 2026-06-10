using System;
using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using UnityEngine;

namespace Combat.Player
{
    /// <summary>
    /// Presenter for combat ability targeting.
    /// Hold key → mouse aim → release key to confirm (adds to execution queue, ends turn).
    /// Ring abilities show the full ring immediately; Line abilities update on each direction change.
    /// </summary>
    public class CombatAbilityPresenter : IDisposable
    {
        private readonly ICombatController _combatController;
        private readonly IBattlefield _battlefield;
        private readonly HexCellController _cellController;
        private readonly IAbilityShapeCalculator _shapeCalculator;
        private readonly IUnit _playerUnit;

        private IAbilityInstance _selectedAbility;
        private HexDirection? _currentDirection;
        private readonly HashSet<HexCoordinates> _highlightedCells = new HashSet<HexCoordinates>();

        public CombatAbilityPresenter(
            ICombatController combatController,
            IBattlefield battlefield,
            HexCellController cellController,
            IAbilityShapeCalculator shapeCalculator,
            IUnit playerUnit)
        {
            _combatController = combatController;
            _battlefield = battlefield;
            _cellController = cellController;
            _shapeCalculator = shapeCalculator;
            _playerUnit = playerUnit;
        }

        /// <summary>
        /// Enters ability aiming mode for the specified ability slot.
        /// Ring abilities immediately highlight their cells; Line waits for a direction.
        /// </summary>
        public void SelectAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= _playerUnit.Abilities.Count)
            {
                Debug.LogWarning($"[CombatAbilityPresenter] Invalid ability index: {abilityIndex}");
                return;
            }

            var ability = _playerUnit.Abilities[abilityIndex];
            if (!ability.IsAvailable)
            {
                Debug.LogWarning($"[CombatAbilityPresenter] Ability {ability.Ability.Name} is on cooldown");
                return;
            }

            _selectedAbility = ability;
            _currentDirection = null;

            if (_selectedAbility.Ability.Shape.Type == AbilityShapeType.Ring)
                ShowAffectedCells(null);
        }

        /// <summary>
        /// Updates the aimed direction and recomputes the Line highlight.
        /// Ignored for Ring abilities.
        /// </summary>
        public void UpdateAimDirection(HexDirection? direction)
        {
            if (_selectedAbility == null) return;
            if (_selectedAbility.Ability.Shape.Type == AbilityShapeType.Ring) return;

            _currentDirection = direction;
            ShowAffectedCells(direction);
        }

        /// <summary>
        /// Confirms the current aim and submits a ScheduleAbilityAction.
        /// For Line: no-op if no direction chosen (release with no aim = cancel silently).
        /// </summary>
        public void ConfirmAim()
        {
            if (_selectedAbility == null)
            {
                Debug.LogWarning("[CombatAbilityPresenter] No ability selected");
                return;
            }

            var shape = _selectedAbility.Ability.Shape;
            AbilityTarget target;

            if (shape.Type == AbilityShapeType.Ring)
            {
                target = AbilityTarget.None();
            }
            else
            {
                if (!_currentDirection.HasValue)
                {
                    CancelAbilitySelection();
                    return;
                }
                target = AbilityTarget.ForDirection(_currentDirection.Value);
            }

            var action = new ScheduleAbilityAction(
                _playerUnit.Owner,
                _playerUnit.Id,
                _selectedAbility.Ability.Id,
                target);

            var result = _combatController.ProcessAction(action);

            if (!result.Success)
                Debug.LogWarning($"[CombatAbilityPresenter] Schedule failed: {result.ErrorMessage}");
            else
                Debug.Log($"[CombatAbilityPresenter] Ability {_selectedAbility.Ability.Name} scheduled");

            CancelAbilitySelection();
        }

        /// <summary>
        /// Submits the queued abilities for execution (Enter key path).
        /// </summary>
        public void ExecuteQueue()
        {
            var action = new ExecuteAbilityQueueAction(_playerUnit.Owner, _playerUnit.Id);
            var result = _combatController.ProcessAction(action);

            if (!result.Success)
                Debug.LogWarning($"[CombatAbilityPresenter] Execute queue failed: {result.ErrorMessage}");
        }

        /// <summary>
        /// Cancels ability targeting and clears all highlights.
        /// </summary>
        public void CancelAbilitySelection()
        {
            ClearAllHighlights();
            _selectedAbility = null;
            _currentDirection = null;
        }

        private void ShowAffectedCells(HexDirection? direction)
        {
            ClearAllHighlights();
            if (_selectedAbility == null) return;

            var cells = _shapeCalculator.GetAffectedCells(
                _selectedAbility.Ability.Shape,
                _playerUnit.Position,
                direction,
                _battlefield.IsCellInBoundary);

            foreach (var cell in cells)
            {
                _cellController.HighlightCell(cell, HighlightType.ValidAbilityTarget);
                _highlightedCells.Add(cell);
            }
        }

        private void ClearAllHighlights()
        {
            foreach (var coords in _highlightedCells)
                _cellController.ClearHighlight(coords);
            _highlightedCells.Clear();
        }

        public void Dispose()
        {
            CancelAbilitySelection();
        }
    }
}
