# Combat Status Effects — Requirements & Design

> The persistent condition layer of combat (Track S): a **status** is a named, data-authored
> condition on a unit that persists across rounds, ticks or gates at one deterministic point,
> and expires — legible on the board and identical in PvE and Arena lockstep. Includes the
> **one modifier model** (S3/P3-13): a stat-modifier status and a body part's always-on passive
> are the same asset, the passive being the duration-less case.
> Status: current as of 2026-07-07. Consumed brief: `product-requirements/combat-status-effects.md`.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** A status is a named condition applied to a unit that persists across rounds. It carries a
  duration in rounds and a kind; it resolves per its kind and is removed when its duration reaches
  zero (or never, for infinite `-1` durations — the part-passive case). Multiple different statuses
  may coexist on one unit.
- **R2** Three kinds ship as the starter vocabulary, all data-authored (a new status is an asset,
  never code): **Damage-over-time** (burn/poison/bleed), **Control** (`Stun` = loses its turn,
  `Root` = cannot move but may act, `Slow` = movement range reduced by a per-stack penalty), and
  **Stat-modifier** (a flat signed magnitude against one stat target). Heal-over-time (regen) is a
  fourth authored kind reusing the same tick.
- **R3 (deterministic tick)** All statuses resolve at **one fixed point: the round-end bookkeeping
  pass** (`RoundLifecycleProcessor.ApplyRoundEndEffects`), which PvE and Arena controllers call from
  the identical `EndRound()` seam. Per unit: `TurnEnd` triggers fire (DoT damage / HoT heal), then
  durations count down, then expired effects are removed. Owner decision 2026-07-07: in this
  phase-based round model "end of the afflicted unit's turn" ≡ end of round for all units.
- **R4** Expiry is visible: the effect drops off the unit's status list (and therefore its on-unit
  icon row) the moment its duration hits zero. A DoT kill at round end settles the fight **at
  `EndRound()` itself** — both controllers run an explicit win check right after the tick.
- **R5 (stack rules)** Re-applying a status already on a unit resolves per its authored
  `StackRule`: **Refresh** (default — duration resets, no pile-up), **StackToCap** (adds a stack up
  to `MaxStacks`, magnitude scaling per the per-stack values; the duration refreshes even at the
  cap), or **Ignore** (no-op while active — no stun chain-lock).
- **R6 (application)** An ability applies a status by referencing which status (+ optional duration
  override) on its definition; application rides the ability's existing targeting/area — every unit
  in the struck cells receives it. The authored duration override reaches the **applied instance**.
- **R7 (on-unit legibility, S2)** Every active status shows on the unit on the board: a world-space
  billboard row (below the plan-icon row) with the status's glyph + remaining rounds (`×N` suffix
  for stacks; no number for infinite/passive). Rebuilt from the immutable state on every state
  change; cleared on death/expiry. Same view pair in PvE and Arena.
- **R8 (one glyph)** The status's `Glyph` sprite is authored once and is the same asset on the unit
  row and in the mutation card's effect badge; a card ability that applies no status shows the
  neutral `glyph_untyped` chevron authored on the prefab.
- **R9 (preview grammar)** The shared formatter `StatusEffectPreviewText` produces the
  "Applies: <status> (N turns)" / "Standing: <status>" line for the ability-preview popover and
  the mutation card description — one grammar for every surface.
- **R10 (one modifier model, S3/P3-13)** Status stat-modifiers and part passives share ONE model:
  `DataDrivenModifierEffect` with a **flat signed `Magnitude`** and a **`StatTarget`**
  (`OutgoingDamage` — added to damage the unit deals; `IncomingDamage` — added to damage the unit
  receives). A part passive is the same asset applied with infinite duration at combat start. The
  old percent modifier and the factory's Buff/Debuff sign-flip are gone; `Buff`/`Debuff` is an
  informational classification only.
- **R11 (movement budget)** `MovementRange.EffectiveFor(unit, baseRange)` is the single source of a
  unit's per-round move budget (Root → 0, Slow → base − penalty·stacks, floored at 0), consumed by
  the action validator, the movement rules, all enemy AIs, and the committed-move resolve (a
  root/slow landing after the commit fizzles the move).
- **R12 (lockstep safety)** Status state rides the immutable `Unit` and is covered by
  `ArenaStateHash` (id + duration + stacks per effect); the modifier math is integer-only. DoT/HoT
  ticks apply **raw** (`ApplyDamage`/`ApplyHealing`, no hit modifiers) — the condition already
  "landed" when applied.

