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
    /// Per window it (1) filters stories to those eligible against the live store (R6/R7) per the
    /// eligibility rules below (world-only vs. actor-scoped casting query); (2) places combat-bearing stories until
    /// <see cref="RunPacingSettings.MinCombatPerWindow"/> is met; (3) fills the remaining platforms from
    /// any eligible story while the narrative weight stays within
    /// <see cref="RunPacingSettings.NarrativeBudgetPerWindow"/> and combat stays under
    /// <see cref="RunPacingSettings.MaxCombatPerWindow"/>; (4) pads to <see cref="RunPacingSettings.WindowSize"/>
    /// with empty fillers. Combat is its own budget dimension, independent of narrative weight.
    ///
    /// Selection prefers continuing a thread already chosen this window, then a seeded pick among ties so
    /// results are deterministic and save-replayable (B2).
    ///
    /// Eligibility resolves over **world/global *and* actor/faction-scoped facts** (D16). A story whose
    /// preconditions reference no context token (<c>$self</c>/<c>$faction</c>/…) is gated on world facts
    /// alone and gets a fresh actor minted for it. A story that *does* reference a context token is gated
    /// as a **casting query** (D11): a live actor (one already minted this run, <see cref="ILiveActorRegistry"/>)
    /// whose facts satisfy the precondition must exist, and that actor is **pinned** and recast into the
    /// story so per-actor facts carry the arc forward. Continuation semantics: a brand-new actor cannot
    /// satisfy a positive actor-scoped precondition, so such a story only opens once an eligible actor
    /// already exists. (Cross-window thread continuity, R8, is still a follow-up.)
    ///
    /// Actor↔story compatibility is otherwise a **soft preference**, not a hard filter (P1: hard
    /// requirements prune, preferences only weight): any archetype can play a world-only story, but one
    /// whose tags overlap the story's tags is preferred. A story is only pruned for actor reasons when
    /// there is no archetype at all. The D11 hard pin overrides this soft preference.
    /// </summary>
    public sealed class RunWindowPlanner : IRunWindowPlanner
    {
        // A precondition subject token starting with this char (e.g. $self/$faction) is context-resolved
        // against an actor casting, so it marks an actor/faction-scoped gate (matches SubjectResolver).
        private const char ContextTokenPrefix = '$';

        private readonly IReadOnlyList<StoryTemplateData> _stories;
        private readonly IReadOnlyList<NpcArchetypeData> _archetypes;
        private readonly IPreconditionEvaluator _evaluator;
        private readonly IActorInstanceFactory _actorFactory;
        private readonly ILiveActorRegistry _liveActors;
        private readonly IRandomSource _random;
        private readonly RunPacingSettings _settings;
        private readonly IGameLogger _logger;

        public RunWindowPlanner(
            IReadOnlyList<StoryTemplateData> stories,
            IReadOnlyList<NpcArchetypeData> archetypes,
            IPreconditionEvaluator evaluator,
            IActorInstanceFactory actorFactory,
            ILiveActorRegistry liveActors,
            IRandomSource random,
            RunPacingSettings settings,
            IGameLogger logger = null)
        {
            _stories = stories ?? System.Array.Empty<StoryTemplateData>();
            _archetypes = archetypes ?? System.Array.Empty<NpcArchetypeData>();
            _evaluator = evaluator;
            _actorFactory = actorFactory;
            _liveActors = liveActors ?? new LiveActorRegistry();
            _random = random;
            _settings = settings;
            _logger = logger;
        }

        /// <summary>An eligible story paired with the actor pinned for it (the recast actor for an
        /// actor-scoped story; null for a world-only story, which mints a fresh actor at placement).</summary>
        private readonly struct EligibleStory
        {
            public EligibleStory(StoryTemplateData story, NpcInstance pinnedActor)
            {
                Story = story;
                PinnedActor = pinnedActor;
            }

            public StoryTemplateData Story { get; }
            public NpcInstance PinnedActor { get; }
        }

        public WindowPlan PlanWindow(int windowIndex, IFactStore facts)
        {
            var platforms = new List<PlannedPlatform>(_settings.WindowSize);
            if (facts == null)
            {
                _logger?.Warning(LogCategory.Narrative,"[RunWindowPlanner] Null fact store - emitting an empty window.");
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

                Place(pick.Value, platforms, usedStoryIds, activeThreads, ref usedWeight, ref combatCount);
            }

            // Phase 2: fill the window from any eligible story within the budgets.
            while (platforms.Count < _settings.WindowSize)
            {
                var pick = ChooseStory(eligible, usedStoryIds, activeThreads, usedWeight, combatCount, requireCombat: false);
                if (pick == null)
                {
                    break;
                }

                Place(pick.Value, platforms, usedStoryIds, activeThreads, ref usedWeight, ref combatCount);
            }

            return PadAndBuild(windowIndex, platforms);
        }

        private List<EligibleStory> BuildEligible(IFactStore facts)
        {
            // Actor-less context for world-only stories: only world/global preconditions resolve.
            var emptyContext = new ContextBag();
            var eligible = new List<EligibleStory>();
            for (int i = 0; i < _stories.Count; i++)
            {
                var story = _stories[i];
                if (story == null)
                {
                    continue;
                }

                if (HasActorScopedPrecondition(story))
                {
                    // Actor-scoped gate (D16/D11): eligible only if a live actor's facts satisfy the
                    // precondition. That actor is pinned and recast (a hard pin overriding the P1 tag
                    // preference) so its facts carry the arc forward.
                    var pinned = FindSatisfyingLiveActor(story, facts);
                    if (pinned != null)
                    {
                        eligible.Add(new EligibleStory(story, pinned));
                    }

                    continue;
                }

                // World-only gate: resolve against an actor-less context (only world/global facts), then
                // mint a fresh actor at placement. Actor matching is a soft preference (P1), not a hard
                // filter: only prune when there is no archetype at all to play the story.
                if (!_evaluator.EvaluateAll(story.Preconditions, facts, emptyContext))
                {
                    continue;
                }

                if (_archetypes.Count == 0)
                {
                    continue;
                }

                eligible.Add(new EligibleStory(story, null));
            }

            eligible.Sort((a, b) => string.CompareOrdinal(a.Story.StoryId, b.Story.StoryId));
            return eligible;
        }

        /// <summary>
        /// True when any precondition references a context token (a <c>$</c>-prefixed subject such as
        /// <c>$self</c>/<c>$faction</c>) — i.e. the story is gated on an actor's (or its faction's) facts
        /// and must be resolved as a casting query rather than a world-only fact lookup.
        /// </summary>
        private static bool HasActorScopedPrecondition(StoryTemplateData story)
        {
            var preconditions = story.Preconditions;
            for (int i = 0; i < preconditions.Count; i++)
            {
                var token = preconditions[i].SubjectToken;
                if (!string.IsNullOrEmpty(token) && token[0] == ContextTokenPrefix)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The casting query (D11): the first live actor (registration order, deterministic) whose facts
        /// satisfy the story's preconditions when bound as <c>$self</c>/<c>$faction</c>. World predicates
        /// in the same set still resolve via their empty subject regardless of the bound actor, so mixed
        /// preconditions work. Returns null when no live actor qualifies.
        /// </summary>
        private NpcInstance FindSatisfyingLiveActor(StoryTemplateData story, IFactStore facts)
        {
            var liveActors = _liveActors.LiveActors;
            for (int i = 0; i < liveActors.Count; i++)
            {
                var actor = liveActors[i];
                if (actor == null)
                {
                    continue;
                }

                var context = new ContextBag()
                    .BindSubject("$self", actor.InstanceId)
                    .BindSubject("$faction", actor.FactionId);
                if (_evaluator.EvaluateAll(story.Preconditions, facts, context))
                {
                    return actor;
                }
            }

            return null;
        }

        private EligibleStory? ChooseStory(IReadOnlyList<EligibleStory> eligible, HashSet<string> usedStoryIds,
            HashSet<string> activeThreads, int usedWeight, int combatCount, bool requireCombat)
        {
            var candidates = new List<EligibleStory>();
            for (int i = 0; i < eligible.Count; i++)
            {
                var entry = eligible[i];
                var story = entry.Story;
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

                candidates.Add(entry);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // Prefer continuing a thread already started this window for coherence; else any candidate.
            var preferred = new List<EligibleStory>();
            for (int i = 0; i < candidates.Count; i++)
            {
                var thread = candidates[i].Story.ThreadId;
                if (!string.IsNullOrEmpty(thread) && activeThreads.Contains(thread))
                {
                    preferred.Add(candidates[i]);
                }
            }

            var pool = preferred.Count > 0 ? preferred : candidates;
            return pool[_random.NextInt(pool.Count)];
        }

        private void Place(EligibleStory entry, List<PlannedPlatform> platforms, HashSet<string> usedStoryIds,
            HashSet<string> activeThreads, ref int usedWeight, ref int combatCount)
        {
            var story = entry.Story;

            // D11 hard pin: a recast actor bypasses the soft P1 archetype preference. A world-only story
            // mints a fresh actor and records it as live so it can be recast later.
            var actor = entry.PinnedActor;
            if (actor == null)
            {
                actor = _actorFactory.Create(MatchArchetype(story));
                _liveActors.Register(actor);
            }

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
