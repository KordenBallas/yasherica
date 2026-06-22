using System.Collections.Generic;
using Core.Logging;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Default <see cref="IRunWindowPlanner"/>: a story-first, budgeted window planner (P2).
    ///
    /// Per window it (1) filters stories whose preconditions pass over the live store (R6/R7) and that
    /// have a matching actor archetype; (2) places combat-bearing stories until
    /// <see cref="RunPacingSettings.MinCombatPerWindow"/> is met; (3) fills the remaining platforms from
    /// any eligible story while the narrative weight stays within
    /// <see cref="RunPacingSettings.NarrativeBudgetPerWindow"/> and combat stays under
    /// <see cref="RunPacingSettings.MaxCombatPerWindow"/>; (4) pads to <see cref="RunPacingSettings.WindowSize"/>
    /// with empty fillers. Combat is its own budget dimension, independent of narrative weight.
    ///
    /// Selection prefers continuing a thread already chosen this window, then a seeded pick among ties so
    /// results are deterministic and save-replayable (B2). Eligibility uses an actor-less context, so
    /// only world/global preconditions resolve at plan time; actor/faction-scoped gating (and cross-window
    /// thread continuity, R8) are follow-ups.
    ///
    /// Actor↔story compatibility is a **soft preference**, not a hard filter (P1: hard requirements prune,
    /// preferences only weight): any archetype can play any story, but one whose tags overlap the story's
    /// tags is preferred. A story is only pruned for actor reasons when there is no archetype at all.
    /// </summary>
    public sealed class RunWindowPlanner : IRunWindowPlanner
    {
        private readonly IReadOnlyList<StoryTemplateData> _stories;
        private readonly IReadOnlyList<NpcArchetypeData> _archetypes;
        private readonly IPreconditionEvaluator _evaluator;
        private readonly IActorInstanceFactory _actorFactory;
        private readonly IRandomSource _random;
        private readonly RunPacingSettings _settings;
        private readonly IGameLogger _logger;

        public RunWindowPlanner(
            IReadOnlyList<StoryTemplateData> stories,
            IReadOnlyList<NpcArchetypeData> archetypes,
            IPreconditionEvaluator evaluator,
            IActorInstanceFactory actorFactory,
            IRandomSource random,
            RunPacingSettings settings,
            IGameLogger logger = null)
        {
            _stories = stories ?? System.Array.Empty<StoryTemplateData>();
            _archetypes = archetypes ?? System.Array.Empty<NpcArchetypeData>();
            _evaluator = evaluator;
            _actorFactory = actorFactory;
            _random = random;
            _settings = settings;
            _logger = logger;
        }

        public WindowPlan PlanWindow(int windowIndex, IFactStore facts)
        {
            var platforms = new List<PlannedPlatform>(_settings.WindowSize);
            if (facts == null)
            {
                _logger?.Warning("[RunWindowPlanner] Null fact store - emitting an empty window.");
                return PadAndBuild(windowIndex, platforms);
            }

            var eligible = BuildEligible(facts);
            var usedStoryIds = new HashSet<string>();
            var activeThreads = new HashSet<string>();
            int usedWeight = 0;
            int combatCount = 0;

            // Phase 1: satisfy the minimum combat budget first.
            while (combatCount < _settings.MinCombatPerWindow && platforms.Count < _settings.WindowSize)
            {
                var pick = ChooseStory(eligible, usedStoryIds, activeThreads, usedWeight, combatCount, requireCombat: true);
                if (pick == null)
                {
                    break;
                }

                Place(pick, platforms, usedStoryIds, activeThreads, ref usedWeight, ref combatCount);
            }

            // Phase 2: fill the window from any eligible story within the budgets.
            while (platforms.Count < _settings.WindowSize)
            {
                var pick = ChooseStory(eligible, usedStoryIds, activeThreads, usedWeight, combatCount, requireCombat: false);
                if (pick == null)
                {
                    break;
                }

                Place(pick, platforms, usedStoryIds, activeThreads, ref usedWeight, ref combatCount);
            }

            return PadAndBuild(windowIndex, platforms);
        }

        private List<StoryTemplateData> BuildEligible(IFactStore facts)
        {
            // Actor-less context: world/global preconditions resolve; $self/$faction-scoped ones fail
            // closed (the actor is matched after the story is chosen, R5).
            var context = new ContextBag();
            var eligible = new List<StoryTemplateData>();
            for (int i = 0; i < _stories.Count; i++)
            {
                var story = _stories[i];
                if (story == null)
                {
                    continue;
                }

                if (!_evaluator.EvaluateAll(story.Preconditions, facts, context))
                {
                    continue;
                }

                // Actor matching is a soft preference (P1), not a hard filter: only prune when there is
                // no archetype at all to play the story.
                if (_archetypes.Count == 0)
                {
                    continue;
                }

                eligible.Add(story);
            }

            eligible.Sort((a, b) => string.CompareOrdinal(a.StoryId, b.StoryId));
            return eligible;
        }

        private StoryTemplateData ChooseStory(IReadOnlyList<StoryTemplateData> eligible, HashSet<string> usedStoryIds,
            HashSet<string> activeThreads, int usedWeight, int combatCount, bool requireCombat)
        {
            var candidates = new List<StoryTemplateData>();
            for (int i = 0; i < eligible.Count; i++)
            {
                var story = eligible[i];
                if (usedStoryIds.Contains(story.StoryId))
                {
                    continue;
                }

                if (usedWeight + story.Weight > _settings.NarrativeBudgetPerWindow)
                {
                    continue;
                }

                bool isCombat = IsCombatBearing(story);
                if (requireCombat && !isCombat)
                {
                    continue;
                }

                if (isCombat && combatCount >= _settings.MaxCombatPerWindow)
                {
                    continue;
                }

                candidates.Add(story);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // Prefer continuing a thread already started this window for coherence; else any candidate.
            var preferred = new List<StoryTemplateData>();
            for (int i = 0; i < candidates.Count; i++)
            {
                var thread = candidates[i].ThreadId;
                if (!string.IsNullOrEmpty(thread) && activeThreads.Contains(thread))
                {
                    preferred.Add(candidates[i]);
                }
            }

            var pool = preferred.Count > 0 ? preferred : candidates;
            return pool[_random.NextInt(pool.Count)];
        }

        private void Place(StoryTemplateData story, List<PlannedPlatform> platforms, HashSet<string> usedStoryIds,
            HashSet<string> activeThreads, ref int usedWeight, ref int combatCount)
        {
            var archetype = MatchArchetype(story);
            var actor = _actorFactory.Create(archetype);
            bool isCombat = IsCombatBearing(story);

            platforms.Add(PlannedPlatform.StoryEncounter(story, actor, isCombat));
            usedStoryIds.Add(story.StoryId);
            usedWeight += story.Weight;
            if (isCombat)
            {
                combatCount++;
            }

            if (!string.IsNullOrEmpty(story.ThreadId))
            {
                activeThreads.Add(story.ThreadId);
            }
        }

        private WindowPlan PadAndBuild(int windowIndex, List<PlannedPlatform> platforms)
        {
            while (platforms.Count < _settings.WindowSize)
            {
                platforms.Add(PlannedPlatform.EmptyFiller());
            }

            return new WindowPlan(windowIndex, platforms);
        }

        private static bool IsCombatBearing(StoryTemplateData story)
        {
            for (int i = 0; i < story.Slots.Count; i++)
            {
                if (story.Slots[i].Kind == SlotKind.Combat)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Chooses an archetype for the story: prefers one whose tags overlap the story's tags (a minimal
        /// P1 compatibility preference), and falls back to any archetype when none overlap. Among equal
        /// candidates a seeded pick keeps selection deterministic. Returns null only when no archetype
        /// exists at all.
        /// </summary>
        private NpcArchetypeData MatchArchetype(StoryTemplateData story)
        {
            var preferred = new List<NpcArchetypeData>();
            var all = new List<NpcArchetypeData>();
            for (int i = 0; i < _archetypes.Count; i++)
            {
                var archetype = _archetypes[i];
                if (archetype == null)
                {
                    continue;
                }

                all.Add(archetype);
                if (ArchetypeMatches(archetype, story))
                {
                    preferred.Add(archetype);
                }
            }

            var pool = preferred.Count > 0 ? preferred : all;
            if (pool.Count == 0)
            {
                return null;
            }

            pool.Sort((a, b) => string.CompareOrdinal(a.ArchetypeId, b.ArchetypeId));
            return pool[_random.NextInt(pool.Count)];
        }

        private static bool ArchetypeMatches(NpcArchetypeData archetype, StoryTemplateData story)
        {
            if (archetype == null)
            {
                return false;
            }

            // A story with no tags accepts any archetype; otherwise require at least one shared tag.
            if (story.StoryTags.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < archetype.ArchetypeTags.Count; i++)
            {
                var tag = archetype.ArchetypeTags[i];
                for (int j = 0; j < story.StoryTags.Count; j++)
                {
                    if (string.Equals(tag, story.StoryTags[j], System.StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
