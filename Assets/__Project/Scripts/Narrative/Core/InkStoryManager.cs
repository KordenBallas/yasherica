using System;
using System.Collections.Generic;
using System.Linq;
using Ink.Runtime;
using UnityEngine;

namespace Narrative
{
    /// <summary>
    /// Wraps Ink.Runtime.Story to provide story management functionality.
    /// Pure C# class - no MonoBehaviour.
    /// </summary>
    public class InkStoryManager : IStoryManager
    {
        private Story _story;
        private string _currentText;
        private List<StoryChoice> _currentChoices = new();
        private List<string> _currentTags = new();

        public event Action<string> OnTextChanged;
        public event Action<IReadOnlyList<StoryChoice>> OnChoicesAvailable;
        public event Action OnStoryEnded;
        public event Action<IReadOnlyList<string>> OnTagsParsed;

        public bool CanContinue => _story?.canContinue ?? false;
        public bool HasChoices => _story?.currentChoices.Count > 0;
        public IReadOnlyList<StoryChoice> CurrentChoices => _currentChoices;
        public IReadOnlyList<string> CurrentTags => _currentTags;
        public string CurrentText => _currentText;

        public void LoadStory(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
            {
                Debug.LogError("[InkStoryManager] Cannot load story: JSON content is null or empty");
                return;
            }

            try
            {
                _story = new Story(jsonContent);
                _currentText = string.Empty;
                _currentChoices.Clear();
                _currentTags.Clear();
                Debug.Log("[InkStoryManager] Story loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InkStoryManager] Failed to load story: {ex.Message}");
                throw;
            }
        }

        public string Continue()
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot continue: no story loaded");
                return string.Empty;
            }

            if (!_story.canContinue)
            {
                Debug.LogWarning("[InkStoryManager] Cannot continue: story cannot continue");
                UpdateChoices();
                CheckForEnd();
                return _currentText;
            }

            _currentText = _story.Continue().Trim();
            UpdateTags();
            UpdateChoices();

            OnTextChanged?.Invoke(_currentText);
            OnTagsParsed?.Invoke(_currentTags);

            if (HasChoices)
            {
                OnChoicesAvailable?.Invoke(_currentChoices);
            }

            CheckForEnd();

            return _currentText;
        }

        public void ChooseChoice(int choiceIndex)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot choose: no story loaded");
                return;
            }

            if (choiceIndex < 0 || choiceIndex >= _story.currentChoices.Count)
            {
                Debug.LogError($"[InkStoryManager] Invalid choice index: {choiceIndex}. Available: {_story.currentChoices.Count}");
                return;
            }

            _story.ChooseChoiceIndex(choiceIndex);
            _currentChoices.Clear();
        }

        public void GoToKnot(string knotName)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot navigate: no story loaded");
                return;
            }

            if (string.IsNullOrEmpty(knotName))
            {
                Debug.LogWarning("[InkStoryManager] Cannot navigate: knot name is empty");
                return;
            }

            try
            {
                _story.ChoosePathString(knotName);
                _currentChoices.Clear();
                Debug.Log($"[InkStoryManager] Navigated to knot: {knotName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InkStoryManager] Failed to navigate to knot '{knotName}': {ex.Message}");
            }
        }

        public object GetVariable(string variableName)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot get variable: no story loaded");
                return null;
            }

            try
            {
                return _story.variablesState[variableName];
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InkStoryManager] Failed to get variable '{variableName}': {ex.Message}");
                return null;
            }
        }

        public void SetVariable(string variableName, object value)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot set variable: no story loaded");
                return;
            }

            try
            {
                _story.variablesState[variableName] = value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InkStoryManager] Failed to set variable '{variableName}': {ex.Message}");
            }
        }

        public int GetVisitCount(string pathString)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot get visit count: no story loaded");
                return 0;
            }

            try
            {
                return _story.state.VisitCountAtPathString(pathString);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InkStoryManager] Failed to get visit count for '{pathString}': {ex.Message}");
                return 0;
            }
        }

        public IReadOnlyList<string> GetTagsForKnot(string knotName)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot get tags: no story loaded");
                return Array.Empty<string>();
            }

            try
            {
                var tags = _story.TagsForContentAtPath(knotName);
                return tags ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InkStoryManager] Failed to get tags for knot '{knotName}': {ex.Message}");
                return Array.Empty<string>();
            }
        }

        public void BindExternalFunction<TResult>(string functionName, Func<TResult> function)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot bind function: no story loaded");
                return;
            }

            _story.BindExternalFunction(functionName, (Func<object>)(() => function()));
        }

        public void BindExternalFunction<T1, TResult>(string functionName, Func<T1, TResult> function)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot bind function: no story loaded");
                return;
            }

            _story.BindExternalFunction(functionName, (Func<T1, object>)((arg) => function(arg)));
        }

        public void BindExternalFunction<T1>(string functionName, Action<T1> function)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot bind function: no story loaded");
                return;
            }

            _story.BindExternalFunction(functionName, function);
        }

        public string SaveState()
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot save state: no story loaded");
                return string.Empty;
            }

            return _story.state.ToJson();
        }

        public void LoadState(string savedState)
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot load state: no story loaded");
                return;
            }

            if (string.IsNullOrEmpty(savedState))
            {
                Debug.LogWarning("[InkStoryManager] Cannot load state: saved state is empty");
                return;
            }

            try
            {
                _story.state.LoadJson(savedState);
                UpdateTags();
                UpdateChoices();
                Debug.Log("[InkStoryManager] State loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InkStoryManager] Failed to load state: {ex.Message}");
            }
        }

        public void ResetStory()
        {
            if (_story == null)
            {
                Debug.LogWarning("[InkStoryManager] Cannot reset: no story loaded");
                return;
            }

            _story.ResetState();
            _currentText = string.Empty;
            _currentChoices.Clear();
            _currentTags.Clear();
            Debug.Log("[InkStoryManager] Story reset to initial state");
        }

        private void UpdateTags()
        {
            _currentTags.Clear();
            if (_story.currentTags != null)
            {
                _currentTags.AddRange(_story.currentTags);
            }
        }

        private void UpdateChoices()
        {
            _currentChoices.Clear();

            if (_story.currentChoices == null || _story.currentChoices.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _story.currentChoices.Count; i++)
            {
                var inkChoice = _story.currentChoices[i];
                var choice = new StoryChoice(i, inkChoice.text.Trim(), inkChoice.tags);
                _currentChoices.Add(choice);
            }
        }

        private void CheckForEnd()
        {
            if (!CanContinue && !HasChoices)
            {
                OnStoryEnded?.Invoke();
            }
        }
    }
}
