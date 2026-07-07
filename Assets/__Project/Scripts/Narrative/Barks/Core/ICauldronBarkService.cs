using System;

namespace Narrative.Barks.Core
{
    /// <summary>
    /// The cauldron's live bark channel (P1-10): game systems report an authored moment
    /// (<see cref="Bark"/>), the service resolves the line against the run's path lean and raises
    /// <see cref="OnBark"/> for the view. Short chatter-register lines only — reveal beats ride the
    /// spine lane, not this.
    /// </summary>
    public interface ICauldronBarkService
    {
        /// <summary>Raised with the selected line whenever a slot fires and its pool is non-empty.</summary>
        event Action<string> OnBark;

        /// <summary>Fires a bark slot; quiet no-op when nothing is authored for it.</summary>
        void Bark(CauldronBarkSlot slot);

        /// <summary>Whether an offer belonging id reads as dark (drives the dark-offer slot).</summary>
        bool IsDarkBelonging(string belongingId);
    }
}
