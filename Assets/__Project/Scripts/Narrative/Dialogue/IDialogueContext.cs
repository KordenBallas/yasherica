using Narrative.Generation;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Provides context for a dialogue session.
    /// Abstracts NpcContent + BoundStory into a unified context for dialogue operations.
    /// </summary>
    public interface IDialogueContext
    {
        /// <summary>
        /// The NPC identifier for this dialogue context.
        /// May be null for non-NPC dialogues.
        /// </summary>
        string NpcId { get; }

        /// <summary>
        /// The Ink knot to start dialogue from.
        /// </summary>
        string DialogueKnot { get; }

        /// <summary>
        /// The bound NPC instance with runtime state.
        /// May be null if NPC instance is not available.
        /// </summary>
        NpcInstance BoundNpcInstance { get; }

        /// <summary>
        /// The bound story containing template and parameters.
        /// May be null for simple NPC dialogues without story generation.
        /// </summary>
        BoundStory BoundStory { get; }

        /// <summary>
        /// Whether this dialogue is part of a side story.
        /// </summary>
        bool IsSideStory { get; }

        /// <summary>
        /// The side story ID if this is a side story dialogue.
        /// </summary>
        string SideStoryId { get; }

        /// <summary>
        /// The speaker name to display.
        /// </summary>
        string SpeakerName { get; }

        /// <summary>
        /// Whether this context has a bound story with Ink content.
        /// </summary>
        bool HasInkContent { get; }
    }
}
