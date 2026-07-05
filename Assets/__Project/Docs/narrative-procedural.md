# Data-Driven Procedural Narrative — Requirements & Design

> Orthogonal, recombinable narrative fragments (actors, dialogue, quests, enemies, stories) composed
> at runtime through a casting layer and coupled only through one unified, namespaced fact space. A
> run director selects storylets by fact preconditions; cross-story connection is emergent, never
> authored as edges between stories.
> Status: current as of 2026-06-18. Implemented as a vertical slice ("The Toll at Razor Pass"); the
> generic engine pieces marked in §6 are deferred.
>
> This document describes the system **as implemented**. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1 — Separation of concerns.** Actor identity, dialogue, quest, enemy, and story are independent
  fragments; no fragment hard-references another.
- **R2 — Definition vs instance.** ScriptableObjects are immutable templates; mutable per-run state
  lives in runtime instances. Ink runs per-session with serializable state.
- **R3 — Casting/binding layer.** A runtime casting associates an actor instance with the content it
  presents this run. Role and hostility are properties of the casting, not the archetype.
- **R4 — Ink integration.** Ink is parameterized via variables; tags are the only bridge to game
  systems; quest/combat logic lives outside Ink and reports results back into Ink variables.
- **R5 — Story as typed slots.** A story is a skeleton of typed slots; fragments are matched into
  slots by semantic tags, never by hard reference.
- **R6 — Run-level director + shared run-state.** Coherence lives one level above the story.
- **R7 — Fact-based emergent coupling.** Stories declare fact preconditions and an effect footprint;
  cross-story connections emerge from overlapping reads/writes.
- **R8 — Threads/arcs.** A thread is a **first-class managed entity** (P2-3): a run-scoped
  `ThreadLedger` tracks each thread's lifecycle (live → resolved / failed) and stage (beats resolved);
  authored `ThreadDefinition` assets give a thread its kind (**ephemeral** — expires after an
  un-advanced lifespan — vs **arc** — expiry-exempt), premise facts (contradiction fails it, incl.
  arcs — the mutual-exclusion mechanism), and resolution conditions (payoff). A small authored
  ceiling caps simultaneously-live threads (advance-over-open; at the cap the planner waits, never
  force-drops). Retirement is a state change + one indicator fact — never a closure beat. A
  run-scoped `StoryRunLedger` guarantees no story beat is ever re-placed once placed/resolved (§2.6).
- **R9 — Multi-namespace facts, uniform machinery.** One store spans `world`/`actor`/`faction`;
  preconditions, effects, and the director operate uniformly over all namespaces.
- **R10 — Actor↔faction interlock.** Predicates/effects may reference both an actor's and its
  faction's state in one pass.
- **R11 — Consequence cascades.** Explicit effects first; a reactive-rule layer is deferred (§6).
- **R12 — Recurring actors.** The same actor instance carries facts across stories within a run.
- **R13 — Debuggability & authoring safety.** A typed fact vocabulary (single source of truth), an
  inspectable run-state, and optional authored spine beats.
- **R14 — Engine constraints.** SOs are the content surface; services are Zenject-injected; save/load
  for all runtime state.

### 1.2 Non-functional requirements

- **N1** Core logic is pure C# (no UnityEngine) and unit-tested (`Narrative.*.Core`).
- **N2** All dependencies wired through Zenject (`NarrativeSliceInstaller`); no service locators.
- **N3** Data definitions are data-only ScriptableObjects; SO→Core via explicit static mappers.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Narrative/
  Facts/Core/    fact store, keys/values, predicates/effects/shapes, evaluator, applier, resolver,
                 typed accessors (WorldFacts/ActorFacts/FactionFacts), registry
  Facts/Data/    FactKeyDefinition, FactKeyRegistry SOs + authoring structs + mapper
  Actors/Core+Data  NpcArchetypeData / NpcInstance ; NpcArchetype SO + mapper
  Dialogue/Core+Data DialogueData / DialogueSession / tag parser ; DialogueDefinition SO + mapper
  Dialogue/      DialogueRunner (tag bridge + suspension state machine)
  Quests/Core+Data  QuestData / QuestObjective / QuestInstance ; QuestDefinition SO + mapper
  Stories/Core+Data StoryTemplateData / StorySlot / StoryRunLedger (placed+resolved record) ; StoryTemplate SO + mapper
  Threads/Core+Data ThreadDefinitionData / ThreadCatalog / ThreadLedger / ThreadMaintenanceService (R8) ;
                 ThreadDefinition SO + mapper
  Casting/Core   Casting, ContextBag, FragmentLibrary, CastingFactory, EnemyFragment
  Director/Core  RunDirector, StoryletSelection, DeterministicRandom (serializable PRNG)
  Runtime/Core   NarrativeSliceBootstrap (footprint derivation + ref validation),
                 StoryResolutionRelay (encounter outcome -> run ledgers)
  Runtime/Snapshots  save DTOs + NarrativeSaveService (boundary) + fact/ledger snapshot mappers
