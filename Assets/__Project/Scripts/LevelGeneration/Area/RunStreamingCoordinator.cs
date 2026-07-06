using System;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Runtime;
using Core.Events;
using Core.Logging;
using Core.Persistence;
using LevelGeneration.Journey;
using LevelGeneration.Surface;
using Narrative.Actors.Core;
using Narrative.Actors.Data;
using Narrative.Casting.Core;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Interaction;
using Narrative.Interaction.Core;
using Narrative.Runtime.Snapshots;
using Narrative.Stories.Core;
using Platform;

namespace LevelGeneration
{
    /// <summary>
    /// Drives streaming, story-first level generation. Generates the first window at <see cref="Begin"/>,
    /// then — on each <see cref="PlatformEvents.OnPlatformEntered"/> into the current frontier window —
    /// locks that window and plans + generates the next one against the **live** fact store (R7), so the
    /// player's choices reshape what comes next but never what they're standing on. Each planned story
    /// platform is realised as an <see cref="NpcContent"/> carrying the planner's minted actor and
    /// committed story; an ambient-combat platform as an <see cref="EnemyContent"/> with the planner's
    /// biome-pool pick; a loot platform via the area generator's biome table roll; the entry adapter
    /// runs story encounters through the data-driven engine. Before each window is planned, the biome
    /// stretch director applies the journey's biome for that window (theme provider + tier fact +
    /// appearance observers), so the planner's live theme reads see the active stretch.
    ///
    /// Save/continue (P2-2, D5): every realised window is RECORDED at plan time, because windows are
    /// planned against the live facts and realised with seeded draws — re-planning them later against
    /// end-state facts would not reproduce them. <see cref="BeginRestored"/> rebuilds the recorded
    /// windows by id with ZERO draws from the shared stream (geometry regrows from per-node seeds;
    /// castings re-resolve through <see cref="CastingSnapshotMapper"/>), restores the streaming
    /// cursors, and future windows plan live exactly as an un-interrupted run would (FR12).
    /// </summary>
    public sealed class RunStreamingCoordinator : IDisposable
    {
        private readonly IRunWindowPlanner _planner;
        private readonly INpcArchetypeCatalog _archetypeCatalog;
        private readonly IModularCharacterFactory _modularFactory;
        private readonly IFactStore _facts;
        private readonly ICastingFactory _castingFactory;
        private readonly IFragmentLibrary _fragmentLibrary;
        private readonly NpcIntentResolver _intentResolver;
        private readonly INpcInteractionService _interactionService;
        private readonly AreaGenerator _areaGenerator;
        private readonly BiomeStretchDirector _biomeDirector;
        private readonly ILiveActorRegistry _actorRegistry;
        private readonly IReadOnlyList<StoryTemplateData> _stories;
        private readonly IGameLogger _logger;
        private readonly IDemoRoleTintApplier _tintApplier;

        private int _windowIndex;
        private int _nextNodeId;
        private HashSet<int> _frontier = new HashSet<int>();
        private bool _subscribed;

        // The realised-window record (P2-2): what each generated window actually carried, so a
        // continue can rebuild it without re-planning (D5). A node is marked Consumed when its
        // platform is EXITED (the encounter/find has been passed) — the platform the hero stands
        // on stays live so its encounter re-begins on resume (FR7), unvisited platforms keep their
        // content, and nothing respawns behind the player (FR8).
        private readonly List<WindowSnapshot> _recordedWindows = new List<WindowSnapshot>();
        private readonly Dictionary<int, WindowNodeSnapshot> _recordsByNodeId =
            new Dictionary<int, WindowNodeSnapshot>();
        private int _currentPlatformNodeId = -1;

