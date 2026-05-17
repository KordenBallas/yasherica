using System;
using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Runtime session for an active Ink story with injected variables.
    /// Bridges bound parameters to the Ink runtime.
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class StorySession
    {
        private string _sessionId;
        private BoundStory _boundStory;
        private IStoryManager _storyManager;
        private StorySessionState _state;
        private Dictionary<string, object> _injectedVariables;
        private List<string> _visitedKnots;
        private string _currentKnot;
        private QuestInstance _associatedQuest;
        private DateTime _startedAt;
        private DateTime? _endedAt;

        /// <summary>
        /// Event fired when the session state changes.
        /// </summary>
        public event Action<StorySession, StorySessionState> OnStateChanged;

        /// <summary>
        /// Event fired when an outcome tag is encountered.
        /// </summary>
        public event Action<StorySession, string, string> OnOutcomeTag;

        /// <summary>
        /// Unique session identifier.
        /// </summary>
        public string SessionId => _sessionId;

        /// <summary>
        /// The bound story data.
        /// </summary>
        public BoundStory BoundStory => _boundStory;

        /// <summary>
        /// The story template definition.
        /// </summary>
        public StoryTemplateDefinition Template => _boundStory?.Template;

        /// <summary>
        /// Current session state.
        /// </summary>
        public StorySessionState State => _state;

        /// <summary>
        /// Variables injected into the Ink runtime.
        /// </summary>
        public IReadOnlyDictionary<string, object> InjectedVariables => _injectedVariables;

        /// <summary>
        /// Knots that have been visited in this session.
        /// </summary>
        public IReadOnlyList<string> VisitedKnots => _visitedKnots;

        /// <summary>
        /// Currently active knot.
        /// </summary>
        public string CurrentKnot => _currentKnot;

        /// <summary>
        /// Quest associated with this story session.
        /// </summary>
        public QuestInstance AssociatedQuest => _associatedQuest;

        /// <summary>
        /// When the session started.
        /// </summary>
        public DateTime StartedAt => _startedAt;

        /// <summary>
        /// When the session ended (if applicable).
        /// </summary>
        public DateTime? EndedAt => _endedAt;

        /// <summary>
        /// Whether the session is currently active.
        /// </summary>
        public bool IsActive => _state == StorySessionState.Active;

        /// <summary>
        /// The story manager handling Ink execution.
        /// </summary>
        public IStoryManager StoryManager => _storyManager;

        /// <summary>
        /// Creates a new story session.
        /// </summary>
        public StorySession(BoundStory boundStory, IStoryManager storyManager)
        {
            _boundStory = boundStory ?? throw new ArgumentNullException(nameof(boundStory));
            _storyManager = storyManager ?? throw new ArgumentNullException(nameof(storyManager));
            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            _state = StorySessionState.Created;
            _injectedVariables = new Dictionary<string, object>();
            _visitedKnots = new List<string>();
            _currentKnot = string.Empty;
        }

        /// <summary>
        /// Initializes and starts the story session.
        /// </summary>
        public void Start()
        {
            if (_state != StorySessionState.Created)
                return;

            // Load the Ink story
            if (_boundStory.Template.HasInkContent)
            {
                _storyManager.LoadStory(_boundStory.Template.GetInkJson());
            }

            // Inject bound parameters as Ink variables
            InjectBoundParameters();

            // Navigate to starting knot
            string startKnot = _boundStory.Template.StartingKnot;
            if (!string.IsNullOrEmpty(startKnot))
            {
                _storyManager.GoToKnot(startKnot);
                _currentKnot = startKnot;
                _visitedKnots.Add(startKnot);
            }

            _state = StorySessionState.Active;
            _startedAt = DateTime.UtcNow;
            OnStateChanged?.Invoke(this, _state);
        }

        /// <summary>
        /// Associates a quest with this session.
        /// </summary>
        public void SetAssociatedQuest(QuestInstance quest)
        {
            _associatedQuest = quest;
        }

        /// <summary>
        /// Records visiting a knot.
        /// </summary>
        public void RecordKnotVisit(string knotName)
        {
            if (string.IsNullOrEmpty(knotName))
                return;

            _currentKnot = knotName;
            if (!_visitedKnots.Contains(knotName))
            {
                _visitedKnots.Add(knotName);
            }
        }

        /// <summary>
        /// Processes a tag from the Ink story.
        /// </summary>
        public void ProcessTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return;

            // Parse outcome tags
            if (tag.StartsWith("outcome:"))
            {
                string outcome = tag.Substring("outcome:".Length);
                OnOutcomeTag?.Invoke(this, "outcome", outcome);
            }
            else if (tag.StartsWith("quest:"))
            {
                string questAction = tag.Substring("quest:".Length);
                OnOutcomeTag?.Invoke(this, "quest", questAction);
            }
            else if (tag.StartsWith("npc_reaction:"))
            {
                string reaction = tag.Substring("npc_reaction:".Length);
                OnOutcomeTag?.Invoke(this, "npc_reaction", reaction);
            }
        }

        /// <summary>
        /// Pauses the session.
        /// </summary>
        public void Pause()
        {
            if (_state == StorySessionState.Active)
            {
                _state = StorySessionState.Paused;
                OnStateChanged?.Invoke(this, _state);
            }
        }

        /// <summary>
        /// Resumes a paused session.
        /// </summary>
        public void Resume()
        {
            if (_state == StorySessionState.Paused)
            {
                _state = StorySessionState.Active;
                OnStateChanged?.Invoke(this, _state);
            }
        }

        /// <summary>
        /// Ends the session with the given outcome.
        /// </summary>
        public void End(StorySessionOutcome outcome)
        {
            if (_state == StorySessionState.Completed)
                return;

            _state = StorySessionState.Completed;
            _endedAt = DateTime.UtcNow;
            OnStateChanged?.Invoke(this, _state);
        }

        /// <summary>
        /// Gets the value of an injected variable.
        /// </summary>
        public T GetVariable<T>(string variableName, T defaultValue = default)
        {
            if (_injectedVariables.TryGetValue(variableName, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            // Try getting from Ink runtime
            var inkValue = _storyManager.GetVariable(variableName);
            if (inkValue is T inkTypedValue)
            {
                return inkTypedValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets a variable in both the injected cache and Ink runtime.
        /// </summary>
        public void SetVariable(string variableName, object value)
        {
            _injectedVariables[variableName] = value;
            _storyManager.SetVariable(variableName, value);
        }

        /// <summary>
        /// Saves the current session state.
        /// </summary>
        public string SaveState()
        {
            return _storyManager.SaveState();
        }

        /// <summary>
        /// Restores a previously saved state.
        /// </summary>
        public void RestoreState(string savedState)
        {
            if (!string.IsNullOrEmpty(savedState))
            {
                _storyManager.LoadState(savedState);
            }
        }

        private void InjectBoundParameters()
        {
            if (_boundStory.BoundParameters == null)
                return;

            foreach (var kvp in _boundStory.BoundParameters)
            {
                var param = kvp.Value;
                if (param?.Slot != null && !string.IsNullOrEmpty(param.Slot.InkVariableName))
                {
                    _injectedVariables[param.Slot.InkVariableName] = param.InkValue;
                    _storyManager.SetVariable(param.Slot.InkVariableName, param.InkValue);
                }
            }
        }
    }

    /// <summary>
    /// State of a story session.
    /// </summary>
    public enum StorySessionState
    {
        Created,
        Active,
        Paused,
        Completed
    }

    /// <summary>
    /// Outcome of a completed story session.
    /// </summary>
    public enum StorySessionOutcome
    {
        Completed,
        Abandoned,
        TimedOut,
        Error
    }
}
