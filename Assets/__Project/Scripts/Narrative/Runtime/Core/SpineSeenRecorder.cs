using System;
using System.Collections.Generic;
using Narrative.Dialogue;
using Narrative.Facts.Core;
using Narrative.Stories.Core;
using Zenject;

namespace Narrative.Runtime.Core
{
    /// <summary>
    /// The single writer of the cross-run spine cursor (D20/P3-3): when a spine reveal-beat's
    /// dialogue ends — with any outcome, walk-away included — its <c>world.&lt;storyId&gt;.spine_seen</c>
    /// meta fact is set, so the reserved lane never re-reveals the beat in a later run. SEEN, not
    /// placed, is the cross-run rule (PO decision): a beat placed but never entered stays spent only
    /// for its own run (the run-scoped <see cref="IStoryRunLedger"/>) and returns to the pool after
    /// death. Kept separate from <see cref="StoryResolutionRelay"/> — that folds outcomes into the
    /// run ledgers; this writes the meta horizon. The fact persists at the next meta flush; a
    /// mid-dialogue quit never fires <see cref="DialogueRunner.OnDialogueEnded"/>, so the encounter
    /// re-begins unseen on continue — consistent with the savepoint model.
    /// </summary>
    public sealed class SpineSeenRecorder : IInitializable, IDisposable
    {
        private readonly DialogueRunner _runner;
        private readonly IFactStore _facts;
        private readonly HashSet<string> _spineStoryIds = new HashSet<string>();

        public SpineSeenRecorder(DialogueRunner runner, IReadOnlyList<StoryTemplateData> stories, IFactStore facts)
        {
            _runner = runner;
            _facts = facts;

            for (int i = 0; i < stories.Count; i++)
            {
                if (stories[i] != null && stories[i].IsSpine)
                {
                    _spineStoryIds.Add(stories[i].StoryId);
                }
            }
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
            if (string.IsNullOrEmpty(storyId) || !_spineStoryIds.Contains(storyId))
            {
                return;
            }

            _facts.SetBool(WorldFacts.SpineSeen, true, storyId);
        }
    }
}
