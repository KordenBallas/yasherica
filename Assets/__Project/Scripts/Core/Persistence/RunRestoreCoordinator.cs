using Core.Logging;
using Zenject;

namespace Core.Persistence
{
    /// <summary>
    /// Drives the one-shot run restore at Area boot (D6). Ordered FIRST among initializables
    /// (before the meta-memory bootstrap and long before the Area entrypoint generates the world),
    /// so that when the streaming coordinator rebuilds the saved windows every service it reads —
    /// facts, RNG state, actors, quests, inventory, allocator cursors — is already savepoint-true.
    /// </summary>
    public sealed class RunRestoreCoordinator : IInitializable
    {
        private readonly RunRestoreContext _context;
        private readonly IRunStateService _runState;
        private readonly IWorldStatePersistence _world;
        private readonly IGameLogger _logger;

        public RunRestoreCoordinator(RunRestoreContext context, IRunStateService runState,
            IWorldStatePersistence world, IGameLogger logger = null)
        {
            _context = context;
            _runState = runState;
            _world = world;
            _logger = logger;
        }

        public void Initialize()
        {
            if (!_context.IsRestoring)
            {
                return;
            }

            _runState.RestoreAll(_context.Snapshot);
            _world.RestoreCursors(_context.Snapshot.World);
            _logger?.Info(LogCategory.Persistence,
                "[RunRestoreCoordinator] Run state restored; the entrypoint will rebuild the saved world.");
        }
    }
}
