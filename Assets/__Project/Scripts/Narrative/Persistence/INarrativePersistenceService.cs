using System;

namespace Narrative.Persistence
{
    /// <summary>
    /// Service interface for saving and loading narrative state.
    /// Orchestrates persistence of quests, NPCs, story progress, and Ink state.
    /// </summary>
    public interface INarrativePersistenceService
    {
        /// <summary>
        /// Event fired when save data is created.
        /// </summary>
        event Action<NarrativeSaveData> OnSaveDataCreated;

        /// <summary>
        /// Event fired when save data is loaded.
        /// </summary>
        event Action<NarrativeSaveData> OnSaveDataLoaded;

        /// <summary>
        /// Event fired when state restoration is complete.
        /// </summary>
        event Action OnStateRestored;

        /// <summary>
        /// Creates a snapshot of the current narrative state.
        /// </summary>
        /// <returns>Serializable save data</returns>
        NarrativeSaveData CreateSaveData();

        /// <summary>
        /// Restores narrative state from save data.
        /// </summary>
        /// <param name="saveData">The save data to restore from</param>
        /// <returns>True if restoration was successful</returns>
        bool RestoreFromSaveData(NarrativeSaveData saveData);

        /// <summary>
        /// Clears all narrative state (for new game).
        /// </summary>
        void ClearState();

        /// <summary>
        /// Saves Ink runtime state for mid-dialogue persistence.
        /// </summary>
        /// <returns>Ink state JSON string</returns>
        string SaveInkState();

        /// <summary>
        /// Restores Ink runtime state.
        /// </summary>
        /// <param name="inkStateJson">Ink state JSON string</param>
        /// <returns>True if restoration was successful</returns>
        bool RestoreInkState(string inkStateJson);

        /// <summary>
        /// Checks if there is a valid save to restore.
        /// </summary>
        /// <param name="saveData">Save data to validate</param>
        /// <returns>True if the save data is valid</returns>
        bool ValidateSaveData(NarrativeSaveData saveData);

        /// <summary>
        /// Gets the current save data version.
        /// </summary>
        int CurrentVersion { get; }
    }
}