### 1.2 Non-functional requirements

- **N1** All status logic is pure C# (`Combat.Core` / `Combat.Execution`) and unit-tested.
- **N2** Dependencies wired through Zenject (`AreaInstaller` / `ArenaInstaller`).
- **N3** `StatusEffectDefinition` is a data-only ScriptableObject; runtime records are built by
  `StatusEffectFactory` (the only SO → Core bridge).

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Combat/
  Core/                      — StackRule, ControlKind, StatTarget, MovementRange,
                               StatusEffect + IStatusEffect, Unit gating
  Core/StatusEffects/        — DataDriven* records, StatusEffectDurations,
                               StatusEffectTriggerProcessor, StatusEffectTriggerType
  Data/Definitions/          — StatusEffectDefinition (+ the ability definitions referencing it)
  Data/Factories/            — StatusEffectFactory, AbilityFactory (SO -> Core)
  Data/Providers/            — StatusEffectDefinitionCatalog (id-indexed Resources load)
  Data/                      — StatusEffectPreviewText (shared "Applies:" grammar)
  Execution/                 — AbilityExecutor (application + stack rules), DamageSystem
                               (two-sided flat modifier math), ActionValidator, EnemyIntentResolver
  Player/                    — UnitStatusIconsPresenter + StatusIconModel (pure presenter)
  View/                      — UnitStatusIconsView (world-space billboard row)
Scripts/Core/DI/AreaInstaller.cs · ArenaInstaller.cs
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `StatusEffect` / `IStatusEffect` | Immutable condition: id, name, type, duration, stacks, `StackRule`; `DecrementDuration` / `AddStack` / `WithDuration` copies |
| `DataDrivenDamageOverTimeEffect` / `...HealOverTimeEffect` | Per-tick damage/heal (+ per-stack scaling, `MaxStacks`) |
| `DataDrivenControlEffect` | `ControlKind` (Stun/Root/Slow) + `MovementPenalty` |
| `DataDrivenModifierEffect` | Flat signed `Magnitude` against a `StatTarget` — the one modifier model (R10) |
| `MovementRange` | Static: the status-gated per-round move budget (R11) |
| `StatusEffectDurations` | Pure round tick: `>0` counts down and drops at 0, `<0` infinite |
| `StatusEffectTriggerProcessor` | Fires `ITriggeredStatusEffect`s per trigger flag (TurnEnd = the canonical tick) |
| `RoundLifecycleProcessor` | The shared PvE/Arena round bookkeeping — the ONE deterministic status resolve point (R3) |

### 2.3 Runtime flow

1. **Application** — `AbilityExecutor.ExecuteAbilityAtCells` (the single path under the PvE player
   queue, PvE enemy intents, and Arena commits) hits a unit with an `IStatusEffectAbility`:
   no existing instance → append `EffectToApply`; existing → resolve per `StackRule` (R5).
2. **Standing passives** — `CharacterCombatInitializer` (PvE) / `ArenaHeroSpawner` (Arena) resolve
   each equipped part's `PassiveAbilityDefinition.Modifier` through `StatusEffectFactory` with
   duration `-1` and seed the unit's status list at combat start.
3. **Gating** — `Unit.ActionState` reads Stun (validator/HUD/intent-resolver all key off it);
   `Unit.CanMove()` + validator/rules/AIs read `MovementRange.EffectiveFor` (R11).
4. **Modifier math** — `DamageSystem.CalculateFinalDamage(attacker, target, base)` =
   `max(0, base + Σ attacker OutgoingDamage + Σ target IncomingDamage)` (magnitude × stacks each).
5. **Tick** — both `EndRound()`s: `ApplyRoundEndEffects` (TurnEnd triggers → decrement → drop) →
   state-changed event → explicit win check (R4) → next round.
6. **Presentation** — `UnitStatusIconsPresenter` rebuilds per-unit `StatusIconModel` lists on every
   state change; `UnitStatusIconsView` renders glyph + turns as a billboard row parented to the
   unit's visual root via `ICombatUnitViewRegistry`.

### 2.4 DI wiring

`AreaInstaller` and `ArenaInstaller` both bind `IStatusEffectDefinitionCatalog →
StatusEffectDefinitionCatalog` (loads `Resources/Combat/StatusEffects`), alongside the existing
`IStatusEffectFactory`/`IAbilityFactory`/`IDamageSystem`/`StatusEffectTriggerProcessor` bindings.
The on-unit row is constructed (not container-bound) by `CombatActiveState.OnEnter` (PvE) and
`ArenaSceneEntrypoint.CreateTelegraphPresentation` (Arena), same idiom as the plan-icon row.

