using System;
using System.Collections.Generic;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Data.Definitions;
using Combat.Input;
using Combat.View;

namespace Combat.Player
{
    /// <summary>
    /// Presenter for the combat action panel UI.
    /// Pure C# class - handles business logic for action panel display and interaction.
    /// Follows MVP pattern - coordinates between view, combat state, and input.
    /// </summary>
    public class CombatActionPanelPresenter : IDisposable
    {
        private readonly ICombatActionPanelView _view;
        private readonly ICombatController _combatController;
        private readonly InputConfig _inputConfig;
        private IUnit _currentUnit;
        private IReadOnlyList<AbilityDefinition> _abilityDefinitions;

        public CombatActionPanelPresenter(
            ICombatActionPanelView view,
            ICombatController combatController,
            InputConfig inputConfig)
        {
            _view = view;
            _combatController = combatController;
            _inputConfig = inputConfig;

            // Subscribe to combat state changes
            if (_combatController != null)
            {
                _combatController.OnStateChanged += HandleStateChanged;
            }

            // Wire up view events (delegate to input mode manager, not implemented here)
            // The input mode manager will handle mode switching
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
            _view.SetMoveAction(unit.CanMove(), GetKeybindLabel(_inputConfig.movementModeKey));
            _view.SetChangeDirectionAction(unit.CanAct, GetKeybindLabel(_inputConfig.changeDirectionKey));
            _view.SetExecuteQueueAction(
                unit.AbilityQueue.Count > 0,
                unit.AbilityQueue.Count,
                GetKeybindLabel(_inputConfig.executeQueueKey));

            // Populate ability slots
            var abilityData = new List<AbilitySlotData>();
            for (int i = 0; i < unit.Abilities.Count; i++)
            {
                var ability = unit.Abilities[i];
                var definition = i < _abilityDefinitions.Count ? _abilityDefinitions[i] : null;
                string keybind = i < _inputConfig.abilityKeys.Count
                    ? GetKeybindLabel(_inputConfig.abilityKeys[i])
                    : "";

                var data = new AbilitySlotData(
                    ability.Ability.Name,
                    definition?.Icon,
                    keybind,
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

        /// <summary>
        /// Converts a KeyCode to a display label.
        /// </summary>
        private string GetKeybindLabel(UnityEngine.KeyCode keyCode)
        {
            // Simple conversion - could be enhanced with custom mappings
            return keyCode.ToString().Replace("Alpha", "").Replace("Mouse0", "LMB").Replace("Mouse1", "RMB");
        }

        public void Dispose()
        {
            if (_combatController != null)
            {
                _combatController.OnStateChanged -= HandleStateChanged;
            }
        }
    }
}
