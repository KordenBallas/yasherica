using System;
using Core.Events;
using Core.Logging;
using Narrative.Dialogue;
using Platform;
using Zenject;

namespace Core.Persistence
{
    /// <summary>
    /// The invisible autosave (A3): captures and writes the whole-run image at every platform
    /// ENTRY (D7). Entered — not Exited — is the savepoint: exit fires first and locks the window
    /// + plans the next one, so at entry the RNG/allocator/ledger state is post-planning (exactly
    /// what must persist) and the entered platform IS the clean start Continue resumes at (FR6/7).
    /// A capture refused by the W3-1 guard (dialogue awaiting an external step) simply skips this
    /// savepoint — the next boundary catches up. The Meta partition flushes at the same points
    /// (D9), so the world memory is never older than the run save.
    /// </summary>
    public sealed class AutosaveService : IInitializable, IDisposable
    {
        private readonly IRunStateService _runState;
        private readonly IRunSaveStore _runStore;
        private readonly IMetaMemoryFlush _metaFlush;
        private readonly Func<DialogueRunnerState> _dialogueState;
        private readonly IGameLogger _logger;
        private readonly ISavepointObserver _observer;

        public AutosaveService(IRunStateService runState, IRunSaveStore runStore,
            IMetaMemoryFlush metaFlush, Func<DialogueRunnerState> dialogueState, IGameLogger logger = null,
            ISavepointObserver observer = null)
        {
            _runState = runState;
            _runStore = runStore;
            _metaFlush = metaFlush;
            _dialogueState = dialogueState;
            _logger = logger;
            _observer = observer;
        }

        public void Initialize()
        {
            PlatformEvents.OnPlatformEntered += OnPlatformEntered;
        }

        public void Dispose()
        {
            PlatformEvents.OnPlatformEntered -= OnPlatformEntered;
        }

        private void OnPlatformEntered(IPlatform platform)
        {
            Save();
        }

        /// <summary>
        /// The graceful-exit savepoint (application quit / editor play-mode stop): everything done
        /// ON the current platform since entering it (a finished conversation, its facts and quest
        /// stages, picked loot) would otherwise be lost to the entry-time save. Stricter than the
        /// regular savepoint: skipped while ANY dialogue is open — quitting mid-conversation keeps
        /// the entry-time save so the platform re-begins clean (FR7), never half-conversed.
        /// </summary>
        public void SaveGraceful()
        {
            if (_dialogueState() != DialogueRunnerState.Ended)
            {
                _logger?.Info(LogCategory.Persistence,
                    "[AutosaveService] Quit savepoint skipped: a dialogue is open; the entry-time save stands (FR7).");
                return;
            }

            Save();
        }

        /// <summary>One savepoint: run image + meta flush. Public so the run bootstrap can take the
        /// initial savepoint right after the world exists (quitting immediately still resumes).</summary>
        public void Save()
        {
            if (!_runState.TryCaptureAll(_dialogueState(), out var snapshot))
            {
                _logger?.Info(LogCategory.Persistence,
                    "[AutosaveService] Savepoint skipped: a dialogue is awaiting an external step (W3-1).");
                return;
            }

            _runStore.Save(snapshot);
            // Observers run between the run write and the meta flush, so a meta fact they record
            // (e.g. the Heat high-water mark) is persisted by this very flush.
            _observer?.OnSavepointCaptured(snapshot);
            _metaFlush.Flush();
        }
    }
}
