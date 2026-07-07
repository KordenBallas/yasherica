# Quest Subsystem — Requirements & Design

> Quests are a recombinable narrative fragment: an authored goal (objectives + tags) plus the fact
> effects and a **declared, rolled reward** applied when it completes or fails. A quest is offered,
> advanced, and finished entirely through Ink tags on the dialogue that carries it, and it bridges to
> the per-run progression record. Its casting/recombination context (how a quest is matched into a
> story slot and paired with an NPC) lives in [Data-Driven Procedural Narrative](narrative-procedural.md);
> this document owns the quest **fragment, lifecycle, reward economy, the Monster verb's
> consequences, and the read-only quest log / saga readout**.
> Status: current as of 2026-07-07 (Track H — the quest-as-reward economy).
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.
>
> ⚠ **Verification status (2026-07-07):** the whole Track H below is **code-complete and edit-mode
> green** (1605/1605 via the clone-batch run), but has **not yet been play-tested by the owner** —
> the on-screen feel of the offer card, the quest-log panel, and the cauldron barks is unconfirmed
> in play mode. Treat the *behaviour* as implemented and the *presentation/feel* as awaiting a
> gameplay pass (tracked in ROADMAP under "Quests").

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
- **R7** A quest reward is **declared, not literal** (P1-5): a reward is `(tier, belonging, payload
  kind)` — never a specific item id. On completion the world **rolls** a concrete artifact or
  Part-Blank matching the declaration, deterministically under the run seed. Belonging is a hard
  filter (an artifact reward's function family, a Part-Blank reward's race); tier is a bias that
  degrades to the nearest authored tier. Item rewards only (currency/experience/ability have no
  receiving system — deferred, §6).
- **R8** Cross-dialogue continuity: a live `QuestInstance` outlives the dialogue that offered it. It is
  held in a run-scoped registry keyed by quest id; a later dialogue carrying the same quest (its Quest
  slot resolves the same `QuestData`) restores that instance and advances/completes it. A quest may be
  offered on platform A and completed on a later platform B.
- **R9** Several offers per NPC (Shape A, P1-9): a story may carry **several Quest slots**, presented
  together in the encounter hand as several resolutions of one situation — same tier, different
  belonging ("pick your currency, not the biggest number"). Each Ink choice/branch selects its offer
  by an `offer-quest: <tag>` argument; picking one mints only that quest, the others are not also
  taken.
- **R10** Competing / mutually-exclusive fork (Shape B, P1-8): two offers on **opposed threads**,
  separated in time. Committing to one writes a fact that contradicts the other thread's premise, so
  the opposing thread **fails on fact-conflict** through the shipped thread-maintenance mechanism
  (a state change + indicator fact, no closing beat). The two are never both completable; authored on
  facts/threads, never balanced by tier or loot.
- **R11** The Monster verb (P1-7): a talkable NPC dying in a dialogue-routed fight — whether the
  player picked the attack card or the NPC self-initiated, one verb, two entry points — **forecloses
  the actor's thread** (state + indicator, no beat), writes `actor.<id>.slain` (the planner never
  recasts a slain actor), and increments the Conquest path lean `world.path_conquest`. Corpse-loot
  rides the **separate** combat/mutation channel and is never balanced against the quest fork.
- **R12** Quest log / saga readout (P1-11): a **read-only** projection of the live quest registry +
  thread ledger — quests grouped by thread into sagas with the thread's lifecycle verdict, each quest
  showing its state (Active / Completed / Failed-Expired), objectives, giver, and the reward as
  **tier + belonging only** (the rolled item is never named). Opening it mutates nothing.

### 1.2 Non-functional requirements

- **N1** `QuestData` / `QuestInstance` / `QuestRewardCore` / `LiveQuestRegistry` / `QuestRewardRoller`
  / `QuestLogModel(+Builder)` / `MonsterVerbConsequences` are pure C# (no UnityEngine) and unit-tested.
- **N2** All wiring is through Zenject; the granter, roller, bark service, and quest-log presenter are
  constructor-injected.
