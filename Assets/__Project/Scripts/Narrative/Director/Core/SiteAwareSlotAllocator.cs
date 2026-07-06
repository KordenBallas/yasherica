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

        /// <summary>The queued (not yet emitted) site-block slots, in emit order (P2-2 run save).</summary>
        public IReadOnlyList<SiteSlot> PendingSlots => System.Array.AsReadOnly(_pending.ToArray());

        /// <summary>The site-spacing cursor (P2-2 run save).</summary>
        public int PlatformsSinceSite => _platformsSinceSite;

        /// <summary>The next run-unique site instance id (P2-2 run save).</summary>
        public int NextInstanceId => _nextInstanceId;

        /// <summary>Restores the run-scoped site cursors from a run save: the pending block queue,
        /// the spacing counter, and the instance-id allocator.</summary>
        public void RestoreState(IReadOnlyList<SiteSlot> pendingSlots, int platformsSinceSite, int nextInstanceId)
        {
            _pending.Clear();
            if (pendingSlots != null)
            {
                foreach (var slot in pendingSlots)
                {
                    _pending.Enqueue(slot);
                }
            }

            _platformsSinceSite = platformsSinceSite;
            _nextInstanceId = nextInstanceId;
        }

        public SlotAllocation AllocateSlot(bool questAvailable, int currentTier)
        {
            if (_pending.Count > 0)
            {
                return ToAllocation(_pending.Dequeue(), currentTier);
            }

            _platformsSinceSite++;

            var inner = _inner.AllocateSlot(questAvailable, currentTier);
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
                    var anchor = Reserve(site);
                    if (site.HasBossLedAnchor && anchor.Beat.Kind == ContentBaseKind.Combat)
                    {
                        // A boss-led site's Combat anchor becomes boss + crew (bandit-camp brief):
                        // only the immediately-emitted anchor converts — queued fill slots stay plain,
                        // and multi-anchor boss sites are unsupported (documented limitation).
                        return AllocateCampAnchor(anchor, site, currentTier);
                    }

                    return ToAllocation(anchor, currentTier);
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

        private SlotAllocation ToAllocation(SiteSlot slot, int currentTier)
        {
            switch (slot.Beat.Kind)
            {
                case ContentBaseKind.Loot:
                    return new SlotAllocation(WorldSlotKind.Loot, 0, slot.Beat.Flavor, slot.Stamp);

                case ContentBaseKind.Combat:
                    return AllocateSiteCombat(slot, currentTier);

                case ContentBaseKind.Npc:
                    return new SlotAllocation(WorldSlotKind.Npc, 0, slot.Beat.Flavor, slot.Stamp);

                default:
                    return new SlotAllocation(WorldSlotKind.Empty, 0, slot.Beat.Flavor, slot.Stamp);
            }
        }

        private SlotAllocation AllocateSiteCombat(SiteSlot slot, int currentTier)
        {
            var pool = ResolveCombatPool(slot.Beat.Flavor, currentTier);
            if (pool.Count == 0)
            {
                return new SlotAllocation(WorldSlotKind.Empty, 0, null, slot.Stamp);
            }

            return new SlotAllocation(WorldSlotKind.Combat, pool[_random.NextInt(pool.Count)],
                slot.Beat.Flavor, slot.Stamp);
        }

        /// <summary>
        /// Converts a boss-led site's Combat anchor into a Camp allocation: rolls the crew size, then
        /// each crew enemy from the anchor flavor's pool — fixed draw order on the shared stream, so
        /// same seed → same camp. An empty pool lands the boss with no crew (his fight still exists via
        /// his own cast enemy); the boss himself is cast by the planner from <see cref="SlotAllocation.Flavor"/>.
        /// </summary>
        private SlotAllocation AllocateCampAnchor(SiteSlot anchor, SiteDefinitionData site, int currentTier)
        {
            int crewCount = site.BossCrewMin;
            if (site.BossCrewMax > site.BossCrewMin)
            {
                crewCount += _random.NextInt(site.BossCrewMax - site.BossCrewMin + 1);
            }

            var pool = ResolveCombatPool(anchor.Beat.Flavor, currentTier);
            int[] crew;
            if (pool.Count == 0 || crewCount == 0)
            {
                crew = System.Array.Empty<int>();
            }
            else
            {
                crew = new int[crewCount];
                for (int i = 0; i < crewCount; i++)
                {
                    crew[i] = pool[_random.NextInt(pool.Count)];
                }
            }

            return new SlotAllocation(WorldSlotKind.Camp, 0, site.BossStoryFlavor, anchor.Stamp, crew);
        }

        /// <summary>
        /// The flavor→unfiltered→empty pool fallback chain shared by site combat and camp crew, gated by
        /// the run-escalation tier (D19): only in-band creatures are drawn, and the unflavored fallback
        /// stays tier-scoped too, so a site fight toughens with the climb like the ambient draw.
        /// </summary>
        private IReadOnlyList<int> ResolveCombatPool(string flavor, int currentTier)
        {
            var theme = _themeProvider.CurrentTheme;
            var pool = _monsterPools.GetPool(theme, flavor, currentTier);
            if (pool.Count == 0 && !string.IsNullOrEmpty(flavor))
            {
                // No in-band enemy carries the flavor tag: fall back to the unfiltered (but still
                // tier-scoped) biome pool so the beat still lands, warned once per flavor.
                pool = _monsterPools.GetPool(theme, currentTier);
                if (pool.Count > 0 && _warnedFlavors.Add(flavor))
                {
                    _logger?.Warning(LogCategory.Narrative,
                        $"[SiteAwareSlotAllocator] No in-band '{theme}' enemy carries the flavor tag " +
                        $"'{flavor}' at tier {currentTier} - falling back to the unfiltered pool.");
                }
            }

            if (pool.Count == 0 && !_warnedEmptyPool)
            {
                _warnedEmptyPool = true;
                _logger?.Warning(LogCategory.Narrative,
                    $"[SiteAwareSlotAllocator] No monster pool authored for biome " +
                    $"'{theme}' at tier {currentTier} - site combat slots downgrade to empty.");
            }

            return pool;
        }
    }
}
