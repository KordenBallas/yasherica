using System;
using System.Collections.Generic;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Data transfer object for ability slot display information.
    /// </summary>
    public struct AbilitySlotData
    {
        public string Name;
        public Sprite Icon;
        public string KeybindLabel;
        public int CooldownRemaining;
        public bool IsAvailable;

        public AbilitySlotData(string name, Sprite icon, string keybindLabel, int cooldownRemaining, bool isAvailable)
        {
            Name = name;
            Icon = icon;
            KeybindLabel = keybindLabel;
            CooldownRemaining = cooldownRemaining;
            IsAvailable = isAvailable;
        }
    }

    /// <summary>
    /// Interface for the combat action panel UI view.
    /// Follows MVP pattern - view is thin and delegates all logic to presenter.
    /// </summary>
    public interface ICombatActionPanelView
    {
        /// <summary>
        /// Shows the action panel.
        /// </summary>
        void ShowPanel();

        /// <summary>
        /// Hides the action panel.
        /// </summary>
        void HidePanel();

        /// <summary>
        /// Configures the move action button.
        /// </summary>
        void SetMoveAction(bool available, string keybindLabel);

        /// <summary>
        /// Configures the change direction action button.
        /// </summary>
        void SetChangeDirectionAction(bool available, string keybindLabel);

        /// <summary>
        /// Configures the execute queue action button.
        /// </summary>
        void SetExecuteQueueAction(bool available, int queueCount, string keybindLabel);

        /// <summary>
        /// Sets all ability slots with their data.
        /// </summary>
        void SetAbilities(IReadOnlyList<AbilitySlotData> abilities);

        /// <summary>
        /// Updates a specific ability's cooldown and availability.
        /// </summary>
        void UpdateAbilityCooldown(int index, int cooldown, bool available);

        /// <summary>
        /// Highlights the selected action button.
        /// </summary>
        void HighlightSelectedAction(int index);

        /// <summary>
        /// Clears all action highlighting.
        /// </summary>
        void ClearHighlight();

        // Events - emitted when user clicks UI buttons
        event Action OnMoveClicked;
        event Action OnChangeDirectionClicked;
        event Action OnExecuteQueueClicked;
        event Action<int> OnAbilityClicked;
    }
}