- **N3** Quests, reward families, and the belonging colours are authored as ScriptableObjects
  (`QuestDefinition`, `RewardFamilyDefinition`, `RaceDefinition`); no code per quest, family, or race.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Narrative/Quests/
  Core/   QuestData, QuestObjective, QuestInstance, QuestState                    — pure C#
          QuestRewardCore, QuestRewardPayloadKind  — the DECLARED reward (tier + belonging + kind)
          ILiveQuestRegistry, LiveQuestRegistry    — run-scoped live-instance store (cross-dialogue, R8)
  Data/   QuestDefinition, QuestObjectiveDefinition, QuestRewardSerial, QuestMapper — SO + mapper
          RewardFamilyDefinition                   — belonging colour/id for artifact rewards (P0-3·b)
Scripts/Narrative/QuestLog/                        — the read-only quest log / saga readout (R12)
  Core/   QuestLogModel, QuestLogModelBuilder       — pure projection of registry + thread ledger
  View/   IQuestLogView, QuestLogView               — toggleable rich-text panel (MonoBehaviour)
          QuestLogPresenter                         — re-projects on every toggle
Scripts/Narrative/Runtime/Core/MonsterVerbConsequences.cs — slain/Conquest facts + thread foreclosure (R11)
Scripts/Narrative/View/{BelongingTintCatalog,IBelongingTintCatalog}.cs — belonging id → colour (P0-3·b)
Scripts/Narrative/Dialogue/DialogueRunner.cs   — the Ink tag bridge; OfferedQuestByTag, OnCombatResolved
Scripts/Loot/Core/{QuestRewardPools,IQuestRewardRoller,QuestRewardRoller}.cs — the reward roll (R7)
Scripts/Loot/Application/QuestRewardGranter.cs — rolls each declared reward and routes it by kind
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `QuestData` | Immutable fragment: id, summary, objectives, tags, on-complete/on-fail effects, `Rewards`, derived `Footprint`. |
| `QuestObjective` | One goal: id, description, kind, target count, completion effects. |
| `QuestInstance` | Per-run mutable state over `QuestData`: `Start` / `AdvanceObjective` / `Complete` / `Fail`; bridges each transition to `IRunProgressionRecorder`. Also carries the offer's **origin** (`GiverDisplayName`, `ThreadId`, set at mint / restore) for the quest-log saga readout — presentation-only, never gating. |
| `QuestRewardCore` + `QuestRewardPayloadKind` | The **declared** reward (R7): a `Tier`, a `BelongingId` (race id for a Part-Blank, reward-family id for an artifact; empty = unconstrained), and a `PayloadKind` (`Artifact` \| `PartBlank`). Never an item id — the concrete item is rolled on completion. |
| `ILiveQuestRegistry` / `LiveQuestRegistry` | Run-scoped store of live `QuestInstance`s keyed by quest id (idempotent `Register`, `TryGet`, ordered `LiveQuests`). The live counterpart of the progression record (which holds only status flags). Enables cross-dialogue continuity (R8) and is what the reward granter and quest log read. |
| `QuestRewardPools` / `IQuestRewardRoller` / `QuestRewardRoller` (Loot.Core) | The reward roll (R7). `QuestRewardPools` is the eligible artifact/blank options projected from the authored catalogs at install; the roller filters by belonging (hard), picks the nearest-tier subset, and draws deterministically from `(runSeed, questId:index)`. UnityEngine-free. |
| `QuestLogModel` / `QuestLogModelBuilder` (Narrative.QuestLog.Core) | The read-only saga readout (R12): quests grouped into `QuestSaga`s by thread (in first-offer order, threadless trailing), each `QuestLogEntry` carrying the job + reward tier/belonging (never an item). Pure projection of the registry + `IThreadLedger`. |
| `MonsterVerbConsequences` (Narrative.Runtime.Core) | The Monster-verb chokepoint (R11): on `DialogueRunner.OnCombatResolved(won)` writes `actor.<id>.slain`, increments `world.path_conquest`, and forecloses the encounter thread (`ThreadRetirementReason.Foreclosed`). |
| `BelongingTintCatalog` (Narrative.View) | Belonging id → authored colour (P0-3·b): merges `RaceDefinition._belongingColor` and `RewardFamilyDefinition._belongingColor` into one lookup the offer card / quest log tint through, so the card never cares whether a belonging is a race or a family. |

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
   for any `Completed` quest not yet paid out (`RewardsGranted`); for each declared `QuestRewardCore` it
   asks the `IQuestRewardRoller` for a concrete item (context key `questId:index`, so the roll is stable
   per quest+reward and re-completing never re-rolls) and **routes it by kind** — an `Artifact` into the
   cauldron inventory (`IInventoryModel.Add`), a `PartBlank` onto the rack (`IBlankRack.TryAdd`; a full
   rack forfeits the blank with a warning, never bypassing the cap). The quest is marked granted
   (idempotent). An empty roll pool logs a warning and grants nothing but still marks paid (an
   unpayable declaration never retries forever). Reading the registry rather than a single dialogue's
   active quest makes the payout land on platform B regardless of which dialogue is current.

