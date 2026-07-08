# Combat Enemy AI — Requirements & Design

> How enemies (and Arena offline dummies) decide their action each round: a simulation-based
> scorer that aims abilities at the units actually standing in the area, plus a two-layer
> difficulty configuration (per-enemy profile dials × one global preset). This is the P2-4
> "smarter ability-using enemy AI" system; the *when/commit* half of the round lives in
> `combat-round-and-telegraph.md`.
> Status: current as of 2026-07-07.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1 — Aim at real targets.** Ability candidates are scored from their actually-affected cells
  (via the same `IAbilityOutcomeCalculator` the ghost telegraph uses), so a line ability fires
  along the facing that hits the most valuable hostiles — never a fixed tie-break direction, never
  a blank cast into empty hexes (an ability that hits no one scores below doing nothing).
- **R2 — Kill-securing.** A hit predicted to finish a hostile earns `KillBonus`; by default a
  guaranteed kill beats spreading the same damage over more targets. The dial is authorable per
  profile and scalable per difficulty.
- **R3 — Friendly-fire avoidance.** Damage (and status) landing on non-hostiles subtracts from the
  candidate's score (`FriendlyFirePenaltyWeight`); PvE enemies no longer consider each other
  targets (see R7).
- **R4 — Value heals by missing HP.** A heal contributes `min(heal, missingHP) × HealWeight` on
  self/allies and nothing on full-HP targets or hostiles.
- **R5 — Move with purpose (lookahead).** A move destination scores its positioning term plus a
  discounted *improvement*: how much the best ability shot from that cell exceeds the best shot
  from the current cell. Improvement — not absolute value — so a unit that can already hit attacks
  instead of endlessly repositioning; a unit that cannot hit walks toward the shot.
- **R6 — Configurable difficulty, two layers.**
  - Per-enemy: `AIProfileDefinition` carries decision-quality dials (score noise, pick-from-top-N,
    mistake chance) and tactical priorities (focus-wounded, friendly-fire penalty, aggression,
    self-preservation) alongside the pre-existing weights.
  - Global: one `DifficultyDefinition` asset (Easy/Normal/Hard presets ship) modulates every
    enemy at once — quality dials compose additively, priority weights multiplicatively.
  - Explicitly NOT here: HP/damage stat multipliers (future Heat system, Track Y).
- **R7 — Mode-appropriate hostility.** Who counts as a target is an injected policy: PvE is
  team-based (`PlayerType.AI` side vs everyone else), the Arena is free-for-all (every other
  unit). Fixes the latent PvE bug where per-enemy `AIPlayer` ids made fellow enemies score as
  targets.
- **R8 — One brain for both modes.** PvE enemies (`EnemyCombatIntegrator`) and Arena offline
  dummies (`ArenaSceneEntrypoint`) build the same `SimulationTacticalAI` through the same
  `AIDecisionMakerFactory`; only the hostility policy and difficulty binding differ per scene.
- **R9 — Determinism.** Same seed + same state ⇒ same decision, with noise and top-N active. All
  randomness flows through one seeded `System.Random` per enemy (`LootSeed.Derive(runSeed,
  "combat-ai:{enemyId}")` in PvE, `"arena-ai:{playerId}"` in the Arena); candidate enumeration
  order is fixed (ability list × fixed facing order, move cells sorted by (Q, R)).

### 1.2 Non-functional requirements

- **N1** The decision pipeline is pure C# (no UnityEngine) under `Combat.Player.AI`, fully
  edit-mode tested.
- **N2** All wiring through Zenject (`AreaInstaller` / `ArenaInstaller`); no service locators.
- **N3** `AIProfileDefinition` / `DifficultyDefinition` are data-only SOs, mapped to pure records
  (`AIProfileMapper` / `DifficultyDefinitionMapper`) at creation/install time — the only SO → Core
  bridge.
