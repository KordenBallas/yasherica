using System;
using System.Collections.Generic;

namespace Narrative
{
    /// <summary>
    /// Interface for managing the Ink story runtime.
    /// Provides operations for loading, navigating, and interacting with Ink stories.
    /// </summary>
    public interface IStoryManager
    {
        /// <summary>
        /// Event fired when the story text changes.
        /// </summary>
        event Action<string> OnTextChanged;

        /// <summary>
        /// Event fired when choices become available.
        /// </summary>
        event Action<IReadOnlyList<StoryChoice>> OnChoicesAvailable;

        /// <summary>
        /// Event fired when the story reaches an end (no more content and no choices).
        /// </summary>
        event Action OnStoryEnded;

        /// <summary>
        /// Event fired when tags are parsed from the current content.
        /// </summary>
        event Action<IReadOnlyList<string>> OnTagsParsed;

        /// <summary>
        /// Whether the story can continue to the next line.
        /// </summary>
        bool CanContinue { get; }

        /// <summary>
        /// Whether the story has choices available.
        /// </summary>
        bool HasChoices { get; }

        /// <summary>
        /// Current available choices.
        /// </summary>
        IReadOnlyList<StoryChoice> CurrentChoices { get; }

        /// <summary>
        /// Tags for the current line of content.
        /// </summary>
        IReadOnlyList<string> CurrentTags { get; }

        /// <summary>
        /// Current line of story text.
        /// </summary>
        string CurrentText { get; }

        /// <summary>
        /// Loads a story from JSON content.
        /// </summary>
        void LoadStory(string jsonContent);

        /// <summary>
        /// Continues the story to the next line.
        /// Returns the text of that line.
        /// </summary>
        string Continue();

        /// <summary>
        /// Selects a choice by index.
        /// </summary>
        void ChooseChoice(int choiceIndex);

        /// <summary>
        /// Navigates to a specific knot/stitch in the story.
        /// </summary>
        void GoToKnot(string knotName);

        /// <summary>
        /// Gets the value of a story variable.
        /// </summary>
        object GetVariable(string variableName);

        /// <summary>
        /// Sets the value of a story variable.
        /// </summary>
        void SetVariable(string variableName, object value);

        /// <summary>
        /// Gets the number of times a knot/stitch has been visited.
        /// </summary>
        int GetVisitCount(string pathString);

        /// <summary>
        /// Gets tags for a specific knot without navigating to it.
        /// </summary>
        IReadOnlyList<string> GetTagsForKnot(string knotName);

        /// <summary>
        /// Binds an external C# function to be callable from Ink.
        /// </summary>
        void BindExternalFunction<TResult>(string functionName, Func<TResult> function);

        /// <summary>
        /// Binds an external C# function with one parameter.
        /// </summary>
        void BindExternalFunction<T1, TResult>(string functionName, Func<T1, TResult> function);

        /// <summary>
        /// Binds an external void function with one parameter.
        /// </summary>
        void BindExternalFunction<T1>(string functionName, Action<T1> function);

        /// <summary>
        /// Saves the current story state to a serializable format.
        /// </summary>
        string SaveState();

        /// <summary>
        /// Loads a previously saved story state.
        /// </summary>
        void LoadState(string savedState);

        /// <summary>
        /// Resets the story to its initial state.
        /// </summary>
        void ResetStory();
    }

    /// <summary>
    /// Represents a choice in the Ink story.
    /// </summary>
    public class StoryChoice
    {
        public int Index { get; }
        public string Text { get; }
        public IReadOnlyList<string> Tags { get; }

        public StoryChoice(int index, string text, IReadOnlyList<string> tags = null)
        {
            Index = index;
            Text = text;
            Tags = tags ?? Array.Empty<string>();
        }
    }
}
