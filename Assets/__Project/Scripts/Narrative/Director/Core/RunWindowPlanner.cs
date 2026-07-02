using System.Collections.Generic;
using Core.Logging;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Default <see cref="IRunWindowPlanner"/>: a density-first window planner (the
    /// world-content-density brief). Per window it (1) filters stories to those eligible against the
    /// live store (R6/R7) per the eligibility rules below (world-only vs. actor-scoped casting query);
    /// (2) asks the <see cref="WorldContentAllocator"/> for each slot's content kind — Empty/traversal
    /// (the majority), simple Loot, ambient Combat from the biome pool, or a rare, spaced Quest slot;
    /// (3) fills only the Quest slots from the eligible stories. There is no narrative weight budget and
    /// no combat quota: quests are governed by rarity + minimum spacing, ambient monsters are the main
    /// combat source, and a story that happens to carry a combat slot is the tolerated exception.
    ///
    /// Story selection prefers continuing a thread already chosen this window, then a seeded pick among
    /// ties so results are deterministic and save-replayable (B2).
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
        private readonly WorldContentAllocator _allocator;
        private readonly IGameLogger _logger;

        public RunWindowPlanner(
            IReadOnlyList<StoryTemplateData> stories,
            IReadOnlyList<NpcArchetypeData> archetypes,
            IPreconditionEvaluator evaluator,
            IActorInstanceFactory actorFactory,
            ILiveActorRegistry liveActors,
            IRandomSource random,
            RunPacingSettings settings,
            WorldContentAllocator allocator,
            IGameLogger logger = null)
        {
            _stories = stories ?? System.Array.Empty<StoryTemplateData>();
            _archetypes = archetypes ?? System.Array.Empty<NpcArchetypeData>();
            _evaluator = evaluator;
            _actorFactory = actorFactory;
            _liveActors = liveActors ?? new LiveActorRegistry();
            _random = random;
            _settings = settings;
            _allocator = allocator;
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

            for (int slot = 0; slot < _settings.WindowSize; slot++)
            {
                // The allocator gets the live availability so a quest slot that cannot be filled (story
                // pool exhausted this window) degrades into the ambient draw instead of a dead platform,
                // and the spacing counter keeps running.
                var allocation = _allocator.AllocateSlot(HasUnusedEligible(eligible, usedStoryIds));
                switch (allocation.Kind)
                {
                    case WorldSlotKind.Quest:
                        var pick = ChooseStory(eligible, usedStoryIds, activeThreads);
                        if (pick == null)
                        {
                            // questAvailable was true, so this is unreachable; guard for safety.
                            platforms.Add(PlannedPlatform.EmptyFiller());
                            break;
                        }

                        Place(pick.Value, platforms, usedStoryIds, activeThreads);
                        break;

                    case WorldSlotKind.Combat:
                        platforms.Add(PlannedPlatform.AmbientCombat(allocation.EnemyId));
                        break;

                    case WorldSlotKind.Loot:
                        platforms.Add(PlannedPlatform.LootDrop());
                        break;

                    default:
                        platforms.Add(PlannedPlatform.EmptyFiller());
                        break;
                }
            }

            return PadAndBuild(windowIndex, platforms);
        }

        private static bool HasUnusedEligible(IReadOnlyList<EligibleStory> eligible, HashSet<string> usedStoryIds)
        {
            for (int i = 0; i < eligible.Count; i++)
            {
                if (!usedStoryIds.Contains(eligible[i].Story.StoryId))
                {
                    return true;
                }
            }

            return false;
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
            HashSet<string> activeThreads)
        {
            var candidates = new List<EligibleStory>();
            for (int i = 0; i < eligible.Count; i++)
            {
                var entry = eligible[i];
                if (usedStoryIds.Contains(entry.Story.StoryId))
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
            HashSet<string> activeThreads)
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

            platforms.Add(PlannedPlatform.StoryEncounter(story, actor, IsCombatBearing(story)));
            usedStoryIds.Add(story.StoryId);

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
