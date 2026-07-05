using System;
using Narrative.Dialogue;
using Narrative.Stories.Core;
using Narrative.Threads.Core;
using Zenject;

namespace Narrative.Runtime.Core
{
    /// <summary>
    /// The single chokepoint that folds an encounter's outcome into the run ledgers (R8/FR1/FR9).
    /// When a dialogue ends, its story is marked resolved (never re-offered — including a walk-away),
    /// and — unless the player walked away — its thread advances a stage, rearming the expiry clock.
    /// Subscribes to <see cref="DialogueRunner.OnDialogueEnded"/>, which fires exactly once per
    /// encounter (the combat-suspension resume ends through the same gate).
    /// </summary>
    public sealed class StoryResolutionRelay : IInitializable, IDisposable
    {
        private readonly DialogueRunner _runner;
        private readonly IStoryRunLedger _storyLedger;
        private readonly IThreadLedger _threadLedger;

        public StoryResolutionRelay(DialogueRunner runner, IStoryRunLedger storyLedger, IThreadLedger threadLedger)
        {
            _runner = runner;
            _storyLedger = storyLedger;
            _threadLedger = threadLedger;
        }

        public void Initialize()
        {
            _runner.OnDialogueEnded += HandleDialogueEnded;
        }

        public void Dispose()
        {
            _runner.OnDialogueEnded -= HandleDialogueEnded;
        }

        private void HandleDialogueEnded(string outcome)
        {
            var storyId = _runner.ActiveStoryId;
            if (string.IsNullOrEmpty(storyId))
            {
                return; // a casting built outside a story (legacy/test path) - nothing to record
            }

            _storyLedger.NoteResolved(storyId);

            // Walking away resolves the story but is not thread progress: a browsed-and-abandoned
            // errand must still lapse (FR4).
            if (outcome == DialogueRunner.LeaveOutcome)
            {
                return;
            }

            var threadId = _runner.ActiveThreadId;
            if (!string.IsNullOrEmpty(threadId))
            {
                _threadLedger.NoteBeatResolved(threadId);
            }
        }
    }
}
