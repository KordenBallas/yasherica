using System;
using System.Collections.Generic;
using Narrative;

namespace Tests.EditMode
{
    /// <summary>
    /// Ink-free fake <see cref="IStoryManager"/> for pure-C# tests. Records LoadStory/LoadState/
    /// SetVariable/GoToKnot calls, and runs a scripted timeline of <see cref="Frame"/>s so the
    /// dialogue runner can be driven end to end (lines with tags, choice points with branches).
    /// </summary>
    public sealed class FakeStoryManager : IStoryManager
    {
        public sealed class Frame
        {
            public string Text = string.Empty;
            public string[] Tags = Array.Empty<string>();
            public StoryChoice[] Choices;     // non-null => choice point
            public Frame[][] Branches;        // Branches[choiceIndex] => frames spliced in on choose

            public static Frame Line(string text, params string[] tags) => new Frame { Text = text, Tags = tags ?? Array.Empty<string>() };
            public static Frame ChoicePoint(StoryChoice[] choices, Frame[][] branches) => new Frame { Choices = choices, Branches = branches };
        }

        private readonly LinkedList<Frame> _frames = new LinkedList<Frame>();

        public string LoadedJson;
        public string LoadedState;
        public string LastKnot;
        public int LastChoiceIndex { get; private set; } = -1;
        public readonly List<KeyValuePair<string, object>> SetVarCalls = new();
        private readonly Dictionary<string, object> _vars = new();

        public event Action<string> OnTextChanged;
        public event Action<IReadOnlyList<StoryChoice>> OnChoicesAvailable;
        public event Action OnStoryEnded;
        public event Action<IReadOnlyList<string>> OnTagsParsed;

        public void Script(params Frame[] frames)
        {
            foreach (var f in frames)
            {
                _frames.AddLast(f);
            }
        }

        public bool CanContinue => _frames.First != null && _frames.First.Value.Choices == null;
        public bool HasChoices => _frames.First != null && _frames.First.Value.Choices != null;
        public IReadOnlyList<StoryChoice> CurrentChoices => HasChoices ? _frames.First.Value.Choices : Array.Empty<StoryChoice>();
        public IReadOnlyList<string> CurrentTags { get; private set; } = Array.Empty<string>();
        public string CurrentText { get; private set; } = string.Empty;

        public void LoadStory(string jsonContent) => LoadedJson = jsonContent;

        public string Continue()
        {
            if (!CanContinue)
            {
                return string.Empty;
            }

            var frame = _frames.First.Value;
            _frames.RemoveFirst();
            CurrentText = frame.Text;
            CurrentTags = frame.Tags;
            OnTextChanged?.Invoke(CurrentText);
            OnTagsParsed?.Invoke(CurrentTags);
            return CurrentText;
        }

        public void ChooseChoice(int choiceIndex)
        {
            LastChoiceIndex = choiceIndex;
            if (!HasChoices)
            {
                return;
            }

            var frame = _frames.First.Value;
            _frames.RemoveFirst();
            CurrentTags = Array.Empty<string>();

            var branch = frame.Branches != null && choiceIndex < frame.Branches.Length ? frame.Branches[choiceIndex] : null;
            if (branch == null)
            {
                return;
            }

            // Splice the branch frames at the front, preserving order.
            var node = _frames.First;
            foreach (var f in branch)
            {
                if (node == null)
                {
                    _frames.AddLast(f);
                }
                else
                {
                    _frames.AddBefore(node, f);
                }
            }
        }

        public void GoToKnot(string knotName) => LastKnot = knotName;
        public object GetVariable(string variableName) => _vars.TryGetValue(variableName, out var v) ? v : null;

        public void SetVariable(string variableName, object value)
        {
            _vars[variableName] = value;
            SetVarCalls.Add(new KeyValuePair<string, object>(variableName, value));
        }

        public int GetVisitCount(string pathString) => 0;
        public IReadOnlyList<string> GetTagsForKnot(string knotName) => Array.Empty<string>();
        public void BindExternalFunction<TResult>(string functionName, Func<TResult> function) { }
        public void BindExternalFunction<T1, TResult>(string functionName, Func<T1, TResult> function) { }
        public void BindExternalFunction<T1>(string functionName, Action<T1> function) { }
        public void BindExternalFunction<T1, T2, TResult>(string functionName, Func<T1, T2, TResult> function) { }
        public void BindExternalFunction<T1, T2>(string functionName, Action<T1, T2> function) { }
        public string SaveState() => "ink-state";
        public void LoadState(string savedState) => LoadedState = savedState;
        public void ResetStory() { }
        public void Reset() { }
    }
}
