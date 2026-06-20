using Narrative.Director.Core;
using Narrative.Dialogue;
using Narrative.Facts.Core;

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

        public NarrativeSaveService(IFactStore store, IRandomSource random, int seed)
        {
            _store = store;
            _random = random;
            _seed = seed;
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

            snapshot = new RunNarrativeSnapshot
            {
                Seed = _seed,
                RngState = _random?.State ?? 0,
                Facts = FactStoreSnapshotMapper.Capture(_store)
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
            if (_random != null)
            {
                _random.State = snapshot.RngState;
            }
        }
    }
}
