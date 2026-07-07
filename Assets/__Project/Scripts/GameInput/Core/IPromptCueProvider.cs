using System;

namespace GameInput.Core
{
    /// <summary>
    /// What every prompt call site reads (Input Foundation R6): the cue text for a named action on the
    /// CURRENTLY active source. Subscribers re-render on <see cref="CuesChanged"/>, which fires the
    /// moment the player switches device — that is what flips "[F] Talk" to "[Y] Talk" live.
    /// </summary>
    public interface IPromptCueProvider
    {
        /// <summary>Cue text for the action on the active source ("F", "RT", "Tap"), or an empty string
        /// when the pair is a deferred gap on that source.</summary>
        string GetCue(GameAction action);

        /// <summary>Cue for combat ability slot <paramref name="slotIndex"/> (0-based, 0..5).</summary>
        string GetAbilitySlotCue(int slotIndex);

        event Action CuesChanged;
    }
}
