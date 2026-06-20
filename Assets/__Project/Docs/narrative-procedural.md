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
| `DialogueRunner` | Drives one `DialogueSession`; dispatches Ink tags to facts/quest/combat (R4); explicit suspension state machine for async combat. |
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
   `ReportCombatResult` resumes it; `speaker:`/`outcome:` drive presentation/termination.
5. Facts written by one story change the eligibility of another at the next `SelectNext` — the only
   coupling between stories (R7).

### 2.4 DI wiring

`Core.DI.NarrativeSliceInstaller` (slice scene only; legacy `NarrativeInstaller` untouched) binds the
fact store/evaluator/applier/resolver, the fragment library + storylets (mapped from inspector SO
lists), the casting factory + director + serializable PRNG (seeded from the run seed via
`LootSeed.Derive`), the dialogue session/runner/tag-parser, and `INarrativeSaveService`. A
`NarrativeSliceBootstrap` `IInitializable` runs footprint derivation + typed-ref validation after build.

---

## 3. ScriptableObject Reference

### `FactKeyDefinition`  (asset menu: `Create → Narrative → Facts → Fact Key`)

The single source of truth for one fact key (R13).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_namespace` | `FactNamespace` | Grouping label: World/Actor/Faction | — |
| `_scope` | `FactScope` | Subject arity: Global / PerActor / PerFaction / PerLocation (A1) | arity comes from here, NOT the namespace |
| `_key` | string | Bare key name, e.g. `pass_cleared` | no namespace prefix |
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
`_threadId` (label only), `_isSpine`. References no other template (R7). The effect footprint is
**derived** by `CastingFactory` over the library (W3-2), not authored here.

`EnemyDefinition` (Combat) gains `_enemyTags` so an enemy matches a story combat slot by tag (W2-6).

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

### The shipped slice ("The Toll at Razor Pass")
- Facts: `world.pass_blocked` (Bool, Global, default true), `world.pass_cleared` (Bool, Global),
  `actor.hostile` (Bool, PerActor), `faction.reputation` (Int, PerFaction).
- `arch_road_bandit` (tags `bandit`, `can-fight`); `dlg_toll_shakedown` → `RazorPassToll.ink`
  (`_declaredFactWrites`: `world.pass_cleared`, `actor.$self.hostile`); `qst_clear_pass`
  (`_onCompleteEffects`: `world.pass_cleared = true`); `enemy_bandit_brute` (tag `bandit`).
- `story_razor_pass_toll` (slots Dialogue[`shakedown`], Quest[`errand`,opt], Combat[`bandit`,opt];
  precondition `world.pass_blocked == true`; thread `road`).
- `story_grateful_caravan` (precondition `world.pass_cleared == true`; dialogue `dlg_caravan_thanks`
  → `CaravanThanks.ink`; thread `trade`) — ineligible until the toll story clears the pass.

**Authoring constraints / gotchas:** every fact key used by a fragment/story must be in the registry
(else fail-closed + warn); an Ink `fact:` tag may only write a shape declared in its dialogue's
`_declaredFactWrites`; a suspending `start-combat:` must be the last tag in its knot step; combat/quest
slots are optional but a slot-dependent tag firing against an empty slot fails closed.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/` (84 pure-C# tests, runnable without the editor):

- `FactStoreTests` — store ops + B4 presence/default + namespace isolation + stable snapshot + validation.
- `FactVocabularyTests`, `TypedFactsTests` — conversion, Core registry, typed accessors, drift check (D3).
- `SubjectResolverTests` — built-in + arbitrary `$<contextKey>` resolution, scoped world facts (A1).
- `PreconditionEvaluatorTests` — every op, presence/default, actor↔faction AND (R10), fail-closed.
- `FactEffectApplierTests` — op×type (B5), footprint guard by token not arity (W4-1), detached-quest write.
- `FragmentDataTests`, `QuestInstanceTests`, `DialogueSessionAndCastingTests` — Core records + lifecycle + W2-4.
- `DeterministicRandomTests` — serializable PRNG replay (B2).
- `CastingFactoryTests` — tag-match fill, optional omission, deterministic tie-break (D1), derived footprint (W3-2).
- `RunDirectorTests` — eligibility filtering and **`CrossStorylet_ChoiceInOneThreadChangesEligibilityInAnother_ViaFacts`** (the executable R7 proof).
- `DialogueTagParserTests`, `DialogueRunnerTests` — tag grammar, suspension/resume (B1), empty-slot fail-closed (W2-2).
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
- **Ambient/character dialogue channel.** The legacy dual-Ink bark channel is intentionally dropped;
  if needed, it belongs in a separate non-narrative system.
- **Dialogue view adapter.** `DialogueRunner` exposes events; a Unity `IDialogueView` adapter that
  subscribes to them is not yet wired into the slice installer.
- **PerLocation-scope content.** The data shape supports it (A1); no slice content uses it yet.
- **Legacy cutover.** The old `NpcDefinition`/`StoryDefinition`/`CompositeDialoguePresenter` path
  remains alongside the new system; migration of `DialogueActiveState`/`NpcContent` and deletion of
  legacy assets is a later, separate step.
