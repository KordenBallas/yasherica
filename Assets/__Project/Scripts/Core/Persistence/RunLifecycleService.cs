using System;
using Combat.Core;
using Combat.Integration;
using Core.Logging;
using Core.SceneFlow;
using Zenject;

namespace Core.Persistence
{
    /// <summary>
    /// Death consumes the run (FR2): on a combat Defeat the Meta partition is flushed FIRST (the
    /// world must remember everything up to and including the fatal run), then the in-progress run
    /// save is deleted — the next launch cannot reload any point of the dead run, so save-scumming
    /// a death is impossible by construction. The death's front-end (O1): after the consume, the
    /// arrival marker is set and the player returns to the Hub — the junkyard reforms him; there is
    /// still no game-over screen. Loader/marker are optional so the pure lifecycle stays testable
    /// and older wiring degrades to the old stranded-in-scene behavior instead of crashing.
    /// </summary>
    public sealed class RunLifecycleService : IInitializable, IDisposable
    {
        private readonly ICombatOutcomeRelay _combatOutcomes;
        private readonly IRunSaveStore _runStore;
        private readonly IMetaMemoryFlush _metaFlush;
        private readonly ISceneLoader _sceneLoader;
        private readonly IHubArrivalStore _hubArrival;
        private readonly IGameLogger _logger;

        public RunLifecycleService(ICombatOutcomeRelay combatOutcomes, IRunSaveStore runStore,
            IMetaMemoryFlush metaFlush, ISceneLoader sceneLoader = null,
            IHubArrivalStore hubArrival = null, IGameLogger logger = null)
        {
            _combatOutcomes = combatOutcomes;
            _runStore = runStore;
            _metaFlush = metaFlush;
            _sceneLoader = sceneLoader;
            _hubArrival = hubArrival;
            _logger = logger;
        }

        public void Initialize()
        {
            _combatOutcomes.CombatEnded += OnCombatEnded;
        }

        public void Dispose()
        {
            _combatOutcomes.CombatEnded -= OnCombatEnded;
        }

        private void OnCombatEnded(CombatPhase phase)
        {
            if (phase != CombatPhase.Defeat)
            {
                return;
            }

            _metaFlush.Flush();
            _runStore.Delete();
            _hubArrival?.MarkDeathReturn();
            _logger?.Info(LogCategory.Persistence,
                "[RunLifecycleService] Death: world memory flushed, run save consumed (FR2); returning to the Hub.");
            _sceneLoader?.Load(SceneNames.Hub);
        }
    }
}
