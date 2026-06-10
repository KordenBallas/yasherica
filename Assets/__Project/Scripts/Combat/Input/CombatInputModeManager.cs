using System;
using UnityEngine;

namespace Combat.Input
{
    /// <summary>
    /// Defines the different input modes available during combat.
    /// </summary>
    public enum CombatInputMode
    {
        Idle,              // No active input mode
        Movement,          // Movement targeting active
        AbilityTargeting,  // Ability targeting active
        ChangeDirection    // Direction change mode active
    }

    /// <summary>
    /// Manages mutual exclusivity between combat input modes.
    /// Ensures only one mode is active at a time.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class CombatInputModeManager : IDisposable
    {
        private CombatInputMode _currentMode = CombatInputMode.Idle;

        public event Action<CombatInputMode> OnModeChanged;

        public CombatInputMode CurrentMode => _currentMode;

        /// <summary>
        /// Enters movement mode, cancelling other active modes.
        /// </summary>
        public void EnterMovementMode()
        {
            if (_currentMode == CombatInputMode.Movement)
                return;

            ExitCurrentMode();
            SetMode(CombatInputMode.Movement);
            Debug.Log("[CombatInputModeManager] Entered Movement mode");
        }

        /// <summary>
        /// Enters ability targeting mode, cancelling other active modes.
        /// </summary>
        public void EnterAbilityMode(int abilityIndex)
        {
            if (_currentMode == CombatInputMode.AbilityTargeting)
                return;

            ExitCurrentMode();
            SetMode(CombatInputMode.AbilityTargeting);
            Debug.Log($"[CombatInputModeManager] Entered Ability Targeting mode (ability {abilityIndex})");
        }

        /// <summary>
        /// Enters change direction mode, cancelling other active modes.
        /// </summary>
        public void EnterChangeDirectionMode()
        {
            if (_currentMode == CombatInputMode.ChangeDirection)
                return;

            ExitCurrentMode();
            SetMode(CombatInputMode.ChangeDirection);
            Debug.Log("[CombatInputModeManager] Entered Change Direction mode");
        }

        /// <summary>
        /// Exits the current mode and returns to Idle.
        /// </summary>
        public void ExitCurrentMode()
        {
            if (_currentMode == CombatInputMode.Idle)
                return;

            Debug.Log($"[CombatInputModeManager] Exiting {_currentMode} mode");
            SetMode(CombatInputMode.Idle);
        }

        private void SetMode(CombatInputMode newMode)
        {
            if (_currentMode == newMode)
                return;

            _currentMode = newMode;
            OnModeChanged?.Invoke(_currentMode);
        }

        public void Dispose()
        {
            ExitCurrentMode();
            OnModeChanged = null;
        }
    }
}
