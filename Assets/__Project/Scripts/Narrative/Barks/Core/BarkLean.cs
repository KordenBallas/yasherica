namespace Narrative.Barks.Core
{
    /// <summary>
    /// The voice's register, derived from the run's path facts — never a stored meter
    /// (cauldron-voice-barks.md FR3).
    /// </summary>
    public enum BarkLean
    {
        /// <summary>Friendship/restraint leads (or nothing does yet): the voice is clipped and sour.</summary>
        Restrained = 0,

        /// <summary>The monster leads: the voice is bolder, proprietary.</summary>
        Indulgent = 1
    }
}
