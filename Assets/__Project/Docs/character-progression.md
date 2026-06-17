# Character Progression — Requirements & Design

> Tracks per-run progression state — quests (active / completed / failed), NPCs encountered, and key
> choices — and exposes it as a queryable record so other systems can react to what the player has
> done this run. This is the M2 substrate the Quests and progression-driven-narrative work build on.
> Status: current as of 2026-06-17.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** Maintain per-run quest state keyed by quest id, with three statuses: `Active`, `Completed`,
  `Failed`.
- **R2** A terminal status (`Completed` / `Failed`) takes precedence over `Active`: starting an
  already-terminal quest does not demote it; completing/failing always overrides.
- **R3** Record which NPCs have been encountered (dialogue started) this run.
- **R4** Record key choices as key/value string pairs (latest write wins).
- **R5** Expose a read-only query surface (status lookups, membership checks, enumerations) separate
  from the write surface (ISP).
- **R6** Evaluate a content gating **condition** string against the record. Grammar (single
  predicate): empty/null → passes; `quest_completed:<id>`, `quest_active:<id>`, `quest_failed:<id>`,
  `npc_encountered:<id>`. An unknown prefix or malformed string fails closed and logs a warning.
- **R7** The narrative reward resolver consumes R6 to gate `RewardSlot.Condition` before rolling a
  reward (see `narrative-generation.md` §2.5).
- **R8** Record NPC encounters and quest-active transitions from the existing dialogue flow without
  new authoring surface: an Ink `start_quest` signal marks a quest active; starting an NPC dialogue
  marks the NPC encountered.

### 1.2 Non-functional requirements

- **N1** Core logic is pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject; no service locators.
- **N3** No new ScriptableObject types — this system owns no authorable data yet.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/CharacterProgression/
  Core/
    QuestStatus.cs               — enum { Active, Completed, Failed }
    IRunProgressionRecord.cs     — read surface (consumers)
    IRunProgressionRecorder.cs   — write surface (recorders)
    RunProgressionRecord.cs      — single impl of both
    RunConditionEvaluator.cs     — condition-string parser over the record
Scripts/Core/DI/AreaInstaller.cs — binds the record + evaluator
```

The whole system lives in the `Yasherica` assembly; the core has no UnityEngine references
(`RunConditionEvaluator` depends only on `Core.Logging.IGameLogger`).

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `QuestStatus` | Quest lifecycle enum (`Active` / `Completed` / `Failed`). |
| `IRunProgressionRecord` | Read: `GetQuestStatus`, `IsQuestActive/Completed/Failed`, `HasEncounteredNpc`, `TryGetChoice`, and `ActiveQuests` / `CompletedQuests` / `FailedQuests` / `EncounteredNpcs` enumerations. |
| `IRunProgressionRecorder` | Write: `StartQuest`, `CompleteQuest`, `FailQuest`, `RecordNpcEncounter`, `RecordChoice`. Idempotent; null/empty ids are safe no-ops. |
| `RunProgressionRecord` | Single class implementing both interfaces; backed by a quest `Dictionary`, an encountered-NPC `HashSet`, and a choice `Dictionary`. Enforces the R2 precedence rule. |
| `RunConditionEvaluator` | `Evaluate(condition, record)` → bool, per the R6 grammar; fails closed + warns on bad input. |

### 2.3 Runtime flow

1. `AreaInstaller` binds one `RunProgressionRecord` instance to both interfaces, plus a
   `RunConditionEvaluator`, for the scene's lifetime (same as `RunSeedProvider`).
2. **Recording.** `DialogueActiveState.OnEnter` calls `RecordNpcEncounter(npcId)` when an NPC
   dialogue starts; `HandleQuestTriggered` (driven by the Ink `start_quest` →
   `IDialoguePresenter.OnQuestTriggered` chain) calls `StartQuest(questId)`.
3. **Consuming.** At narrative generation, `RewardResolver` resolves `IRunProgressionRecord` +
   `RunConditionEvaluator` and skips any `RewardSlot` whose `Condition` does not hold.

### 2.4 DI wiring

`AreaInstaller.InstallGameCoreBindings()`:

```csharp
Container.Bind(typeof(IRunProgressionRecord), typeof(IRunProgressionRecorder))
    .To<RunProgressionRecord>().AsSingle();
Container.Bind<RunConditionEvaluator>().AsSingle();
```

`RunConditionEvaluator` resolves `IGameLogger` (bound by `InventoryInstaller`, same SceneContext).
`NarrativeInstaller` injects the record + evaluator into `RewardResolver` via its `FromMethod`
construction.

---

## 3. ScriptableObject Reference

This system owns **no ScriptableObject types**. It records runtime state only; there is nothing for
a designer to author here. Condition strings are authored on other systems' SOs (today:
`RewardSlot.Condition` on `StoryDefinition`, see `narrative-generation.md`).

---

## 4. Adding Content

Not applicable — there is no authorable content. To make a reward conditional on run state, set the
`Condition` field on the story's `RewardSlot` using the §2.2 grammar (recipe in
`narrative-generation.md` §3). New recording hooks or condition predicates are code changes, not
content.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `RunProgressionRecordTests` — quest status transitions and R2 precedence, idempotency,
  null/empty-id safety, NPC-encounter set, choice store, query/enumeration correctness.
- `RunConditionEvaluatorTests` — empty passes; each predicate true/false; whitespace trimming;
  unknown prefix and malformed strings fail closed and warn.
- `RewardResolverConditionTests` — a `RewardSlot` with an unmet condition is skipped; met/empty
  condition resolves; the record-less resolver constructor disables gating.

The recording hooks in `DialogueActiveState` (a thin MonoBehaviour-driven state) are verified
manually in play mode.

---

## 6. Known limitations / open points

- **Single-predicate conditions only.** No boolean AND/OR composition yet.
- **No `StoryNodeRequirement` gating.** `RequiredActiveQuests` / `RequiredEncounteredNpcs` are still
  unused; wiring them into story selection depends on the Quests slice (a `QuestDefinition` SO and
  story-level required-progression fields) and will reuse `RunConditionEvaluator`.
- **No quest completion/failure recording yet.** Only quest-*active* (Ink `start_quest`) and NPC
  encounters are recorded automatically; `CompleteQuest` / `FailQuest` exist on the recorder but are
  not driven from gameplay until the explicit Ink quest-completion signal (Quests slice).
- **No generic choice recording from Ink.** `RecordChoice` ships and is tested, but there is no
  `record_choice` Ink external function wiring it yet.
- **Scene-scoped, not run-scoped across scenes.** The record is an `AsSingle` in the scene context
  (matching `RunSeedProvider`), so it resets on area-scene reload. Cross-scene/cross-session
  persistence depends on the backlog "Run-state persistence/save-load" item; this record is the
  natural thing that item will serialize.
- **`GameContext` not yet backed by the record.** Feeding progression into narrative generation is
  the progression-driven-narrative slice.