---

## 3. ScriptableObject Reference

### `StatusEffectDefinition`  (asset menu: `Create → Combat → Status Effects → Status Effect`)

Loaded from `Resources/Combat/StatusEffects/` by `StatusEffectDefinitionCatalog` (id-indexed;
duplicate ids warn and keep the first).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_id` | int | Stable identity; the glyph lookup key and the re-application match key | unique across statuses |
| `_name` | string | Display name (unit row hover-free read, preview text) | |
| `_description` | string | Designer-facing description | |
| `_glyph` | Sprite | The ONE glyph (R8): on-unit row + card effect badge | shape-distinct, not colour-only |
| `_type` | `StatusEffectType` | Kind: `DamageOverTime` / `HealOverTime` / `Control` / `Buff` / `Debuff` (Buff/Debuff = stat-modifier, informational polarity) | Debuff |
| `_duration` | int | Default duration in rounds; `-1` = infinite (part passive) | 2 |
| `_stackRule` | `StackRule` | `Refresh` (default) / `StackToCap` / `Ignore` (R5) | Refresh |
| `_maxStacks` | int | Stack cap for StackToCap (0 = unlimited) | 0 |
| `_triggerType` | `StatusEffectTriggerType` | When it fires; **`TurnEnd` is the canonical deterministic tick** (R3) | TurnEnd |
| `_damagePerTrigger` | int | DoT damage per tick | 0 |
| `_healPerTrigger` | int | HoT heal per tick | 0 |
| `_controlKind` | `ControlKind` | Control only: `Stun` / `Root` / `Slow` | Stun |
| `_movementPenalty` | int | Slow only: cells removed per stack | 1 |
| `_statTarget` | `StatTarget` | Modifier only: `OutgoingDamage` / `IncomingDamage` | OutgoingDamage |
| `_magnitude` | int | Modifier only: flat SIGNED delta (Weakened −5 outgoing, Hardened −5 incoming) — never sign-flipped by type | 0 |
| `_hpThreshold` / `_thresholdDirection` | float / enum | OnThreshold trigger settings (legacy trigger, unused by the starter set) | 0.5 / Below |
| `_damagePerStack` / `_healPerStack` | int | StackToCap scaling: added to the per-trigger value per stack | 0 |

Abilities reference a status via `StatusEffectAbilityDefinition` / `HybridAbilityDefinition`
(`_statusEffect` + `_durationOverride`, `-1` = the status default); a part passive via
`PassiveAbilityDefinition._modifier` (any kind is valid — a HoT passive is permanent regen).

### Authored starter set (demo values, tunable — the balance ledger owns final numbers)

All at `Resources/Combat/StatusEffects/`, all `TurnEnd`:

| Asset | id | Kind | Payload | Dur | StackRule | Glyph |
|---|---|---|---|---|---|---|
| `Status_Burn` | 1 | DoT | 8/round | 2 | Refresh | `glyph_burn` flame |
| `Status_Poison` | 2 | DoT | 4/round/stack | 3 | StackToCap 3 | `glyph_poison` droplet |
| `Status_Bleed` | 3 | DoT | 6/round | 2 | Refresh | `glyph_bleed` slash |
| `Status_Stun` | 4 | Control·Stun | skips its turn | 1 | Ignore | `glyph_stun` star |
| `Status_Root` | 5 | Control·Root | no move, may act | 2 | Refresh | `glyph_root` shackle |
| `Status_Slow` | 6 | Control·Slow | −1 move cell | 2 | Refresh | `glyph_slow` spiral |
| `Status_Weakened` | 7 | Debuff | Outgoing −5 | 2 | Refresh | `glyph_weakened` arrow-down |
| `Status_Empowered` | 8 | Buff | Outgoing +5 | 2 | Refresh | `glyph_empowered` arrow-up |
| `Status_Hardened` | 9 | Buff | Incoming −5 | 2 | Refresh | `glyph_hardened` shield |
| `Status_Regen` | 10 | HoT | +5/round | 3 | Refresh | `glyph_regen` heart-plus |

Glyph PNGs (placeholder art, 64×64 white-on-transparent) live at
`Resources/Combat/StatusEffects/Glyphs/` incl. the neutral `glyph_untyped` chevron (the card
badge's "no status" mark — there is **no** elemental damage-type system; see ROADMAP S5).

Demo wiring: `Ability_FireBreath` (Burn) → `Part_Head_A` · `Ability_VenomSpit` (Poison) →
`Part_SpineSerpent` + `TestEnemyDefinition` · `Ability_StunClap` (Stun) → `Part_LegsSpider` ·
`Ability_Cripple` (Slow) + `Passive_Regenerator` → `Part_Tail_A` · `Ability_Weaken` (Weakened) →
`Part_ArmL_A` · `Ability_WarCry` (Empowered) → `Part_ArmR_A` · `Passive_HardenedHide` →
`Part_Torso_B`. Hardened-timed vs Hardened-as-passive is the R10 one-model demonstration.

---

## 4. Adding Content

### Add a status (no code)

1. Author (or reuse) a glyph sprite under `Resources/Combat/StatusEffects/Glyphs/` — shape-distinct.
2. `Create → Combat → Status Effects → Status Effect`; fill §3 (unique `_id`, kind fields for the
   chosen `_type`, keep `_triggerType = TurnEnd`).
3. Save it under `Resources/Combat/StatusEffects/` — the catalog picks it up by id.

### Hang a status on an ability (no code)

1. `Create → Combat → Abilities → Status Effect Ability` (or `Hybrid Ability` for damage + status).
2. Point `_statusEffect` at the status asset; set `_durationOverride` (`-1` = status default);
   set the ability `_icon` (the status glyph works well for the demo).
3. Save under `Resources/Abilities/Data/` and add it to a part's `_activeAbilities` (or an
   enemy definition's `_abilities`).

### Grant a standing passive from a part (no code)

1. `Create → Combat → Abilities → Passive Ability`; point `_modifier` at any status asset (its
   authored duration is ignored — passives last the whole combat).
2. Add it to the part's `_passiveAbilities`.

**Authoring constraints / gotchas:** ids must be unique (duplicates keep the first); a Control
status only reads `_controlKind`/`_movementPenalty`; a Buff/Debuff only reads
`_statTarget`/`_magnitude` — author the magnitude **signed** (a Buff with a negative incoming
magnitude is Hardened); combos are out for v1 — author each status independent.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `StatusEffectStackRuleTests` — R5 through the real executor path (refresh resets / cap
  accumulates + refreshes at cap / ignore no-op / poison 4→8 scaling).
- `ControlGatingTests` — R2/R11: stun gates everything + skips committed intents; root pins but
  acts + fizzles committed moves; slow shrinks the budget in validator + rules; budget floor.
- `RoundEndStatusTests` — R3/R4/R12: tick-then-decrement order, visible expiry, two-round totals,
  raw DoT despite Hardened, HoT clamp, infinite passive persistence, DoT kill.
- `StatusEffectFactoryTests` — SO→Core mapping (kind routing, signed magnitude + target, control
  kind/penalty, stack rule/cap) + the duration-override fix reaching `EffectToApply`.
- `StatusPreviewTextTests` — R9 grammar (singular/plural, override wins, no-status, standing).
- `DamageSystemModifierTests` — R10 flat two-sided math (both sides, stacks, floor at 0, null target).
- `ArenaLockstepTests.StatusApplication_TickAndExpiry_StayInLockstepAcrossRounds` — R12 end-to-end
  (two client sims, identical hashes across apply/tick/expiry rounds).
- `ArenaEdgeCaseTests.DoTKillAtRoundEnd_DecidesTheMatchImmediately` — R4 at the controller seam.

Verified manually (play mode): the on-unit icon row look/placement, the card badge, popover text.

---

## 6. Known limitations / open points

- **Ghost preview ignores statuses** — `AbilityOutcomeCalculator` predicts damage only; the ghost
  does not show "will apply Burn". (ROADMAP)
- **Self-buffs are not expressible** — ability areas exclude the caster's own cell, so `WarCry`
  buffs adjacent units, not the caster. Needs an "includes self" shape flag. (ROADMAP)
- **Cleanse / immunity flag** — FR11's thin removal/immunity flag is not built (expiry is the only
  removal today). (ROADMAP)
- **Status combos (S4)** and **AI reasoning about statuses** (avoiding DoT, choosing to cleanse) —
  parked; Track K's smarter AI owns the latter.
- **Max-HP stat target** — `StatTarget` covers outgoing/incoming damage only; regen is HoT.
- **TurnStart / OnApply / OnRemove / OnThreshold triggers** exist as flags but no starter content
  uses them; only TurnEnd is exercised.
- **Glyph art is placeholder** (code-drawn white shapes) — designer swap by overwriting the PNG
  bodies (GUIDs stable).
- **Status VFX / tick animation** — render-look / VFX-language work, not built here.