- **N4** Plan-time cost only: candidates ≈ abilities×6 + up to 64 move cells (+ the same again per
  move cell for lookahead), computed once per enemy per round at the Plan phase — never per frame.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Combat/Player/AI/            — pure C# (no UnityEngine)
  AIBehaviorProfile.cs               — per-enemy tuning record (SO snapshot)
  AIDifficultySettings.cs            — global difficulty record (SO snapshot)
  AITuning.cs                        — profile × difficulty composition + pipeline constants
  IHostilityPolicy.cs                — who is a target
  TeamHostilityPolicy.cs             — PvE: AI side vs everyone else
  FreeForAllHostilityPolicy.cs       — Arena: everyone
  AICandidate.cs / AICandidateKind.cs / AIScoredCandidate.cs / AIFacings.cs
  AICandidateEnumerator.cs           — fixed-order candidate list
  AIActionScorer.cs                  — simulation-based scoring (+ move lookahead)
  AIDecisionQualityFilter.cs         — mistake / noise / top-N (the only RNG consumer)
  SimulationTacticalAI.cs            — IAIDecisionMaker orchestrator
  IAIDifficultySource.cs / StaticAIDifficultySource.cs
  AIDecisionMakerFactory.cs          — the one place tactical brains are built
Scripts/Combat/Data/Definitions/     — AIProfileDefinition, DifficultyDefinition + mappers
Scripts/Core/DI/AreaInstaller.cs · ArenaInstaller.cs — policy/difficulty/factory bindings
```

`SimpleRandomAI` (in `Combat/Player/`) remains as the `AIPersonality.SimpleRandom` content value
and dev-scene fallback. The former `TacticalAI` / `ConfigurableTacticalAI` are retired — the
simulation AI with the default profile subsumes both.

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `AIBehaviorProfile` | Immutable per-enemy tuning; `Default` mirrors the SO field initializers |
| `AIDifficultySettings` | Immutable global modulation; `Neutral` is the identity |
| `AITuning` | `Compose(profile, difficulty)` → the effective dials; owns `LookaheadDiscount = 0.6`, `EndTurnBaselineScore = 10`, `MaxEvaluatedMovePositions = 64` |
| `AICandidateEnumerator` | Deterministic candidate list: ability × facing (Line: 6 fixed-order; Ring: one), free move cells sorted by (Q, R) capped at 64, end turn |
| `AIActionScorer` | `ScoreAll`: fold simulated `UnitOutcome`s into scores (see §2.3); computes the lookahead baseline once per decision |
| `AIDecisionQualityFilter` | One mistake roll → per-candidate noise (always drawn) → stable sort → uniform pick among top N; defaults degenerate to argmax |
| `SimulationTacticalAI` | `IAIDecisionMaker`: enumerate → score → filter → `IAction` |
| `AIDecisionMakerFactory` | Composes tuning from profile + bound difficulty and builds the AI per enemy seed |

### 2.3 Scoring model

For each simulated `UnitOutcome` of an ability candidate (all weights from `AITuning`):

| Outcome | Contribution |
|---|---|
| Damage on hostile | `+ dmg × DamageWeight × AggressionWeight` |
| Predicted kill of hostile (`hp − dmg + heal ≤ 0`) | `+ KillBonus × AggressionWeight` |
| Per damaged hostile | `+ FocusWoundedWeight × (1 − hpFraction)` |
| Damage / kill on non-hostile (incl. self) | `− dmg × FriendlyFirePenaltyWeight` / `− KillBonus × FriendlyFirePenaltyWeight` |
| Heal on self/ally | `+ min(heal, missingHP) × HealWeight`; 0 on hostiles |
| Status ability, per hostile hit not already carrying the effect | `+ StatusEffectBonus`; `− StatusEffectBonus` per non-hostile hit |

Moves: approach gradient when healthy (`(10 − dist) × 2`, `+ CloseRangeBonus` adjacent), retreat
(`dist × 3 × SelfPreservationWeight`) when at/below `DefensiveHpThreshold`, `− SurroundPenalty`
per hostile within 2 — deliberately an order of magnitude below damage terms — plus
`max(0, bestShotFrom(dest) − bestShotFrom(here)) × LookaheadDiscount`. End turn scores a flat 10.

The scorer reuses `IAbilityOutcomeCalculator.ComputeForFacing` (with a new hypothetical-origin
overload for lookahead), so AI expectations, the ghost telegraph, and execution agree by
construction. In lookahead the caster's own outcome is excluded (it still occupies its old cell
in the simulated state).

### 2.4 DI wiring

Both installers bind, next to the outcome calculator:

- `IHostilityPolicy` → `TeamHostilityPolicy` (`AreaInstaller`) / `FreeForAllHostilityPolicy`
  (`ArenaInstaller`).
- `IAIDifficultySource` → `StaticAIDifficultySource` holding
  `DifficultyDefinitionMapper.ToSettings(asset)`; the asset comes from the installer's
  `_difficulty` inspector field, falling back to
  `Resources.Load<DifficultyDefinition>("Combat/Difficulty/NormalDifficulty")`, then to `Neutral`
  with a warning (soft degrade, never a failed install).
- `AIDecisionMakerFactory` `AsSingle()`.

Consumers: `EnemyCombatIntegrator.CreateDecisionMaker` (PvE — `Tactical` personality →
`factory.Create(AIProfileMapper.ToProfile(enemy profile), seed)`), `ArenaSceneEntrypoint.
SeatOfflinePlayers` (dummies → `factory.Create(AIBehaviorProfile.Default, seed)`).

---

## 3. ScriptableObject Reference

### `AIProfileDefinition`  (asset menu: `Create → Combat → AI → AI Profile`)

Assigned per enemy via `EnemyDefinition._aiProfile`. Existing asset:
`Resources/Enemies/AIProfiles/TestAIProfile.asset`. Legacy assets lacking the newer fields keep
working — missing YAML keys deserialize to the C# defaults below, which reproduce the sharp
pre-P2-4 behavior.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_basePersonality` | `AIPersonality` | `SimpleRandom` or `Tactical` (the simulation AI) | `SimpleRandom` |
| `_movementRange` | int [0–5] | Move cells considered per decision | 3 |
| `_damageWeight` | float | × damage dealt to hostiles | 2 |
| `_healWeight` | float | × HP actually restored | 1.5 |
| `_killBonus` | float | Bonus per predicted finishing hit | 50 |
| `_statusEffectBonus` | float | Per hostile hit not already carrying the effect | 30 |
| `_focusWoundedWeight` | float ≥0 | × missing-HP fraction per damaged hostile | 25 |
| `_friendlyFirePenaltyWeight` | float ≥0 | × damage landing on non-hostiles (subtracted) | 2 |
| `_aggressionWeight` | float [0–3] | Scales all damage + kill terms | 1 |
| `_selfPreservationWeight` | float [0–3] | Scales the retreat term when wounded | 1 |
| `_scoreNoise` | float [0–50] | ± uniform noise per candidate score | 0 |
| `_pickFromTopN` | int [1–10] | Uniform pick among the N best | 1 |
| `_mistakeChance` | float [0–1] | Chance of a uniformly random action | 0 |
| `_defensiveHpThreshold` | float [0–1] | HP fraction at/below which the unit retreats | 0.5 |
| `_closeRangeBonus` | float | Move bonus for ending adjacent to a hostile (healthy only) | 20 |
| `_surroundPenalty` | float | Per hostile within 2 cells of a move destination | 15 |

