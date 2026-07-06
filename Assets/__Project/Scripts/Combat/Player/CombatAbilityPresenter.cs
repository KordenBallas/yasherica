using System;
using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Core.Logging;
using UnityEngine;

namespace Combat.Player
{
    /// <summary>
    /// Presenter for combat ability targeting.
    /// Hold key → mouse aim → release key to confirm (adds to execution queue, ends turn).
    /// Aim = turn: the aim input rotates the unit (free ChangeDirectionAction), and the
    /// highlight always reads the unit's current facing — one facing for the whole queue.
    /// </summary>
    public class CombatAbilityPresenter : IDisposable
    {
        private readonly ICombatController _combatController;
        private readonly IBattlefield _battlefield;
        private readonly HexCellController _cellController;
        private readonly IAbilityShapeCalculator _shapeCalculator;
        private readonly IUnit _playerUnit;
        private readonly IGameLogger _logger;

        private IAbilityInstance _selectedAbility;
        private readonly HashSet<HexCoordinates> _highlightedCells = new HashSet<HexCoordinates>();

        public CombatAbilityPresenter(
            ICombatController combatController,
            IBattlefield battlefield,
            HexCellController cellController,
            IAbilityShapeCalculator shapeCalculator,
            IUnit playerUnit,
            IGameLogger logger)
        {
            _combatController = combatController;
            _battlefield = battlefield;
            _cellController = cellController;
            _shapeCalculator = shapeCalculator;
            _playerUnit = playerUnit;
            _logger = logger;
        }

        /// <summary>
        /// Enters ability aiming mode for the specified ability slot.
        /// Ring abilities highlight their ring; Line abilities highlight along the unit's
        /// current facing immediately (there is always a facing).
        /// </summary>
        public void SelectAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= _playerUnit.Abilities.Count)
            {
                _logger.Warning(LogCategory.Combat,$"[CombatAbilityPresenter] Invalid ability index: {abilityIndex}");
                return;
            }

            var ability = _playerUnit.Abilities[abilityIndex];
            if (!ability.IsAvailable)
            {
                _logger.Warning(LogCategory.Combat,$"[CombatAbilityPresenter] Ability {ability.Ability.Name} is on cooldown");
                return;
            }