Scripts/Core/DI/NarrativeSliceInstaller.cs
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `FactStore` / `IFactStore` | The one unified namespaced fact store (R6/R9); stable-ordered snapshot; fail-closed on unknown/mistyped keys. |
| `FactKeyRegistry` (Core) | Authoritative fact vocabulary (R13); per-key type/scope/default. |
| `PreconditionEvaluator` | Resolves subject token → reads store → applies comparison with presence/default rule; AND-composes; fail-closed. |
| `FactEffectApplier` | Footprint-gated write chokepoint (R7): validates each write against the *fragment's own* footprint by namespace+key+unresolved subject token; op×type legality. |
| `SubjectResolver` | Resolves `$self`/`$target`/`$faction`/`$<contextKey>` against the casting context (R10); fail-closed on unknown token. |
| `CastingFactory` / `Casting` | Fills typed slots by tag (R5) into a casting (R3); derives a story's advisory footprint over the library (W3-2). |
| `RunDirector` | Selects eligible storylets by fact preconditions (R7) with a seeded, save-replayable PRNG (B2). |
| `ILiveActorRegistry` / `LiveActorRegistry` | Run-scoped set of minted `NpcInstance`s in deterministic registration order (R12); the windowed planner registers each fresh actor and queries it to recast a recurring actor (D11). |
| `IThreadCatalog` / `ThreadCatalog` | Authored thread vocabulary (R8/D13): kind, premise, resolution conditions, lifespan per thread id; an undeclared label resolves to an implicit ephemeral default. |
| `IThreadLedger` / `ThreadLedger` | Run-scoped thread lifecycle (R8/FR1): state (live/resolved/failed + retirement reason), stage, expiry clock; first-open order is planner-driven and replay-stable. |
| `IThreadMaintenance` / `ThreadMaintenanceService` | The per-window lifecycle tick (D13/D14): folds advance flags, then resolution → premise-conflict → expiry (conflict outranks the clock; arcs never expire). Retirement writes the `world.<threadId>.thread_retired` indicator fact — no closure beat. Zero PRNG draws. |
| `IStoryRunLedger` / `StoryRunLedger` | Run-scoped placed/resolved story record (FR9): a beat already placed, resolved, or on a retired thread is never re-placed as fresh across window boundaries. Ambient-colour chatter bypasses it (may repeat). |
| `StoryResolutionRelay` | Folds an encounter's end into the ledgers: story → resolved always; thread stage++ only when the outcome isn't `leave` (a browsed-and-abandoned errand still lapses). |
| `DialogueRunner` | Drives one `DialogueSession`; dispatches Ink tags to facts/quest/combat (R4); explicit suspension state machine — `AwaitingExternal` for async combat, `AwaitingContinue` to gate one readable line at a time (`Continue()` advances). |
| `EncounterCardHandPresenter` | MVP presenter (pure-C#): composes the typed encounter card hand from the runner's events and drives `IEncounterCardHandView` (§2.7). Replaces `DialogueRunnerViewPresenter`, now dormant/unbound. |
| `QuestInstance` | Quest lifecycle; returns effects to apply; bridges legacy `IRunProgressionRecorder`. |

### 2.3 Runtime flow

1. The director's storylets, fragments, and fact registry are mapped from SOs at install
   (`NarrativeSliceInstaller`); `NarrativeSliceBootstrap` caches each story's advisory footprint and
   validates the typed refs.
2. `RunDirector.SelectNext(storylets, store, context)` returns the eligible storylets (preconditions
   pass) and a seeded pick.
3. `CastingFactory.Cast(story, actor, library)` fills the story's slots by tag → a `Casting` with a
   dialogue, optional quest, optional enemy, and a `ContextBag` (`$self`/`$faction`, `npc_name`, and
   the `quest_available`/`combat_available` flags).
4. `DialogueRunner.Begin(casting)` starts a fresh `DialogueSession`, pumps Ink, and dispatches tags:
   `fact:` writes (footprint-gated), `offer-quest:` starts the quest and
   `advance-objective:`/`complete-quest:`/`fail-quest:` drive its lifecycle (gated against the quest's
   own footprint — see [Quest Subsystem](quest-subsystem.md)), `start-combat:` suspends until
   `ReportCombatResult` resumes it; `speaker:`/`outcome:` drive presentation/termination. Each readable
   line parks the runner in `AwaitingContinue`; `DialogueRunnerViewPresenter` drives the `IDialogueView`
   and calls `Continue()` on the view's continue/skip input, so multi-line knots are read one line at a
   time (no-text tag steps flow without gating).
5. Facts written by one story change the eligibility of another at the next `SelectNext` — the only
   coupling between stories (R7).

### 2.4 DI wiring

`Core.DI.NarrativeSliceInstaller` (the only narrative installer in the Area scene since the legacy
cutover) binds the fact store/evaluator/applier/resolver, the fragment library + storylets (mapped from
inspector SO lists or auto-loaded from `Resources/Narrative/*`), the casting factory + director +
windowed planner + serializable PRNG (seeded from the run seed via `LootSeed.Derive`), the
world-content-density settings + biome monster-pool catalog + run-scoped `WorldContentAllocator`
(§2.6; mapped from `WorldContentDensityConfig` / `BiomeMonsterPoolDefinition` assets or auto-loaded
from `Resources/Narrative/WorldContentDensityConfig` and `Resources/Combat/MonsterPools`), the
actor-instance factory + encounter orchestrator (§2.5), the thread subsystem (`IThreadCatalog` mapped
from `ThreadDefinition` assets or auto-loaded from `Resources/Narrative/Threads`, the run-scoped
`IThreadLedger` + `IStoryRunLedger`, `IThreadMaintenance`, and the `StoryResolutionRelay`
`IInitializable`), the dialogue session/runner/tag-parser, the
`IEncounterCardHandView` + `EncounterCardHandPresenter` (the card-hand UI, §2.7; instantiated from the
`Resources` prefab), and `INarrativeSaveService` (fed the fact registry + both ledgers so its snapshot
partitions run/meta facts and captures the thread state). A `NarrativeSliceBootstrap` `IInitializable`
runs footprint derivation + typed-ref validation after build, and notes any story thread label with no
`ThreadDefinition` asset (it runs as an implicit ephemeral thread).

### 2.5 Encounter entry (runtime orchestration)

The runtime flow in §2.3 is driven by two small pure-C# pieces, so the chain that the slice's tests
exercised has a single production entry point:

- `ActorInstanceFactory` (`IActorInstanceFactory`) mints a per-run `NpcInstance` from an
  `NpcArchetypeData` when an actor is placed: a run-unique, deterministic instance id (a monotonic
  ordinal per factory), a display name drawn from the archetype's pool via the seeded PRNG, and the
  archetype's faction. The caller keeps the instance for the actor's lifetime so per-actor facts
  (`actor.<InstanceId>.*`) carry across its encounters (R12).
- `EncounterDirector.BeginEncounter(NpcInstance)` is the orchestrator: it binds the actor's
  `$self`/`$faction` subject tokens, asks `RunDirector.SelectNext` for an eligible storylet against the
  live store (R6/R7), `CastingFactory.Cast`s the chosen storylet onto the actor (R3/R5), and calls
  `DialogueRunner.Begin`. It returns `false` (runner untouched) when the actor is null, no storylet is
  eligible, or the storylet cannot be cast — the caller then completes the encounter without a dialogue.

`EncounterDirector` only resolves-and-begins; the platform-state adapter that triggers it and routes the
runner's combat/quest/ended signals back into gameplay is `DialogueActiveState` (the legacy cutover is done).

### 2.6 Windowed director (planner core — allocation is density-first, selection is story-first)

The world is planned a window at a time (a window = the next `WindowSize` platforms ahead of the
player) from the **four content kinds** of the world-content-density brief
(`design/world/content-kinds.md` vocabulary): **Empty/traversal** (the deliberate majority),
**Loot·scattered** (simple low-tier finds), **Combat·wild-beast** (ambient aggressive monsters from
the biome pool — the main combat source), and **NPC·quest-bearer** (rare, spaced). There is **no
narrative weight budget and no combat quota**: quest rarity + spacing governs stories, and a story
that happens to carry a combat slot is the tolerated exception. `RunWindowPlanner`
(`IRunWindowPlanner`, `Narrative.Director.Core`) is pure C# and deterministic:

`PlanWindow(windowIndex, IFactStore) → WindowPlan`
0. **Thread lifecycle tick** (`ThreadMaintenanceService.Tick`, R8/D13) — before anything else, each
   live thread folds its advance flag into the expiry clock, then retires if due: **resolution**
   (authored payoff conditions hold) → resolved; **conflict** (a written fact contradicts its authored
   premise — the mutual-exclusion mechanism; applies to arcs too) → failed; **expiry** (an *ephemeral*
   thread un-advanced past its authored lifespan; arcs are exempt) → failed. Conflict outranks expiry.
   Retirement is a state change + one `world.<threadId>.thread_retired` indicator fact (`"expired"` /
   `"conflict"`) — **no closure beat is placed**. Running the check here (never off `OnFactChanged`)
   keeps the lifecycle a deterministic step of the planning sequence (D21).
1. **Eligibility** — keep stories whose preconditions pass over the live store (R6/R7), resolved over
   **world/global *and* actor/faction-scoped facts** (D16). A story is classified by its preconditions:
   - **World-only** (no precondition references a `$`-context token): evaluated with an actor-less
     context (world/global facts only); a fresh actor is minted for it at placement. Actor compatibility
     does **not** gate eligibility (see below); it is pruned for actor reasons only when no archetype
     exists at all.
   - **Actor/faction-scoped** (any precondition uses `$self`/`$faction`/…): resolved as a **casting
     query** (D11) — the planner can't look the fact up against a fixed subject before an actor is cast,
     so it asks "does a *live* actor (one already minted this run, `ILiveActorRegistry`) whose facts
     satisfy the precondition exist?" The first such actor (registration order, deterministic) makes the
     story eligible and is **pinned** for it. None → ineligible. World predicates in the same set still
     resolve via their empty subject, so mixed preconditions work. This is **continuation semantics**: a
     brand-new actor (no facts) can't satisfy a positive actor-scoped precondition, so the gate only
     opens once a qualifying actor already exists — a story meant to open for any fresh actor must use
     world/global preconditions.

   Quest-channel candidates then pass the **run-scoped continuity gates** (FR9 — no stale
   re-placement): a story already **placed or resolved** this run (`StoryRunLedger`), or whose thread
   is **retired**, never re-enters a plan — the look-ahead plans against the *current* story/thread
   state, not stale facts. Ambient-colour chatter is exempt (it may repeat; it is not a beat).
2. **Slot allocation** — for each of the window's `WindowSize` slots, `WorldContentAllocator`
   (run-scoped, sharing the director's seeded stream) decides the content kind:
   - **Quest gate first**: the spacing counter must exceed
     `WorldContentDensitySettings.MinPlatformsBetweenQuests` (a **hard invariant carried across
     window boundaries** — the counter is allocator state, so quests never cluster back-to-back), a
     seeded 1-in-`AveragePlatformsPerQuest` roll must hit, and a **placeable** story must exist —
     unused this window **and**, if threaded, either advancing a live thread or opening a new one
     **below the concurrency ceiling** (`RunPacingSettings.MaxLiveThreads`, D14/FR7). At the ceiling
     the planner opens no new thread — the slot **degrades into the ambient draw** and it *waits*
     for a live thread to resolve/expire (never force-drops one). Availability and the pick share
     one placeability predicate, so a granted quest slot can always be filled. When any gate fails
     the slot degrades and the counter keeps running, so a quest lands at the next opportunity
     rather than being forfeited.
   - **Ambient weighted draw** otherwise: an integer-weighted pick among Empty
     (`EmptyWeight`) / Loot (`LootWeight`) / Combat (`CombatWeight`). A Combat slot draws its enemy
     id from the current biome's `IBiomeMonsterPoolCatalog` pool at flat difficulty (an unauthored
     pool downgrades the slot to Empty, warned once).
3. **Story selection** — only for Quest slots: pick among the placeable stories (advance-over-open
   preference + seeded tie-break, below) and place it with its actor. Placement records the beat in
   the `StoryRunLedger` and **opens its thread** in the `ThreadLedger` (idempotent) with the kind
   authored on its `ThreadDefinition` — or as an implicit ephemeral default for a bare label.

Selection prefers **advancing a thread that is live in the ledger over opening a new one** (FR3 —
within the window and across windows; this subsumes the old window-local coherence preference), then a
seeded pick among ties so plans are save-replayable (B2). A **world-only** placed story gets an actor minted via
`IActorInstanceFactory` and **registered** in `ILiveActorRegistry`, so it can be recast later (R12/D11);
its archetype is chosen by a **soft preference**, not a hard filter (P1: hard requirements prune,
preferences only weight) — the planner prefers an `NpcArchetype` whose tags overlap the story's tags, but
falls back to any archetype when none overlap. An **actor-scoped** placed story instead reuses the
**pinned** actor resolved by the casting query above; the pin is a **hard pin** (D11) that bypasses the
soft tag preference so an arc cannot be broken by casting a different archetype. Output `WindowPlan` is an
ordered list of `PlannedPlatform` (`Story`/`Combat`/`Loot`/`Empty`; a `Combat` platform carries the
biome-pool `EnemyId`) the level generator maps to platforms.

`ILiveActorRegistry`/`LiveActorRegistry` (`Narrative.Actors.Core`, pure C#) is the run-scoped set of
minted actors in deterministic registration order — the seam recurring-actor casting reads. It is bound
`AsSingle` for the run; repopulating it on load is part of the deferred window/horizon save-state (§6).

**Streaming & entry (now wired).** `RunStreamingCoordinator` (`LevelGeneration`) drives generation:
`Begin()` generates window 0; on each `PlatformEvents.OnPlatformExited` from a frontier platform it
locks that window and plans + generates the next against the live store (exit, not entry, so an
engaging player's fact writes land before the next window is planned). Before each window is
planned, `BiomeStretchDirector.ApplyForWindow` applies the **biome journey**'s stretch for that
window (see `biome-journey.md`): the active biome now advances along the run in authored, seeded,
tier-climbing stretches, switching `ICurrentThemeProvider` and publishing `run_escalation_tier` —
the allocators are unchanged (they already read the theme provider live per allocation). Each planned story platform is
realised as an `NpcContent` carrying the minted actor + committed story (its visual spawns from the
archetype assembly via `IModularCharacterFactory`; `INpcArchetypeCatalog` resolves id → archetype SO).
An **ambient combat** platform is realised as an `EnemyContent` with the planner's biome-pool
`EnemyId` — the content-driven state factory routes it to the combat states, a fight with no quest or
dialogue attached. A **loot** platform maps to `PlatformContentType.Loot`: `AreaGenerator` rolls the
biome `_platformTable` deterministically (`LootRollContext(theme, "platform:<nodeId>")`) and
`PlatformLootSpawnCoordinator` spawns the pickups. On entry, `DialogueActiveState` calls
`EncounterDirector.BeginPlanned(story, actor)` (cast + begin, no re-selection) and routes outcomes:
combat → `EnemyContent` + `CombatActiveState`, then `CombatActiveState` feeds
`DialogueRunner.ReportCombatResult` so post-combat lines/facts replay before the platform completes; a
normal end completes the platform. `AreaSceneEntrypoint` drives the coordinator instead of the legacy
generator.

**Sites layer (world-sites brief — see `world-sites.md` for the full system).** The planner now runs
on the `IWorldSlotAllocator` seam: `SiteAwareSlotAllocator` wraps the untouched
`WorldContentAllocator` (an unauthored site catalog is a bit-exact passthrough) and reserves
**contiguous multi-platform site blocks** — a landed quest may pull a settlement via
`TryReserveSettlement` (`site:<id>` story tag = hard request, else a `WildQuestWeight` roll), and a
rare spaced ambient roll pulls landmarks; a run-scoped pending queue lets a block span window
boundaries. Two planner-side additions: a `WorldSlotKind.Npc` slot is filled by an **ambient-colour
story** matching the slot's flavor tag (e.g. `townsfolk`), and stories tagged with any
`ISiteCatalog.NpcFillFlavors` entry are **excluded from quest picks**. `PlannedPlatform` and
`GraphNode` carry the beat's `Flavor` + `SiteStamp`; a site loot beat's flavor biases the biome
table via `LootRollContext.Tags`.

Open points this stage: biome is fixed (Forest — biome selection along the run is a separate item).
The quest-spacing counter is allocator state and is not yet save-captured (rides the window/horizon
save-state item, §6). An archetype with no `_assembly` runs its dialogue but spawns no visible NPC
body (logged warning).

### 2.7 Encounter card-hand (presentation, MVP)

The encounter is presented as a **situation bubble + a composed hand of typed cards**, not a line-reading
panel with an Ink choice list. This is a presentation + choice-selection layer over the unchanged
`DialogueRunner`; the fact/quest/tag engine (§2.3) is untouched.

- **Pieces (`Narrative.Encounter` + `Narrative.View`).** `EncounterCardHandPresenter` (pure C#,
  `IInitializable`) subscribes to the runner's `OnSpeakerChanged`/`OnLine`/`OnChoices`/`OnCombatTriggered`/
  `OnDialogueEnded` and drives `IEncounterCardHandView` (the thin `EncounterCardHandView` MonoBehaviour +
  per-card `EncounterCardView`). `EncounterCardType` (`QuestOffer`/`Attack`/`Leave`/`Talk`) and the
  `EncounterCardViewData` DTO are the view contract.
- **Composition.** Each readable line updates the situation bubble (tap-to-continue preserves the runner's
  `AwaitingContinue` gate). At a decision point the hand is composed as: one **QuestOffer** card per Ink
  choice; an **Attack** card from any Ink choice tagged `# card: attack` (its authored combat consequence
  runs through Ink) **or**, only when the casting is combat-capable and no choice authored one, a
  **system-added** Attack card (pick → `DialogueRunner.TriggerCombat`); and an always-present **Leave** card
  (pick → `DialogueRunner.Leave`). During plain narration only the Leave card shows, so a system Attack can
  never be picked mid-narration and skip an authored Ink combat branch.
- **Runner verbs.** `DialogueRunner` exposes `CombatAvailable` / `CombatEnemyId` (read from the active
  casting), `TriggerCombat()` (suspends to `AwaitingExternal` and fires `OnCombatTriggered` exactly like a
  `start-combat:` tag, so `ReportCombatResult` resume + the platform combat route are reused), and `Leave()`
  (a guarded graceful end with outcome `"leave"`; refused while suspended on combat).
- **Choice-tag authoring.** The `# card: attack` tag must follow **shown** (non-bracketed) choice text so it
  lands in the Ink `Choice.tags` the presenter reads (`* Drop the grain. # card: attack`); a tag after a
  `[bracketed]` choice goes to post-selection output instead and is not seen pre-selection.
- **Wiring.** `NarrativeSliceInstaller` binds `IEncounterCardHandView` from
  `Prefabs/UI/Encounter/EncounterCardHandView` and `EncounterCardHandPresenter` (`AsSingle().NonLazy()`).
  `DialogueActiveState` no longer depends on the view (the NPC is visible as a 3D body; no portrait panel in
  the MVP). The legacy `IDialogueView` / `DialogueView` / `DialogueRunnerViewPresenter` are left in the repo
  **dormant (unbound)** for the §6 removal item.

**Presentation feel upgrade (Hades-style box).** The view layer over this seam is the bottom-centre
dialogue box documented in **`encounter-dialogue-ui.md`**: a portrait + name, the line revealed **word
by word** (tap to complete), cards centred above the box, the **quest card labelled with the job**
(title + summary from `DialogueRunner.OfferedQuest`), and author-marked **`[[ ]]` key words** tinted in
both lines and cards (`KeywordHighlightFormatter`). The presenter stays UnityEngine-free — it forwards
the NPC archetype **id** (`DialogueRunner.EncounterArchetypeId` / `EncounterDisplayName`) and the view
resolves the portrait via `INpcArchetypeCatalog`. The conversation engine is unchanged.

Open points this stage: the card visual is a placeholder per-type tint (reward tier-glow / belonging color
is gated on the crafting tier model); the fully system-driven Monster verb (thread closure, conquest facts,
corpse-loot routing, cauldron bark, post-combat write-backs relocated out of Ink) and several offers per NPC
are deferred (§6).

---

## 3. ScriptableObject Reference

### `FactKeyDefinition`  (asset menu: `Create → Narrative → Facts → Fact Key`)

The single source of truth for one fact key (R13).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_namespace` | `FactNamespace` | Grouping label: World/Actor/Faction | — |
| `_scope` | `FactScope` | Subject arity: Global / PerActor / PerFaction / PerLocation / PerThread (A1) | arity comes from here, NOT the namespace |
| `_key` | string | Bare key name, e.g. `barn_raided` | no namespace prefix |
| `_horizon` | `FactHorizon` | Lifetime horizon (D20): **Run** resets on death; **Meta** persists across runs | default Run — every pre-P2-3 key stays run-scoped |
| `_valueType` | `FactValueType` | Bool/Int/Float/String | — |
| `_defaultBool/_defaultInt/_defaultFloat/_defaultString` | typed | Value when unset (participates in comparisons, B4) | type-zero |
| `_description` | string | Author documentation | — |

### `FactKeyRegistry`  (asset menu: `Create → Narrative → Facts → Fact Key Registry`)

Holds `_keys: List<FactKeyDefinition>`; wired into `NarrativeSliceInstaller` and mapped to the Core
registry. Every fact key referenced by a fragment/story must be declared here, or writes/reads fail closed.

### `NpcArchetype`  (asset menu: `Create → Narrative → Actors → Archetype`)

Identity only (R1). `_archetypeId`, `_displayNamePool`, `_assembly` (`CharacterAssemblyDefinition` for
visuals), `_portrait`, `_factionId` (id only), `_baseDisposition` (personality seed — NOT hostility),
`_archetypeTags`. References no dialogue/quest/enemy/story.

### `DialogueDefinition`  (asset menu: `Create → Narrative → Dialogue → Dialogue`)

`_dialogueId`, `_inkJsonAsset` (compiled Ink TextAsset; must not hardcode NPCs/items), `_startKnot`,
`_declaredVariables` (injected on fresh start), `_declaredFactWrites` (`FactKeyShape[]` — the
**authoritative** set of key shapes its `fact:` tags may write, the runtime write-gate), `_dialogueTags`.

### `QuestDefinition` (+ `QuestObjectiveDefinition`, `QuestRewardSerial`)  (asset menu: `Create → Narrative → Quests → Quest`)

The quest fragment, its lifecycle (offer → advance → complete/fail), and its item rewards are owned by
the **[Quest Subsystem](quest-subsystem.md)** doc (full field reference + authoring recipes there).
Summary: `_questId`, `_displayName`, `_summary`, `_objectives`, `_questTags`, `_onCompleteEffects`,
`_onFailEffects`, `_rewards`. The union of all its effect shapes is the quest's own footprint.

### `StoryTemplate` (+ `StorySlotDefinition`)  (asset menu: `Create → Narrative → Stories → Story Template`)

`_storyId`, `_slots` (`_slotId`, `_kind` Dialogue/Quest/Combat, `_requiredTags`, `_optional`),
`_preconditions` (`FactPredicateSerial[]`), `_ownEffects` (optional story-level writes), `_storyTags`,
`_threadId` (the id of the thread this story is a beat of — matches a `ThreadDefinition` asset, or
runs as an implicit ephemeral thread when none is authored; empty = a threadless one-shot), `_isSpine`,
`_weight` (pacing cost — how much of a window's narrative budget
this story consumes; a story is still one platform, NOT a difficulty or span measure). References no other
template (R7). The effect footprint is **derived** by `CastingFactory` over the library (W3-2), not
authored here.

`EnemyDefinition` (Combat) gains `_enemyTags` so an enemy matches a story combat slot by tag (W2-6).

### `ThreadDefinition`  (asset menu: `Create → Narrative → Threads → Thread`)

Declares one narrative thread (R8/D13; consumed as the Core `ThreadDefinitionData` via
`ThreadDefinitionMapper` → `ThreadCatalog`, never directly). Authoring one is only needed to make a
thread an **arc**, give it premise/resolution facts, or tune its lifespan — a bare `_threadId` label
on stories runs as an implicit ephemeral thread with the tuning-level default lifespan.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_threadId` | string | Must match the `_threadId` label on the stories forming this thread's beats | — |
| `_kind` | `ThreadKind` | **Ephemeral** (expires when un-advanced past its lifespan) / **Arc** (a long storyline — expiry-exempt, but NOT conflict-exempt) | Ephemeral |
| `_premise` | `FactPredicateSerial[]` | Predicates that must all **hold**; a live thread whose premise a written fact contradicts retires as failed/conflict — the mutual-exclusion mechanism ("join the Foxes" premise: `joined_lizards == false`) | empty = never conflicts. **World-scoped only** (no `$` tokens — warned at map time; they'd fail closed) |
| `_resolutionConditions` | `FactPredicateSerial[]` | When these all hold the thread retires as **resolved** (payoff reached), freeing ceiling room | empty = stays live until expiry/conflict. **Gotcha:** use a fact written *at* the payoff beat, never the fact that *gates* the payoff beat — or the thread retires before its finale can place |
| `_lifespanWindows` | int ≥ 1 | Windows the thread may go without an advance (a resolved beat) before it silently expires | 3; ignored for Arc |
| `_description` | string | Author documentation | — |

Shipped assets: `Resources/Narrative/Threads/DemoThread_BarnRaid.asset` (ephemeral, lifespan 4) and
`DemoThread_FrogMarsh.asset` (**arc** — the fox-passport induction must survive slow play). The
retirement indicator key ships as `Resources/Narrative/Facts/Fact_ThreadRetired.asset`
(`world.<threadId>.thread_retired`, PerThread String) in the demo registry.

### `RunPacingConfig`  (asset menu: `Create → Narrative → Director → Run Pacing Config`)

The windowed director's window mechanics (consumed as the Core `RunPacingSettings` via
`RunPacingConfigMapper`, never directly). `_windowSize` (platforms per planning window),
`_lookAheadWindows`, `_maxLiveThreads` (the D14 ceiling on simultaneously-live threads, ephemeral +
arc together; at the cap no new thread opens), `_defaultEphemeralLifespanWindows` (expiry lifespan
for thread labels with no `ThreadDefinition` asset). The former narrative/combat budget fields were
superseded by `WorldContentDensityConfig` (below). No asset is currently authored — the mapper's
defaults (`4` / `1` / `3` / `3`) run.

### `WorldContentDensityConfig`  (asset menu: `Create → Narrative → Director → World Content Density Config`)

The **one asset governing world fullness** (the world-content-density brief; consumed as the Core
`WorldContentDensitySettings` via `WorldContentDensityConfigMapper`, never directly). Shipped asset:
`Resources/Narrative/WorldContentDensityConfig.asset` (the installer's auto-load path).

| Field | Type | Default | Meaning |
| --- | --- | --- | --- |
| `_averagePlatformsPerQuest` | int ≥ 1 | 10 | ~1 quest per N platforms (a seeded 1-in-N roll per slot) |
| `_minPlatformsBetweenQuests` | int ≥ 0 | 4 | hard minimum platforms between two quests; 1+ forbids back-to-back |
| `_emptyWeight` | int ≥ 0 | 65 | ambient-draw share that stays empty/traversal (the world's breath) |
| `_lootWeight` | int ≥ 0 | 15 | ambient-draw share carrying a simple low-tier loot find |
| `_combatWeight` | int ≥ 0 | 20 | ambient-draw share carrying an ambient monster from the biome pool |

### `BiomeMonsterPoolDefinition`  (asset menu: `Create → Combat → Enemies → Biome Monster Pool`)

One ambient-monster pool per biome theme (flat difficulty; consumed as an enemy-id map via
`BiomeMonsterPoolMapper` → `BiomeMonsterPoolCatalog`, never directly). Shipped asset:
`Resources/Combat/MonsterPools/MonsterPool_Forest.asset` (the installer's auto-load folder; first
authored pool per theme wins, duplicates are warned and ignored).

| Field | Type | Meaning |
| --- | --- | --- |
| `_theme` | `LevelTheme` | the biome this pool belongs to (Forest/Desert/Mountain/Cave) |
| `_enemies` | `List<EnemyDefinition>` | the enemies an ambient combat platform in this biome may spawn |

Pooled enemies must resolve in the combat `IEnemyDataProvider`; `AreaInstaller` auto-loads
`Resources/Enemies/Definitions` **and** `Resources/Narrative/Enemies` (deduped by enemy id) so both
authoring locations work.

**Authoring structs:** `FactPredicateSerial` (`namespace`, `subjectToken`, `key`, `op` `ComparisonOp`,
typed value), `FactEffectSerial` (same with `op` `FactEffectOp`), `FactKeyShape` (namespace, subject
token, key, value type — a permitted write target, no op/value).

---

## 4. Adding Content

A designer assembles the vertical slice (or new narrative content) entirely from assets. Recipe:

### Add a fact key
1. `Create → Narrative → Facts → Fact Key`; set namespace, **scope**, key, value type, default.
2. Add it to the `FactKeyRegistry` asset's `_keys` list.

### Mark a fact meta-scoped (persists across runs, D20)
1. On the `FactKeyDefinition`, set `_horizon` to **Meta**. That's it — the save boundary partitions
   the store by horizon (`RunNarrativeSnapshot.Facts` vs `.MetaFacts`), so save/load (P2-2) persists
   each side separately. Leave `_horizon` at **Run** (the default) for anything that should reset on
   death. No shipped demo fact is meta yet; the cross-run store + its consumers ride P2-2/P3-3.

### Add a thread (kind / premise / lifespan — R8/D13)
1. Pick a thread id and put it in the `_threadId` field of every story that forms the thread's beats.
   For a plain short errand chain you can stop here — a bare label runs as an **implicit ephemeral
   thread** with the default lifespan (`RunPacingConfig._defaultEphemeralLifespanWindows`).
2. To make it an **arc**, give it **premise facts** (mutual exclusion), **resolution conditions**, or
   a custom lifespan: `Create → Narrative → Threads → Thread` under `Resources/Narrative/Threads/`
   (the installer's auto-load path); set `_threadId` to the same label, then `_kind`, `_premise`,
   `_resolutionConditions`, `_lifespanWindows` (see the §3 field table and its resolution-fact gotcha).
3. Every fact used in `_premise`/`_resolutionConditions` must be declared in the `FactKeyRegistry`
   (world-scoped — no `$` tokens). No code, no installer edit.

### Add an actor archetype
1. `Create → Narrative → Actors → Archetype`; set id, name pool, assembly, portrait, faction id, tags.

### Add a dialogue
1. Author a `.ink` file under `Resources/Stories/...` using only tags to reach systems
   (`speaker:`, `fact:`, `offer-quest:`, `advance-objective:`, `complete-quest:`, `fail-quest:`,
   `start-combat:`, `outcome:`). Declare any variables the runner
   injects (`npc_name`, `quest_available`, `combat_available`, `combat_won`, `quest_accepted`).
   **Encounter card model (§2.7):** Ink choices become **cards**. A *leave* card is added by the presenter
   (do not author a "walk away" choice); an *attack* card is added when the casting is combat-capable, or you
   may tag a combat-bearing choice `# card: attack` to keep its in-Ink consequence — the tag must follow
   **shown (non-bracketed)** choice text (`* Drop the grain. # card: attack`) to land in `Choice.tags`.
2. Compile it to JSON (Ink integration) and `Create → Narrative → Dialogue → Dialogue`; assign the JSON,
   declare variables, and list every fact-write shape under `_declaredFactWrites`, add matching tags.

### Add a quest
1. `Create → Narrative → Quests → Quest`; add objectives and on-complete/on-fail fact effects, plus tags.

### Add an enemy (combat slot)
1. On the `EnemyDefinition`, add an `_enemyTags` entry (e.g. `bandit`).

### Add a story template
1. `Create → Narrative → Stories → Story Template`; add typed slots with required tags, fact
   preconditions, optional story-level effects, tags, and a thread label.
2. Add the archetype, dialogue, quest, enemy, and story to the `NarrativeSliceInstaller` lists in the
   scene; assign the `FactKeyRegistry`.

### Tune world fullness (density)
1. Open `Resources/Narrative/WorldContentDensityConfig.asset` (or `Create → Narrative → Director →
   World Content Density Config` and place it at that path for auto-load).
2. Dial quest rarity (`_averagePlatformsPerQuest`), quest spacing (`_minPlatformsBetweenQuests`), and
   the ambient empty/loot/combat mix (`_emptyWeight`/`_lootWeight`/`_combatWeight`). No code change;
   the same run seed still produces the same world.

### Add an ambient monster / a biome monster pool
1. Author the enemy as an `EnemyDefinition` under `Resources/Enemies/Definitions` (or
   `Resources/Narrative/Enemies`) — id, HP, abilities, AI profile, loot slots.
2. Add it to the biome's `BiomeMonsterPoolDefinition` `_enemies` list. New biome pool:
   `Create → Combat → Enemies → Biome Monster Pool` under `Resources/Combat/MonsterPools/`, set
   `_theme`. One pool per theme (duplicates are ignored with a warning).

### Ready-made Demo content

A `Demo*` asset set ships under `Resources/Narrative/` (`Facts/`, `Actors/`, `Dialogue/`, `Enemies/`,
`Stories/`, `Threads/`) — eleven `FactKeyDefinition`s (incl. `Fact_ThreadRetired`) +
`DemoFactKeyRegistry`, and the two thread declarations (`DemoThread_BarnRaid` ephemeral,
`DemoThread_FrogMarsh` arc). When the
`NarrativeSliceInstaller` inspector lists are left empty it **auto-loads** these from those Resources
paths (`ResolveAssetsFromResources`), so the slice works without per-scene wiring; assigning assets in
the inspector overrides the fallback.

### The shipped slice — two parallel threads (barn arc + marsh passport)
Two fact-driven threads run side by side, exercising **D5/D15 fact-based selection**, the **D11/D16
recurring-actor** path, the **cross-actor moral fork** (`quest-as-reward.md` §4), **passport/faction
gating** (D15/D16), and **multi-thread** within-window coherence (D12/D14). All archetypes use
`PlaceholderAssembly_A` for a visible body.
- Facts (all Bool, Global, default false, in `DemoFactKeyRegistry` — except `looted_barn`, PerActor,
  and `reads_as_tier`, Int/PerFaction):
  *barn_raid thread* — `world.barn_raided` (raider world gate), `actor.looted_barn` (raider-arc carry),
  the partition facts `world.barn_quest_offered`, `world.barn_quest_accepted`, `world.grain_recovered`,
  `world.raider_bribed`, and `world.raider_offer_taken` (the fork's power side). *frog_marsh thread* —
  `faction.<raceId>.reads_as_tier` (the passport tier, D15/D16 — written by the race passport
  projector from the equipped body, races-passport.md), `world.frog_quest_offered`,
  `world.frog_quest_accepted`.
- Archetypes: `arch_barn_raider` (`raider`,`can-fight`), `arch_villager` (`villager`,`farmer` — the barn
  victim + both window-2 villager reactions), `arch_frogfolk` (`frogfolk`,`elder`,`marsh`; faction
  `marsh_folk` — the marsh hermit + frog elder).
- Dialogues: `dlg_barn_raid`/`BarnRaid.ink` (the **Attack** card — fight choice tagged `# card: attack` →
  `start-combat:` + on `combat_won` writes `world.grain_recovered` and clears `actor.$self.looted_barn`; a
  **let-go** card → writes `world.raider_bribed`, leaves `looted_barn` true; both also write `world.barn_raided`
  + `actor.$self.looted_barn`; walking away is the system **Leave** card), `dlg_raider_motive`/`RaiderMotive.ink`
  (the **counter-offer fork** — a single **quest-offer** card OFFERS `raider-run`, writes
  `world.raider_offer_taken` + clears `actor.$self.looted_barn` to close the arc, D13), `dlg_barn_victim`/`BarnVictim.ink`
  (a single **quest-offer** card writes `world.barn_quest_offered` + `world.barn_quest_accepted`; declining is the
  system **Leave** card, leaving `barn_quest_accepted` at default false),
  `dlg_grateful_farmer`/`GratefulFarmer.ink` (the reward beat — offers/advances/completes the bounty
  quest via `offer-quest:`/`advance-objective:`/`complete-quest:`, no fact writes) and
  `dlg_starving_village`/`StarvingVillage.ink` (reaction beat, no fact writes). *frog_marsh:*
  `dlg_marsh_pool`/`MarshPool.ink` (the passport **hint** — teaches that the marsh opens to fox
  markers; no fact writes since the tier is body-derived),
  `dlg_frog_elder_closed`/`FrogElderClosed.ink` (passport-negative reaction, no fact writes),
  `dlg_frog_elder_open`/`FrogElderOpen.ink` (passport-positive — writes `world.frog_quest_offered` on
  meeting, a quest-offer card OFFERS `frog-errand` + writes `world.frog_quest_accepted`), and
  `dlg_frog_marsh_thanks`/`FrogMarshThanks.ink` (advances/completes the frog errand). Uses
  `enemy_bandit_brute` (tag `bandit`) for the raid combat slot.
- Quests: `qst_barn_bounty`/`DemoQst_BarnBounty` (tag `bounty`, objective `obj_return_grain`, no fact
  effects, reward `1× rock`) fills `story_grateful_farmer`'s optional Quest slot, exercising the quest
  loop end-to-end (offer → advance → complete → item reward on platform completion);
  `qst_raider_run`/`DemoQst_RaiderRun` (tag `raider-run`, reward `1× fire` — power currency; offered, not
  completed in the demo) fills the raider story's new Quest slot; `qst_frog_errand`/`DemoQst_FrogErrand`
  (tag `frog-errand`, reward `1× water` — access currency) runs the frog loop offer → complete. The
  raider/frog rewards pay in **different archetypes, same tier** (Fork A — `quest-as-reward.md` §3). See
  [Quest Subsystem](quest-subsystem.md).
- The `barn_raid` thread spans two windows:
  - *Window 1* places two openers (both world-gated, so they co-appear): `story_barn_victim` (precond
    **world** `barn_quest_offered == false`; tags `villager`,`barn` → villager) where accepting the plea
    (the quest-offer card) writes `world.barn_quest_accepted == true`, and declining (the system Leave card)
    leaves it at default false; and `story_barn_raid`
    (precond **world** `barn_raided == false`; tags `barn`,`raider` → barn-raider; **optional Combat slot**
    req tag `bandit` → `enemy_bandit_brute`) which mints a raider and writes `world.barn_raided` +
    `actor.$self.looted_barn`. The raider choice then forks: **fight** suspends on `start-combat:` and, on the
    `combat_won` write-back, sets `world.grain_recovered` and clears `actor.$self.looted_barn`; **let-go**
    sets `world.raider_bribed` and leaves `looted_barn` true.
  - *Window 2* the director places **exactly one** of three reactions, selected purely from those facts —
    `grain_recovered` (A/C) and `looted_barn` (B) are mutually exclusive by the raider choice, so the
    preconditions partition the outcome space:

    | Candidate | Precondition | Actor |
    |---|---|---|
    | **A** `story_grateful_farmer` (carries the `bounty` quest) | `barn_quest_accepted == true` **AND** `grain_recovered == true` | fresh `arch_villager` |
    | **B** `story_raider_motive` | **actor-scoped** `actor.$self.looted_barn == true` | the **same** raider, recast (hard pin, §2.6) |
    | **C** `story_starving_village` | `barn_quest_accepted == false` **AND** `grain_recovered == true` | fresh `arch_villager` |

    B's `motive` tag deliberately overlaps no archetype — placement proves the recast pin overrides the P1
    tag preference. This is the in-engine analogue of the `RunWindowPlannerTests` partition + raider-arc proof.
    Reaction **B is now the cross-actor moral fork**: the recast raider OFFERS `qst_raider_run` (power
    currency) — the mutually-exclusive counterpart to the farmer's bounty taken on the window-1 victim
    platform. Same tier, opposed facts (`raider_offer_taken` vs. `barn_quest_accepted`), separated in time on
    the shared actor's thread (`quest-as-reward.md` §4); accepting clears `looted_barn` so the arc closes (D13).
- The **`frog_marsh` thread** runs **in parallel** with `barn_raid` (distinct `_threadId`s eligible together,
  so a window may hold beats of both — the multi-thread within-window coherence test, D12/D14). It is gated
  on the **real passport tier** `faction.fox.reads_as_tier` (D15/D16), projected from the equipped body by
  the race passport system (races-passport.md) — the fox is the Forest race, so the marsh reads fox markers:
  - While `reads_as_tier(fox) < 1`: `story_marsh_pool` (a hint beat teaching the rule — wear the fox's
    marks) and `story_frog_elder_closed` (the closed-door reaction, no quest) are eligible.
  - Equipping one fox-tagged part (a mutation) raises the tier to 1: the closed/pool stories drop out and
    `story_frog_elder_open` (`reads_as_tier(fox) >= 1`) becomes eligible (guarded by
    `frog_quest_offered == false` so it offers once), OFFERING `qst_frog_errand`
    (access currency) and writing `frog_quest_accepted`.
  - With `frog_quest_accepted == true`: `story_frog_marsh_thanks` (the thread's second sequential beat)
    advances + completes the errand. The tier crossing 0→1 swapping `FrogElderClosed` for
    `FrogElderOpen` is the **passport flip** in miniature — now read off the body, not a card.
- *Pacing:* the slice relies on the installer's default `RunPacingSettings` (no `RunPacingConfig` asset wired):
  `windowSize 4`, `narrativeBudgetPerWindow 30` (two weight-10 openers fit window 1), `maxCombatPerWindow 2`,
  `minCombatPerWindow 1` — satisfied in window 1 by `story_barn_raid`'s combat-bearing optional slot.
- *Ink compile:* there is no inklecate in the repo — every story ships with **hand-authored compiled JSON**
  kept in lockstep with its `.ink` (offer stories crib `BarnVictim.json`'s single bracketed-choice skeleton;
  linear reaction stories crib `GratefulFarmer.json`). `BarnRaid`'s fight choice carries the `card: attack`
  Choice.tag; `BarnVictim`/`FrogElderOpen`/`RaiderMotive` are offer cards; `MarshPool` is a fact-setter card;
  `FrogElderClosed`/`StarvingVillage` are linear reactions; `GratefulFarmer`/`FrogMarshThanks` carry the
  quest-loop tags. If the Ink editor package is present it will re-derive any `.json` from its `.ink` on import.

**Authoring constraints / gotchas:** every fact key used by a fragment/story must be in the registry
(else fail-closed + warn); an Ink `fact:` tag may only write a shape declared in its dialogue's
`_declaredFactWrites`; a suspending `start-combat:` must be the last tag in its knot step; combat/quest
slots are optional but a slot-dependent tag firing against an empty slot fails closed.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/` (pure-C# tests, runnable without the editor;
full project suite 1234/1234 green via the clone-project batch runner, 2026-07-05):

- `FactStoreTests` — store ops + B4 presence/default + namespace isolation + stable snapshot + validation.
- `FactVocabularyTests`, `TypedFactsTests` — conversion, Core registry, typed accessors, drift check (D3).
- `SubjectResolverTests` — built-in + arbitrary `$<contextKey>` resolution, scoped world facts (A1).
- `PreconditionEvaluatorTests` — every op, presence/default, actor↔faction AND (R10), fail-closed.
- `FactEffectApplierTests` — op×type (B5), footprint guard by token not arity (W4-1), detached-quest write.
- `FragmentDataTests`, `QuestInstanceTests`, `DialogueSessionAndCastingTests` — Core records + lifecycle + W2-4.
- `DeterministicRandomTests` — serializable PRNG replay (B2).
- `CastingFactoryTests` — tag-match fill, optional omission, deterministic tie-break (D1), derived footprint (W3-2).
- `ActorInstanceFactoryTests` — seeded name pick, faction/archetype propagation, unique instance ids, null fail-closed.
- `EncounterDirectorTests` — eligible→cast→begin, none-eligible/uncastable/null→false, and the R7
  fact-coupling proof **through the orchestrator** (one encounter's fact write changes the next's eligibility).
- `RunDirectorTests` — eligibility filtering and **`CrossStorylet_ChoiceInOneThreadChangesEligibilityInAnother_ViaFacts`** (the executable R7 proof).
- `WorldContentAllocatorTests` — the density allocator: per-kind weight extremes, quest gate
  (every-slot / spacing invariant / unavailable-degrades-without-reset), biome-pool enemy pick +
  empty-pool downgrade, all-zero weights, same-seed identical sequence.
- `RunWindowPlannerTests` — window padding, ambient Loot/Combat emission per density weights (with the
  biome-pool `EnemyId`), quest spacing held across consecutive windows, same-seed identical plans
  across all kinds, R7 eligibility shift across windows,
  archetype match/skip, actor assignment, and the **D11+D16 raider-arc proof**
  (`RecurringActor_RecastIntoMotiveStory_ByActorScopedFact`): an actor-scoped fact written after window N
  makes a motive story eligible in window N+1 and recasts the *same* `NpcInstance`; plus
  `ActorScopedStory_WithoutALiveActor_IsIneligible` (the gate can't pass world-only) and
  `WrongActor_DoesNotSatisfyActorScopedGate`. The **barn-demo partition proof**
  (`BarnDemo_Window2SelectionPartitions_ByWindow1Choices`) drives all four window-1 choice combos and
  asserts each yields exactly one window-2 story (A/B/C), with the two let-go combos recasting the same
  raider `InstanceId` — the executable D5/D15 + D11/D16 acceptance test. The deeper-web cases extend it:
  `PassportFact_FlipsClosedDoorToOpen` (D15/D16 — one world fact on opposite values swaps the closed-door
  story for the open-door one), `CrossActorFork_RaiderCounterOfferEligible_AfterBountyTaken` (the
  cross-actor moral fork — the recast raider's counter-offer becomes eligible after the farmer's bounty is
  taken), and `TwoThreads_BothEligibleInOneWindow` (multi-thread — `barn_raid` + `frog_marsh` beats co-occur).
- `DialogueTagParserTests`, `DialogueRunnerTests` — tag grammar, suspension/resume (B1), empty-slot fail-closed (W2-2),
  continue-gated multi-line pumping, and the card-hand verbs (`CombatAvailable`/`CombatEnemyId`, `TriggerCombat`
  suspend+emit, `Leave` graceful end / refused mid-combat).
- `EncounterCardHandPresenterTests` — card-hand composition (QuestOffer per Ink choice, tagged-vs-system Attack,
  always-present Leave, narration shows only Leave) and pick routing (offer→`SelectChoice`, system attack→`TriggerCombat`,
  tagged attack→Ink choice, Leave→end) (§2.7).
- `ThreadLedgerTests` — thread lifecycle: first-open order, idempotent open, stage/advance flag,
  terminal-state-wins, live count (R8/FR1); plus `ThreadCatalogTests` (implicit ephemeral default).
- `StoryRunLedgerTests` — placed/resolved record: idempotent placement, resolve upsert for
  planner-never-placed (legacy path) stories (FR9).
- `ThreadMaintenanceTests` — retirement semantics: premise conflict (incl. **arc** threads),
  ephemeral-only expiry, advance rearms the clock, conflict-beats-expiry, resolution-beats-conflict,
  the `thread_retired` indicator write, retired threads untouched by later ticks (FR4–FR6, S6–S8).
- `RunWindowPlannerThreadTests` — the P2-3 planner acceptance suite:
  `PlacedStory_NotRePlacedNextWindow_EvenWithTruePreconditions` (**the FR9 bug-fix proof** — fails on
  the pre-P2-3 planner), `ResolvedStory_NeverRePlaced`, `FailedThreadBeat_NeverPlaced`,
  `AmbientColourStory_StaysOutsideTheRunLedger` (chatter may repeat),
  `ConsequenceBeat_HeldUntilCauseFactLive_NeverBeforeCause` (FR8 causal order),
  `AtCap_NewThreadOpenerNotPlaced_SlotDegrades` / `AtCap_AdvancingBeatStillPlaced` /
  `BelowCap_AdvancePreferredOverOpen` (FR3/FR7 ceiling + advance-over-open),
  `ConflictedThread_StoriesExcluded_IndicatorWritten_NoClosureBeat`,
  `EphemeralThread_ExpiresThroughPlannerTicks_ArcDoesNot`, and
  `SameSeedAndSameResolutions_IdenticalThreadLifecycle` (FR12 deterministic replay).
- `StoryResolutionRelayTests` — encounter outcome → ledgers: engaged end advances the thread,
  `Leave` resolves the story but not the thread (the errand still lapses), story-less castings are
  ignored, the post-combat resume advances exactly once.
- `NarrativeSnapshotTests` — fact store round-trip, stable order, PRNG capture (B2), suspended-save
  refusal (W3-1), the **run/meta horizon partition** (`Capture_SplitsRunAndMetaByHorizon` /
  `Restore_RoundTripsBothHorizons`, D20 — unknown keys partition as run-scoped), and the thread/story
  ledger round-trip (`Ledgers_CaptureAndRestore_RoundTripThreadLifecycleAndStoryRecord`).

Unity-side classes (SOs, mappers from SO, `NarrativeSliceInstaller`, Ink) are compile-checked and
verified by entering the slice scene; the Ink→JSON compile and `.asset` wiring are editor steps.

---

## 6. Known limitations / open points

> **Planned design (NOT implemented).** The following are deferred (see ROADMAP):

- **Director pacing.** `RunDirector` (the legacy per-encounter selector) selects by eligibility +
  seeded pick only; it also bypasses the thread ledgers (its resolutions upsert into the
  `StoryRunLedger` via the relay, but it does not open threads or respect the ceiling). The
  streaming path is the governed one.
- **Thread resolution is AND-only.** A fork payoff ("grain recovered OR raider paid off") cannot be
  expressed as `_resolutionConditions` until OR-composition lands (P3-5) — such threads (the demo
  `barn_raid`) leave resolution empty and close by expiry/conflict instead.
- **Reactive-rule cascade layer (R11).** Cascades are expressed as explicit authored effects; a central
  reaction layer is deferred.
- **OR/boolean precondition composition.** Preconditions are AND-only.
- **Save/load file IO (R14).** The serializable boundary (`INarrativeSaveService`, snapshot DTOs, PRNG
  state, the run/meta fact partition + thread/story ledgers) exists and is tested; the file
  writer/reader, the full run-state aggregate (quests/castings/sessions assembly), and the **cross-run
  meta-fact store** (what actually carries `FactHorizon.Meta` facts between runs) are deferred
  (P2-2). Suspended dialogues are non-savepoints (W3-1 option a).
- **Live-actor registry is not yet save-captured.** `ILiveActorRegistry` (recurring-actor casting,
  D11) holds the run's minted actors in memory; `RunNarrativeSnapshot` does not yet persist/repopulate
  it, so a mid-run save would lose recurring-actor continuity. Folds into the window/horizon save-state
  item (ROADMAP).
- **Quest-spacing counter is not yet save-captured.** `WorldContentAllocator` carries
  `platformsSinceQuest` across windows in memory only; a mid-run save/reload would reset quest spacing.
  Folds into the same window/horizon save-state item (ROADMAP).
- **Site allocator state is not yet save-captured.** `SiteAwareSlotAllocator` (world-sites) adds a
  pending block queue, a site-spacing counter, and a site instance counter — all run-scoped, in
  memory only; a mid-run save/reload would drop a half-drained site block. Folds into the same
  window/horizon save-state item (ROADMAP).
- **Biome journey save-state.** The biome now advances along the run (`biome-journey.md` — the
  fixed-Forest hardcode is gone), but the journey's own `DeterministicRandom` state is not yet
  captured by the save boundary; folds into the same window/horizon save-state item (ROADMAP).
- **Remaining director gaps (design handoff `narrative-director-requirements.md`).** With D13/D14
  (first-class threads, closure pressure, concurrency cap) and the D20 run/meta **boundary** shipped
  (P2-3), the director still owes: D7 spine reserved lane + per-run reveal cap (P3-1), D19 escalation
  tier gating (P3-2), and the D20 cross-run **consumers** (mirror-lore echoes, cauldron memory, spine
  cursor — read side of the meta horizon, P3-3 after P2-2). A player-facing thread readout / saga
  view (surfacing `ThreadLedger` state + the `thread_retired` indicators) sits with the quest-log UI
  item (P1-11).
- **Ambient/character dialogue channel.** The legacy dual-Ink bark channel is intentionally dropped;
  if needed, it belongs in a separate non-narrative system.
- **Whole-dialogue skip/abort.** The view's continue and skip inputs both advance one gated line
  (`DialogueRunner.Continue`); a true skip-to-end / abort of the whole conversation has no runner path yet.

### Encounter card model — replaces branching dialogue (scaffold DONE; deeper verbs deferred)

Design intent from `design/narrative/npc-encounter-cards.md` (decisions 2026-06-27; ROADMAP
"Data-Driven Procedural Narrative" + "Quests"). An NPC encounter becomes **a hand of action-cards**,
not a read-the-lines-and-pick-a-reply conversation. **The MVP card-hand scaffold is now implemented —
see §2.7** (situation bubble + composed QuestOffer/Attack/Leave card hand over the unchanged runner).
The remaining items below are still deferred. What changes vs. the as-implemented flow above (§2.3/§2.6):

- **Two visual registers replace the dialogue panel.** A plain **situation bubble** (one terse line
  above the NPC, ambient) and, on approach, a **centred hand of 1..N typed cards**. Every player
  action in the short exchange = picking a card. This replaces the line-by-line, continue-gated
  reading UI and the Ink **choice list** (`DialogueRunner.OnChoices`/`SelectChoice`/`StoryChoice` →
  `IDialogueView.ShowChoices`, `DialogueRunnerViewPresenter.HandleChoices`).
- **Card types:** *quest-offer* (the ornate framed card — glow=tier, color=belonging, item hidden;
  see [Quest Subsystem](quest-subsystem.md) §6), *combat/attack* (present only on some NPCs; the
  Monster verb — see Quest §6), *leave/skip*.
- **What is kept (do not remove):** the tag/effect machinery is the **card outcome channel**. A
  chosen card still fires `fact:` / `offer-quest:` / `advance-objective:` / `complete-quest:` /
  `start-combat:` against the casting's footprint exactly as today — only the *presentation +
  choice-selection* layer changes. Facts remain the sole coupling between stories (R7).
- **To remove (planned):** the branching-choice presentation — the Ink `*`-choice authoring in the
  field `.ink` files, the runner's `OnChoices`/`SelectChoice` path and `StoryChoice`, and the
  dialogue-reading parts of `IDialogueView` (`SetDialogueText` / `ShowChoices` / `PlayTypewriterEffect`
  / continue-gating) + their `DialogueRunnerViewPresenter` handlers. They are superseded by a
  card-hand View + Presenter (MVP) over the same runner events. (Today's field stories are already
  flat — `BarnVictim`/`BarnRaid` carry only accept/decline and fight/let-go — so this is a
  presentation swap + one new verb, not a teardown of the fact/quest engine.)
- **Multiple offers on one NPC.** A single story/NPC may present **several quest-offer cards at once**
  (several resolution paths to one situation), bound by "different currency, not more". This is a new
  authoring shape — see [Quest Subsystem](quest-subsystem.md) §6. Distinct from the **cross-actor
  moral fork** (victim vs. robber), which stays time-separated on a shared-actor thread (Quest §6).
- **Attack card = player trigger for thread closure (D13).** Choosing the combat card (or NPC
  self-initiation) closes that actor's thread (`narrative-director-requirements.md` D13), routes
  corpse-loot to the **separate** combat/mutation loot channel (not the quest economy), writes
  Conquest/path facts, and fires the cauldron tempter bark (Quest §6).
- **PerLocation-scope content.** The data shape supports it (A1); no slice content uses it yet.
- **Legacy cutover — done.** The old `NpcDefinition`/`StoryDefinition`/`CompositeDialoguePresenter`/
  `LevelNarrativeGenerator` path, `NarrativeInstaller`, and assets are deleted; `DialogueActiveState`/
  `NpcContent` run the streaming engine. (`narrative-generation.md` is superseded.) The unused one-shot
  `ScenarioGenerator`/`PlatformGraphGenerator` pipeline was deleted too.