### `DifficultyDefinition`  (asset menu: `Create → Combat → AI → Difficulty`)

Loaded from `Resources/Combat/Difficulty/` (installer field first, `NormalDifficulty` fallback).
Shipping presets: `EasyDifficulty.asset`, `NormalDifficulty.asset`, `HardDifficulty.asset`.
**No stat multipliers live here** — HP/damage scaling is Heat (Track Y) territory.

| Field | Type | Meaning | Easy / Normal / Hard |
|---|---|---|---|
| `_difficultyId` | string | Stable id (future persisted selection) | easy / normal / hard |
| `_displayName` | string | UI label | Easy / Normal / Hard |
| `_extraScoreNoise` | float ≥0 | Added to every profile's `_scoreNoise` | 25 / 8 / 0 |
| `_extraTopN` | int [0–9] | Added to every profile's `_pickFromTopN` | 2 / 1 / 0 |
| `_extraMistakeChance` | float [0–1] | Added to `_mistakeChance` (clamped to 1) | 0.25 / 0.08 / 0 |
| `_aggressionScale` | float [0–3] | × profile `_aggressionWeight` | 0.8 / 1 / 1.15 |
| `_killSecuringScale` | float [0–3] | × profile `_killBonus` | 0.6 / 1 / 1.25 |
| `_statusValueScale` | float [0–3] | × profile `_statusEffectBonus` | 1 / 1 / 1 |