            _selectedAbility = ability;
            ShowAffectedCells();
        }

        /// <summary>
        /// Aim = turn. Rotates the unit toward the aimed direction (free action, unlimited)
        /// and re-highlights from the new facing. Ignored for Ring abilities.
        /// </summary>
        public void UpdateAimDirection(HexDirection? direction)
        {
            if (_selectedAbility == null) return;
            if (_selectedAbility.Ability.Shape.Type == AbilityShapeType.Ring) return;
            if (!direction.HasValue) return;

            var liveUnit = GetLiveUnit();
            if (liveUnit == null || liveUnit.FacingDirection == direction.Value) return;

            var result = _combatController.ProcessAction(
                new ChangeDirectionAction(_playerUnit.Owner, _playerUnit.Id, direction.Value));

            if (!result.Success)
            {
                _logger.Warning(LogCategory.Combat,$"[CombatAbilityPresenter] Turn failed: {result.ErrorMessage}");
                return;
            }

            ShowAffectedCells();
        }

        /// <summary>
        /// Confirms the aim and submits a ScheduleAbilityAction. The ability carries no
        /// direction — it will fire along the unit's facing at execution time.
        /// </summary>
        public void ConfirmAim()
        {
            if (_selectedAbility == null)
            {
                _logger.Warning(LogCategory.Combat,"[CombatAbilityPresenter] No ability selected");
                return;
            }

            var abilityId = _selectedAbility.Ability.Id;
            var action = new ScheduleAbilityAction(
                _playerUnit.Owner,
                _playerUnit.Id,
                abilityId);

            var result = _combatController.ProcessAction(action);

            if (!result.Success)
                _logger.Warning(LogCategory.Combat,$"[CombatAbilityPresenter] Schedule failed: {result.ErrorMessage}");
            else
                _logger.Info(LogCategory.Combat,$"[CombatAbilityPresenter] Ability {_selectedAbility.Ability.Name} scheduled");

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
                _logger.Warning(LogCategory.Combat,$"[CombatAbilityPresenter] Execute queue failed: {result.ErrorMessage}");
        }

        /// <summary>
        /// Cancels ability targeting and clears all highlights.
        /// </summary>
        public void CancelAbilitySelection()
        {
            ClearAllHighlights();
            _selectedAbility = null;
        }

        /// <summary>
        /// Enters volley aim (D2, Enter held): highlights the whole queued volley along the unit's
        /// current facing, so the player sees where the queue will fire before turning/releasing.
        /// </summary>
        public void BeginVolleyAim()
        {
            ShowQueuedVolleyCells();
        }

        /// <summary>
        /// Aim = turn for the whole volley: rotates the unit toward the aimed direction (a free,
        /// unlimited <see cref="ChangeDirectionAction"/>, no ability selected) and re-highlights the
        /// queued volley from the new facing — the shipped global-facing model re-points the entire
        /// queue and its telegraphs at once (D2).
        /// </summary>
        public void UpdateVolleyAim(HexDirection? direction)
        {
            if (!direction.HasValue)
            {
                ShowQueuedVolleyCells();
                return;
            }

            var liveUnit = GetLiveUnit();
            if (liveUnit == null || liveUnit.FacingDirection == direction.Value)
            {
                ShowQueuedVolleyCells();
                return;
            }

            var result = _combatController.ProcessAction(
                new ChangeDirectionAction(_playerUnit.Owner, _playerUnit.Id, direction.Value));

            if (!result.Success)
            {
                _logger.Warning(LogCategory.Combat,$"[CombatAbilityPresenter] Volley turn failed: {result.ErrorMessage}");
                return;
            }

            ShowQueuedVolleyCells();
        }

        /// <summary>Clears the volley-aim highlight (on execute or right-click cancel).</summary>
        public void EndVolleyAim()
        {
            ClearAllHighlights();
        }

        /// <summary>
        /// Highlights every queued ability's affected cells from the unit's live position + facing —
        /// the whole volley the queue will fire, recomputed on each turn so the read stays honest.
        /// </summary>
        private void ShowQueuedVolleyCells()
        {
            ClearAllHighlights();

            var liveUnit = GetLiveUnit();
            if (liveUnit == null) return;

            foreach (var scheduled in liveUnit.AbilityQueue)
            {
                var shape = scheduled.Ability.Ability.Shape;
                var direction = shape.Type == AbilityShapeType.Line
                    ? liveUnit.FacingDirection
                    : (HexDirection?)null;

                var cells = _shapeCalculator.GetAffectedCells(
                    shape,
                    liveUnit.Position,
                    direction,
                    _battlefield.IsCellInBoundary);

                foreach (var cell in cells)
                {
                    _cellController.HighlightCell(cell, HighlightType.ValidAbilityTarget);
                    _highlightedCells.Add(cell);
                }
            }
        }

        private void ShowAffectedCells()
        {
            ClearAllHighlights();
            if (_selectedAbility == null) return;

            var liveUnit = GetLiveUnit();
            if (liveUnit == null) return;

            var direction = _selectedAbility.Ability.Shape.Type == AbilityShapeType.Line
                ? liveUnit.FacingDirection
                : (HexDirection?)null;

            var cells = _shapeCalculator.GetAffectedCells(
                _selectedAbility.Ability.Shape,
                liveUnit.Position,
                direction,
                _battlefield.IsCellInBoundary);

            foreach (var cell in cells)
            {
                _cellController.HighlightCell(cell, HighlightType.ValidAbilityTarget);
                _highlightedCells.Add(cell);
            }
        }

        /// <summary>
        /// The unit's current immutable snapshot in the live combat state (position and
        /// facing move on every action; the injected reference is the initial snapshot).
        /// </summary>
        private IUnit GetLiveUnit()
        {
            return _combatController.CombatState?.GetUnit(_playerUnit.Id) ?? _playerUnit;
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