### 2.4 The reward roll (R7 / P1-5)

`QuestRewardRoller` derives its own `System.Random` from `(runSeed, "quest-reward:" + contextKey)`,
mirroring `LootRollService`, so outcomes are reproducible within a run and independent of roll order.
The **belonging** is a **hard filter** — the offer card's colour can never promise a family the roll
won't deliver, so an artifact reward stays inside its declared reward-family and a Part-Blank reward
inside its declared race (empty belonging = draw from the whole pool). The **tier** is a bias with
graceful degrade: the roller keeps the candidates whose tier is closest to the declared tier (exact
match when it exists), so a sparse pool still pays out at the promised family. An impossible belonging
is an **authoring error** the granter logs, never a silent substitution. The eligible pools
(`QuestRewardPools`) are projected once at install from `IArtifactCatalog` (`RewardFamilyId`, `Tier`)
and `IPartBlankDataSource` (`RaceId`) into pure records, so the roller is UnityEngine-free.

### 2.5 Belonging colour (P0-3·b)

A reward's **belonging** is surfaced as a colour on the offer card and the quest log. It is read from
the payload kind: a **Part-Blank** reward's belonging is the blank's **race** colour
(`RaceDefinition._belongingColor`); an **Artifact** reward's belonging is a coarse **function-family**
colour (`RewardFamilyDefinition._belongingColor`). `BelongingTintCatalog` merges both authored sources
into one id→colour lookup so the card/log tint through a single seam and never branch on the kind.
The colour the card shows and the belonging the roll uses are the **same declared value**.

### 2.6 The Monster verb's consequences (R11 / P1-7)

`MonsterVerbConsequences` (bound `NonLazy` in `NarrativeSliceInstaller`) subscribes to
`DialogueRunner.OnCombatResolved`, which fires once with the win/loss **before Ink resumes** (so a
post-combat branch already sees the written facts). On a **win** it: writes `actor.<id>.slain` for the
encounter actor (the planner's `FindSatisfyingLiveActor` skips a slain actor, so his future arc is
forfeit — the in-fiction price of the extra corpse loot); increments `world.path_conquest` (the
Conquest path lean the passport gating and the cauldron barks read); and forecloses the encounter's
thread (`IThreadLedger.Fail(..., Foreclosed)` + the `thread_retired` indicator fact, the same grammar
as conflict/expiry retirement — no closing beat). A **loss** writes nothing. Both entry points (the
Attack card via `DialogueRunner.TriggerCombat`, and NPC self-initiation via a `start-combat:` tag)
route through the one resume, so the verb has a single consequence path. **Corpse-loot is untouched
here** — it drops on the existing `EnemyLootDropper` combat channel, orthogonal to the quest-reward
economy by design.

### 2.7 The quest log / saga readout (R12 / P1-11)

`QuestLogPresenter` (bound `NonLazy`) toggles the `IQuestLogView` and, on every open, re-projects the
**current** `ILiveQuestRegistry` + `IThreadLedger` through `QuestLogModelBuilder` — no cached
staleness, no mutation. The builder groups quests by their thread label into `QuestSaga`s (first-offer
order; threadless quests trail as single-entry groups) and maps the thread's lifecycle to a
`QuestSagaState` (Live / Completed / FailedByConflict / Foreclosed / Expired). Each `QuestLogEntry`
carries the job (title, giver, summary, objectives with progress) and the reward telegraph as **tier +
belonging only** — the model has no field that could name the rolled item, so the log can never become
a reward catalog. `QuestLogView` is a thin MonoBehaviour that polls the toggle key (default **J**, the
same input-adapter pattern as `NpcInteractionInput`) and paints the model as rich text; the exact
panel art is a later render-look pass.

