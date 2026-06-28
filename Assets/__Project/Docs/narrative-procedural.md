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
windowed planner + serializable PRNG (seeded from the run seed via `LootSeed.Derive`), the actor-instance
factory + encounter orchestrator (§2.5), the dialogue session/runner/tag-parser, the
`IEncounterCardHandView` + `EncounterCardHandPresenter` (the card-hand UI, §2.7; instantiated from the
`Resources` prefab), and `INarrativeSaveService`. A `NarrativeSliceBootstrap` `IInitializable` runs
footprint derivation + typed-ref validation after build.

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

### `QuestDefinition` (+ `QuestObjectiveDefinition`, `QuestRewardSerial`)  (asset menu: `Create → Narrative → Quests → Quest`)

The quest fragment, its lifecycle (offer → advance → complete/fail), and its item rewards are owned by
the **[Quest Subsystem](quest-subsystem.md)** doc (full field reference + authoring recipes there).
Summary: `_questId`, `_displayName`, `_summary`, `_objectives`, `_questTags`, `_onCompleteEffects`,
`_onFailEffects`, `_rewards`. The union of all its effect shapes is the quest's own footprint.

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

### Ready-made Demo content

A `Demo*` asset set ships under `Resources/Narrative/` (`Facts/`, `Actors/`, `Dialogue/`, `Enemies/`,
`Stories/`) — ten `FactKeyDefinition`s + `DemoFactKeyRegistry`. When the
`NarrativeSliceInstaller` inspector lists are left empty it **auto-loads** these from those Resources
paths (`ResolveAssetsFromResources`), so the slice works without per-scene wiring; assigning assets in
the inspector overrides the fallback.

### The shipped slice — two parallel threads (barn arc + marsh passport)
Two fact-driven threads run side by side, exercising **D5/D15 fact-based selection**, the **D11/D16
recurring-actor** path, the **cross-actor moral fork** (`quest-as-reward.md` §4), **passport/faction
gating** (D15/D16), and **multi-thread** within-window coherence (D12/D14). All archetypes use
`PlaceholderAssembly_A` for a visible body.
- Facts (all Bool, Global, default false, in `DemoFactKeyRegistry` — except `looted_barn`, PerActor):
  *barn_raid thread* — `world.barn_raided` (raider world gate), `actor.looted_barn` (raider-arc carry),
  the partition facts `world.barn_quest_offered`, `world.barn_quest_accepted`, `world.grain_recovered`,
  `world.raider_bribed`, and `world.raider_offer_taken` (the fork's power side). *frog_marsh thread* —
  `world.reads_as_frogfolk` (the passport fact, D15/D16), `world.frog_quest_offered`,
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
  `dlg_marsh_pool`/`MarshPool.ink` (the passport **setter** — a card writes `world.reads_as_frogfolk`),
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
  purely on the **passport fact** `world.reads_as_frogfolk` (D15/D16), which a card sets (standing in for the
  mutation→fact projection):
  - While `reads_as_frogfolk == false`: `story_marsh_pool` (the setter card → sets `reads_as_frogfolk`) and
    `story_frog_elder_closed` (the closed-door reaction, no quest) are eligible. Taking the MarshPool card
    **flips the passport true**.
  - Once `reads_as_frogfolk == true`: the closed/pool stories drop out and `story_frog_elder_open` becomes
    eligible (guarded by `frog_quest_offered == false` so it offers once), OFFERING `qst_frog_errand`
    (access currency) and writing `frog_quest_accepted`.
  - With `frog_quest_accepted == true`: `story_frog_marsh_thanks` (the thread's second sequential beat)
    advances + completes the errand. The same one fact flipping false→true swapping `FrogElderClosed` for
    `FrogElderOpen` is the **passport flip** in miniature.
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

Edit-mode suites in `Assets/__Project/Tests/EditMode/` (132 pure-C# tests, runnable without the editor):

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
