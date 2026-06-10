using System;
using System.Collections.Generic;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Generation
{
    /// <summary>
    /// Picks stories and NPCs for a level based on density and filter configuration.
    /// Assigns one NPC per story (combat stories are matched first, since they
    /// require combat-capable NPCs), then adds extra character-only NPCs.
    /// </summary>
    public class LevelNarrativeGenerator : ILevelNarrativeGenerator
    {
        private readonly IStoryPool _storyPool;
        private readonly INpcPool _npcPool;
        private readonly IRewardResolver _rewardResolver;
        private readonly System.Random _random;

        public LevelNarrativeGenerator(
            IStoryPool storyPool,
            INpcPool npcPool,
            IRewardResolver rewardResolver)
            : this(storyPool, npcPool, rewardResolver, new System.Random()) { }

        public LevelNarrativeGenerator(
            IStoryPool storyPool,
            INpcPool npcPool,
            IRewardResolver rewardResolver,
            System.Random random)
        {
            _storyPool = storyPool ?? throw new ArgumentNullException(nameof(storyPool));
            _npcPool = npcPool ?? throw new ArgumentNullException(nameof(npcPool));
            _rewardResolver = rewardResolver ?? throw new ArgumentNullException(nameof(rewardResolver));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public LevelNarrative Generate(LevelNarrativeConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _npcPool.ResetAssignments();

            var assignments = new List<NpcAssignment>();
            int storyAssignmentCount = 0;

            // Step 1: Filter stories
            var filteredStories = _storyPool.Filter(
                config.Theme,
                config.Difficulty,
                config.RequiredTags,
                config.ExcludedTags);

            // Step 2: Pick N stories randomly (within density bounds)
            int storyCount = PickCount(config.MinStories, config.MaxStories, filteredStories.Count);
            var selectedStories = PickRandom(filteredStories, storyCount);

            // Step 3: Assign one NPC per story. Combat stories draw from a strict
            // subset of NPCs (combat-capable only), so they are matched first;
            // otherwise a non-combat story can consume the last combat-capable NPC
            // and orphan a combat story.
            var npcByStoryIndex = new NpcDefinition[selectedStories.Count];
            AssignNpcsForPass(selectedStories, config, npcByStoryIndex, combatPass: true);
            AssignNpcsForPass(selectedStories, config, npcByStoryIndex, combatPass: false);

            for (int i = 0; i < selectedStories.Count; i++)
            {
                if (npcByStoryIndex[i] == null)
                    continue;

                var story = selectedStories[i];
                var rewards = _rewardResolver.Resolve(story);
                assignments.Add(new NpcAssignment(npcByStoryIndex[i], story, rewards));
                storyAssignmentCount++;

                _storyPool.RecordUsage(story.StoryId);
            }

            // Step 4: Add extra NPCs (character-only, no story)
            int totalNpcsTarget = PickCount(config.MinNpcs, config.MaxNpcs, _npcPool.Count);
            int extraNpcsNeeded = Math.Max(0, totalNpcsTarget - assignments.Count);

            if (extraNpcsNeeded > 0)
            {
                var remainingNpcs = _npcPool.Filter(config.RequiredTags, null);
                var extraNpcs = PickRandom(remainingNpcs, Math.Min(extraNpcsNeeded, remainingNpcs.Count));

                for (int i = 0; i < extraNpcs.Count; i++)
                {
                    _npcPool.MarkAssigned(extraNpcs[i].NpcId);
                    assignments.Add(NpcAssignment.CharacterOnly(extraNpcs[i]));
                }
            }

            Debug.Log($"[LevelNarrativeGenerator] Generated {assignments.Count} assignments " +
                      $"({storyAssignmentCount} with stories, {assignments.Count - storyAssignmentCount} character-only)");

            return new LevelNarrative(assignments);
        }

        private void AssignNpcsForPass(
            IReadOnlyList<StoryDefinition> stories,
            LevelNarrativeConfig config,
            NpcDefinition[] npcByStoryIndex,
            bool combatPass)
        {
            for (int i = 0; i < stories.Count; i++)
            {
                var story = stories[i];
                if (story.CanTransitionToCombat != combatPass)
                    continue;

                var availableNpcs = _npcPool.Filter(config.RequiredTags, null);

                // Combat stories require a combat-capable NPC; pairing any NPC would start
                // an enemy-less fight. This activates the CanTransitionToCombat config flag.
                var candidates = combatPass
                    ? FilterCombatCapableNpcs(availableNpcs)
                    : availableNpcs;

                if (candidates.Count == 0)
                {
                    Debug.LogWarning(combatPass
                        ? $"[LevelNarrativeGenerator] No combat-capable NPC available for combat story '{story.StoryId}'. Story skipped."
                        : $"[LevelNarrativeGenerator] No NPCs available for story '{story.StoryId}'");
                    continue;
                }

                var npc = candidates[_random.Next(candidates.Count)];
                _npcPool.MarkAssigned(npc.NpcId);
                npcByStoryIndex[i] = npc;
            }
        }

        private int PickCount(int min, int max, int available)
        {
            int upper = Math.Min(max, available);
            if (upper < min)
                return upper;

            return _random.Next(min, upper + 1);
        }

        private static IReadOnlyList<NpcDefinition> FilterCombatCapableNpcs(IReadOnlyList<NpcDefinition> npcs)
        {
            var result = new List<NpcDefinition>();
            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i].CanBecomeEnemy)
                    result.Add(npcs[i]);
            }
            return result;
        }

        private List<T> PickRandom<T>(IReadOnlyList<T> source, int count)
        {
            if (count <= 0 || source.Count == 0)
                return new List<T>();

            if (count >= source.Count)
                return new List<T>(source);

            // Fisher-Yates on indices
            var indices = new List<int>(source.Count);
            for (int i = 0; i < source.Count; i++)
                indices.Add(i);

            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            var result = new List<T>(count);
            for (int i = 0; i < count; i++)
                result.Add(source[indices[i]]);

            return result;
        }
    }
}
