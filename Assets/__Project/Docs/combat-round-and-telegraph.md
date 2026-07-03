# Combat Round & Telegraph — Requirements & Design

> The combat round structure (Plan → Act → Resolve with locked, revealed enemy intents) and the
> readability layer on top of it: hero facing legibility, overhead plan icons on every unit, and
> the one-shot / hover-replay ghost preview of an ability's full outcome. Implements the three
> verified PO briefs `combat-hero-facing.md`, `combat-turn-intent-phase.md`, and
> `combat-ability-ghost-telegraph.md` (Track C) as one combat pass.
> Status: current as of 2026-07-03.
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
- **R4** Resolution order is **player, then enemies**: player actions resolve live during Act;
  enemy intents resolve one by one (with visual pacing) in Resolve. Win conditions are checked
  after every resolved intent — combat can end mid-resolve.
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

**Facing legibility** (brief `combat-hero-facing.md`; the aiming model is in `ability-subsystem.md`)

- **R8** A unit's model visually points along its domain `FacingDirection` (`UnitFacingRotator`
  slerps the yaw on every state change) — reading an enemy's facing is reading where its committed
  blow will land, and the enemy turns toward its committed facing when it schedules (AI
  turn-and-schedule) and again at resolve.

**Overhead plan icons** (brief `combat-ability-ghost-telegraph.md` §5)

- **R9** Every unit — player **and** enemy — shows its plan as **icons above it, in order**:
  the player's queued abilities (by `ExecutionOrder`), an enemy's committed intent (its ability's
  icon, or a `»` glyph for a committed move). Enemy icons appear the moment plans are revealed at
  EnemyPlan, before the player acts. Dead units show nothing; an executed queue clears the row.

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
  Core/            RoundPhase, EnemyIntent, AbilityOutcome, FacingGeometry,
                   DisplacementResolver (pure C#)
  TurnManagement/  EnemyIntentPlanner (pure), TurnManager (round counter, acting player)
  Execution/       EnemyIntentResolver, AbilityExecutor.ExecuteAbilityAtCells,
                   AbilityOutcomeCalculator (pure)
  Controller/      CombatController — the round orchestrator (StartRound / CheckTurnEnd /
                   ResolveNextEnemyIntent / EndRound)
  Player/          EnemyRoundController (paced resolve coroutine),
                   UnitPlanIconsPresenter, GhostPlaybackPresenter,
                   GhostPlaybackPlan(+Builder), PlanIconModel (pure presenters/models)
  View/            UnitFacingRotator, UnitOverheadIconsView, AbilityIconMarker,
                   GhostPlaybackView, GhostVisualCloner, CombatUnitViewRegistry,
                   TelegraphStyle (thin MonoBehaviours + constants)
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
  RoundPhase = PlayerAct; OnTurnStarted(human)

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
  `UnitPlanIconsPresenter`) and `GhostPlaybackView` (+ pure `GhostPlaybackPresenter` +
  `AbilityIconHoverController`); `OnExit` disposes them all.
- Icons: code-built world-space rows (`SpriteRenderer` per ability icon via
  `AbilityDefinitionCatalog`, TMP `»` for moves), billboarded, each icon carrying a small trigger
  `BoxCollider` + `AbilityIconMarker { UnitId, QueueIndex, IsEnemyIntent }`. Hover raycast filters
  `Physics.RaycastAll` hits by that component — no layer/project-settings changes.
- Ghosts: `GhostVisualCloner` instantiates the unit visual under an **inactive holder** (so no
  cloned combat component ever wakes up), strips behaviours/colliders/physics/labels, and swaps
  every renderer to one shared alpha-blend ghost material. `GhostPlaybackView` fades via that
  shared material (one ghost at a time by design).
- Sizing/timing constants live in `TelegraphStyle` (SO-ification is a ROADMAP follow-up).

### 2.5 DI wiring

`AreaInstaller`: `EnemyIntentPlanner`, `EnemyIntentResolver`, `IAbilityOutcomeCalculator`,
`IAbilityDefinitionCatalog`, `ICombatUnitViewRegistry`, `EnemyRoundController` — all `AsSingle`.
`CombatControllerFactory` threads the planner/resolver into each `CombatController` it creates.
`TurnManager` still implements `ITurnManager` but is degenerate: `CurrentPlayer` is pinned to the
human player (every `IsPlayerTurn` consumer keeps working) and `NextTurn()` only advances the
round counter.

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

Verified manually in play mode (thin adapters): `CombatController` round orchestration,
`EnemyRoundController` pacing, `UnitFacingRotator`, `UnitOverheadIconsView`, `GhostPlaybackView`,
`GhostVisualCloner`, `AbilityIconHoverController`.

---

## 6. Known limitations / open points

- **No queue simulation.** Ghost previews run against the current board, so a chain (ability B
  after A's push) may not preview perfectly — accepted per the brief; "dry-run the queue then
  preview" is a later upgrade. *(ROADMAP)*
- **Resolution order is fixed player-then-enemies.** Initiative/speed-based ordering is deferred.
  *(ROADMAP)*
- **TacticalAI aims blindly**: it scores an ability equally for all six facings, so ties break
  deterministically toward the first direction — pre-existing; the smarter-AI item covers it.
  *(ROADMAP)*
- **Ghost caster is a static clone** (no skeletal animation): `_animationTrigger` is authored but
  not consumed; playing the ability's animation on the ghost is a tech-art follow-up. *(ROADMAP)*
- **Move-intent icon is a placeholder `»` glyph**; committed-move destination is not drawn on the
  board. *(ROADMAP)*
- **`TelegraphStyle` constants are code constants**, not a config SO. *(ROADMAP)*
- **Ring push unsupported** (`ability-subsystem.md` §5). *(ROADMAP)*
- **Round-effects cadence** changed from per-own-turn to per-round-all-units — equivalent for a
  two-party fight; would need revisiting if a third party ever joins a combat.
- **Enemy plans are one action per round** (matching the old one-action-per-turn shape); the brief
  wording "whole planned action(s)" is satisfied trivially. Multi-action enemy plans would need an
  AI rework.