        public RunStreamingCoordinator(
            IRunWindowPlanner planner,
            INpcArchetypeCatalog archetypeCatalog,
            IModularCharacterFactory modularFactory,
            IFactStore facts,
            ICastingFactory castingFactory,
            IFragmentLibrary fragmentLibrary,
            NpcIntentResolver intentResolver,
            INpcInteractionService interactionService,
            AreaGenerator areaGenerator,
            BiomeStretchDirector biomeDirector,
            ILiveActorRegistry actorRegistry = null,
            IReadOnlyList<StoryTemplateData> stories = null,
            IGameLogger logger = null,
            IDemoRoleTintApplier tintApplier = null)
        {
            _planner = planner;
            _archetypeCatalog = archetypeCatalog;
            _modularFactory = modularFactory;
            _facts = facts;
            _castingFactory = castingFactory;
            _fragmentLibrary = fragmentLibrary;
            _intentResolver = intentResolver;
            _interactionService = interactionService;
            _areaGenerator = areaGenerator;
            _biomeDirector = biomeDirector;
            _actorRegistry = actorRegistry;
            _stories = stories;
            _logger = logger;
            _tintApplier = tintApplier;
        }

        /// <summary>Generates the first window and returns its entry platform; begins listening for advances.</summary>
        public IPlatform Begin()
        {
            _areaGenerator.Initialize();
            GenerateNextWindow();
            Subscribe();

            var entry = _areaGenerator.EntryPlatform;
            _currentPlatformNodeId = entry?.Id ?? -1;
            return entry;
        }

        /// <summary>
        /// Rebuilds a saved world image (P2-2 continue): replays the biome stretch per window (the
        /// journey is seeded and idempotent, so it needs no persisted state), maps every recorded
        /// node back to a graph node BY ID — consumed nodes content-free, story nodes re-cast through
        /// the id mappers — then restores the streaming cursors. No draw is taken from the shared
        /// seeded stream, so the restored <c>IRandomSource.State</c> stays exactly savepoint-faithful.
        /// Returns the platform the hero stood on (its clean start, FR7), or the entry platform.
        /// </summary>
        public IPlatform BeginRestored(WorldStateSnapshot world)
        {
            if (world == null || world.Windows.Count == 0)
            {
                return Begin();
            }

            _areaGenerator.Initialize();
            _recordedWindows.Clear();
            _recordsByNodeId.Clear();

            foreach (var window in world.Windows)
            {
                _biomeDirector?.ApplyForWindow(window.WindowIndex);
                var nodes = MapRestoredWindow(window);
                _areaGenerator.AppendPlatforms(nodes);
                _recordedWindows.Add(window);
                foreach (var node in window.Nodes)
                {
                    _recordsByNodeId[node.NodeId] = node;
                }
            }

            _windowIndex = world.WindowIndex;
            _nextNodeId = world.NextNodeId;
            _frontier = new HashSet<int>(world.FrontierNodeIds);
            Subscribe();

            _logger?.Info(LogCategory.LevelGeneration,
                $"[RunStreamingCoordinator] Restored {world.Windows.Count} window(s); resuming at window {_windowIndex}.");

            if (_areaGenerator.TryGetPlatform(world.CurrentPlatformNodeId, out var current))
            {
                _currentPlatformNodeId = world.CurrentPlatformNodeId;
                return current;
            }

            var entry = _areaGenerator.EntryPlatform;
            _currentPlatformNodeId = entry?.Id ?? -1;
            return entry;
        }

        /// <summary>
        /// The world section of the run save: every realised window (nodes the hero has EXITED are
        /// already marked consumed — nothing respawns behind the player), the streaming cursors,
        /// and the platform the hero currently stands on. Allocator cursors are appended by the
        /// persistence bridge, which owns those references.
        /// </summary>
        public WorldStateSnapshot CaptureWorld()
        {
            var world = new WorldStateSnapshot
            {
                WindowIndex = _windowIndex,
                NextNodeId = _nextNodeId,
                CurrentPlatformNodeId = _currentPlatformNodeId
            };
            world.FrontierNodeIds.AddRange(_frontier);
            world.Windows.AddRange(_recordedWindows);
            return world;
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            // Advance on EXIT of a frontier platform, not entry: a proximity encounter (and its
            // fact writes — quest-offered flags, the marsh passport, etc.) runs on the player's F
            // press, which has happened by the time they leave the platform. Planning the next window
            // on exit therefore sees the player's resolved choices, restoring the fact ordering the
            // old auto-on-land encounters gave the streaming planner. (Layout is a linear chain
            // appended to a persistent cursor, so the later trigger does not strand the player.)
            PlatformEvents.OnPlatformExited += OnPlatformExited;
            // Entered tracks where the hero stands — the savepoint's "current platform" (FR4).
            PlatformEvents.OnPlatformEntered += OnPlatformEntered;
            _subscribed = true;
        }

