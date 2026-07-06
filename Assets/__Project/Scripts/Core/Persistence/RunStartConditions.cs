using System;
using LevelGeneration;

namespace Core.Persistence
{
    /// <summary>
    /// The one Area-scoped answer to "how does this run start" (O1): the Hub's two launch choices.
    /// On a continue the starting biome comes from the run save (the setup already happened and the
    /// journey replays from the seed, so the window-0 override must ride the save); on a fresh boot
    /// the one-shot run-setup file is consumed. Empty values mean defaults — bare hero, seeded
    /// window-0 pick — so direct editor Area play without a Hub visit keeps working.
    /// </summary>
    public sealed class RunStartConditions
    {
        public static readonly RunStartConditions Empty =
            new RunStartConditions(string.Empty, string.Empty);

        public RunStartConditions(string startingPartId, string startingBiomeName)
        {
            StartingPartId = startingPartId ?? string.Empty;
            StartingBiomeName = startingBiomeName ?? string.Empty;
        }

        /// <summary>The part to install on the hero at run start; empty = launch bare.</summary>
        public string StartingPartId { get; }

        /// <summary>The entry homeland as a <see cref="LevelTheme"/> name; empty = seeded pick.</summary>
        public string StartingBiomeName { get; }

        public bool TryGetStartingTheme(out LevelTheme theme)
        {
            return Enum.TryParse(StartingBiomeName, out theme)
                   && Enum.IsDefined(typeof(LevelTheme), theme);
        }

        /// <summary>
        /// Resolves the conditions for one Area boot. Restore wins (and any leftover setup file is
        /// stale — it belonged to a launch that never became this run — so it is deleted); a fresh
        /// boot consumes the setup file on read (a lost setup only ever belonged to a run that does
        /// not exist yet); nothing on disk yields <see cref="Empty"/>.
        /// </summary>
        public static RunStartConditions Resolve(RunRestoreContext restoreContext, IRunSetupStore setupStore)
        {
            if (restoreContext != null && restoreContext.IsRestoring)
            {
                setupStore?.Delete();
                return new RunStartConditions(string.Empty, restoreContext.Snapshot.StartingBiome);
            }

            if (setupStore != null && setupStore.TryLoad(out var setup))
            {
                setupStore.Delete();
                return new RunStartConditions(setup.StartingPartId, setup.StartingBiome);
            }

            return Empty;
        }
    }
}
