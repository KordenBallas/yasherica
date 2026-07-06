using System.Collections.Generic;
using Core.Persistence;
using Narrative.Director.Core;
using World.Sites.Core;

namespace LevelGeneration
{
    /// <summary>
    /// The level-generation side of <see cref="IWorldStatePersistence"/>: composes the streaming
    /// coordinator's window record with the allocator cursors (quest spacing, site blocks) into one
    /// world section, and restores those cursors on continue. The coordinator itself is built by
    /// the Area entrypoint (not the container), so it attaches here after creation; capturing
    /// before a world exists yields null and the save simply carries an empty world section.
    /// </summary>
    public sealed class WorldStatePersistenceBridge : IWorldStatePersistence
    {
        private readonly WorldContentAllocator _contentAllocator;
        private readonly SiteAwareSlotAllocator _siteAllocator;

        private RunStreamingCoordinator _coordinator;

        public WorldStatePersistenceBridge(WorldContentAllocator contentAllocator,
            SiteAwareSlotAllocator siteAllocator)
        {
            _contentAllocator = contentAllocator;
            _siteAllocator = siteAllocator;
        }

        /// <summary>Called by the Area entrypoint once the run's coordinator exists.</summary>
        public void Attach(RunStreamingCoordinator coordinator) => _coordinator = coordinator;

        public WorldStateSnapshot Capture()
        {
            if (_coordinator == null)
            {
                return null;
            }

            var world = _coordinator.CaptureWorld();
            world.PlatformsSinceQuest = _contentAllocator.PlatformsSinceQuest;
            world.SiteAllocator = CaptureSiteAllocator();
            return world;
        }

        /// <summary>Restores the allocator cursors from a loaded save — the coordinator's own
        /// cursors are restored by <see cref="RunStreamingCoordinator.BeginRestored"/>.</summary>
        public void RestoreCursors(WorldStateSnapshot world)
        {
            if (world == null)
            {
                return;
            }

            _contentAllocator.RestoreCursor(world.PlatformsSinceQuest);

            var site = world.SiteAllocator ?? new SiteAllocatorSnapshot();
            var pending = new List<SiteSlot>(site.PendingSlots.Count);
            foreach (var slot in site.PendingSlots)
            {
                pending.Add(new SiteSlot(
                    new ContentBeat((ContentBaseKind)slot.BeatKind, slot.Flavor),
                    new SiteStamp(slot.SiteId, slot.SiteInstanceId, slot.SiteIndex,
                        slot.SiteFootprint, slot.SiteDressingThemeId)));
            }

            _siteAllocator.RestoreState(pending, site.PlatformsSinceSite, site.NextSiteInstanceId);
        }

        private SiteAllocatorSnapshot CaptureSiteAllocator()
        {
            var snapshot = new SiteAllocatorSnapshot
            {
                PlatformsSinceSite = _siteAllocator.PlatformsSinceSite,
                NextSiteInstanceId = _siteAllocator.NextInstanceId
            };

            foreach (var slot in _siteAllocator.PendingSlots)
            {
                snapshot.PendingSlots.Add(new PendingSiteSlotDto
                {
                    BeatKind = (int)slot.Beat.Kind,
                    Flavor = slot.Beat.Flavor,
                    SiteId = slot.Stamp.SiteId,
                    SiteInstanceId = slot.Stamp.InstanceId,
                    SiteIndex = slot.Stamp.Index,
                    SiteFootprint = slot.Stamp.Footprint,
                    SiteDressingThemeId = slot.Stamp.DressingThemeId
                });
            }

            return snapshot;
        }
    }
}
