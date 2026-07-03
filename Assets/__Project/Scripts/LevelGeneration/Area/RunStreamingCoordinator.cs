using System;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Runtime;
using Core.Events;
using Core.Logging;
using Narrative.Actors.Data;
using Narrative.Casting.Core;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Interaction;
using Narrative.Interaction.Core;
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
    /// runs story encounters through the data-driven engine.
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
        private readonly IGameLogger _logger;

        private int _windowIndex;
        private int _nextNodeId;
        private HashSet<int> _frontier = new HashSet<int>();
        private bool _subscribed;

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
            IGameLogger logger = null)
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
            _logger = logger;
        }

        /// <summary>Generates the first window and returns its entry platform; begins listening for advances.</summary>
        public IPlatform Begin()
        {
            _areaGenerator.Initialize();
            GenerateNextWindow();

            if (!_subscribed)
            {
                // Advance on EXIT of a frontier platform, not entry: a proximity encounter (and its
                // fact writes — quest-offered flags, the marsh passport, etc.) runs on the player's F
                // press, which has happened by the time they leave the platform. Planning the next window
                // on exit therefore sees the player's resolved choices, restoring the fact ordering the
                // old auto-on-land encounters gave the streaming planner. (Layout is a linear chain
                // appended to a persistent cursor, so the later trigger does not strand the player.)
                PlatformEvents.OnPlatformExited += OnPlatformExited;
                _subscribed = true;
            }

            return _areaGenerator.EntryPlatform;
        }

        private void OnPlatformExited(IPlatform platform)
        {
            // Leaving any platform of the latest window locks it and opens the next.
            if (platform != null && _frontier.Contains(platform.Id))
            {
                GenerateNextWindow();
            }
        }

        private void GenerateNextWindow()
        {
            var plan = _planner.PlanWindow(_windowIndex, _facts);
            var nodes = MapWindow(plan);
            _areaGenerator.AppendPlatforms(nodes);
            _frontier = new HashSet<int>(nodes.Select(n => n.Id));
            _logger?.Info(LogCategory.LevelGeneration,$"[RunStreamingCoordinator] Generated window {_windowIndex} with {nodes.Count} platforms.");
            _windowIndex++;
        }

        private List<GraphNode> MapWindow(WindowPlan plan)
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
                            _modularFactory, _interactionService)
                    };
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
            }

            return nodes;
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                PlatformEvents.OnPlatformExited -= OnPlatformExited;
                _subscribed = false;
            }
        }
    }
}
