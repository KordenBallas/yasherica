using Narrative.Generation;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Interface for initializing dialogue sessions.
    /// Handles story loading, external function binding, and parameter injection.
    /// </summary>
    public interface IDialogueSessionInitializer
    {
        /// <summary>
        /// Initializes a dialogue session from a context.
        /// Loads story content, binds external functions, and injects parameters.
        /// </summary>
        /// <param name="context">The dialogue context to initialize from</param>
        void InitializeSession(IDialogueContext context);

        /// <summary>
        /// Initializes a dialogue session from a bound story.
        /// </summary>
        /// <param name="boundStory">The bound story with parameters</param>
        void InitializeSession(BoundStory boundStory);

        /// <summary>
        /// Ensures external functions are bound to the current story.
        /// Can be called independently when story is already loaded.
        /// </summary>
        void EnsureExternalFunctionsBound();
    }
}