### 2.8 DI wiring

`ILiveQuestRegistry` is bound `AsSingle` to `LiveQuestRegistry` in `NarrativeSliceInstaller` (run-scoped,
beside `ILiveActorRegistry`). `DialogueRunner` is bound `AsSingle` in the same installer and
constructor-injects the registry. `QuestRewardGranter` is bound to `IQuestRewardGranter` in
`LootInstaller` and constructor-injects `ILiveQuestRegistry` + `IInventoryModel` + `IBlankRack` +
`IQuestRewardRoller`. `QuestRewardPools` (projected from `IArtifactCatalog` + `IPartBlankDataSource`)
and `IQuestRewardRoller` are also bound in `LootInstaller`. The completion hook is
`PlatformCompletedState` (covers both completion routes).

`NarrativeSliceInstaller` additionally binds: `IBelongingTintCatalog` (from the loaded `RaceDefinition`s
+ `RewardFamilyDefinition`s), `MonsterVerbConsequences` (`NonLazy`), and — when their prefabs load — the
quest-log view + `QuestLogPresenter` (`Prefabs/UI/QuestLogPanel`) and the bark channel (see
[Cauldron Barks](cauldron-barks.md)). A missing prefab degrades to the feature simply being absent, never
an error. `NarrativeSliceInstaller` + `MutationInstaller` (which owns the `IBlankRack` +
`ISocketingTrendSource`) ship together in the Area scene, so these cross-installer resolves are one
shared container.

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
| `_rewards` | `QuestRewardSerial[]` | **Declared** rewards rolled on completion (P1-5). Each: `_payloadKind` (Artifact \| PartBlank), `_tier` (≥0, the glow scale), `_belongingId` (race id for a blank, reward-family id for an artifact; empty = unconstrained). **No item id.** | item rewards only |

The union of every fact-write shape the quest can emit (complete + fail + objective effects) is the
quest's **own footprint** — its runtime write-gate. Rewards are item grants, not fact writes, so they
are not in the footprint.

### `RewardFamilyDefinition`  (asset menu: `Create → Narrative → Quests → Reward Family`)