Composition (`AITuning.Compose`): quality dials add, priority weights multiply; a `Neutral`
difficulty (all zeros / ones) leaves every profile exactly as authored.

---

## 4. Adding Content

### Add a difficulty preset

1. `Create → Combat → AI → Difficulty`; name it (e.g. `NightmareDifficulty`).
2. Fill the fields per §3 — negative-quality dials (noise/topN/mistake) make enemies sloppier,
   priority scales above 1 make them meaner. Leave stat scaling to Heat.
3. Place the asset under `Resources/Combat/Difficulty/`.
4. Activate it: assign it to the `_difficulty` field of `AreaInstaller` (Journey) and/or
   `ArenaInstaller` (Arena) in the scene — or overwrite `NormalDifficulty.asset`'s values, which
   both installers fall back to.
5. Validate: enter play mode; the install log warns only when no asset resolves.

### Tune an enemy's AI profile

1. `Create → Combat → AI → AI Profile`; set `_basePersonality: Tactical`.
2. Dial the weights per §3 (e.g. a berserker: `_aggressionWeight 2`, `_selfPreservationWeight 0`,
   `_friendlyFirePenaltyWeight 0.5`; a coward-caster: `_defensiveHpThreshold 0.8`,
   `_selfPreservationWeight 2`).
3. Point the enemy's `EnemyDefinition._aiProfile` at the new asset.
4. Validate: enter combat with that enemy; its telegraphed intents should reflect the personality.

**Authoring constraints / gotchas:** an enemy with no profile (or `SimpleRandom` personality)
still works — no-profile Tactical enemies use the sharp `AIBehaviorProfile.Default`. Difficulty
quality dials only ever *degrade* decisions; a "harder" preset is priority scales, not negative
noise.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `SimulationTacticalAITests` — line aiming (R1), kill-securing + dial (R2), friendly fire + dial
  + FFA policy (R3/R7), heal-by-missing-HP (R4), move lookahead (R5), seeded determinism with
  noise (R9), difficulty monotonicity (Easy picks the optimum less often than Neutral; certain
  mistake abandons argmax) (R6).
- `AIDecisionQualityFilterTests` — argmax degeneration, mistake path, top-N spread bounds, seed
  determinism, stable-sort tie order.
- `AITuningTests` — Neutral identity (back-compat), additive/multiplicative composition, clamps.
- `AIDefinitionMapperTests` — SO → record mapping, null → Default/Neutral, fresh-asset defaults
  (legacy back-compat; the SO-based cases need the Unity engine and no-op on the CLI runner).
- `AbilityOutcomeCalculatorTests` — the hypothetical-origin overload: parity with the default
  overload and origin-relative cells.
- `EnemyIntentPlannerTests.Plan_SameSeededAI_SameState_YieldsIdenticalIntents` — determinism
  through the intent-lock path with the new AI.

Verified manually (play mode): telegraphed enemy facings point at the hero party; Easy vs Hard
presets read as sloppier vs sharper aim.

---

## 6. Known limitations / open points

- **No status-avoidance reasoning.** The AI values *applying* statuses against real targets, but
  does not avoid standing in telegraphed areas, dispel, or reason about statuses on itself.
- **One action per round — no volley planning.** Human players can queue multiple abilities in a
  round; the AI commits exactly one action (`EnemyIntentPlanner` requests a single `IAction`).
- **Single bound difficulty, no selection UI.** The preset is whatever the installer binds; a
  settings screen + persisted choice behind `IAIDifficultySource` is roadmapped.
- **Simultaneous-plan drift.** All enemies plan against the same round-start snapshot, so two
  enemies may commit to killing the same unit; by intent-lock design (dodged blows whiff) this is
  accepted, not a bug.
- **Positioning constants are code-level.** The approach/retreat gradients live as constants in
  `AIActionScorer`; only their weights are authorable.
