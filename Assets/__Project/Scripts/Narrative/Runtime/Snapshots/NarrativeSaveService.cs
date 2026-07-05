using Narrative.Director.Core;
using Narrative.Dialogue;
using Narrative.Facts.Core;
using Narrative.Stories.Core;
using Narrative.Threads.Core;

namespace Narrative.Runtime.Snapshots
{
    /// <summary>
    /// Minimal <see cref="INarrativeSaveService"/> covering the determinism-critical run-state: the
    /// unified fact store and the serializable PRNG state (B2). Quests/castings/sessions are appended
    /// by the run-state aggregate when present. Enforces the W3-1 suspended-session guard. The actual
    /// file IO is a ROADMAP item — this assembles/parses the in-memory snapshot only.
    /// </summary>
    public sealed class NarrativeSaveService : INarrativeSaveService
    {
        private readonly IFactStore _store;
        private readonly IRandomSource _random;
        private readonly int _seed;
        private readonly IFactKeyRegistry _registry;
        private readonly IThreadLedger _threads;
        private readonly IStoryRunLedger _stories;

        public NarrativeSaveService(IFactStore store, IRandomSource random, int seed,
            IFactKeyRegistry registry = null, IThreadLedger threads = null, IStoryRunLedger stories = null)
        {
            _store = store;
            _random = random;
            _seed = seed;
            _registry = registry;
            _threads = threads;
            _stories = stories;
        }

        public bool CanCapture(DialogueRunnerState dialogueState)
        {
            // W3-1 (option a): suspension is a non-savepoint.
            return dialogueState != DialogueRunnerState.AwaitingExternal;
        }

        public bool TryCapture(DialogueRunnerState dialogueState, out RunNarrativeSnapshot snapshot)
        {
            if (!CanCapture(dialogueState))
            {
                snapshot = null;
                return false;
            }

            // With a vocabulary present the capture is partitioned by lifetime horizon (D20), so
            // save/load can persist run and meta facts separately; without one everything is treated
            // as run-scoped (the pre-D20 behavior).
            snapshot = new RunNarrativeSnapshot
            {
                Seed = _seed,
                RngState = _random?.State ?? 0,
                Facts = _registry == null
                    ? FactStoreSnapshotMapper.Capture(_store)
                    : FactStoreSnapshotMapper.Capture(_store, _registry, FactHorizon.Run),
                MetaFacts = _registry == null
                    ? new FactStoreSnapshot()
                    : FactStoreSnapshotMapper.Capture(_store, _registry, FactHorizon.Meta),
                Threads = RunLedgerSnapshotMapper.CaptureThreads(_threads),
                StoryLedger = RunLedgerSnapshotMapper.CaptureStories(_stories)
            };
            return true;
        }

        /// <summary>Restores the fact store and PRNG state from a snapshot (B2 determinism).</summary>
        public void Restore(RunNarrativeSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            FactStoreSnapshotMapper.Restore(snapshot.Facts, _store);
            FactStoreSnapshotMapper.Restore(snapshot.MetaFacts, _store);
            RunLedgerSnapshotMapper.RestoreThreads(snapshot.Threads, _threads);
            RunLedgerSnapshotMapper.RestoreStories(snapshot.StoryLedger, _stories);
            if (_random != null)
            {
                _random.State = snapshot.RngState;
            }
        }
    }
}
