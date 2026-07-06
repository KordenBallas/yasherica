# Combat Round & Telegraph — Requirements & Design

> The combat round structure (Plan → Act → Resolve with locked, revealed enemy intents) and the
> readability layer on top of it: hero facing legibility, overhead plan icons on every unit, and
> the one-shot / hover-replay ghost preview of an ability's full outcome. Implements the three
> verified PO briefs `combat-hero-facing.md`, `combat-turn-intent-phase.md`, and
> `combat-ability-ghost-telegraph.md` (Track C) as one combat pass, plus the D2 initiative +
> turn-order strip (`combat-initiative-and-turn-queue.md`; the aim/fire input half lives in
> `ability-subsystem.md` R8) and the D3 ability animation + move arrow + enemy readiness cue
> (`combat-ability-animation.md`, R15–R18).
> Status: current as of 2026-07-05.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.
> The ability model itself (shapes, facing-relative aiming, queueing, push displacement,
> authoring) is owned by `ability-subsystem.md`.

---

## 1. Requirements

### 1.1 Functional requirements

**The round: Plan → Act → Resolve** (brief `combat-turn-intent-phase.md`)

- **R1** A combat round has three phases (`RoundPhase`): **EnemyPlan** — every enemy decides its
  action up front, all together, and the decisions are locked and revealed; **PlayerAct** — the
  player queues/executes freely against the live board, seeing all enemy intents; **EnemyResolve**
  — the committed enemy actions fire exactly as revealed.
- **R2** An enemy's committed intent (`EnemyIntent`) snapshots at plan time: the action, the
  **committed facing**, the plan-time origin, and the **exact cells** a committed ability will
  strike. Resolution never recomputes and never re-targets: if the player dodged, the blow
  **whiffs** (the committed cells hold nobody) — the intended payoff, not a bug. If the player
  moved *into* the committed cells, they are hit.
- **R3** A committed **move** whose destination became invalid or occupied at resolve **fizzles**
  (the enemy stays; it does not pick a new destination). A dead or stunned committer is skipped.
- **R4** Resolution order is **initiator-led for the opening round, player-then-enemies after**
  (D2, `combat-initiative-and-turn-queue.md`): the fight's **initiator acts first** in round 1 — a
  player-initiated fight runs Act→Resolve (player first), an enemy-initiated fight (ambush or an NPC
  that turned hostile) runs Resolve→Act (enemies first). Rounds 2+ are player-then-enemies. Within
  each phase, player actions resolve live during Act; enemy intents resolve one by one (with visual
  pacing) in Resolve. Win conditions are checked after every resolved intent — combat can end
  mid-resolve. (The multi-round lead policy — alternate/persist/re-roll — stays deferred.)
- **R5** The player is **not** locked: only enemy intent is pre-committed. Player queueing /
  execution semantics are unchanged (`ability-subsystem.md` R7–R8). Player actions are accepted
  only during PlayerAct; free actions (turning) stay legal after the unit has acted.
- **R6** Round bookkeeping (status-effect TurnStart/TurnEnd triggers, duration ticking, cooldown
  decrement, `HasActedThisTurn` reset) happens **once per round for all units** at round end —
  the same cadence each unit had under the old round-robin.
- **R7** **Determinism**: enemy planning is deterministic per run seed. Each enemy's decision
  maker is seeded `LootSeed.Derive(runSeed, "combat-ai:{enemyId}")`; units are planned in
  ascending UnitId order; same seed + same state → identical committed plans. The AI *scoring*
  (`TacticalAI` / `ConfigurableTacticalAI`) is untouched — only decide-timing (round start) and
  commitment (lock + reveal) changed.

**Initiative & turn-order strip** (brief `combat-initiative-and-turn-queue.md`, D2)

