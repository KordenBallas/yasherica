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
- **R8 — Threads/arcs.** (Labels only this pass — see §6.)
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
  Stories/Core+Data StoryTemplateData / StorySlot ; StoryTemplate SO + mapper
  Casting/Core   Casting, ContextBag, FragmentLibrary, CastingFactory, EnemyFragment
  Director/Core  RunDirector, StoryletSelection, DeterministicRandom (serializable PRNG)
  Runtime/Core   NarrativeSliceBootstrap (footprint derivation + ref validation)
  Runtime/Snapshots  save DTOs + NarrativeSaveService (boundary)
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
| `DialogueRunner` | Drives one `DialogueSession`; dispatches Ink tags to facts/quest/combat (R4); explicit suspension state machine — `AwaitingExternal` for async combat, `AwaitingContinue` to gate one readable line at a time (`Continue()` advances). |
| `DialogueRunnerViewPresenter` | MVP presenter (pure-C#): turns the runner's events into `IDialogueView` calls and forwards the view's choice/continue/skip input back into the runner; re-exposes combat/quest signals. |
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
   `fact:` writes (footprint-gated), `offer-quest:` starts the quest, `start-combat:` suspends until
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
windowed planner + serializable PRNG (seeded from the run seed via `LootSeed.Derive`), the actor-instance
factory + encounter orchestrator (§2.5), the dialogue session/runner/tag-parser, the `IDialogueView`
(instantiated from the `Resources` prefab), and `INarrativeSaveService`. A `NarrativeSliceBootstrap`
`IInitializable` runs footprint derivation + typed-ref validation after build.

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

### 2.6 Windowed director (planner core — selection is story-first)

Story selection is **story-first and budgeted**, planned a window at a time (a window = the next
`WindowSize` platforms ahead of the player). `RunWindowPlanner` (`IRunWindowPlanner`,
`Narrative.Director.Core`) is pure C# and deterministic:

`PlanWindow(windowIndex, IFactStore) → WindowPlan`
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
2. **Combat minimum** — place combat-bearing stories (those with a `Combat` slot) until
   `MinCombatPerWindow` is met.
3. **Narrative fill** — add eligible stories while the summed `StoryTemplate.Weight` stays within
   `NarrativeBudgetPerWindow` and combat stays under `MaxCombatPerWindow`. Combat is a **separate budget
   dimension** from narrative weight.
4. **Pad** — fill the rest of the window with empty fillers.

Selection prefers continuing a thread already chosen this window (coherence), then a seeded pick among
ties so plans are save-replayable (B2). A **world-only** placed story gets an actor minted via
`IActorInstanceFactory` and **registered** in `ILiveActorRegistry`, so it can be recast later (R12/D11);
its archetype is chosen by a **soft preference**, not a hard filter (P1: hard requirements prune,
preferences only weight) — the planner prefers an `NpcArchetype` whose tags overlap the story's tags, but
falls back to any archetype when none overlap. An **actor-scoped** placed story instead reuses the
**pinned** actor resolved by the casting query above; the pin is a **hard pin** (D11) that bypasses the
soft tag preference so an arc cannot be broken by casting a different archetype. Output `WindowPlan` is an
ordered list of `PlannedPlatform` (`Story`/`Combat`/`Loot`/`Empty`) the level generator maps to platforms.

`ILiveActorRegistry`/`LiveActorRegistry` (`Narrative.Actors.Core`, pure C#) is the run-scoped set of
minted actors in deterministic registration order — the seam recurring-actor casting reads. It is bound
`AsSingle` for the run; repopulating it on load is part of the deferred window/horizon save-state (§6).

**Streaming & entry (now wired).** `RunStreamingCoordinator` (`LevelGeneration`) drives generation:
`Begin()` generates window 0; on each `PlatformEvents.OnPlatformEntered` into the current frontier it
locks that window and plans + generates the next against the live store. Each planned story platform is
realised as an `NpcContent` carrying the minted actor + committed story (its visual spawns from the
archetype assembly via `IModularCharacterFactory`; `INpcArchetypeCatalog` resolves id → archetype SO).
On entry, `DialogueActiveState` calls `EncounterDirector.BeginPlanned(story, actor)` (cast + begin, no
re-selection) and routes outcomes: combat → `EnemyContent` + `CombatActiveState`, then
`CombatActiveState` feeds `DialogueRunner.ReportCombatResult` so post-combat lines/facts replay before
the platform completes; a normal end completes the platform. `AreaSceneEntrypoint` drives the coordinator
instead of the legacy generator.

Open points this stage: loot is not placed on the streaming path (fillers are empty), biome is fixed
(Forest). An archetype with no `_assembly` runs its dialogue but spawns no visible NPC body (logged warning).

---

## 3. ScriptableObject Reference

### `FactKeyDefinition`  (asset menu: `Create → Narrative → Facts → Fact Key`)

The single source of truth for one fact key (R13).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_namespace` | `FactNamespace` | Grouping label: World/Actor/Faction | — |
| `_scope` | `FactScope` | Subject arity: Global / PerActor / PerFaction / PerLocation (A1) | arity comes from here, NOT the namespace |
| `_key` | string | Bare key name, e.g. `barn_raided` | no namespace prefix |
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

### `QuestDefinition` (+ `QuestObjectiveDefinition`)  (asset menu: `Create → Narrative → Quests → Quest`)

`_questId`, `_displayName`, `_summary`, `_objectives` (`_objectiveId`, `_description`, `_kind`,
`_targetCount`, `_completionEffects`), `_questTags`, `_onCompleteEffects`, `_onFailEffects`
(`FactEffectSerial[]`). The union of all its effect shapes is the quest's own footprint.

### `StoryTemplate` (+ `StorySlotDefinition`)  (asset menu: `Create → Narrative → Stories → Story Template`)

`_storyId`, `_slots` (`_slotId`, `_kind` Dialogue/Quest/Combat, `_requiredTags`, `_optional`),
`_preconditions` (`FactPredicateSerial[]`), `_ownEffects` (optional story-level writes), `_storyTags`,
`_threadId` (label only), `_isSpine`, `_weight` (pacing cost — how much of a window's narrative budget
this story consumes; a story is still one platform, NOT a difficulty or span measure). References no other
template (R7). The effect footprint is **derived** by `CastingFactory` over the library (W3-2), not
authored here.

`EnemyDefinition` (Combat) gains `_enemyTags` so an enemy matches a story combat slot by tag (W2-6).

### `RunPacingConfig`  (asset menu: `Create → Narrative → Director → Run Pacing Config`)

The windowed director's pacing budget (consumed as the Core `RunPacingSettings` via
`RunPacingConfigMapper`, never directly). `_windowSize` (platforms per planning window),
`_lookAheadWindows`, `_narrativeBudgetPerWindow` (max summed story weight), `_minCombatPerWindow` /
`_maxCombatPerWindow` (combat is a separate budget dimension).

**Authoring structs:** `FactPredicateSerial` (`namespace`, `subjectToken`, `key`, `op` `ComparisonOp`,
typed value), `FactEffectSerial` (same with `op` `FactEffectOp`), `FactKeyShape` (namespace, subject
token, key, value type — a permitted write target, no op/value).

---

## 4. Adding Content

A designer assembles the vertical slice (or new narrative content) entirely from assets. Recipe:

### Add a fact key
1. `Create → Narrative → Facts → Fact Key`; set namespace, **scope**, key, value type, default.
2. Add it to the `FactKeyRegistry` asset's `_keys` list.

### Add an actor archetype
1. `Create → Narrative → Actors → Archetype`; set id, name pool, assembly, portrait, faction id, tags.

### Add a dialogue
1. Author a `.ink` file under `Resources/Stories/...` using only tags to reach systems
   (`speaker:`, `fact:`, `offer-quest:`, `start-combat:`, `outcome:`). Declare any variables the runner
   injects (`npc_name`, `quest_available`, `combat_available`, `combat_won`, `quest_accepted`).
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

### Ready-made Demo content

A `Demo*` asset set ships under `Resources/Narrative/` (`Facts/`, `Actors/`, `Dialogue/`, `Enemies/`,
`Stories/`) — the six barn `FactKeyDefinition`s + `DemoFactKeyRegistry`. When the
`NarrativeSliceInstaller` inspector lists are left empty it **auto-loads** these from those Resources
paths (`ResolveAssetsFromResources`), so the slice works without per-scene wiring; assigning assets in
the inspector overrides the fallback.

### The shipped slice — the Barn arc (two-window reactive demo)
A single fact-driven storyline that exercises **D5/D15 fact-based selection** and the **D11/D16
recurring-actor** path. All archetypes use `PlaceholderAssembly_A` for a visible body.
- Facts (all in `DemoFactKeyRegistry`): `world.barn_raided` (Bool, Global — raider world gate),
  `actor.looted_barn` (Bool, **PerActor** — the raider-arc carry fact), and the four partition facts
  `world.barn_quest_offered`, `world.barn_quest_accepted`, `world.grain_recovered`, `world.raider_bribed`
  (all Bool, Global, default false).
- Archetypes: `arch_barn_raider` (`raider`,`can-fight`), `arch_villager` (`villager`,`farmer` — the barn
  victim + both window-2 villager reactions).
- Dialogues: `dlg_barn_raid`/`BarnRaid.ink` (two choices: **fight** → `start-combat:` + on `combat_won`
  writes `world.grain_recovered` and clears `actor.$self.looted_barn`; **let-go** → writes
  `world.raider_bribed`, leaves `looted_barn` true; both also write `world.barn_raided` +
  `actor.$self.looted_barn`), `dlg_raider_motive`/`RaiderMotive.ink` (clears `actor.$self.looted_barn`),
  `dlg_barn_victim`/`BarnVictim.ink` (writes `world.barn_quest_offered` + `world.barn_quest_accepted`),
  `dlg_grateful_farmer`/`GratefulFarmer.ink` and `dlg_starving_village`/`StarvingVillage.ink` (reaction
  beats, no fact writes). Uses `enemy_bandit_brute` (tag `bandit`) for the raid combat slot.
- The `barn_raid` thread spans two windows:
  - *Window 1* places two openers (both world-gated, so they co-appear): `story_barn_victim` (precond
    **world** `barn_quest_offered == false`; tags `villager`,`barn` → villager) where the player accepts or
    refuses the plea (writes `world.barn_quest_accepted`); and `story_barn_raid`
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
    | **A** `story_grateful_farmer` | `barn_quest_accepted == true` **AND** `grain_recovered == true` | fresh `arch_villager` |
    | **B** `story_raider_motive` | **actor-scoped** `actor.$self.looted_barn == true` | the **same** raider, recast (hard pin, §2.6) |
    | **C** `story_starving_village` | `barn_quest_accepted == false` **AND** `grain_recovered == true` | fresh `arch_villager` |

    B's `motive` tag deliberately overlaps no archetype — placement proves the recast pin overrides the P1
    tag preference. This is the in-engine analogue of the `RunWindowPlannerTests` partition + raider-arc proof.
- *Pacing:* the slice relies on the installer's default `RunPacingSettings` (no `RunPacingConfig` asset wired):
  `windowSize 4`, `narrativeBudgetPerWindow 30` (two weight-10 openers fit window 1), `maxCombatPerWindow 2`,
  `minCombatPerWindow 1` — satisfied in window 1 by `story_barn_raid`'s combat-bearing optional slot.
- *Ink compile:* the barn dialogues `BarnRaid`, `BarnVictim`, `GratefulFarmer`, `StarvingVillage` ship with
  **seed compiled JSON** (resolves the `DialogueDefinition` reference); the editor's auto-compiler
  (`compileAllFilesAutomatically`) re-derives the real `.json` (with choices/branches/combat) from the `.ink`
  on import. `RaiderMotive` ships with real compiled JSON.

**Authoring constraints / gotchas:** every fact key used by a fragment/story must be in the registry
(else fail-closed + warn); an Ink `fact:` tag may only write a shape declared in its dialogue's
`_declaredFactWrites`; a suspending `start-combat:` must be the last tag in its knot step; combat/quest
slots are optional but a slot-dependent tag firing against an empty slot fails closed.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/` (96 pure-C# tests, runnable without the editor):

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
- `RunWindowPlannerTests` — budget cap, combat min/max, determinism, R7 eligibility shift across windows,
  archetype match/skip, actor assignment, and the **D11+D16 raider-arc proof**
  (`RecurringActor_RecastIntoMotiveStory_ByActorScopedFact`): an actor-scoped fact written after window N
  makes a motive story eligible in window N+1 and recasts the *same* `NpcInstance`; plus
  `ActorScopedStory_WithoutALiveActor_IsIneligible` (the gate can't pass world-only) and
  `WrongActor_DoesNotSatisfyActorScopedGate`. The **barn-demo partition proof**
  (`BarnDemo_Window2SelectionPartitions_ByWindow1Choices`) drives all four window-1 choice combos and
  asserts each yields exactly one window-2 story (A/B/C), with the two let-go combos recasting the same
  raider `InstanceId` — the executable D5/D15 + D11/D16 acceptance test.
- `DialogueTagParserTests`, `DialogueRunnerTests` — tag grammar, suspension/resume (B1), empty-slot fail-closed (W2-2), continue-gated multi-line pumping.
- `NarrativeSnapshotTests` — fact store round-trip, stable order, PRNG capture (B2), suspended-save refusal (W3-1).

Unity-side classes (SOs, mappers from SO, `NarrativeSliceInstaller`, Ink) are compile-checked and
verified by entering the slice scene; the Ink→JSON compile and `.asset` wiring are editor steps.

---

## 6. Known limitations / open points

> **Planned design (NOT implemented).** The following are deferred (see ROADMAP):

- **First-class threads (R8).** `_threadId` is a string label this pass; first-class `Thread` entities
  with an id + stage, and director thread-balancing, are deferred. `CrossStoryletEligibilityTests`
  proves R7 cross-storylet coupling; threads here are labels.
- **Director pacing.** `RunDirector` selects by eligibility + seeded pick only; no pacing/quotas.
- **Reactive-rule cascade layer (R11).** Cascades are expressed as explicit authored effects; a central
  reaction layer is deferred.
- **OR/boolean precondition composition.** Preconditions are AND-only.
- **Save/load file IO (R14).** The serializable boundary (`INarrativeSaveService`, snapshot DTOs, PRNG
  state) exists and is tested; the file writer/reader and full run-state aggregate (quests/castings/
  sessions assembly) are deferred. Suspended dialogues are non-savepoints (W3-1 option a).
- **Live-actor registry is not yet save-captured.** `ILiveActorRegistry` (recurring-actor casting,
  D11) holds the run's minted actors in memory; `RunNarrativeSnapshot` does not yet persist/repopulate
  it, so a mid-run save would lose recurring-actor continuity. Folds into the window/horizon save-state
  item (ROADMAP).
- **Remaining director gaps (design handoff `narrative-director-requirements.md`).** Beyond the
  Priority-1 actor/faction eligibility (D16) + recurring-actor casting (D11) delivered here, the
  director still owes: D7 spine reserved lane + per-run reveal cap, D13/D14 first-class threads with
  closure pressure + concurrency cap, D19 escalation tier gating, D20 meta-scoped fact horizon.
- **Ambient/character dialogue channel.** The legacy dual-Ink bark channel is intentionally dropped;
  if needed, it belongs in a separate non-narrative system.
- **Whole-dialogue skip/abort.** The view's continue and skip inputs both advance one gated line
  (`DialogueRunner.Continue`); a true skip-to-end / abort of the whole conversation has no runner path yet.
- **PerLocation-scope content.** The data shape supports it (A1); no slice content uses it yet.
- **Legacy cutover — done.** The old `NpcDefinition`/`StoryDefinition`/`CompositeDialoguePresenter`/
  `LevelNarrativeGenerator` path, `NarrativeInstaller`, and assets are deleted; `DialogueActiveState`/
  `NpcContent` run the streaming engine. (`narrative-generation.md` is superseded.) The unused one-shot
  `ScenarioGenerator`/`PlatformGraphGenerator` pipeline was deleted too.
