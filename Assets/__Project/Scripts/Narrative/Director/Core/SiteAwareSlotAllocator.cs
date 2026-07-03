using System.Collections.Generic;
using Core.Logging;
using Loot.Core;
using World.Sites.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Site-aware <see cref="IWorldSlotAllocator"/> (the world-sites brief): wraps the untouched
    /// <see cref="WorldContentAllocator"/> so Wild slots keep the exact density behavior, and layers
    /// contiguous site-block reservation on top. An unauthored (empty) site catalog makes this a pure
    /// passthrough.
    ///
    /// A block's shape — footprint, beats, flavors, stamps — is resolved fully at trigger time from the
    /// shared director stream; its slots are queued and drained first by subsequent calls, so a block
    /// spans window boundaries naturally (the same run-scoped pattern as the quest-spacing counter).
    /// Two trigger channels, derived from each site's anchor beat:
    /// ambient-channel sites (Camp/Ruin/Lair) ride a rare, spaced roll mirroring the quest gate;
    /// quest-channel settlements (Village/City) are reserved by the planner through
    /// <see cref="TryReserveSettlement"/> when the density allocator lands a quest slot — a
    /// <c>site:&lt;id&gt;</c> story tag is a hard request, otherwise a weighted roll against
    /// <see cref="WorldContentDensitySettings.WildQuestWeight"/> may leave the quest a lone wanderer.
    /// Quest slots take precedence over the ambient site gate.
    /// </summary>
    public sealed class SiteAwareSlotAllocator : IWorldSlotAllocator
    {
        private const string SiteTagPrefix = "site:";

        private readonly WorldContentAllocator _inner;
        private readonly ISiteCatalog _catalog;
        private readonly SiteBlockBuilder _blockBuilder;
        private readonly WorldContentDensitySettings _settings;
        private readonly IBiomeMonsterPoolCatalog _monsterPools;
        private readonly ICurrentThemeProvider _themeProvider;
        private readonly IRandomSource _random;
        private readonly IGameLogger _logger;

        private readonly Queue<SiteSlot> _pending = new Queue<SiteSlot>();
        private readonly HashSet<string> _warnedFlavors = new HashSet<string>(System.StringComparer.Ordinal);
        private int _platformsSinceSite;
        private int _nextInstanceId = 1;
        private bool _warnedEmptyPool;

        public SiteAwareSlotAllocator(
            WorldContentAllocator inner,
            ISiteCatalog catalog,
            SiteBlockBuilder blockBuilder,
            WorldContentDensitySettings settings,
            IBiomeMonsterPoolCatalog monsterPools,
            ICurrentThemeProvider themeProvider,
            IRandomSource random,
            IGameLogger logger = null)
        {
            _inner = inner;
            _catalog = catalog;
            _blockBuilder = blockBuilder;
            _settings = settings;
            _monsterPools = monsterPools;
            _themeProvider = themeProvider;
            _random = random;
            _logger = logger;

            // Start "far enough" so the run's opening stretch may legally carry a site.
            _platformsSinceSite = _settings.MinPlatformsBetweenSites;
        }

        public SlotAllocation AllocateSlot(bool questAvailable)
        {
            if (_pending.Count > 0)
            {
                return ToAllocation(_pending.Dequeue());
            }

            _platformsSinceSite++;

            var inner = _inner.AllocateSlot(questAvailable);
            if (inner.Kind == WorldSlotKind.Quest)
            {
                // The rarer beat wins the slot; the planner decides via TryReserveSettlement whether
                // this quest also pulls a settlement into being.
                return inner;
            }

            if (ShouldTriggerAmbientSite())
            {
                var site = PickWeighted(_catalog.AmbientSites, extraWeight: 0);
                if (site != null)
                {
                    // The site claims the slot: the inner ambient draw is superseded by the anchor.
                    return ToAllocation(Reserve(site));
                }
            }

            return inner;
        }

        public SiteStamp TryReserveSettlement(IReadOnlyList<string> anchorStoryTags)
        {
            var requested = FindRequestedSite(anchorStoryTags);
            if (requested != null)
            {
                return Reserve(requested).Stamp;
            }

            // No roll on an unauthored channel, so an empty catalog stays a bit-exact passthrough.
            if (_catalog.QuestSites.Count == 0)
            {
                return SiteStamp.Wild;
            }

            var site = PickWeighted(_catalog.QuestSites, _settings.WildQuestWeight);
            return site == null ? SiteStamp.Wild : Reserve(site).Stamp;
        }

        /// <summary>Builds the block, queues everything after the anchor, and restarts site spacing.</summary>
        private SiteSlot Reserve(SiteDefinitionData site)
        {
            var block = _blockBuilder.Build(site, _random, _nextInstanceId++);
            for (int i = 1; i < block.Count; i++)
            {
                _pending.Enqueue(block[i]);
            }

            // Queued slots never pass the counter, so 0 here measures spacing from the block's end.
            _platformsSinceSite = 0;

            _logger?.Info(LogCategory.Narrative,
                $"[SiteAwareSlotAllocator] Reserved site '{site.SiteId}' " +
                $"(instance {block[0].Stamp.InstanceId}, footprint {block.Count}).");
            return block[0];
        }

        private bool ShouldTriggerAmbientSite()
        {
            return _settings.AveragePlatformsPerAmbientSite > 0
                && _catalog.AmbientSites.Count > 0
                && _platformsSinceSite > _settings.MinPlatformsBetweenSites
                && _random.NextInt(_settings.AveragePlatformsPerAmbientSite) == 0;
        }

        /// <summary>
        /// Seeded weighted pick over a channel's sites; <paramref name="extraWeight"/> is the "no site"
        /// share (the quest channel's Wild outcome). Null = no site.
        /// </summary>
        private SiteDefinitionData PickWeighted(IReadOnlyList<SiteDefinitionData> sites, int extraWeight)
        {
            int total = extraWeight;
            for (int i = 0; i < sites.Count; i++)
            {
                total += sites[i].TriggerWeight;
            }

            if (total <= 0)
            {
                return null;
            }

            int roll = _random.NextInt(total) - extraWeight;
            if (roll < 0)
            {
                return null;
            }

            for (int i = 0; i < sites.Count; i++)
            {
                roll -= sites[i].TriggerWeight;
                if (roll < 0)
                {
                    return sites[i];
                }
            }

            return null;
        }

        private SiteDefinitionData FindRequestedSite(IReadOnlyList<string> storyTags)
        {
            if (storyTags == null)
            {
                return null;
            }

            for (int i = 0; i < storyTags.Count; i++)
            {
                var tag = storyTags[i];
                if (tag != null && tag.StartsWith(SiteTagPrefix, System.StringComparison.Ordinal))
                {
                    var site = _catalog.Get(tag.Substring(SiteTagPrefix.Length));
                    if (site != null)
                    {
                        return site;
                    }

                    _logger?.Warning(LogCategory.Narrative,
                        $"[SiteAwareSlotAllocator] Story demands unknown site via tag '{tag}' - " +
                        "the quest stays Wild.");
                }
            }

            return null;
        }

        private SlotAllocation ToAllocation(SiteSlot slot)
        {
            switch (slot.Beat.Kind)
            {
                case ContentBaseKind.Loot:
                    return new SlotAllocation(WorldSlotKind.Loot, 0, slot.Beat.Flavor, slot.Stamp);

                case ContentBaseKind.Combat:
                    return AllocateSiteCombat(slot);

                case ContentBaseKind.Npc:
                    return new SlotAllocation(WorldSlotKind.Npc, 0, slot.Beat.Flavor, slot.Stamp);

                default:
                    return new SlotAllocation(WorldSlotKind.Empty, 0, slot.Beat.Flavor, slot.Stamp);
            }
        }

        private SlotAllocation AllocateSiteCombat(SiteSlot slot)
        {
            var theme = _themeProvider.CurrentTheme;
            var pool = _monsterPools.GetPool(theme, slot.Beat.Flavor);
            if (pool.Count == 0 && !string.IsNullOrEmpty(slot.Beat.Flavor))
            {
                // No enemy carries the flavor tag: fall back to the unfiltered biome pool so the
                // beat still lands (the fight matters more than its flavor), warned once per flavor.
                pool = _monsterPools.GetPool(theme);
                if (pool.Count > 0 && _warnedFlavors.Add(slot.Beat.Flavor))
                {
                    _logger?.Warning(LogCategory.Narrative,
                        $"[SiteAwareSlotAllocator] No '{theme}' enemy carries the flavor tag " +
                        $"'{slot.Beat.Flavor}' - falling back to the unfiltered pool.");
                }
            }

            if (pool.Count == 0)
            {
                if (!_warnedEmptyPool)
                {
                    _warnedEmptyPool = true;
                    _logger?.Warning(LogCategory.Narrative,
                        $"[SiteAwareSlotAllocator] No monster pool authored for biome " +
                        $"'{theme}' - site combat slots downgrade to empty.");
                }

                return new SlotAllocation(WorldSlotKind.Empty, 0, null, slot.Stamp);
            }

            return new SlotAllocation(WorldSlotKind.Combat, pool[_random.NextInt(pool.Count)],
                slot.Beat.Flavor, slot.Stamp);
        }
    }
}