One coarse function family of the artifact reward economy (P1-5 / P0-3·b). Authored under
`Resources/Rewards/`; loaded by `NarrativeSliceInstaller` into `BelongingTintCatalog`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_familyId` | string | Stable id, matched by `ArtifactDefinition._rewardFamilyId` and a quest reward's `_belongingId`. | lowercase, no spaces (demo: `power`, `utility`) |
| `_displayName` | string | UI name. | |
| `_belongingColor` | Color | The card/log tint for artifact rewards of this family (the artifact-side counterpart of `RaceDefinition._belongingColor`). | white |

### New fields on existing SOs (P1-5 / P0-3·b)

| SO | Field | Meaning |
|---|---|---|
| `ArtifactDefinition` | `_rewardFamilyId` | The reward family this artifact rolls under (empty = never rolled by a family-constrained quest reward). Also supplies the offer-card belonging colour. |
| `PartBlankDefinition` | `_raceId` (`[RaceId]`) | The race this blank belongs to for the reward economy (empty = kindless); the reward roll's race filter + the card belonging colour. Distinct from `_speciesArchetypeId` until the species-vs-race reconcile (Track J). |
| `MutationConfig` | `_temptationRarityTier` | An unseal offer at/above this rarity tier reads as a monstrous temptation (fires the cauldron temptation bark; below it a modest part fires the restraint bark). |

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

### Add a declared reward to a quest (P1-5)

1. On the `QuestDefinition`, add a `_rewards` entry: choose `_payloadKind` (Artifact or PartBlank), set
   `_tier` (the potency the offer card glows with — the same scale as artifact tiers), and set
   `_belongingId`:
   - **PartBlank →** a **race id** (e.g. `fox`); belonging colour = that race's colour. Reads as an
     access/passport-flavoured reward.
   - **Artifact →** a **reward-family id** (e.g. `power`, `utility`); belonging colour = that family's
     colour. Reads as a power/combat vs utility/access reward.
   - Leave `_belongingId` empty to draw from the whole pool of that kind (the card tints neutral).
2. Do **not** name an item — the world rolls a concrete artifact/blank of that tier + belonging on
   completion. A different concrete item is possible across runs (non-catalog); the tier + belonging
   always hold. No Ink change.

### Add a reward family / mark artifacts and blanks

1. `Create → Narrative → Quests → Reward Family` under `Resources/Rewards/`; set `_familyId`,
   `_displayName`, `_belongingColor`.
2. On each `ArtifactDefinition`, set `_rewardFamilyId` to that family; on each `PartBlankDefinition`,
   set `_raceId` to the race it belongs to. Keep race ids and family ids **disjoint** (they share one
   belonging namespace; a collision resolves race-first).

### Author several offers on one NPC (Shape A, P1-9)

1. On the storylet, add **several Quest slots**, each `_requiredTags` matching a different quest's
   `_questTags`; author all offers at the **same `_tier`**, each a **different `_belongingId`**.
2. In the dialogue `.ink`, give each offer choice an `offer-quest: <tag>` tag on **both** the choice
   (labels its card before the pick) and its branch body (mints the picked instance). Picking one
   resolves the situation; the others are not also taken. *(Demo: `story_frog_elder_open` offers
   `frog-errand` (tier 1, utility) and `frog-guard` (tier 1, power).)*

### Author a competing fork (Shape B, P1-8)

1. Author the two offers as **two storylets on opposed threads** (a `ThreadDefinition` each), same
   tier, opposed premise facts: each side's `_premise` holds the OTHER side's commit fact **false**
   (e.g. `barn_raid` premise `raider_offer_taken == false`; `raider_pact` premise
   `grain_recovered == false`). Do **not** author an A→B link — the director places B later off the
   thread; committing to one writes the fact that fails the other on conflict. *(Demo:
   `DemoThread_BarnRaid` vs `DemoThread_RaiderPact`.)*

### Mark an NPC as attackable (the Monster verb, P1-7)

The attack card appears on an eligible casting (a filled combat slot / `# card: attack` Ink choice, or
NPC self-initiation via a `start-combat:` tag — see `narrative-procedural.md` and
`attack-card-monster-verb.md`). No quest authoring is needed for the **consequences**: killing the NPC
automatically forecloses its thread, writes `actor.<id>.slain` + `world.path_conquest`, and drops
corpse-loot on the combat channel. Leave ordinary townsfolk without a combat slot so no "kill me" card
is offered.

**Authoring constraints / gotchas:** a `complete-quest:`/`fail-quest:`/`advance-objective:` tag with
no active quest fails closed with a warning; completion is explicit — advancing all objectives does
**not** auto-complete the quest; an `offer-quest: <tag>` with no matching quest slot fails closed
(sets `quest_accepted = false`); a reward whose `_belongingId` matches no artifact family / race yields
an empty roll pool (logged, nothing granted). The new facts `world.path_conquest`,
`world.path_restraint`, and `actor.<id>.slain` must be in the `FactKeyRegistry` (the demo registry
declares them).

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
- `QuestRewardGranterTests` — a `Completed` quest rolls an artifact of the declared family into a fake
  inventory / a blank of the declared race onto a fake rack; granting is idempotent; an empty pool
  grants nothing but marks paid; a full rack forfeits the blank without throwing; active-but-not-
  completed and no-quest grant nothing.
- `QuestRewardRollerTests` (P1-5) — the roll honours the declared family/race, prefers the exact tier
  and degrades to nearest, fails (never substitutes) on an unknown family, draws from the whole pool
  on an empty belonging, is deterministic under seed+context, and can differ across seeds (non-catalog).
- `MultipleOffersTests` (P1-9) — the hand shows both offers each labelled with its own quest, same
  tier / different belonging; picking one mints only that quest; an unknown `offer-quest:` tag fails
  closed; the untagged legacy single-offer still resolves; a revisited casting restores the picked
  quest, not the first slot.
