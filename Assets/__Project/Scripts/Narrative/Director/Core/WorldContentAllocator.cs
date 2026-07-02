using Core.Logging;
using Loot.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Allocates each upcoming platform slot one of the four world content kinds (the
    /// world-content-density brief): a rare, spaced NPC quest, or an ambient weighted draw among
    /// empty/traversal, simple loot, and ambient combat.
    ///
    /// Quest gate, per slot: the spacing counter must exceed
    /// <see cref="WorldContentDensitySettings.MinPlatformsBetweenQuests"/> (a hard invariant that
    /// carries across window boundaries — this class is run-scoped), a 1-in-
    /// <see cref="WorldContentDensitySettings.AveragePlatformsPerQuest"/> seeded roll must hit, and the
    /// caller must report an eligible story (<c>questAvailable</c>). When any of these fails the slot
    /// degrades into the ambient draw and the counter keeps running, so a quest lands at the next
    /// opportunity rather than being forfeited (PO decision).
    ///
    /// Ambient combat draws its enemy from the current biome's authored pool at flat difficulty; an
    /// unauthored pool downgrades the slot to Empty (warned once). All draws come from the single
    /// shared <see cref="IRandomSource"/> stream in a fixed order, so allocation is deterministic and
    /// save-replayable alongside story selection (B2).
    /// </summary>
    public sealed class WorldContentAllocator
    {
        private readonly WorldContentDensitySettings _settings;
        private readonly IBiomeMonsterPoolCatalog _monsterPools;
        private readonly ICurrentThemeProvider _themeProvider;
        private readonly IRandomSource _random;
        private readonly IGameLogger _logger;

        private int _platformsSinceQuest;
        private bool _warnedEmptyPool;

        public WorldContentAllocator(
            WorldContentDensitySettings settings,
            IBiomeMonsterPoolCatalog monsterPools,
            ICurrentThemeProvider themeProvider,
            IRandomSource random,
            IGameLogger logger = null)
        {
            _settings = settings;
            _monsterPools = monsterPools;
            _themeProvider = themeProvider;
            _random = random;
            _logger = logger;

            // Start "far enough" from a phantom quest-before-the-run so window 0 may legally carry one.
            _platformsSinceQuest = _settings.MinPlatformsBetweenQuests;
        }

        public SlotAllocation AllocateSlot(bool questAvailable)
        {
            _platformsSinceQuest++;

            if (questAvailable
                && _platformsSinceQuest > _settings.MinPlatformsBetweenQuests
                && _random.NextInt(_settings.AveragePlatformsPerQuest) == 0)
            {
                _platformsSinceQuest = 0;
                return new SlotAllocation(WorldSlotKind.Quest);
            }

            return AllocateAmbient();
        }

        private SlotAllocation AllocateAmbient()
        {
            int total = _settings.EmptyWeight + _settings.LootWeight + _settings.CombatWeight;
            if (total <= 0)
            {
                // All-zero weights: an authored "nothing but traversal" world, not an error.
                return new SlotAllocation(WorldSlotKind.Empty);
            }

            int roll = _random.NextInt(total);
            if (roll < _settings.EmptyWeight)
            {
                return new SlotAllocation(WorldSlotKind.Empty);
            }

            if (roll < _settings.EmptyWeight + _settings.LootWeight)
            {
                return new SlotAllocation(WorldSlotKind.Loot);
            }

            return AllocateCombat();
        }

        private SlotAllocation AllocateCombat()
        {
            var pool = _monsterPools.GetPool(_themeProvider.CurrentTheme);
            if (pool.Count == 0)
            {
                if (!_warnedEmptyPool)
                {
                    _warnedEmptyPool = true;
                    _logger?.Warning(LogCategory.Narrative,
                        $"[WorldContentAllocator] No ambient monster pool authored for biome " +
                        $"'{_themeProvider.CurrentTheme}' - combat slots downgrade to empty.");
                }

                return new SlotAllocation(WorldSlotKind.Empty);
            }

            return new SlotAllocation(WorldSlotKind.Combat, pool[_random.NextInt(pool.Count)]);
        }
    }
}
