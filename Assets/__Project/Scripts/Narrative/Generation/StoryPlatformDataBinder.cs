using System;
using System.Collections.Generic;
using LevelGeneration;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Generation
{
    /// <summary>
    /// Interface for binding generated story data to platform data.
    /// Bridges NarrativeGenerator output to StoryPlatformData.
    /// </summary>
    public interface IStoryPlatformDataBinder
    {
        /// <summary>
        /// Binds BoundStory from generation result to matching StoryPlatformData.
        /// Main stories are bound to key platforms first, then side stories to remaining NPC platforms.
        /// </summary>
        /// <param name="result">The generation result containing story sessions</param>
        /// <param name="scenarioData">The scenario data with platform requirements</param>
        void BindGeneratedStories(GenerationResult result, ScenarioData scenarioData);

        /// <summary>
        /// Binds a single BoundStory to a StoryPlatformData.
        /// </summary>
        /// <param name="boundStory">The bound story to attach</param>
        /// <param name="platformData">The platform data to attach to</param>
        /// <param name="sessionId">Optional session ID</param>
        void BindStoryToPlatform(BoundStory boundStory, StoryPlatformData platformData, string sessionId = null);

        /// <summary>
        /// Event fired when a story is bound to platform data.
        /// </summary>
        event Action<StoryPlatformData, BoundStory> OnStoryBound;
    }

    /// <summary>
    /// Default implementation of IStoryPlatformDataBinder.
    /// Handles main story vs side story differentiation and platform matching.
    /// </summary>
    public class StoryPlatformDataBinder : IStoryPlatformDataBinder
    {
        public event Action<StoryPlatformData, BoundStory> OnStoryBound;

        public void BindGeneratedStories(GenerationResult result, ScenarioData scenarioData)
        {
            if (result == null || !result.Success)
            {
                Debug.LogWarning("[StoryPlatformDataBinder] Cannot bind: generation result is null or failed");
                return;
            }

            if (scenarioData?.RequiredPlatforms == null)
            {
                Debug.LogWarning("[StoryPlatformDataBinder] Cannot bind: scenario data has no platforms");
                return;
            }

            int totalStories = result.StorySessions?.Count ?? 0;
            int totalPlatforms = scenarioData.RequiredPlatforms.Count;
            Debug.Log($"[StoryPlatformDataBinder] Binding {totalStories} stories to {totalPlatforms} platforms");

            // Separate stories by type
            var mainStories = new List<(StorySession session, BoundStory story)>();
            var sideStories = new List<(StorySession session, BoundStory story)>();

            foreach (var session in result.StorySessions)
            {
                var boundStory = session.BoundStory;
                if (boundStory?.Template == null)
                    continue;

                if (boundStory.Template.StoryType == StoryType.Chapter)
                {
                    mainStories.Add((session, boundStory));
                }
                else
                {
                    sideStories.Add((session, boundStory));
                }
            }

            Debug.Log($"[StoryPlatformDataBinder] Categorized: {mainStories.Count} main stories, {sideStories.Count} side stories");

            // Track bound platforms to avoid duplicates
            var boundPlatformIndices = new HashSet<int>();
            int mainStoriesBound = 0;
            int sideStoriesBound = 0;

            // Phase 1: Bind main stories to key platforms first
            foreach (var (session, boundStory) in mainStories)
            {
                var keyPlatformIndex = FindKeyPlatformIndex(scenarioData, boundPlatformIndices);
                if (keyPlatformIndex >= 0)
                {
                    var platform = scenarioData.RequiredPlatforms[keyPlatformIndex];
                    EnsureStoryData(platform);
                    BindStoryToPlatform(boundStory, platform.StoryData, session.SessionId);
                    boundPlatformIndices.Add(keyPlatformIndex);
                    mainStoriesBound++;
                    Debug.Log($"[StoryPlatformDataBinder] Main story '{boundStory.Template.DisplayName}' bound to key platform {keyPlatformIndex}");
                }
                else
                {
                    // Fallback: bind to first available NPC platform
                    var npcPlatformIndex = FindNpcPlatformIndex(scenarioData, boundPlatformIndices, boundStory);
                    if (npcPlatformIndex >= 0)
                    {
                        var platform = scenarioData.RequiredPlatforms[npcPlatformIndex];
                        EnsureStoryData(platform);
                        BindStoryToPlatform(boundStory, platform.StoryData, session.SessionId);
                        boundPlatformIndices.Add(npcPlatformIndex);
                        mainStoriesBound++;
                        Debug.Log($"[StoryPlatformDataBinder] Main story '{boundStory.Template.DisplayName}' bound to NPC platform {npcPlatformIndex} (no key platform available)");
                    }
                    else
                    {
                        Debug.LogWarning($"[StoryPlatformDataBinder] Unmatched main story: '{boundStory.Template.DisplayName}' (SessionId: {session.SessionId})");
                    }
                }
            }

            // Phase 2: Bind side stories to remaining NPC platforms
            foreach (var (session, boundStory) in sideStories)
            {
                // Try to match by NPC ID first
                var matchedIndex = FindPlatformByNpcId(scenarioData, boundPlatformIndices, boundStory);
                if (matchedIndex >= 0)
                {
                    var platform = scenarioData.RequiredPlatforms[matchedIndex];
                    EnsureStoryData(platform);
                    BindStoryToPlatform(boundStory, platform.StoryData, session.SessionId);
                    boundPlatformIndices.Add(matchedIndex);
                    sideStoriesBound++;
                    Debug.Log($"[StoryPlatformDataBinder] Side story '{boundStory.Template.DisplayName}' bound to platform {matchedIndex} (NPC match)");
                    continue;
                }

                // Try to match by side story ID
                matchedIndex = FindPlatformBySideStoryId(scenarioData, boundPlatformIndices, boundStory);
                if (matchedIndex >= 0)
                {
                    var platform = scenarioData.RequiredPlatforms[matchedIndex];
                    EnsureStoryData(platform);
                    BindStoryToPlatform(boundStory, platform.StoryData, session.SessionId);
                    boundPlatformIndices.Add(matchedIndex);
                    sideStoriesBound++;
                    Debug.Log($"[StoryPlatformDataBinder] Side story '{boundStory.Template.DisplayName}' bound to platform {matchedIndex} (SideStoryId match)");
                    continue;
                }

                // Fallback: bind to any available NPC platform
                var npcPlatformIndex = FindNpcPlatformIndex(scenarioData, boundPlatformIndices, boundStory);
                if (npcPlatformIndex >= 0)
                {
                    var platform = scenarioData.RequiredPlatforms[npcPlatformIndex];
                    EnsureStoryData(platform);
                    BindStoryToPlatform(boundStory, platform.StoryData, session.SessionId);
                    boundPlatformIndices.Add(npcPlatformIndex);
                    sideStoriesBound++;
                    Debug.Log($"[StoryPlatformDataBinder] Side story '{boundStory.Template.DisplayName}' bound to platform {npcPlatformIndex} (fallback)");
                }
                else
                {
                    Debug.LogWarning($"[StoryPlatformDataBinder] Unmatched side story: '{boundStory.Template.DisplayName}' (SessionId: {session.SessionId})");
                }
            }

            // Log summary
            int unboundMain = mainStories.Count - mainStoriesBound;
            int unboundSide = sideStories.Count - sideStoriesBound;
            Debug.Log($"[StoryPlatformDataBinder] Binding complete: " +
                     $"{mainStoriesBound}/{mainStories.Count} main, {sideStoriesBound}/{sideStories.Count} side. " +
                     $"Unbound: {unboundMain} main, {unboundSide} side");

            // Log unbound NPC platforms for debugging
            LogUnboundNpcPlatforms(scenarioData, boundPlatformIndices);
        }

        public void BindStoryToPlatform(BoundStory boundStory, StoryPlatformData platformData, string sessionId = null)
        {
            if (platformData == null)
            {
                Debug.LogWarning("[StoryPlatformDataBinder] Cannot bind: platform data is null");
                return;
            }

            platformData.GeneratedBoundStory = boundStory;
            platformData.SessionId = sessionId ?? Guid.NewGuid().ToString("N").Substring(0, 8);

            // Update platform data with bound story information
            if (boundStory != null)
            {
                // Set NPC ID from primary bound NPC if not already set
                if (string.IsNullOrEmpty(platformData.NpcId) &&
                    boundStory.BoundNpcs != null &&
                    boundStory.BoundNpcs.Count > 0)
                {
                    platformData.NpcId = boundStory.BoundNpcs[0].NpcId;
                }

                // Set dialogue knot from template if not already set
                if (string.IsNullOrEmpty(platformData.DialogueKnot) &&
                    boundStory.Template != null)
                {
                    platformData.DialogueKnot = boundStory.Template.StartingKnot;
                }

                // Mark as key node if this is a main story
                if (boundStory.Template?.StoryType == StoryType.Chapter)
                {
                    platformData.IsKeyNode = true;
                }

                Debug.Log($"[StoryPlatformDataBinder] Bound story '{boundStory.Template?.DisplayName}' " +
                         $"to platform (NpcId: {platformData.NpcId}, SessionId: {platformData.SessionId}, " +
                         $"Type: {boundStory.Template?.StoryType})");
            }

            OnStoryBound?.Invoke(platformData, boundStory);
        }

        /// <summary>
        /// Finds the first key platform that hasn't been bound yet.
        /// </summary>
        private int FindKeyPlatformIndex(ScenarioData scenarioData, HashSet<int> boundIndices)
        {
            for (int i = 0; i < scenarioData.RequiredPlatforms.Count; i++)
            {
                if (boundIndices.Contains(i))
                    continue;

                var platform = scenarioData.RequiredPlatforms[i];
                if (platform.StoryData?.IsKeyNode == true)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Finds an NPC platform that matches the bound story's primary NPC.
        /// </summary>
        private int FindPlatformByNpcId(ScenarioData scenarioData, HashSet<int> boundIndices, BoundStory boundStory)
        {
            if (boundStory?.BoundNpcs == null || boundStory.BoundNpcs.Count == 0)
                return -1;

            string primaryNpcId = boundStory.BoundNpcs[0].NpcId;
            if (string.IsNullOrEmpty(primaryNpcId))
                return -1;

            for (int i = 0; i < scenarioData.RequiredPlatforms.Count; i++)
            {
                if (boundIndices.Contains(i))
                    continue;

                var platform = scenarioData.RequiredPlatforms[i];
                if (platform.StoryData?.NpcId == primaryNpcId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Finds a platform that matches the story's side story ID.
        /// </summary>
        private int FindPlatformBySideStoryId(ScenarioData scenarioData, HashSet<int> boundIndices, BoundStory boundStory)
        {
            if (boundStory?.Template == null)
                return -1;

            string storyId = boundStory.Template.StoryId;
            if (string.IsNullOrEmpty(storyId))
                return -1;

            for (int i = 0; i < scenarioData.RequiredPlatforms.Count; i++)
            {
                if (boundIndices.Contains(i))
                    continue;

                var platform = scenarioData.RequiredPlatforms[i];
                if (platform.StoryData?.SideStoryId == storyId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Finds any NPC platform that hasn't been bound yet.
        /// Prefers platforms that match content type.
        /// </summary>
        private int FindNpcPlatformIndex(ScenarioData scenarioData, HashSet<int> boundIndices, BoundStory boundStory)
        {
            // First pass: look for platforms with NPC content type
            for (int i = 0; i < scenarioData.RequiredPlatforms.Count; i++)
            {
                if (boundIndices.Contains(i))
                    continue;

                var platform = scenarioData.RequiredPlatforms[i];
                if (platform.ContentTypes != null && platform.ContentTypes.Contains(PlatformContentType.Npc))
                {
                    return i;
                }
            }

            // Second pass: look for any platform with StoryData
            for (int i = 0; i < scenarioData.RequiredPlatforms.Count; i++)
            {
                if (boundIndices.Contains(i))
                    continue;

                var platform = scenarioData.RequiredPlatforms[i];
                if (platform.StoryData != null)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Ensures a platform has StoryData initialized.
        /// </summary>
        private void EnsureStoryData(PlatformRequirement platform)
        {
            if (platform.StoryData == null)
            {
                platform.StoryData = new StoryPlatformData();
            }
        }

        /// <summary>
        /// Logs any NPC platforms that didn't receive a story binding.
        /// </summary>
        private void LogUnboundNpcPlatforms(ScenarioData scenarioData, HashSet<int> boundIndices)
        {
            var unboundNpcPlatforms = new List<int>();

            for (int i = 0; i < scenarioData.RequiredPlatforms.Count; i++)
            {
                if (boundIndices.Contains(i))
                    continue;

                var platform = scenarioData.RequiredPlatforms[i];
                if (platform.ContentTypes != null && platform.ContentTypes.Contains(PlatformContentType.Npc))
                {
                    unboundNpcPlatforms.Add(i);
                }
            }

            if (unboundNpcPlatforms.Count > 0)
            {
                Debug.Log($"[StoryPlatformDataBinder] {unboundNpcPlatforms.Count} NPC platform(s) without bound stories: [{string.Join(", ", unboundNpcPlatforms)}]");
            }
        }
    }
}
