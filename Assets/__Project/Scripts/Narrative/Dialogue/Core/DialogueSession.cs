using System.Collections.Generic;
using Narrative.Casting.Core;
using Narrative.Facts.Core;

namespace Narrative.Dialogue.Core
{
    /// <summary>
    /// One conversation (R2 per-session): a thin wrapper over a single <see cref="IStoryManager"/>
    /// (Ink runtime) plus the dialogue fragment's footprint handle. The suspension state machine and
    /// tag dispatch live in the runner; this type owns load/continue/choose, variable injection, and
    /// save/restore.
    ///
    /// Fresh-start vs restore (W2-4): <see cref="StartFresh"/> injects the declared variables from the
    /// casting context (names, items, availability flags); <see cref="Restore"/> trusts the serialized
    /// Ink state and injects NOTHING, so play-time-modified variables survive a reload.
    /// </summary>
    public sealed class DialogueSession
    {
        private readonly IStoryManager _story;

        public string DialogueId { get; private set; }
        public IReadOnlyList<FactKeyShapeCore> Footprint { get; private set; } = System.Array.Empty<FactKeyShapeCore>();

        public DialogueSession(IStoryManager story)
        {
            _story = story;
        }

        public void StartFresh(DialogueData data, ContextBag context)
        {
            Bind(data);
            _story.LoadStory(data.InkJson);

            // Inject only declared variables for which the context supplies a value.
            for (int i = 0; i < data.DeclaredVariables.Count; i++)
            {
                var name = data.DeclaredVariables[i];
                if (context != null && context.TryGetVariable(name, out var value))
                {
                    _story.SetVariable(name, value);
                }
            }

            // Navigate to the declared start knot. Ink does NOT auto-enter a knot named "start" — a
            // fresh story begins at top-level flow, and these dialogue files are knot-only (no top-level
            // content), so without this the runner finds nothing to continue and ends immediately.
            if (!string.IsNullOrEmpty(data.StartKnot))
            {
                _story.GoToKnot(data.StartKnot);
            }
        }

        public void Restore(DialogueData data, string inkState)
        {
            Bind(data);
            _story.LoadStory(data.InkJson);
            _story.LoadState(inkState); // trust serialized variables/flow; do NOT re-inject (W2-4)
        }

        public bool CanContinue => _story.CanContinue;
        public bool HasChoices => _story.HasChoices;
        public string CurrentText => _story.CurrentText;
        public IReadOnlyList<StoryChoice> CurrentChoices => _story.CurrentChoices;
        public IReadOnlyList<string> CurrentTags => _story.CurrentTags;

        public string Continue() => _story.Continue();
        public void Choose(int index) => _story.ChooseChoice(index);
        public void SetVariable(string name, object value) => _story.SetVariable(name, value);
        public object GetVariable(string name) => _story.GetVariable(name);
        public string SaveState() => _story.SaveState();

        private void Bind(DialogueData data)
        {
            DialogueId = data.DialogueId;
            Footprint = data.DeclaredFactWrites;
        }
    }
}
