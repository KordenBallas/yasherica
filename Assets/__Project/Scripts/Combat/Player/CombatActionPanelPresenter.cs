using System;
using System.Collections.Generic;
using Combat.Controller;
using Combat.Core;
using Combat.Data.Definitions;
using Combat.View;
using GameInput.Core;

namespace Combat.Player
{
    /// <summary>
    /// Presenter for the combat action panel UI.
    /// Pure C# class - handles business logic for action panel display and interaction.
    /// Follows MVP pattern - coordinates between view, combat state, and input. Keybind labels come
    /// from <see cref="IPromptCueProvider"/> for the ACTIVE input source (Input Foundation R6), and
    /// the whole panel re-renders the moment the player switches device.
    /// </summary>
    public class CombatActionPanelPresenter : IDisposable
    {
        private readonly ICombatActionPanelView _view;
        private readonly ICombatController _combatController;
        private readonly IPromptCueProvider _cues;
        private IUnit _currentUnit;
        private IReadOnlyList<AbilityDefinition> _abilityDefinitions;

        public CombatActionPanelPresenter(
            ICombatActionPanelView view,
            ICombatController combatController,
            IPromptCueProvider cues)
        {
            _view = view;
            _combatController = combatController;
            _cues = cues;

            // Subscribe to combat state changes
            if (_combatController != null)
            {
                _combatController.OnStateChanged += HandleStateChanged;
            }

            // Device switches re-label every button (e.g. "M" → "LT") without waiting for a state change.
            if (_cues != null)
            {
                _cues.CuesChanged += HandleCuesChanged;
            }
        }

        /// <summary>
        /// Sets the unit whose actions are displayed in the panel.
        /// </summary>
        public void SetUnit(IUnit unit, IReadOnlyList<AbilityDefinition> abilityDefinitions)
        {
            _currentUnit = unit;
            _abilityDefinitions = abilityDefinitions;

            if (unit == null)
            {
                _view.HidePanel();
                return;
            }

            // Populate action buttons
            _view.SetMoveAction(unit.CanMove(), _cues.GetCue(GameAction.CombatMoveMode));
            _view.SetChangeDirectionAction(unit.CanAct, _cues.GetCue(GameAction.CombatChangeDirection));
            _view.SetExecuteQueueAction(
                unit.AbilityQueue.Count > 0,
                unit.AbilityQueue.Count,
                _cues.GetCue(GameAction.Fire));

            // Populate ability slots
            var abilityData = new List<AbilitySlotData>();
            for (int i = 0; i < unit.Abilities.Count; i++)
            {
                var ability = unit.Abilities[i];
                var definition = i < _abilityDefinitions.Count ? _abilityDefinitions[i] : null;

                var data = new AbilitySlotData(
                    ability.Ability.Name,
                    definition?.Icon,
                    _cues.GetAbilitySlotCue(i),
                    ability.CurrentCooldown,
                    ability.IsAvailable
                );
                abilityData.Add(data);
            }

            _view.SetAbilities(abilityData);
            _view.ShowPanel();
        }

        /// <summary>
        /// Clears the current unit and hides the panel.
        /// </summary>
        public void ClearUnit()
        {
            _currentUnit = null;
            _view.HidePanel();
        }

        /// <summary>
        /// Refreshes the panel display based on current combat state.
        /// Called automatically when combat state changes.
        /// </summary>
        private void HandleStateChanged(ICombatState newState)
        {
            if (_currentUnit == null)
                return;

            // Get updated unit from state
            var updatedUnit = newState.GetUnit(_currentUnit.Id);
            if (updatedUnit == null)
            {
                ClearUnit();
                return;
            }

            // Refresh display with updated unit (keep definitions)
            SetUnit(updatedUnit, _abilityDefinitions);
        }

        private void HandleCuesChanged()
        {
            if (_currentUnit != null)
            {
                SetUnit(_currentUnit, _abilityDefinitions);
            }
        }

        public void Dispose()
        {
            if (_combatController != null)
            {
                _combatController.OnStateChanged -= HandleStateChanged;
            }

            if (_cues != null)
            {
                _cues.CuesChanged -= HandleCuesChanged;
            }
        }
    }
}
