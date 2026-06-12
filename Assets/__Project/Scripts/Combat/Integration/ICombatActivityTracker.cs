using System;

namespace Combat.Integration
{
    /// <summary>
    /// Read-only signal telling other subsystems (e.g. inventory) whether combat
    /// is currently active, without coupling them to combat internals.
    /// </summary>
    public interface ICombatActivityTracker
    {
        bool IsCombatActive { get; }

        event Action<bool> OnCombatActivityChanged;
    }
}
