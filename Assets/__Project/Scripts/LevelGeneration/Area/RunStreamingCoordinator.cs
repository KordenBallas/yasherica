using System;
using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Runtime;
using Core.Events;
using Core.Logging;
using Narrative.Actors.Data;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Platform;

namespace LevelGeneration
{
    /// <summary>
    /// Drives streaming, story-first level generation. Generates the first window at <see cref="Begin"/>,
    /// then — on each <see cref="PlatformEvents.OnPlatformEntered"/> into the current frontier window —
    /// locks that window and plans + generates the next one against the **live** fact store (R7), so the
    /// player's choices reshape what comes next but never what they're standing on. Each planned story
    /// platform is realised as an <see cref="NpcContent"/> carrying the planner's minted actor and
    /// committed story; the entry adapter runs it through the data-driven engine.
    /// </summary>
    public sealed class RunStreamingCoordinator : IDisposable
    {
        private readonly IRunWindowPlanner _planner;
        private readonly INpcArchetypeCatalog _archetypeCatalog;
        private readonly IModularCharacterFactory _modularFactory;
        private readonly IFactStore _facts;
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
            AreaGenerator areaGenerator,
            IGameLogger logger = null)
        {
            _planner = planner;
            _archetypeCatalog = archetypeCatalog;
            _modularFactory = modularFactory;
            _facts = facts;
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
                PlatformEvents.OnPlatformEntered += OnPlatformEntered;
                _subscribed = true;
            }

            return _areaGenerator.EntryPlatform;
        }

        private void OnPlatformEntered(IPlatform platform)
        {
            // Entering any platform of the latest window locks it and opens the next.
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
            _logger?.Info($"[RunStreamingCoordinator] Generated window {_windowIndex} with {nodes.Count} platforms.");
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
                    IsKeyPlatform = planned.Kind == PlannedPlatformKind.Story
                };

                if (planned.Kind == PlannedPlatformKind.Story && planned.Actor != null)
                {
                    var archetype = _archetypeCatalog.Get(planned.Actor.ArchetypeId);
                    node.ContentTypes = new List<PlatformContentType> { PlatformContentType.Npc };
                    node.PrebuiltContent = new List<IPlatformContent>
                    {
                        new NpcContent(archetype, planned.Actor, planned.Story, _modularFactory)
                    };
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
                PlatformEvents.OnPlatformEntered -= OnPlatformEntered;
                _subscribed = false;
            }
        }
    }
}
