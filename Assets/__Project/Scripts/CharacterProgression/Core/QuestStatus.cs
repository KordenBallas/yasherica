namespace CharacterProgression.Core
{
    /// <summary>
    /// Lifecycle state of a quest within the current run.
    /// </summary>
    public enum QuestStatus
    {
        /// <summary>Quest is started and in progress.</summary>
        Active,

        /// <summary>Quest finished successfully.</summary>
        Completed,

        /// <summary>Quest ended in failure.</summary>
        Failed
    }
}