- **R4a** **The initiator leads the opening round.** Who caused the fight is captured as a
  `CombatInitiator` (`Player` / `Enemy`) and threaded to the controller: a player-chosen Attack
  (the encounter's system Attack card **or** a `card: attack` Ink choice) → `Player`; an ambush /
  aggro cross, or a plain `start-combat:` tag (an NPC turning hostile on its own) → `Enemy` (the
  default). `RoundLeadPolicy.EnemyLeadsThisRound(turnNumber, initiator)` gates the reorder to round 1
  only. Determinism holds: same seed + same initiator → same order.
- **R4b** A **horizontal turn-order strip** in the **top-right** lists the round's actors in order —
  the player and each **live** enemy — leader-first per R4a, with the current side and any
  already-acted side marked, dead units dropped, cleared when combat ends. It is a **read-out only**
  (adds no way to act). Demo placement — final HUD layout/portraits are a later pass.

**Facing legibility** (brief `combat-hero-facing.md`; the aiming model is in `ability-subsystem.md`)

- **R8** A unit's model visually points along its domain `FacingDirection` (`UnitFacingRotator`
  slerps the yaw on every state change) — reading an enemy's facing is reading where its committed
  blow will land, and the enemy turns toward its committed facing when it schedules (AI
  turn-and-schedule) and again at resolve.

**Overhead plan icons** (brief `combat-ability-ghost-telegraph.md` §5)

- **R9** Every unit — player **and** enemy — shows its plan as **icons above it, in order**:
  the player's queued abilities (by `ExecutionOrder`), an enemy's committed **ability** intent (its
  ability icon). Enemy icons appear the moment plans are revealed at EnemyPlan, before the player
  acts. Dead units show nothing; an executed queue clears the row. *(D3: a committed **move** no
  longer shows an overhead `»` glyph — its read is the board direction arrow, R17.)*

**Ghost preview** (brief `combat-ability-ghost-telegraph.md` §2–§4, §6–§7)

- **R10** **On queue-submit** (any scheduling path — detected as queue growth on a human unit), a
  **ghost of the ability's full outcome plays once** then fades: a translucent clone of the caster
  on its cell facing the volley, a translucent clone of each displaced unit at its **predicted
  destination**, and a damage/heal number above each struck unit.
- **R11** **Hovering any plan icon replays** that entry's ghost against the **current** board —
  player icons re-check the plan, enemy icons read the enemy's committed cast (its committed
  cells/facing). Hover-exit stops the replay. One ghost plays at a time.
- **R12** The preview is computed against the **current board state** — it does **not** simulate
  earlier queued abilities (accepted tradeoff per the brief; a queue dry-run is a later upgrade).
- **R13** The ghost is honest: predicted damage numbers and predicted landing cells are computed
  by the same math the executor runs (`IDamageSystem.CalculateFinalDamage`, shared
  `AbilityShapeCalculator` + `DisplacementResolver`), so on an unchanged board the prediction
  equals the execution result (test-asserted).
- **R14** The ghost reads unmistakably as a preview: uniform translucent tint (alpha-blend
  Standard material), fade-in → hold → fade-out, all clutter (HP labels, icon rows) stripped
  from clones. The aim-time affected-cell highlight (the "where" layer) is untouched.

**Ability animation, move arrow & enemy readiness** (brief `combat-ability-animation.md`, D3)

- **R15** **Every ability plays a code-authored placeholder animation that spans its whole affected
  area** — a **cell-sweep**: each struck cell flashes/pops, swept outward along the caster's line for
  a **Line** and simultaneously for a **Ring**, matched to the ability's shape (not a caster-only
  pose). Shape-driven; `AbilityDefinition._animationTrigger` stays the seam for real clips later.
- **R16** The animation plays in **two treatments**: **translucent** as the ghost preview (on
  queue-submit and on hover-replay, for the player's queue **and** an enemy's committed intent, R10/R11)
  and **opaque** on **live execution** — for the player's queue **and** the enemy's paced resolve, so
  an enemy action is **visibly not instant**. Live playback is emitted from the shared executor after
  the outcome applies (`AbilityFiredCue`), so it never changes outcomes, cells, or timing.
- **R17** An enemy's committed **move** shows a **direction arrow on the board** (from its plan-time
  origin toward the committed destination) during the plan phase — replacing the `»` glyph. If the
  move whiffs because the board shifted, that is consistent with the committed-intent model (R2/R3).
- **R18** An enemy holding a **committed intent reads as "armed / about to act"** during the plan
  phase, via two combined cues: its overhead plan icons go **restless** (jitter/pulse) **and** it holds
  a placeholder **wind-up body pose** (a transform lean/scale/bob — the rig has no attack state). The
  cue is **enemies only**, **uniform** (no turn-order escalation), and **clears when the round enters
  `EnemyResolve`** (the enemy acts/moves) — the move arrow and pose both stop, so the pose never fights
  the enemy's movement. The pose is applied as an additive offset (never an absolute position). Presentation only.

