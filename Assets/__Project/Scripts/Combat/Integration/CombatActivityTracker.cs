using System;

namespace Combat.Integration
{
    /// <summary>
    /// Combat activity flag updated by CombatActiveState on enter/exit.
    /// Consumers depend on ICombatActivityTracker; only the combat layer mutates it.
    /// </summary>
    public class CombatActivityTracker : ICombatActivityTracker
    {
        public bool IsCombatActive { get; private set; }

        public event Action<bool> OnCombatActivityChanged;

        public void SetCombatActive(bool isActive)
        {
            if (IsCombatActive == isActive)
            {
                return;
            }

            IsCombatActive = isActive;
            OnCombatActivityChanged?.Invoke(isActive);
        }
    }
}
