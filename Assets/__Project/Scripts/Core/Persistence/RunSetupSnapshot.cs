using System;

namespace Core.Persistence
{
    /// <summary>
    /// The Hub → Area launch hand-off (O1): the two start-of-run choices, written by the Hub at
    /// launch and consumed once by the next fresh Area boot. Deliberately NOT part of run.json —
    /// a run save's existence means "there is a run to continue" (<see cref="RunRestoreContext"/>),
    /// which a staged launch is not yet. Empty fields are valid defaults: no part = bare launch,
    /// no biome = seeded window-0 pick.
    /// </summary>
    [Serializable]
    public class RunSetupSnapshot : IVersionedSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version;

        /// <summary>The chosen starting part id; empty = launch bare (the kindless default).</summary>
        public string StartingPartId = string.Empty;

        /// <summary>The chosen entry homeland as a <c>LevelTheme</c> name; empty = seeded pick.</summary>
        public string StartingBiome = string.Empty;

        int IVersionedSnapshot.Version => Version;
    }
}