### 1.2 Non-functional requirements

- **N1** Round/intent/outcome logic is pure C# (no UnityEngine beyond math types) and unit-tested;
  Unity-side classes are thin adapters.
- **N2** All container dependencies wired through Zenject (`AreaInstaller`); per-combat presenters
  are constructed by `CombatActiveState` alongside the existing combat presenters.
- **N3** No new prefabs: icons, ghosts, and labels are code-built (the `NpcOverheadView`
  precedent); the only authored content is ability `.asset` data.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Combat/
  Core/            RoundPhase, EnemyIntent, AbilityOutcome, AbilityFiredCue (D3),
                   FacingGeometry, DisplacementResolver (pure C#)
  TurnManagement/  EnemyIntentPlanner (pure), TurnManager (round counter, acting player)
  Execution/       EnemyIntentResolver, AbilityExecutor.ExecuteAbilityAtCells (emits AbilityFiredCue),
                   AbilityOutcomeCalculator (pure), IAbilityFiredSink/AbilityFiredSink (D3 relay)
  Controller/      CombatController — the round orchestrator (StartRound / CheckTurnEnd /
                   ResolveNextEnemyIntent / EndRound)
  Player/          EnemyRoundController (paced resolve coroutine),
                   UnitPlanIconsPresenter, GhostPlaybackPresenter,
                   GhostPlaybackPlan(+Builder), PlanIconModel,
                   EnemyIntentTelegraphPresenter(+Model, D3) (pure presenters/models)
  View/            UnitFacingRotator, UnitOverheadIconsView, AbilityIconMarker,
                   GhostPlaybackView, GhostVisualCloner, CombatUnitViewRegistry,
                   AbilityCellFlash/AbilityAreaSweep, LiveAbilityAnimationView,
                   EnemyIntentTelegraphView (D3), TelegraphStyle (thin MonoBehaviours + constants)
  Input/           AbilityIconHoverController (thin hover raycast adapter)
  Data/Providers/  AbilityDefinitionCatalog (icon lookup by ability id)
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `RoundPhase` | `EnemyPlan / PlayerAct / EnemyResolve` — carried on `CombatState`, orthogonal to `CombatPhase` (Setup/Combat/Victory/Defeat). |
| `EnemyIntent` | One enemy's locked plan: `UnitId`, the committed `IAction`, `CommittedFacing`, `CommittedOrigin`, `CommittedCells` (snapshotted at plan time — icons, ghost, and resolution all read this same data). |
| `EnemyIntentPlanner` | Plan phase: asks each enemy unit's `AIPlayer.RequestAction` in UnitId order and wraps the decision into an `EnemyIntent` (cells via the shared `AbilityShapeCalculator`). |
| `EnemyIntentResolver` | Resolve phase: fires one intent verbatim — sets the committed facing, calls `ExecuteAbilityAtCells` on the committed cells, starts the cooldown; committed moves fizzle when blocked. |
| `AbilityOutcome` / `UnitOutcome` | Predicted result of one ability: affected cells + per-unit damage/heal and From→To displacement. |
| `AbilityOutcomeCalculator` | Computes an `AbilityOutcome` without mutating state, mirroring executor semantics exactly (same damage calc, same farthest-first `DisplacementResolver` order) — `ComputeForFacing` (player/live) and `ComputeCommitted` (enemy intent). |
| `GhostPlaybackPlan` / `GhostPlaybackPlanBuilder` | Maps an outcome into world space (caster position + volley look direction + markers) for the ghost view; hex→world is injected, so the mapping is pure. |
| `PlanIconModel` | One overhead icon: queued player ability / enemy ability intent / enemy move glyph. |

### 2.3 Runtime flow — one round

```
StartRound (CombatController)
  RoundPhase = EnemyPlan
  EnemyIntentPlanner.Plan(state)        every enemy decides, in UnitId order
  state = state.WithEnemyIntents(...)   locked
  OnEnemyPlansRevealed                  UnitPlanIconsPresenter draws enemy icons
  RoundLeadPolicy.EnemyLeadsThisRound?  (opening round + enemy initiator)
    enemy-led → RoundPhase = EnemyResolve   (enemies resolve first)
    else      → BeginPlayerAct()            RoundPhase = PlayerAct; OnTurnStarted(human)

  Per-round flags (_playerActedThisRound / _enemiesResolvedThisRound) drive the second phase:
    CheckTurnEnd (player done)   → EnemyResolve if enemies haven't resolved, else EndRound
    Resolve exhausted            → BeginPlayerAct() if the player hasn't acted, else EndRound

PlayerAct
  player turns freely (free ChangeDirectionAction), schedules (queue icons appear,
  GhostPlaybackPresenter auto-plays the submit ghost), executes the queue —
  all resolved live; hovering any icon replays its ghost via AbilityIconHoverController
  CheckTurnEnd: all player units acted → RoundPhase = EnemyResolve

EnemyResolve (EnemyRoundController coroutine, 0.4–0.5 s pacing)
  while CombatController.ResolveNextEnemyIntent():
      EnemyIntentResolver.Resolve(state, intent)   fires as shown; whiff/fizzle rules
      OnStateChanged; CheckWinConditions            combat may end mid-resolve
  intents exhausted → EndRound:
      round effects for ALL units, cooldown decrement, acted reset, round++ → StartRound
```

Combat startup: `CombatActiveState` initializes the controller and battlefield, then runs one
sequenced coroutine — character init → enemy init → `CombatController.BeginRounds()` — so the
first Plan phase commits intents against the **full** board.

### 2.4 Presentation wiring

- `CombatUnitViewRegistry` (bound `AsSingle`) maps unit id → visual root; populated by
  `CharacterCombatInitializer` / `EnemyCombatIntegrator` (which also attach `UnitFacingRotator`),
  cleared on combat exit.
- `CombatActiveState.OnEnter` creates per-combat: `UnitOverheadIconsView` (+ pure
  `UnitPlanIconsPresenter`), `GhostPlaybackView` (+ pure `GhostPlaybackPresenter` +
  `AbilityIconHoverController`), the D2 **`TurnOrderStripView`** (+ pure `TurnOrderStripPresenter`),
  and the D3 **`LiveAbilityAnimationView`** + **`EnemyIntentTelegraphView`** (+ pure
  `EnemyIntentTelegraphPresenter`); `OnExit` disposes them all. The strip is a **code-built**
  screen-space overlay (its own `ScreenSpaceOverlay` Canvas + top-right `HorizontalLayoutGroup` of
  per-actor cells), following the same no-new-prefab convention as the icon/ghost views (N3). PvE
  (Area) only — the Arena orders by its own `IArenaResolutionOrder` and its presentation is Track G.
- **D3 ability animation** (`combat-ability-animation.md`): the placeholder motion is one shared
  cell-sweep — `AbilityAreaSweep` spawns a self-animating `AbilityCellFlash` (a ground quad that pops
  → fades) per affected cell, staggered by distance from the caster for a Line (sweep) and together for
  a Ring. It is played **opaque** by `LiveAbilityAnimationView` (subscribed to the executor's
  `IAbilityFiredSink` — player queue + enemy resolve) and **translucent** by `GhostPlaybackView` (over
  the plan's `AffectedCellPositions`, alongside the existing outcome clones/labels). Timing/tint live in
  `TelegraphStyle` (`AbilitySweepSeconds` ≤ the enemy resolve beat). `AbilityFiredCue` is pure — the
  executor stays UnityEngine-free.
- **D3 enemy telegraph** (`EnemyIntentTelegraphView`, driven by the pure `EnemyIntentTelegraphPresenter`
  reading `EnemyIntents`): a ground **move-direction arrow** (`LineRenderer` chevron toward the committed
  destination) and the **"armed" wind-up pose** (a transform lean/scale/bob on the enemy visual root,
  position/scale only — `UnitFacingRotator` owns rotation — cleared when the intent resolves). The
  **restless-icon** half of the readiness cue lives in `UnitOverheadIconsView` (jitter/pulse on rows
  built from enemy intents). Enemies only, uniform.
- Icons: code-built world-space rows (`SpriteRenderer` per ability icon via
  `AbilityDefinitionCatalog`; the committed-move `»` glyph was retired in D3 — R17), billboarded, each
  icon carrying a small trigger `BoxCollider` + `AbilityIconMarker { UnitId, QueueIndex, IsEnemyIntent }`.
  Hover raycast filters
  `Physics.RaycastAll` hits by that component — no layer/project-settings changes.
- Ghosts: `GhostVisualCloner` instantiates the unit visual under an **inactive holder** (so no
  cloned combat component ever wakes up), strips behaviours/colliders/physics/labels, and swaps
  every renderer to one shared alpha-blend ghost material. `GhostPlaybackView` fades via that
  shared material (one ghost at a time by design).
- Sizing/timing constants live in `TelegraphStyle` (SO-ification is a ROADMAP follow-up).

### 2.5 DI wiring

`AreaInstaller`: `EnemyIntentPlanner`, `EnemyIntentResolver`, `IAbilityOutcomeCalculator`,
`IAbilityDefinitionCatalog`, `ICombatUnitViewRegistry`, `EnemyRoundController`, and the D3
`IAbilityFiredSink` (`AbilityFiredSink`) — all `AsSingle`. The sink is `[InjectOptional]` on
`AbilityExecutor`, so headless/tests and the Arena (which does not bind it) stay null-safe.
`CombatControllerFactory` threads the planner/resolver into each `CombatController` it creates.
`TurnManager` still implements `ITurnManager` but is degenerate: `CurrentPlayer` is pinned to the
human player (every `IsPlayerTurn` consumer keeps working) and `NextTurn()` only advances the
round counter.

**Initiator threading (D2).** The fight's `CombatInitiator` is captured at the engagement sites and
stored on `EnemyContent.Initiator` (default `Enemy`): the `DialogueRunner.OnCombatTriggered` event
carries it (player-Attack vs. `start-combat:` tag, distinguished by a one-shot player-combat latch so
a `card: attack` Ink choice reads as player-initiated), `DialogueActiveState` stamps it onto every
latched `EnemyContent`, and `NpcEncounterStarter` sets `Enemy` for an ambush. `CombatActiveState.OnEnter`
reduces the engaged contents to one opening initiator (`Player` if any engaged content is player-led)
and passes it to `ICombatController.Initialize(..., openingInitiator)`, exposed as
`ICombatController.OpeningInitiator` for the strip. `ArenaCombatController` ignores it (Arena orders by
`IArenaResolutionOrder`).

---

## 3. ScriptableObject Reference

This system introduces **no new SO type**. It consumes:

- `AbilityDefinition._pushDistance` and `_icon` — documented in `ability-subsystem.md` §3.1
  (`_pushDistance` feeds the push + the ghost's displacement markers; `_icon` feeds the overhead
  plan icons via `AbilityDefinitionCatalog`, which indexes `Resources/Abilities/Data/` by id).
- `HexDirectionConfig` — the existing facing→offset mapping (`Resources/HexDirectionConfig.asset`).

---

## 4. Adding Content

### Add a push ability (fully telegraphed: icon, intent, ghost with displacement)

1. Duplicate `Resources/Abilities/Data/TestPushAbility.asset` (a `DamageAbilityDefinition`).
2. Set a **unique `_id`**, name, `_shape: Line`, `_lineLength`, `_cooldownDuration`, `_damage`,
   and `_pushDistance` (cells shoved away from the caster; `0` disables the push). Assign `_icon`
   — it is what appears above units planning/queueing this ability.
3. Keep the asset in `Resources/Abilities/Data/` (the icon catalog indexes that folder).
4. Grant it: add the asset to a `PartDefinition`'s **Active Abilities** (primary designer
   surface — the hero's combat set is built from equipped parts), or to an
   `EnemyDefinition._abilities` list (the enemy will commit it as a revealed intent), or to
   `HeroDefinition._abilities` (fallback only, used when no equipped part grants an active).
5. Validate in play mode: the enemy's icon shows at round start; hovering it replays a ghost with
   the pushed units' destination clones; queuing it yourself plays the submit ghost once.

**Authoring constraints / gotchas:** `_id` must be unique across `Resources/Abilities/Data/`
(duplicate ids are dropped by the catalog with a warning); `_pushDistance` on a Ring ability is
ignored (no line direction); the ghost shows `_icon`-less abilities as empty sprites — always
assign an icon.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/` (all pure C#; runnable via the bundled
Roslyn workaround when the editor holds the project lock):

- `UnitFacingTests`, `FacingGeometryTests` — facing on the unit, offset geometry.
- `ActionValidatorFacingTests` — free-action gate (turning legal after acting; everything blocked
  outside PlayerAct or while stunned).
- `AbilityExecutorFacingTests` — the global-facing acceptance: turning between schedule and
  execute re-points the volley; two queued abilities fire along the final facing; Ring ignores
  facing.
- `EnemyIntentPlannerTests` — all enemies planned up front in UnitId order, committed cells
  snapshotted, same seed → identical plans.
- `EnemyIntentResolverTests` — whiff after dodge, bait (hits whoever stands there now), fires from
  committed cells even if the caster was displaced, move fizzle, dead-caster skip, cooldown start.
- `CombatStateRoundTests` — `RoundPhase`/`EnemyIntents` threading through every `With*` copy.
- `DisplacementResolverTests`, `AbilityExecutorPushTests` — push geometry and executor push
  (farthest-first, corpse-stays, committed path parity).
- `AbilityOutcomeCalculatorTests` — the ghost-honesty property: predicted outcome equals actual
  execution; prediction never mutates state.
- `UnitPlanIconsPresenterTests` — icon models per unit kind, reveal timing, cleared rows.
- `GhostPlaybackPlanTests` — outcome→world mapping, replay-reflects-current-board.
- `RoundLeadPolicyTests` (D2) — the opening-round-only initiator-lead decision.
- `TurnOrderStripPresenterTests` (D2) — actors leader-first, enemies in resolution order, dead
  dropped, current/already-acted side derived from phase + lead, cleared on game end.
- `DialogueRunnerTests` / `EncounterCardHandPresenterTests` (D2) — the captured initiator: a
  `start-combat:` tag reads `Enemy`, a system/`card: attack` Attack reads `Player`.
- `AbilityFiredCueTests` (D3) — executing an ability notifies the fired-cue sink with the caster,
  shape, and struck cells; the executor is null-safe when no sink is bound.
- `EnemyIntentTelegraphPresenterTests` (D3) — armed enemies only (dead/player excluded), a committed
  move carries its from/to cells, cleared on game end.
- `GhostPlaybackPlanTests` (D3, extended) — the plan carries the affected-cell world positions +
  sweep origin, line vs ring.

Verified manually in play mode (thin adapters): `CombatController` round orchestration (incl. the
D2 initiator-led phase reorder), `EnemyRoundController` pacing, the D2 aim/fire input
(`PCInputController` Enter hold-to-aim/release-to-execute, `AbilityInputHandler` volley routing), the
D3 ability animation (`AbilityCellFlash`/`AbilityAreaSweep`, `LiveAbilityAnimationView`, the animated
ghost) + enemy telegraph (`EnemyIntentTelegraphView` arrow/pose, the restless-icon jitter/pulse),
`TurnOrderStripView`, `UnitFacingRotator`, `UnitOverheadIconsView`, `GhostPlaybackView`,
`GhostVisualCloner`, `AbilityIconHoverController`.

---

## 6. Known limitations / open points

- **No queue simulation.** Ghost previews run against the current board, so a chain (ability B
  after A's push) may not preview perfectly — accepted per the brief; "dry-run the queue then
  preview" is a later upgrade. *(ROADMAP)*
- **Initiator-led opening round only (D2).** The opening round leads with the fight's initiator;
  rounds 2+ are player-then-enemies. **Speed/stat-based** initiative (a fast unit leaping ahead) and
  the **multi-round lead policy** (alternate/persist/re-roll) stay deferred. *(ROADMAP — Track K)*
- **TacticalAI aims blindly**: it scores an ability equally for all six facings, so ties break
  deterministically toward the first direction — pre-existing; the smarter-AI item covers it.
  *(ROADMAP)*
- **Ability animation + enemy pose are code-authored placeholders (D3).** Abilities play a
  shape-driven **cell-sweep** and armed enemies a **transform wind-up pose**; production clips/VFX
  and consuming `AbilityDefinition._animationTrigger` (a real skeletal animation per ability) are a
  later art pass. The pose writes the enemy root's position/scale, so it is cleared before a move
  resolves to avoid fighting the mover. *(ROADMAP — P5-9 / render-look)*
- **`TelegraphStyle` constants are code constants**, not a config SO (now also the D3 sweep/arrow/
  readiness dials). *(ROADMAP)*
- **Ring push unsupported** (`ability-subsystem.md` §5). *(ROADMAP)*
- **Round-effects cadence** changed from per-own-turn to per-round-all-units — equivalent for a
  two-party fight; would need revisiting if a third party ever joins a combat.
- **Enemy plans are one action per round** (matching the old one-action-per-turn shape); the brief
  wording "whole planned action(s)" is satisfied trivially. Multi-action enemy plans would need an
  AI rework.
