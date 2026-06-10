using System;

namespace Combat.Player
{
    /// <summary>
    /// Tracks the active input mode (movement or ability aiming) to prevent conflicts.
    /// Mutual exclusion is enforced at the PCInputController level; this class provides
    /// a shared signal for handlers that need to know the current mode.
    /// </summary>
    public class CombatInputModeManager : IDisposable
    {
        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
        }
    }
}
