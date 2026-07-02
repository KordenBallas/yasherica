using System;
using Core.Logging;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Platform content for pure dialogue (no visible NPC).
    /// Used for environmental storytelling, signs, terminals, etc.
    /// </summary>
    public class DialogueContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Dialogue;

        /// <summary>
        /// The Ink knot to start dialogue from.
        /// </summary>
        public string DialogueKnot { get; set; }

        /// <summary>
        /// Optional speaker name to display (for narration, signs, etc.).
        /// </summary>
        public string SpeakerName { get; set; }

        /// <summary>
        /// Whether dialogue should auto-start when platform is entered.
        /// </summary>
        public bool AutoStart { get; set; } = true;

        /// <summary>
        /// Whether the player can exit dialogue early.
        /// </summary>
        public bool AllowEarlyExit { get; set; } = true;

        /// <summary>
        /// Optional trigger object in the scene.
        /// </summary>
        public GameObject TriggerObject { get; private set; }

        /// <summary>
        /// Event fired when dialogue is triggered.
        /// </summary>
        public event Action<DialogueContent> OnDialogueTriggered;

        /// <summary>
        /// Event fired when dialogue completes.
        /// </summary>
        public event Action<DialogueContent> OnDialogueCompleted;

        public DialogueContent()
        {
        }

        public DialogueContent(string dialogueKnot, string speakerName = null)
        {
            DialogueKnot = dialogueKnot;
            SpeakerName = speakerName;
        }

        /// <summary>
        /// Checks if this content has valid dialogue.
        /// </summary>
        public bool HasDialogue => !string.IsNullOrEmpty(DialogueKnot);

        public override void Initialize(IPlatform platform)
        {
            if (!HasDialogue)
            {
                Logger?.Warning(LogCategory.Platform,$"[DialogueContent] No dialogue knot assigned for platform {platform.Id}");
            }
        }

        public override void OnPlatformEntered(IPlatform platform)
        {
            if (!HasDialogue)
                return;

            if (AutoStart)
            {
                TriggerDialogue();
            }
        }

        /// <summary>
        /// Triggers the dialogue to start.
        /// </summary>
        public void TriggerDialogue()
        {
            if (!HasDialogue)
            {
                Logger?.Warning(LogCategory.Platform,"[DialogueContent] Cannot trigger dialogue: no knot assigned");
                return;
            }

            Logger?.Info(LogCategory.Platform,$"[DialogueContent] Triggering dialogue: {DialogueKnot}");
            OnDialogueTriggered?.Invoke(this);
        }

        /// <summary>
        /// Notifies that dialogue has completed.
        /// </summary>
        public void NotifyDialogueCompleted()
        {
            Logger?.Info(LogCategory.Platform,$"[DialogueContent] Dialogue completed: {DialogueKnot}");
            OnDialogueCompleted?.Invoke(this);
        }
    }
}
