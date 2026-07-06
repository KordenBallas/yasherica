using System;
using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Fan-in for the live ability-fired cue (D3). The executor is pure C# and shared by the player's
    /// queue and the enemy resolve, so it notifies this sink (bound scene-wide) whenever an ability
    /// executes; a presentation view subscribes once to <see cref="Fired"/> and plays the live
    /// animation. Optional on the executor so headless/tests stay null-safe (mirrors
    /// <c>ICombatOutcomeRelay</c>).
    /// </summary>
    public interface IAbilityFiredSink
    {
        event Action<AbilityFiredCue> Fired;

        void Notify(AbilityFiredCue cue);
    }
}