- `CompetingOffersForkTests` (P1-8) — committing to either side fails the opposing thread on conflict
  through the shipped maintenance tick (never both completable); the dark-offer bark fires once when
  the Monster-lean offer is presented.
- `MonsterVerbConsequencesTests` (P1-7) — a win writes `slain` + `path_conquest` and forecloses the
  thread with the indicator fact; self-initiation has the same consequences; a loss writes nothing and
  keeps the thread live; a threadless encounter still writes the actor/path facts.
- `QuestLogModelBuilderTests` (P1-11) — quests group by thread into sagas in offer order (threadless
  trailing); saga state projects the thread lifecycle (conflict/foreclosed/expired/live); entries carry
  state + objectives + progress; the reward telegraph is tier + belonging only (no item); an empty
  registry yields the empty model.
- `CauldronBarkServiceTests` (P1-10) — see [Cauldron Barks](cauldron-barks.md).
- `RunWindowPlannerTests` — a slain actor is never recast (his actor-scoped arc stories go ineligible).

The full EditMode suite is **1605/1605 green** via the clone-batch run (2026-07-07). The DI binding,
the `PlatformCompletedState` hook, and the on-screen feel (offer card, quest-log panel, barks) are
**not yet verified in play mode** — the owner's gameplay pass is pending (⚠ header, §6).

---

## 6. Known limitations / open points

- ⚠ **Gameplay-untested (2026-07-07).** The whole quest-as-reward economy below is code-complete and
  edit-mode green, but the owner has **not** play-tested it. The behaviour is implemented; the
  on-screen feel (offer-card mystery slot + glow/tint, the quest-log panel, the cauldron barks) is
  unconfirmed in play mode. Tracked in ROADMAP "Quests".
- Reward types are **item-only**. Currency / experience / ability rewards have no receiving system —
  **deferred (P1-12)** until a currency model lands; quests pay items + facts/doors for now.
- **Restore is by quest id via the casting slot.** A later dialogue restores an in-flight quest only if
  its Quest slot resolves the same `QuestData` (the planner/author must place that quest on the
  continuing platform). There is no out-of-band "advance any quest by id" tag path yet.
- `StoryNodeRequirement` (`RequiredActiveQuests` / `RequiredEncounteredNpcs`) is authored but not yet
  consumed in story selection (Character Progression ROADMAP item).
- The offer card telegraphs the **first** declared reward only; a multi-reward quest still reads as one
  mystery slot (the card face is terse by design).
- The **exact roll tables / bias weights** are code-track tuning; the current roller is a
  nearest-tier-within-family draw with uniform weights (no rarity curve yet).
- The quest log is a flat rich-text panel (no scroll / filter / sort); the panel art and layout are a
  later render-look pass.
- **Reward-belonging namespace collision** is resolved race-first and warns nowhere; keep race ids and
  reward-family ids disjoint when authoring.

### Implemented — quest-as-reward economy (Track H, 2026-07-07)

Design intent from `design/narrative/quest-as-reward.md`; PO briefs `quest-reward-rolled.md`,
`quest-offer-card.md`, `multiple-and-competing-offers.md`, `attack-card-monster-verb.md`,
`cauldron-voice-barks.md`, `quest-log-and-saga.md`. All shipped (P1-12 currency sinks deferred):

- **Reward declared as `tier + belonging + payload kind`, rolled** (R7, §2.4) — replaces the old
  `(artifactId, count)`; the loot layer rolls a concrete artifact or Part-Blank on completion.
- **The offer card telegraphs the reward** (P1-6) — tier as glow, belonging as colour, item hidden as
  `?`, with hover-inspect for the job detail; see [Encounter Dialogue UI](encounter-dialogue-ui.md).
- **Several offers per NPC** (R9, Shape A) and the **competing time-separated fork** (R10, Shape B).
- **The Monster verb's consequences** (R11, §2.6) — thread foreclosure + slain/Conquest facts +
  separate corpse-loot channel.
- **The cauldron tempter / restraint / trend barks** — see [Cauldron Barks](cauldron-barks.md).
- **The quest log / saga readout** (R12, §2.7).
