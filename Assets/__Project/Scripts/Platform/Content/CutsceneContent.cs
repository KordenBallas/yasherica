using System;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Platform content for story cutscenes.
    /// Supports Ink-driven cutscenes with camera control hints.
    /// </summary>
    public class CutsceneContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Cutscene;

        /// <summary>
        /// The Ink knot for this cutscene.
        /// </summary>
        public string CutsceneKnot { get; set; }

        /// <summary>
        /// Unique identifier for this cutscene.
        /// </summary>
        public string CutsceneId { get; set; }

        /// <summary>
        /// Whether the cutscene should auto-play when platform is entered.
        /// </summary>
        public bool AutoPlay { get; set; } = true;

        /// <summary>
        /// Whether the player can skip this cutscene.
        /// </summary>
        public bool Skippable { get; set; } = true;

        /// <summary>
        /// Whether player control is disabled during cutscene.
        /// </summary>
        public bool DisablePlayerControl { get; set; } = true;

        /// <summary>
        /// Whether this cutscene has been played.
        /// </summary>
        public bool HasPlayed { get; private set; }

        /// <summary>
        /// Event fired when cutscene starts.
        /// </summary>
        public event Action<CutsceneContent> OnCutsceneStarted;

        /// <summary>
        /// Event fired when cutscene ends (completed or skipped).
        /// </summary>
        public event Action<CutsceneContent, bool> OnCutsceneEnded;

        public CutsceneContent()
        {
        }

        public CutsceneContent(string cutsceneId, string cutsceneKnot)
        {
            CutsceneId = cutsceneId;
            CutsceneKnot = cutsceneKnot;
        }

        /// <summary>
        /// Checks if this content has a valid cutscene.
        /// </summary>
        public bool HasCutscene => !string.IsNullOrEmpty(CutsceneKnot);

        public override void Initialize(IPlatform platform)
        {
            if (!HasCutscene)
            {
                Debug.LogWarning($"[CutsceneContent] No cutscene knot assigned for platform {platform.Id}");
            }
        }

        public override void OnPlatformEntered(IPlatform platform)
        {
            if (!HasCutscene || HasPlayed)
                return;

            if (AutoPlay)
            {
                StartCutscene();
            }
        }

        /// <summary>
        /// Starts the cutscene.
        /// </summary>
        public void StartCutscene()
        {
            if (!HasCutscene)
            {
                Debug.LogWarning("[CutsceneContent] Cannot start cutscene: no knot assigned");
                return;
            }

            if (HasPlayed)
            {
                Debug.Log($"[CutsceneContent] Cutscene already played: {CutsceneId}");
                return;
            }

            Debug.Log($"[CutsceneContent] Starting cutscene: {CutsceneId}");
            OnCutsceneStarted?.Invoke(this);
        }

        /// <summary>
        /// Completes the cutscene normally.
        /// </summary>
        public void CompleteCutscene()
        {
            EndCutscene(wasSkipped: false);
        }

        /// <summary>
        /// Skips the cutscene.
        /// </summary>
        public void SkipCutscene()
        {
            if (!Skippable)
            {
                Debug.Log("[CutsceneContent] Cutscene cannot be skipped");
                return;
            }

            EndCutscene(wasSkipped: true);
        }

        private void EndCutscene(bool wasSkipped)
        {
            HasPlayed = true;
            Debug.Log($"[CutsceneContent] Cutscene ended: {CutsceneId} (skipped: {wasSkipped})");
            OnCutsceneEnded?.Invoke(this, wasSkipped);
        }
    }
}
