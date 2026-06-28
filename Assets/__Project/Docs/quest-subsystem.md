# Quest Subsystem — Requirements & Design

> Quests are a recombinable narrative fragment: an authored goal (objectives + tags) plus the fact
> effects and item rewards applied when it completes or fails. A quest is offered, advanced, and
> finished entirely through Ink tags on the dialogue that carries it, and it bridges to the
> per-run progression record. Its casting/recombination context (how a quest is matched into a story
> slot and paired with an NPC) lives in [Data-Driven Procedural Narrative](narrative-procedural.md);
> this document owns the quest **fragment, lifecycle, and rewards**.
> Status: current as of 2026-06-27.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** A quest is an authored fragment distinct from the story/dialogue that offers it: identity,
  summary, objectives, tags, on-complete/on-fail fact effects, and item rewards. It references no NPC,
  dialogue, or enemy — it is matched into a story's quest slot by tag (see narrative §2).
- **R2** Lifecycle: `NotStarted → Active → Completed | Failed`. Each transition is idempotent and
  emits the fact effects the caller applies against the quest's own footprint.
- **R3** The lifecycle is driven by Ink tags on the carrying dialogue — the only Ink→C# channel:
  `offer-quest:` starts it, `advance-objective:` advances an objective, `complete-quest:` /
  `fail-quest:` finish it. Tags firing with no active quest fail closed (warn + no-op).
- **R4** Objectives track progress (count toward `_targetCount`) and emit their own completion
  effects, but advancing an objective never completes the quest — completion is the explicit
  `complete-quest:` signal (author control).
- **R5** A finishing quest writes its fact effects gated against **its own footprint** (W2-1), not the
  dialogue session's, so a quest validated and applied after its dialogue has ended still checks
  against the shapes it declared.
- **R6** Every transition is recorded in the per-run progression record (start/complete/fail) so
  run-state-gated content (reward conditions, story gating) sees it. See
  [Character Progression](character-progression.md).
- **R7** Item rewards live on the quest. On completion, each reward (artifact id + count) is granted
  to the player inventory through the platform-completion hook.
- **R8** Cross-dialogue continuity: a live `QuestInstance` outlives the dialogue that offered it. It is
  held in a run-scoped registry keyed by quest id; a later dialogue carrying the same quest (its Quest
  slot resolves the same `QuestData`) restores that instance and advances/completes it. A quest may be
  offered on platform A and completed on a later platform B.

### 1.2 Non-functional requirements

- **N1** `QuestData` / `QuestInstance` / `QuestRewardCore` / `LiveQuestRegistry` are pure C# (no
  UnityEngine) and unit-tested.
- **N2** All wiring is through Zenject; the granter is constructor-injected.
- **N3** Quests are authored as `QuestDefinition` ScriptableObjects; no code per quest.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Narrative/Quests/
  Core/   QuestData, QuestObjective, QuestInstance, QuestState, QuestRewardCore   — pure C#
          ILiveQuestRegistry, LiveQuestRegistry  — run-scoped live-instance store (cross-dialogue, R8)
  Data/   QuestDefinition, QuestObjectiveDefinition, QuestRewardSerial, QuestMapper — SO + mapper
Scripts/Narrative/Dialogue/DialogueRunner.cs   — the Ink tag bridge that drives the lifecycle
Scripts/Loot/Application/QuestRewardGranter.cs — grants item rewards on completion
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `QuestData` | Immutable fragment: id, summary, objectives, tags, on-complete/on-fail effects, `Rewards`, derived `Footprint`. |
| `QuestObjective` | One goal: id, description, kind, target count, completion effects. |
| `QuestInstance` | Per-run mutable state over `QuestData`: `Start` / `AdvanceObjective` / `Complete` / `Fail`; bridges each transition to `IRunProgressionRecorder`. |
| `QuestRewardCore` | An item reward: artifact definition id + count (≥1). Item rewards only. |
| `ILiveQuestRegistry` / `LiveQuestRegistry` | Run-scoped store of live `QuestInstance`s keyed by quest id (idempotent `Register`, `TryGet`, ordered `LiveQuests`). The live counterpart of the progression record (which holds only status flags). Enables cross-dialogue continuity (R8) and is what the reward granter scans. |