        private void OnPlatformExited(IPlatform platform)
        {
            if (platform == null)
            {
                return;
            }

            // A left-behind platform's encounter/find has been passed: it must not respawn on a
            // continue (FR8). Sticky — a later backtrack never un-consumes it.
            if (_recordsByNodeId.TryGetValue(platform.Id, out var record))
            {
                record.Consumed = true;
            }

            // Leaving any platform of the latest window locks it and opens the next.
            if (_frontier.Contains(platform.Id))
            {
                GenerateNextWindow();
            }
        }

        private void OnPlatformEntered(IPlatform platform)
        {
            if (platform != null)
            {
                _currentPlatformNodeId = platform.Id;
            }
        }

        private void GenerateNextWindow()
        {
            // Apply the biome stretch BEFORE planning: the planner's allocators and the area
            // generator's loot rolls read the theme provider live.
            _biomeDirector?.ApplyForWindow(_windowIndex);

            var plan = _planner.PlanWindow(_windowIndex, _facts);
            var record = new WindowSnapshot { WindowIndex = _windowIndex };
            var nodes = MapWindow(plan, record);
            _areaGenerator.AppendPlatforms(nodes);
            _recordedWindows.Add(record);
            _frontier = new HashSet<int>(nodes.Select(n => n.Id));
            _logger?.Info(LogCategory.LevelGeneration,$"[RunStreamingCoordinator] Generated window {_windowIndex} with {nodes.Count} platforms.");
            _windowIndex++;
        }

        private List<GraphNode> MapWindow(WindowPlan plan, WindowSnapshot record)
        {
            var nodes = new List<GraphNode>();
            foreach (var planned in plan.Platforms)
            {
                var node = new GraphNode
                {
                    Id = _nextNodeId++,
                    Type = planned.IsCombat ? PlatformType.Combat : PlatformType.Simple,
                    IsKeyPlatform = planned.Kind == PlannedPlatformKind.Story,
                    // Site membership + beat flavor ride onto the node: the loot roll biases by the
                    // flavor tag, and the M5 dressing pass will read the stamp.
                    Site = planned.Site,
                    ContentFlavor = planned.Flavor
                };

                CastingSnapshot castingRecord = null;
                if (planned.Kind == PlannedPlatformKind.Story && planned.Actor != null)
                {
                    var archetype = _archetypeCatalog.Get(planned.Actor.ArchetypeId);

                    // Cast once, here, against the live facts: this is the "encounter placed" moment the
                    // intent is evaluated at (R3). The casting is reused by the encounter so the seeded
                    // fragment/name picks stay deterministic and the shown marker matches what plays.
                    var casting = _castingFactory.Cast(planned.Story, planned.Actor, _fragmentLibrary);
                    var intent = _intentResolver.Resolve(casting, planned.Story);

                    node.ContentTypes = new List<PlatformContentType> { PlatformContentType.Npc };
                    node.PrebuiltContent = new List<IPlatformContent>
                    {
                        new NpcContent(archetype, planned.Actor, planned.Story, casting, intent,
                            _modularFactory, _interactionService, _tintApplier)
                    };
                    castingRecord = CastingSnapshotMapper.Capture(casting);
                }
                else if (planned.Kind == PlannedPlatformKind.Camp && planned.Actor != null)
                {
                    // Boss-led camp (bandit-camp brief): the boss NPC gates the fight (no aggro on
                    // landing — CombatAutoStartRule) and his crew is pre-placed as EnemyContent so
                    // engaging him sweeps them all into the one combat.
                    var archetype = _archetypeCatalog.Get(planned.Actor.ArchetypeId);
                    var casting = _castingFactory.Cast(planned.Story, planned.Actor, _fragmentLibrary);
                    var intent = _intentResolver.Resolve(casting, planned.Story);

                    node.ContentTypes = new List<PlatformContentType>
                    {
                        PlatformContentType.Npc, PlatformContentType.Enemy
                    };
                    var campContents = new List<IPlatformContent>
                    {
                        new NpcContent(archetype, planned.Actor, planned.Story, casting, intent,
                            _modularFactory, _interactionService, _tintApplier, isCampBoss: true)
                    };
                    foreach (var crewId in planned.CrewEnemyIds)
                    {
                        campContents.Add(new EnemyContent { EnemyId = crewId });
                    }

                    node.PrebuiltContent = campContents;
                    castingRecord = CastingSnapshotMapper.Capture(casting);
                }
                else if (planned.Kind == PlannedPlatformKind.Combat)
                {
                    // Ambient monster (Combat·wild-beast): the planner picked the enemy from the biome
                    // pool; the content-driven state factory routes this to the combat states — a fight
                    // with no quest or dialogue attached.
                    node.ContentTypes = new List<PlatformContentType> { PlatformContentType.Enemy };
                    node.PrebuiltContent = new List<IPlatformContent>
                    {
                        new EnemyContent { EnemyId = planned.EnemyId }
                    };
                }
                else if (planned.Kind == PlannedPlatformKind.Loot)
                {
                    // Simple loot (Loot·scattered): the area generator rolls the biome platform table
                    // deterministically for this node and the spawn coordinator places the pickups.
                    node.ContentTypes = new List<PlatformContentType> { PlatformContentType.Loot };
                }
                else
                {
                    node.ContentTypes = new List<PlatformContentType> { PlatformContentType.None };
                }

                nodes.Add(node);
                var nodeRecord = RecordNode(node, planned, castingRecord);
                record.Nodes.Add(nodeRecord);
                _recordsByNodeId[node.Id] = nodeRecord;
            }

            return nodes;
        }

