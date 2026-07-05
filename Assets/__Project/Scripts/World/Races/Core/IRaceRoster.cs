using System.Collections.Generic;

namespace World.Races.Core
{
    /// <summary>
    /// Read-only lookup over the authored race roster. The roster is pure data — adding a race is
    /// authoring one RaceDefinition asset, never code (race-roster-and-passport.md FR3).
    /// </summary>
    public interface IRaceRoster
    {
        /// <summary>All races in stable authored order (drives deterministic fact writes, FR8).</summary>
        IReadOnlyList<RaceData> All { get; }

        bool Contains(string raceId);

        bool TryGet(string raceId, out RaceData race);
    }
}