### 2.3 Runtime flow (the loop)

1. The dialogue's `offer-quest:` tag → `DialogueRunner.HandleOfferQuest` creates a `QuestInstance`
   from the casting's quest slot, calls `Start()` (records `StartQuest`), **registers it in
   `ILiveQuestRegistry`**, sets the Ink `quest_accepted` variable, and raises `OnQuestStarted`. On a
   later `Begin`, if the casting carries a quest already in the registry (same id), the runner
   **restores** that live instance instead of nulling its active quest — so the offer (platform A) and
   the completion (platform B) can be different dialogues (R8). A restored instance is reused as-is (no
   second `Start()`); an `offer-quest:` re-fired on it is a no-op on lifecycle.
2. `advance-objective: <objectiveId> [amount]` → `AdvanceObjective`; any objective-completion effects
   are applied against the quest footprint.
3. `complete-quest:` → `Complete()` (records `CompleteQuest`, returns on-complete effects, applied
   against the quest footprint), raises `OnQuestCompleted`. `fail-quest:` is the symmetric path.
4. When the platform finishes (dialogue ended **or** combat won), `PlatformCompletedState.OnEnter`
   calls `IQuestRewardGranter.GrantFor`. `QuestRewardGranter` **scans `ILiveQuestRegistry.LiveQuests`**
   for any `Completed` quest not yet paid out (`RewardsGranted`); each `QuestRewardCore` is added to the
   inventory via `IInventoryModel.Add` and the quest is marked granted (idempotent). Reading the registry
   rather than a single dialogue's active quest makes the payout land on platform B regardless of which
   dialogue is current.

### 2.4 DI wiring

`ILiveQuestRegistry` is bound `AsSingle` to `LiveQuestRegistry` in `NarrativeSliceInstaller` (run-scoped,
beside `ILiveActorRegistry`). `DialogueRunner` is bound `AsSingle` in the same installer and
constructor-injects the registry. `QuestRewardGranter` is bound to `IQuestRewardGranter` in
`LootInstaller` and constructor-injects `ILiveQuestRegistry` + `IInventoryModel` (it no longer depends on
`DialogueRunner`). The completion hook is `PlatformCompletedState` (covers both completion routes).

---

## 3. ScriptableObject Reference

