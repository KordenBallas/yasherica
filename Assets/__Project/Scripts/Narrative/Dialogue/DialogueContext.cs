using Narrative.Data.Definitions;
using Narrative.Generation;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Default implementation of IDialogueContext.
    /// Provides unified context for dialogue sessions regardless of source.
    /// </summary>
    public class DialogueContext : IDialogueContext
    {
        public string NpcId { get; }
        public string DialogueKnot { get; }
        public NpcInstance BoundNpcInstance { get; }
        public BoundStory BoundStory { get; }
        public bool IsSideStory { get; }
        public string SideStoryId { get; }
        public string SpeakerName { get; }
        public bool HasInkContent { get; }

        private DialogueContext(
            string npcId,
            string dialogueKnot,
            NpcInstance boundNpcInstance,
            BoundStory boundStory,
            bool isSideStory,
            string sideStoryId,
            string speakerName,
            bool hasInkContent)
        {
            NpcId = npcId;
            DialogueKnot = dialogueKnot;
            BoundNpcInstance = boundNpcInstance;
            BoundStory = boundStory;
            IsSideStory = isSideStory;
            SideStoryId = sideStoryId;
            SpeakerName = speakerName;
            HasInkContent = hasInkContent;
        }

        /// <summary>
        /// Creates a context for NPC dialogue without a bound story.
        /// </summary>
        public static DialogueContext ForNpc(
            string npcId,
            string dialogueKnot,
            string speakerName,
            NpcInstance npcInstance = null)
        {
            return new DialogueContext(
                npcId: npcId,
                dialogueKnot: dialogueKnot,
                boundNpcInstance: npcInstance,
                boundStory: null,
                isSideStory: false,
                sideStoryId: null,
                speakerName: speakerName,
                hasInkContent: !string.IsNullOrEmpty(dialogueKnot));
        }

        /// <summary>
        /// Creates a context for NPC dialogue with a bound story.
        /// </summary>
        public static DialogueContext ForNpcWithStory(
            string npcId,
            NpcInstance npcInstance,
            BoundStory boundStory)
        {
            var template = boundStory?.Template;
            return new DialogueContext(
                npcId: npcId,
                dialogueKnot: template?.StartingKnot,
                boundNpcInstance: npcInstance,
                boundStory: boundStory,
                isSideStory: false,
                sideStoryId: null,
                speakerName: npcInstance?.DisplayName,
                hasInkContent: template?.HasInkContent == true);
        }

        /// <summary>
        /// Creates a context for a side story dialogue.
        /// </summary>
        public static DialogueContext ForSideStory(
            string sideStoryId,
            SideStoryDefinition sideStory,
            string npcId = null)
        {
            return new DialogueContext(
                npcId: npcId,
                dialogueKnot: sideStory?.StartingKnot,
                boundNpcInstance: null,
                boundStory: null,
                isSideStory: true,
                sideStoryId: sideStoryId,
                speakerName: null,
                hasInkContent: sideStory?.HasInkContent == true);
        }

        /// <summary>
        /// Creates a context for a side story with a bound story.
        /// </summary>
        public static DialogueContext ForSideStoryWithBoundStory(
            string sideStoryId,
            BoundStory boundStory,
            string npcId = null)
        {
            var template = boundStory?.Template;
            var primaryNpc = boundStory?.BoundNpcs?.Count > 0 ? boundStory.BoundNpcs[0] : null;

            return new DialogueContext(
                npcId: npcId ?? primaryNpc?.NpcId,
                dialogueKnot: template?.StartingKnot,
                boundNpcInstance: primaryNpc,
                boundStory: boundStory,
                isSideStory: true,
                sideStoryId: sideStoryId,
                speakerName: primaryNpc?.DisplayName,
                hasInkContent: template?.HasInkContent == true);
        }

        /// <summary>
        /// Creates a context for pure dialogue (no NPC, no story).
        /// </summary>
        public static DialogueContext ForDialogue(
            string dialogueKnot,
            string speakerName = null)
        {
            return new DialogueContext(
                npcId: null,
                dialogueKnot: dialogueKnot,
                boundNpcInstance: null,
                boundStory: null,
                isSideStory: false,
                sideStoryId: null,
                speakerName: speakerName,
                hasInkContent: !string.IsNullOrEmpty(dialogueKnot));
        }
    }
}
