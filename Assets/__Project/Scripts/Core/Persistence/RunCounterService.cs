using Narrative.Facts.Core;
using Zenject;

namespace Core.Persistence
{
    /// <summary>
    /// Advances the meta-scoped run counter (<c>world.run_count</c>) once per fresh run start, so
    /// spine soft floors ("not before run N", D7/P3-1) have a number to gate on. A continue is the
    /// same run resuming, never a new one. Runs after <see cref="MetaMemoryBootstrap"/> has loaded
    /// the cross-run memory (so the increment lands on the persisted value) and before the
    /// entrypoint plans window 0 (so the very first window already sees the current run's number).
    /// Persistence itself is free: the key's Meta horizon rides every meta flush.
    /// </summary>
    public sealed class RunCounterService : IInitializable
    {
        private readonly RunRestoreContext _restoreContext;
        private readonly IFactStore _facts;

        public RunCounterService(RunRestoreContext restoreContext, IFactStore facts)
        {
            _restoreContext = restoreContext;
            _facts = facts;
        }

        public void Initialize()
        {
            if (_restoreContext.IsRestoring)
            {
                return;
            }

            _facts.SetInt(WorldFacts.RunCount, _facts.GetInt(WorldFacts.RunCount) + 1);
        }
    }
}
