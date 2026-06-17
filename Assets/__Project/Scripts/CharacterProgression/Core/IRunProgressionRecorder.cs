namespace CharacterProgression.Core
{
    /// <summary>
    /// Write surface for the current run's progression state. Used by the
    /// dialogue/platform layer to record what the player did. Read access is
    /// the separate <see cref="IRunProgressionRecord"/> (ISP).
    /// All methods are idempotent and ignore null/empty ids.
    /// </summary>
    public interface IRunProgressionRecorder
    {
        /// <summary>Marks a quest active. No-op if it is already completed or failed.</summary>
        void StartQuest(string questId);

        /// <summary>Marks a quest completed (overrides any prior status).</summary>
        void CompleteQuest(string questId);

        /// <summary>Marks a quest failed (overrides any prior status).</summary>
        void FailQuest(string questId);

        /// <summary>Records that an NPC was encountered this run.</summary>
        void RecordNpcEncounter(string npcId);

        /// <summary>Records a key choice as a key/value pair (latest write wins).</summary>
        void RecordChoice(string key, string value);
    }
}
