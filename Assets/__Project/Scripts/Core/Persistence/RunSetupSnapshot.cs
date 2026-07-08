using System;
using System.Collections.Generic;

namespace Core.Persistence
{
    /// <summary>
    /// The Hub → Area launch hand-off (O1): the start-of-run choices, written by the Hub at
    /// launch and consumed once by the next fresh Area boot. Deliberately NOT part of run.json —
    /// a run save's existence means "there is a run to continue" (<see cref="RunRestoreContext"/>),
    /// which a staged launch is not yet. Empty fields are valid defaults: no part = bare launch,
    /// no biome = seeded window-0 pick, no pact = Heat 0. New fields must be additive with such
    /// defaults — the version must NOT bump for them (a mismatch is the corrupt case).
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

        /// <summary>The sealed Heat pact (Track Y); empty = Heat 0, the bare run.</summary>
        public List<HeatPactEntryDto> Heat = new List<HeatPactEntryDto>();

        int IVersionedSnapshot.Version => Version;
    }
}