### `QuestDefinition` (+ `QuestObjectiveDefinition`, `QuestRewardSerial`)  (asset menu: `Create → Narrative → Quests → Quest`)

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_questId` | string | Stable identity; recorded in the progression record, referenced by reward/story conditions. | empty = invalid |
| `_displayName` | string | Player-facing title. | |
| `_summary` | string | Player-facing description. | |
| `_objectives` | `QuestObjectiveDefinition[]` | Goals; each has `_objectiveId`, `_description`, `_kind`, `_targetCount`, `_completionEffects` (`FactEffectSerial[]`). | empty = no progress gating |
| `_questTags` | string[] | Matches the quest into a story Quest slot by tag (narrative §2). | |
| `_onCompleteEffects` | `FactEffectSerial[]` | Facts written on `complete-quest:`. | part of footprint |
| `_onFailEffects` | `FactEffectSerial[]` | Facts written on `fail-quest:`. | part of footprint |
| `_rewards` | `QuestRewardSerial[]` | Item rewards granted on completion. Each: `_artifactId` (artifact definition id) + `_count` (≥1). | item rewards only |

The union of every fact-write shape the quest can emit (complete + fail + objective effects) is the
quest's **own footprint** — its runtime write-gate. Rewards are item grants, not fact writes, so they
are not in the footprint.

---

## 4. Adding Content

### Add a quest

1. `Create → Narrative → Quests → Quest`; set `_questId`, name, summary.
2. Add `_objectives` (each with a unique `_objectiveId` and a `_targetCount`); optionally give an
   objective `_completionEffects`.
3. Set `_questTags` so a story's Quest slot can match it (`StoryTemplate` slot `_requiredTags`).
4. Author `_onCompleteEffects` / `_onFailEffects` — every fact key used must be in the
   `FactKeyRegistry` (else fail-closed + warn at play time).
5. In the carrying dialogue's `.ink`, drive the lifecycle with tags: `offer-quest: <slotTag>`, then
   (optionally) `advance-objective: <objectiveId>`, then `complete-quest:` or `fail-quest:` on the
   appropriate branch.

### Offer and complete across two platforms (cross-dialogue, R8)

To split a quest over two platforms, give **both** stories a Quest slot whose `_requiredTags` match the
quest's `_questTags` (so each casting resolves the same `QuestData`):

1. **Offer platform (earlier window):** the story's `.ink` fires `offer-quest: <slotTag>` — this mints
   and registers the live instance.
2. **Completion platform (later window):** the continuing story's `.ink` does **not** re-offer; it fires
   `advance-objective:` / `complete-quest:` directly. The runner restores the registered instance on
   `Begin` (same quest id), so completion lands here and the reward is granted on this platform.

The demo wires exactly this: `story_barn_victim` (window 1) offers `qst_barn_bounty` on the "bring your
grain back" branch; `story_grateful_farmer` (window 2) restores and completes it, granting the reward.

### Add an item reward to a quest

1. On the `QuestDefinition`, add a `_rewards` entry: set `_artifactId` to an existing
   `ArtifactDefinition` id and `_count` (≥1).
2. No code or Ink change — rewards are granted automatically when the quest completes and its platform
   finishes.

**Authoring constraints / gotchas:** a `complete-quest:`/`fail-quest:`/`advance-objective:` tag with
no active quest (the dialogue never offered one, or the slot was empty) fails closed with a warning;
completion is explicit — advancing all objectives does **not** auto-complete the quest; reward
`_artifactId` must match an authored artifact or the grant is skipped.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `QuestInstanceTests` — lifecycle transitions, objective advancement, footprint, recorder bridge.
- `DialogueTagParserTests` — parses `complete-quest:` / `fail-quest:` / `advance-objective:`.
- `DialogueRunnerTests` — offer→complete applies effects + records + raises `OnQuestCompleted`; the
  fail path; complete-with-no-active-quest warns and no-ops; objective advancement applies objective
  effects without completing the quest; **cross-dialogue (R8): a quest offered on one `Begin` is
  restored on a later `Begin` carrying the same quest and completed there (same instance, no second
  `Start`).**
- `LiveQuestRegistryTests` — `Register`/`TryGet` round-trip, idempotency on quest id (first instance
  kept), null ignored, registration order preserved.
- `QuestRewardGranterTests` — a `Completed` quest in the registry grants each reward by count to a fake
  inventory; granting is idempotent; an active-but-not-completed quest and no-quest grant nothing.

The DI binding and the `PlatformCompletedState` hook are verified by playing the barn demo.

---

## 6. Known limitations / open points

- Reward types are **item-only**. The legacy multi-type reward machinery (currency / experience /
  ability) was removed; those reward kinds have no receiving system (Cross-cutting ROADMAP item).
- **Restore is by quest id via the casting slot.** A later dialogue restores an in-flight quest only if
  its Quest slot resolves the same `QuestData` (the planner/author must place that quest on the
  continuing platform). There is no out-of-band "advance any quest by id" tag path yet; the
  `complete-quest:`/`advance-objective:` tags still operate on the runner's restored active quest.
- Quest/objective state and the live registry are run-scoped only; not yet captured in the narrative save
  snapshot (ROADMAP "Window/horizon save-state" — now also covers the `ILiveQuestRegistry` set).
- `StoryNodeRequirement` (`RequiredActiveQuests` / `RequiredEncounteredNpcs`) is authored but not yet
  consumed in story selection (Character Progression ROADMAP item).

### Planned — quest-as-reward economy (NOT implemented)

Design intent from `design/narrative/quest-as-reward.md` (ROADMAP "Quests"). None of the following is
built yet. **Dependency:** the reward **tier** (the card glow *and* the roll) rides the crafting
artifact-trait/tier model (`design/crafting/model.md`, `design/needs-code.md`) — there is no tier on
`ArtifactDefinition` today — so the tier-bearing parts are **gated on that model** (ROADMAP "Crafting").
The non-tier enablers (cross-dialogue continuity, competing-offer mutual-exclusion facts) can precede it.

- **Reward declared as `tier + archetype-bias`, rolled — not a literal id.** Today `QuestRewardCore` /
  `QuestRewardSerial` hold a fixed `(artifactId, count)` (R7, §3). Planned: a reward declares a **reward
  tier + archetype-bias**, and the loot system **rolls** the actual artifact on completion (extends
  `QuestRewardGranter` to roll against loot tables by tier + bias). Keeps the offer card's glow honest and
  the world non-catalog ("direction + floor, not vending"). Item rewards remain the only kind.
- **Offer presented as a "rare card" — one card-type in the encounter hand.** *(Card-hand scaffold
  **DONE** — the offer is now a `QuestOffer` card in the composed encounter hand; see
  `narrative-procedural.md` §2.7 + CHANGELOG. The **tier (frame glow) + reward belonging (archetype/race
  color-marker)** treatment is still **gated on the crafting tier model** below — the MVP card uses a
  placeholder per-type tint and shows the choice label, never the exact item.)* A plain speech-bubble
  carries chatter; a **quest offer** is an ornate framed card. The offer card is **one card-type within
  the broader encounter card hand** (`design/narrative/npc-encounter-cards.md`, `narrative-procedural.md`
  §2.7/§6): branching dialogue is cut and every player action becomes picking a card. The scaffold is a
  thin card-hand View + Presenter over the `DialogueRunner` offer/choice events; the tier-glow /
  archetype-color grammar is a `design/art/` concern layered on later. The old line-reading + Ink
  choice-list dialogue UI (`IDialogueView.ShowChoices`, the `OnChoices`/`SelectChoice` path) is now
  dormant, pending the removal follow-up.
- **Several quest-offer cards on one NPC (new authoring shape).** A single story/NPC may present
  **multiple quest offers at once** — several resolution paths to the *same* situation — selectable
  together in one hand. Same tier + "different currency, not more" (no dominant pick). Today a story
  carries a **single** Quest slot (one offer); this needs a story/casting to carry **several** offers
  and the card hand to present them. Distinct from the time-separated cross-actor fork below.
- **Competing / mutually-exclusive offers, same tier, separated in time.** Two offers coupled on a
  shared-actor thread (director D10–D12, `narrative-director-requirements.md`) where accepting/declining
  the first gates the eligibility of a **same-tier** second offer placed a couple platforms later. The
  cross-dialogue quest-continuity prerequisite (the live `QuestInstance` outliving a single dialogue) is
  now **done** (R8); this still needs the mutual-exclusion facts and the same-tier offer placement. The
  choice reads as *whose side / which facts*, not "better loot."
- **Attack card — the Monster verb (present only on some NPCs).** A combat card in the hand (or NPC
  self-initiation) that, on pick: **closes the actor's thread** (director D13), drops corpse-loot on the
  **separate** combat/mutation loot channel (`loot-subsystem.md` enemy drops — never balance the quest
  fork on it), writes **Conquest/path facts**, and fires the cauldron tempter bark. The no-kill branches
  pay in a currency killing can't give (access/doors, a recurring ally), so the corpse-loot asymmetry is
  offset in fiction, not by reward tier.
- **Cauldron-voice tempter hook on the dark offer.** A bark slot fired when a power-archetype (Monster-path)
  offer or the attack card is presented, so the temptation rides fiction rather than weighting the loot.
  Ties to the cauldron-voice design (`design/narrative/cauldron-voice.md`).