        private static WindowNodeSnapshot RecordNode(GraphNode node, PlannedPlatform planned,
            CastingSnapshot casting)
        {
            return new WindowNodeSnapshot
            {
                NodeId = node.Id,
                Kind = (int)planned.Kind,
                IsCombat = planned.IsCombat,
                EnemyId = planned.EnemyId,
                CrewEnemyIds = new List<int>(planned.CrewEnemyIds),
                ContentFlavor = node.ContentFlavor ?? string.Empty,
                SiteId = node.Site.SiteId,
                SiteInstanceId = node.Site.InstanceId,
                SiteIndex = node.Site.Index,
                SiteFootprint = node.Site.Footprint,
                SiteDressingThemeId = node.Site.DressingThemeId,
                ShapeKind = (int)PlatformContentKindResolver.Resolve(node),
                Casting = casting
            };
        }

        private List<GraphNode> MapRestoredWindow(WindowSnapshot window)
        {
            var nodes = new List<GraphNode>();
            foreach (var snapshot in window.Nodes)
            {
                var node = new GraphNode
                {
                    Id = snapshot.NodeId,
                    Type = snapshot.IsCombat ? PlatformType.Combat : PlatformType.Simple,
                    IsKeyPlatform = (PlannedPlatformKind)snapshot.Kind == PlannedPlatformKind.Story,
                    Site = new World.Sites.Core.SiteStamp(snapshot.SiteId, snapshot.SiteInstanceId,
                        snapshot.SiteIndex, snapshot.SiteFootprint, snapshot.SiteDressingThemeId),
                    ContentFlavor = snapshot.ContentFlavor,
                    // The surface must regrow exactly as first generated, even when the content that
                    // sized it does not respawn (consumed) or degraded (missing fragment).
                    ShapeKindOverride = (PlatformContentKind)snapshot.ShapeKind,
                    ContentTypes = new List<PlatformContentType> { PlatformContentType.None }
                };

                if (!snapshot.Consumed)
                {
                    RealizeRestoredContent(node, snapshot);
                }

                nodes.Add(node);
            }

            return nodes;
        }

