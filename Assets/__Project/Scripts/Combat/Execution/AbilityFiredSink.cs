using System;
using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Plain relay implementation of <see cref="IAbilityFiredSink"/> — re-raises every cue on
    /// <see cref="Fired"/>. Bound scene-wide (AsSingle) so the per-fight executor and the presentation
    /// view meet through it.
    /// </summary>
    public sealed class AbilityFiredSink : IAbilityFiredSink
    {
        public event Action<AbilityFiredCue> Fired;

        public void Notify(AbilityFiredCue cue) => Fired?.Invoke(cue);
    }
}
