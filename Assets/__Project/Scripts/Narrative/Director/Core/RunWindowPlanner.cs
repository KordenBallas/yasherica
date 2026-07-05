using System.Collections.Generic;
using Core.Logging;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Facts.Core;
using Narrative.Stories.Core;
using Narrative.Threads.Core;
using World.Sites.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Default <see cref="IRunWindowPlanner"/>: a density-first window planner (the
    /// world-content-density brief). Per window it (1) filters stories to those eligible against the
    /// live store (R6/R7) per the eligibility rules below (world-only vs. actor-scoped casting query);
    /// (2) asks the <see cref="IWorldSlotAllocator"/> for each slot's content kind — Empty/traversal
    /// (the majority), simple Loot, ambient Combat from the biome pool, a rare, spaced Quest slot, or a
    /// site Npc fill; (3) fills the Quest slots from the quest-eligible stories and the Npc slots from
    /// flavor-tagged ambient-colour stories (world-sites brief). A landed quest may pull a settlement
    /// block into being via <see cref="IWorldSlotAllocator.TryReserveSettlement"/>. There is no
    /// narrative weight budget and no combat quota: quests are governed by rarity + minimum spacing,
    /// ambient monsters are the main combat source, and a story that happens to carry a combat slot is
    /// the tolerated exception.
    ///
    /// Threads are first-class (R8/D13/D14): the window starts with the <see cref="IThreadMaintenance"/>
    /// tick (retiring expired/conflicted threads), quest-channel stories already placed or resolved this
    /// run — or belonging to a retired thread — never re-enter a plan (FR9), a story that would *open* a
    /// new thread is held while the live-thread ceiling is reached (FR7; the slot degrades into the
    /// ambient draw — the planner waits, it never force-drops), and story selection prefers advancing a
    /// thread that is live in the <see cref="IThreadLedger"/> over opening a new one (FR3), then a seeded
    /// pick among ties so results are deterministic and save-replayable (B2). Ambient-colour chatter
    /// stays outside the run ledger by design — a repeating chatter pool is not a story beat.
    ///
    /// Eligibility resolves over **world/global *and* actor/faction-scoped facts** (D16). A story whose
    /// preconditions reference no context token (<c>$self</c>/<c>$faction</c>/…) is gated on world facts
    /// alone and gets a fresh actor minted for it. A story that *does* reference a context token is gated
    /// as a **casting query** (D11): a live actor (one already minted this run, <see cref="ILiveActorRegistry"/>)
    /// whose facts satisfy the precondition must exist, and that actor is **pinned** and recast into the
    /// story so per-actor facts carry the arc forward. Continuation semantics: a brand-new actor cannot
    /// satisfy a positive actor-scoped precondition, so such a story only opens once an eligible actor
    /// already exists.
    ///
    /// Actor↔story compatibility is otherwise a **soft preference**, not a hard filter (P1: hard
    /// requirements prune, preferences only weight): any archetype can play a world-only story, but one
    /// whose tags overlap the story's tags is preferred. A story is only pruned for actor reasons when
    /// there is no archetype at all. The D11 hard pin overrides this soft preference.
    ///
    /// Causal order across windows (FR8) needs no extra hold machinery here: a consequence beat's
    /// prerequisite fact is only written when its cause resolves, so eligibility against the live store
    /// keeps it out until then, and the run-scoped story ledger keeps the cause from re-placing. A
    /// thinner window while a beat is held is accepted — correctness over density (D3).
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
        private readonly IWorldSlotAllocator _allocator;
        private readonly IThreadLedger _threadLedger;
        private readonly IStoryRunLedger _storyLedger;
        private readonly IThreadCatalog _threadCatalog;
        private readonly IThreadMaintenance _maintenance;
        private readonly ISiteCatalog _siteCatalog;
        private readonly IGameLogger _logger;
        private readonly HashSet<string> _warnedNpcFlavors =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        public RunWindowPlanner(
            IReadOnlyList<StoryTemplateData> stories,
            IReadOnlyList<NpcArchetypeData> archetypes,
            IPreconditionEvaluator evaluator,
            IActorInstanceFactory actorFactory,
            ILiveActorRegistry liveActors,
            IRandomSource random,
            RunPacingSettings settings,
            IWorldSlotAllocator allocator,
            IThreadLedger threadLedger,
            IStoryRunLedger storyLedger,
            IThreadCatalog threadCatalog,
            IThreadMaintenance maintenance,
            ISiteCatalog siteCatalog = null,
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
            _threadLedger = threadLedger;
            _storyLedger = storyLedger;
            _threadCatalog = threadCatalog;
            _maintenance = maintenance;
            _siteCatalog = siteCatalog ?? new SiteCatalog(null);
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

            // Thread lifecycle tick first (D13/D14): retire expired/conflicted threads before
            // eligibility is computed, so this window already plans against the current thread state.
            _maintenance.Tick(windowIndex, facts);

            // Stories tagged with an NPC fill flavor (e.g. townsfolk) are ambient colour: they fill
            // site Npc slots by flavor and must never satisfy the rare quest slot. Quest-channel
            // stories additionally pass the run-scoped continuity gates (FR9): never re-place a beat
            // already placed/resolved this run, never place a beat of a retired thread.
            var eligible = BuildEligible(facts);
            var questEligible = new List<EligibleStory>();
            var ambientEligible = new List<EligibleStory>();
            for (int i = 0; i < eligible.Count; i++)
            {
                var entry = eligible[i];
                if (IsAmbientColour(entry.Story))
                {
                    ambientEligible.Add(entry);
                    continue;
                }

                if (_storyLedger.IsPlacedOrResolved(entry.Story.StoryId) ||
                    (!string.IsNullOrEmpty(entry.Story.ThreadId) && _threadLedger.IsRetired(entry.Story.ThreadId)))
                {
                    continue;
                }

                questEligible.Add(entry);
            }

            var usedStoryIds = new HashSet<string>();

            for (int slot = 0; slot < _settings.WindowSize; slot++)
            {
                // The allocator gets the live availability so a quest slot that cannot be filled (story
                // pool exhausted this window, or only new-thread openers left at the concurrency cap)
                // degrades into the ambient draw instead of a dead platform, and the spacing counter
                // keeps running. Availability and the pick share one predicate so a granted quest slot
                // can always be filled.
                var allocation = _allocator.AllocateSlot(HasPlaceable(questEligible, usedStoryIds));
                switch (allocation.Kind)
                {
                    case WorldSlotKind.Quest:
                        var pick = ChooseStory(questEligible, usedStoryIds);
                        if (pick == null)
                        {
                            // questAvailable was true, so this is unreachable; guard for safety.
                            platforms.Add(PlannedPlatform.EmptyFiller());
                            break;
                        }

                        // The landed quest may pull a settlement into being (world-sites brief): a
                        // site:<id> story tag is a hard request, otherwise a seeded wild-vs-settlement
                        // roll. The quest platform itself is the block's anchor slot.
                        var stamp = _allocator.TryReserveSettlement(pick.Value.Story.StoryTags);
                        Place(pick.Value, platforms, usedStoryIds, windowIndex, recordRunState: true, null, stamp);
                        break;

                    case WorldSlotKind.Npc:
                        PlaceAmbientNpc(allocation, ambientEligible, platforms, usedStoryIds, windowIndex);
                        break;

                    case WorldSlotKind.Combat:
                        platforms.Add(PlannedPlatform.AmbientCombat(allocation.EnemyId, allocation.Flavor,
                            allocation.Site));
                        break;

                    case WorldSlotKind.Loot:
                        platforms.Add(PlannedPlatform.LootDrop(allocation.Flavor, allocation.Site));
                        break;

                    default:
                        platforms.Add(PlannedPlatform.EmptyFiller(allocation.Site));
                        break;
                }
            }

            return PadAndBuild(windowIndex, platforms);
        }

        /// <summary>
        /// Fills a site Npc slot (e.g. NPC·townsfolk) with an eligible ambient-colour story matching the
        /// slot's flavor. Unused stories are preferred, but a small chatter pool may repeat (with a fresh
        /// actor) rather than leaving the site platform dead. No candidate at all degrades to Empty.
        /// </summary>
        private void PlaceAmbientNpc(SlotAllocation allocation, List<EligibleStory> ambientEligible,
            List<PlannedPlatform> platforms, HashSet<string> usedStoryIds, int windowIndex)
        {
            var candidates = new List<EligibleStory>();
            var unused = new List<EligibleStory>();
            for (int i = 0; i < ambientEligible.Count; i++)
            {
                if (!HasTag(ambientEligible[i].Story.StoryTags, allocation.Flavor))
                {
                    continue;
                }

                candidates.Add(ambientEligible[i]);
                if (!usedStoryIds.Contains(ambientEligible[i].Story.StoryId))
                {
                    unused.Add(ambientEligible[i]);
                }
            }

            var pool = unused.Count > 0 ? unused : candidates;
            if (pool.Count == 0)
            {
                if (_warnedNpcFlavors.Add(allocation.Flavor))
                {
                    _logger?.Warning(LogCategory.Narrative,
                        $"[RunWindowPlanner] No eligible story carries the NPC fill flavor " +
                        $"'{allocation.Flavor}' - the site slot stays empty.");
                }

                platforms.Add(PlannedPlatform.EmptyFiller(allocation.Site));
                return;
            }

            // Ambient colour stays outside the run ledger (recordRunState: false) - the small chatter
            // pool may repeat across windows by design; it is not a story beat.
            Place(pool[_random.NextInt(pool.Count)], platforms, usedStoryIds, windowIndex,
                recordRunState: false, allocation.Flavor, allocation.Site);
        }

        private bool IsAmbientColour(StoryTemplateData story)
        {
            var flavors = _siteCatalog.NpcFillFlavors;
            if (flavors.Count == 0)
            {
                return false;
            }

            foreach (var flavor in flavors)
            {
                if (HasTag(story.StoryTags, flavor))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasTag(IReadOnlyList<string> tags, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], tag, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The single placement predicate the allocator's quest availability and the pick both use.
        /// A candidate is placeable when it is unused this window and — if it belongs to a thread —
        /// either advances a live thread (always allowed, FR3) or opens a new one below the
        /// concurrency ceiling (FR7). Threadless one-shots ignore the ceiling: it is about the
        /// legibility of storylines, not ambient one-shots. Opening a thread mid-window raises
        /// <see cref="IThreadLedger.LiveCount"/> immediately, so the cap holds within a window too.
        /// </summary>
        private bool IsPlaceable(EligibleStory entry, HashSet<string> usedStoryIds)
        {
            if (usedStoryIds.Contains(entry.Story.StoryId))
            {
                return false;
            }

            var threadId = entry.Story.ThreadId;
            if (string.IsNullOrEmpty(threadId))
            {
                return true;
            }

            if (_threadLedger.TryGet(threadId, out var record))
            {
                // Retired threads were pruned at partition time; guard anyway so a mid-window
                // transition can never slip a dead thread's beat through.
                return record.State == ThreadState.Live;
            }

            return _threadLedger.LiveCount < _settings.MaxLiveThreads;
        }

        private bool HasPlaceable(IReadOnlyList<EligibleStory> eligible, HashSet<string> usedStoryIds)
        {
            for (int i = 0; i < eligible.Count; i++)
            {
                if (IsPlaceable(eligible[i], usedStoryIds))
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

        private EligibleStory? ChooseStory(IReadOnlyList<EligibleStory> eligible, HashSet<string> usedStoryIds)
        {
            var candidates = new List<EligibleStory>();
            for (int i = 0; i < eligible.Count; i++)
            {
                if (IsPlaceable(eligible[i], usedStoryIds))
                {
                    candidates.Add(eligible[i]);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // Advance-over-open (FR3): prefer a beat of a thread that is already live in the run
            // ledger — within this window and across windows — so threads reach a payoff instead of
            // accumulating; else any candidate.
            var preferred = new List<EligibleStory>();
            for (int i = 0; i < candidates.Count; i++)
            {
                var thread = candidates[i].Story.ThreadId;
                if (!string.IsNullOrEmpty(thread) &&
                    _threadLedger.TryGet(thread, out var record) && record.State == ThreadState.Live)
                {
                    preferred.Add(candidates[i]);
                }
            }

            var pool = preferred.Count > 0 ? preferred : candidates;
            return pool[_random.NextInt(pool.Count)];
        }

        private void Place(EligibleStory entry, List<PlannedPlatform> platforms, HashSet<string> usedStoryIds,
            int windowIndex, bool recordRunState, string flavor = null, SiteStamp site = default)
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

            platforms.Add(PlannedPlatform.StoryEncounter(story, actor, IsCombatBearing(story), flavor, site));
            usedStoryIds.Add(story.StoryId);

            if (!recordRunState)
            {
                return;
            }

            // Quest-channel continuity (FR9/FR1): the beat is recorded so it never re-places, and its
            // thread opens (idempotent) with the authored kind - or the implicit ephemeral default.
            _storyLedger.NotePlaced(story.StoryId, story.ThreadId, windowIndex);
            if (!string.IsNullOrEmpty(story.ThreadId))
            {
                var definition = _threadCatalog.GetOrImplicitDefault(story.ThreadId);
                _threadLedger.Open(story.ThreadId, definition.Kind, windowIndex);
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
