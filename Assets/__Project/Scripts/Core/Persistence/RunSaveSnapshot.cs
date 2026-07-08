using System;
using System.Collections.Generic;
using Narrative.Runtime.Snapshots;

namespace Core.Persistence
{
    /// <summary>
    /// The whole-run continue image (FR4/FR5): one aggregate written to <c>run.json</c> at each
    /// savepoint and consumed by a death. Composes the per-system snapshot sections; sections for
    /// the world window plan, hero body, and player stuff join as their capture paths land.
    /// Version is stamped by the store on save — a file without (or with a foreign) version is the
    /// FR14 corrupt case.
    /// </summary>
    [Serializable]
    public class RunSaveSnapshot : IVersionedSnapshot
    {
        // Additive fields with valid defaults must NOT bump this — a version mismatch is the
        // corrupt case (the file is consumed), which would cost players their run on upgrade.
        public const int CurrentVersion = 2;

        public int Version;
        public long SavedAtUtcTicks;

        /// <summary>The ROOT run seed (the one every subsystem derives its context seed from) —
        /// distinct from <c>Narrative.Seed</c>, which is already the derived narrative-slice seed.</summary>
        public int RunSeed;

        /// <summary>The Hub-chosen entry homeland as a <c>LevelTheme</c> name (O1); empty = the
        /// seeded window-0 pick. Must ride the save because the biome journey replays from the seed
        /// and persists no cursor — without it a resume would re-roll the entry biome.</summary>
        public string StartingBiome = string.Empty;

        /// <summary>The run's sealed Heat pact (Track Y); empty = Heat 0. Rides the save because the
        /// pact is locked for the run (FR2) and every rule seam re-derives from it on resume.</summary>
        public List<HeatPactEntryDto> Heat = new List<HeatPactEntryDto>();

        public RunNarrativeSnapshot Narrative = new RunNarrativeSnapshot();
        public WorldStateSnapshot World = new WorldStateSnapshot();
        public HeroBodySnapshot Body = new HeroBodySnapshot();
        public PlayerStuffSnapshot Stuff = new PlayerStuffSnapshot();

        int IVersionedSnapshot.Version => Version;
    }
}
