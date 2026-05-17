using System;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Default implementation of story policy provider.
    /// Provides predefined policies for Main Story and Side Story types
    /// based on NARRATIVE.md specification (Sections 4, 8.3-8.5, 8.9).
    /// Pure C# class - no Unity dependencies.
    /// </summary>
    public class StoryPolicyProvider : IStoryPolicyProvider
    {
        private readonly StoryPolicy _mainStoryPolicy;
        private readonly StoryPolicy _sideStoryPolicy;
        private readonly StoryPolicy _dialoguePolicy;
        private readonly StoryPolicy _eventPolicy;

        /// <summary>
        /// Creates a new story policy provider with default policies.
        /// </summary>
        public StoryPolicyProvider()
        {
            _mainStoryPolicy = CreateMainStoryPolicy();
            _sideStoryPolicy = CreateSideStoryPolicy();
            _dialoguePolicy = CreateDialoguePolicy();
            _eventPolicy = CreateEventPolicy();
        }

        public StoryPolicy MainStoryPolicy => _mainStoryPolicy;
        public StoryPolicy SideStoryPolicy => _sideStoryPolicy;

        public StoryPolicy GetPolicy(StoryType storyType)
        {
            return storyType switch
            {
                StoryType.Chapter => _mainStoryPolicy,
                StoryType.SideStory => _sideStoryPolicy,
                StoryType.Dialogue => _dialoguePolicy,
                StoryType.Event => _eventPolicy,
                _ => _sideStoryPolicy
            };
        }

        public int CalculateSideStoryCount(int totalPlatforms)
        {
            // Formula from NARRATIVE.md Section 8.3:
            // sideStoryCount = max(0, (TotalPlatforms / 3) - 1)
            //
            // Results:
            // | TotalPlatforms | Side Stories |
            // |----------------|--------------|
            // | 1-5            | 0            |
            // | 6-8            | 1            |
            // | 9-11           | 2            |
            // | 12+            | 3+           |
            return Math.Max(0, (totalPlatforms / 3) - 1);
        }

        /// <summary>
        /// Creates the main story (Chapter) policy.
        /// Main stories provide the "narrative spine" of the game.
        /// </summary>
        private static StoryPolicy CreateMainStoryPolicy()
        {
            return new StoryPolicy(
                basePriorityBonus: 25,           // Higher base priority in scoring
                keyProgressionBonus: 20,         // +20 if IsKeyProgression == true
                selectionOrder: 1,               // Selected FIRST before side stories
                hasPrimaryNpcAccess: true,       // Primary reservation - first pick of NPCs
                requiredCountPerArea: 1,         // Exactly 1 per area (mandatory)
                isFailureCritical: true,         // Critical failure
                continueOnFailure: false,        // Generation may halt on failure
                failureLogLevel: FailureLogLevel.Critical, // Critical warning logged
                cooldownMultiplier: 1.5f,        // Longer cooldowns (multiplied)
                typicallyRequiresTheme: true,    // May require specific theme
                hasStrictChapterConstraints: true, // Often has strict chapter range
                varietyBonus: 15,                // Bonus for not recently played
                themeMatchBonus: 10              // Bonus for theme match
            );
        }

        /// <summary>
        /// Creates the side story policy.
        /// Side stories are optional, procedural content for variety.
        /// </summary>
        private static StoryPolicy CreateSideStoryPolicy()
        {
            return new StoryPolicy(
                basePriorityBonus: 0,            // Lower base priority
                keyProgressionBonus: 0,          // No key progression bonus (rarely flagged)
                selectionOrder: 2,               // Selected AFTER main story
                hasPrimaryNpcAccess: false,      // Secondary - uses remaining pool
                requiredCountPerArea: 0,         // 0+ based on platform count
                isFailureCritical: false,        // Non-critical failure
                continueOnFailure: true,         // Generation continues on failure
                failureLogLevel: FailureLogLevel.Warning, // Warning logged
                cooldownMultiplier: 1.0f,        // Standard cooldowns
                typicallyRequiresTheme: false,   // Usually theme-flexible
                hasStrictChapterConstraints: false, // Usually flexible across chapters
                varietyBonus: 15,                // Same variety bonus
                themeMatchBonus: 10              // Same theme match bonus
            );
        }

        /// <summary>
        /// Creates the dialogue story policy.
        /// Dialogues are NPC conversations without quest objectives.
        /// </summary>
        private static StoryPolicy CreateDialoguePolicy()
        {
            return new StoryPolicy(
                basePriorityBonus: 0,
                keyProgressionBonus: 0,
                selectionOrder: 3,               // Selected after main and side stories
                hasPrimaryNpcAccess: false,
                requiredCountPerArea: 0,
                isFailureCritical: false,
                continueOnFailure: true,
                failureLogLevel: FailureLogLevel.Warning,
                cooldownMultiplier: 0.5f,        // Shorter cooldowns
                typicallyRequiresTheme: false,
                hasStrictChapterConstraints: false,
                varietyBonus: 10,
                themeMatchBonus: 5
            );
        }

        /// <summary>
        /// Creates the event story policy.
        /// Events are one-off narrative moments.
        /// </summary>
        private static StoryPolicy CreateEventPolicy()
        {
            return new StoryPolicy(
                basePriorityBonus: 5,
                keyProgressionBonus: 10,
                selectionOrder: 2,               // Same priority as side stories
                hasPrimaryNpcAccess: false,
                requiredCountPerArea: 0,
                isFailureCritical: false,
                continueOnFailure: true,
                failureLogLevel: FailureLogLevel.Warning,
                cooldownMultiplier: 2.0f,        // Events have longer cooldowns
                typicallyRequiresTheme: false,
                hasStrictChapterConstraints: false,
                varietyBonus: 20,                // Higher variety bonus (one-time feel)
                themeMatchBonus: 10
            );
        }
    }
}