        private void RealizeRestoredContent(GraphNode node, WindowNodeSnapshot snapshot)
        {
            switch ((PlannedPlatformKind)snapshot.Kind)
            {
                case PlannedPlatformKind.Story:
                    var casting = CastingSnapshotMapper.Restore(snapshot.Casting, _actorRegistry,
                        _fragmentLibrary, _logger);
                    var story = FindStory(snapshot.Casting?.StoryId);
                    var archetype = casting != null ? _archetypeCatalog.Get(casting.Actor.ArchetypeId) : null;
                    if (casting == null || story == null || archetype == null)
                    {
                        // FR14 tolerance: content removed between builds degrades this platform to
                        // an empty one (same surface) instead of crashing the resume.
                        _logger?.Warning(LogCategory.LevelGeneration,
                            $"[RunStreamingCoordinator] Node {snapshot.NodeId}: story platform could not " +
                            "be re-realised (missing story/actor/archetype); restored empty.");
                        return;
                    }

                    var intent = _intentResolver.Resolve(casting, story);
                    node.ContentTypes = new List<PlatformContentType> { PlatformContentType.Npc };
                    node.PrebuiltContent = new List<IPlatformContent>
                    {
                        new NpcContent(archetype, casting.Actor, story, casting, intent,
                            _modularFactory, _interactionService, _tintApplier)
                    };
                    break;

                case PlannedPlatformKind.Camp:
                    var bossCasting = CastingSnapshotMapper.Restore(snapshot.Casting, _actorRegistry,
                        _fragmentLibrary, _logger);
                    var bossStory = FindStory(snapshot.Casting?.StoryId);
                    var bossArchetype = bossCasting != null
                        ? _archetypeCatalog.Get(bossCasting.Actor.ArchetypeId)
                        : null;

                    var campContents = new List<IPlatformContent>();
                    if (bossCasting != null && bossStory != null && bossArchetype != null)
                    {
                        var bossIntent = _intentResolver.Resolve(bossCasting, bossStory);
                        campContents.Add(new NpcContent(bossArchetype, bossCasting.Actor, bossStory,
                            bossCasting, bossIntent, _modularFactory, _interactionService, _tintApplier,
                            isCampBoss: true));
                    }
                    else
                    {
                        // FR14 tolerance: a missing boss story degrades the camp to a plain crew
                        // fight (the crew still restores below) instead of crashing the resume.
                        _logger?.Warning(LogCategory.LevelGeneration,
                            $"[RunStreamingCoordinator] Node {snapshot.NodeId}: camp boss could not be " +
                            "re-realised (missing story/actor/archetype); restored as a plain fight.");
                    }

                    if (snapshot.CrewEnemyIds != null)
                    {
                        foreach (var crewId in snapshot.CrewEnemyIds)
                        {
                            campContents.Add(new EnemyContent { EnemyId = crewId });
                        }
                    }

                    if (campContents.Count == 0)
                    {
                        return;
                    }

                    var campTypes = new List<PlatformContentType>();
                    if (campContents[0] is NpcContent)
                    {
                        campTypes.Add(PlatformContentType.Npc);
                    }
                    if (campContents.Exists(c => c is EnemyContent))
                    {
                        campTypes.Add(PlatformContentType.Enemy);
                    }

                    node.ContentTypes = campTypes;
                    node.PrebuiltContent = campContents;
                    break;

                case PlannedPlatformKind.Combat:
                    node.ContentTypes = new List<PlatformContentType> { PlatformContentType.Enemy };
                    node.PrebuiltContent = new List<IPlatformContent>
                    {
                        new EnemyContent { EnemyId = snapshot.EnemyId }
                    };
                    break;

                case PlannedPlatformKind.Loot:
                    // The generator re-rolls the biome table with the same per-node context — the
                    // loot roll service is stateless per context key, so the find reproduces.
                    node.ContentTypes = new List<PlatformContentType> { PlatformContentType.Loot };
                    break;
            }
        }

        private StoryTemplateData FindStory(string storyId)
        {
            if (string.IsNullOrEmpty(storyId) || _stories == null)
            {
                return null;
            }

            foreach (var story in _stories)
            {
                if (string.Equals(story.StoryId, storyId, StringComparison.Ordinal))
                {
                    return story;
                }
            }

            return null;
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                PlatformEvents.OnPlatformExited -= OnPlatformExited;
                PlatformEvents.OnPlatformEntered -= OnPlatformEntered;
                _subscribed = false;
            }
        }
    }
}
