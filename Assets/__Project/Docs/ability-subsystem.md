# Ability Subsystem — Requirements & Design

Part of the **Combat System**. This document describes the ability subsystem as implemented: what abilities are, how they are targeted, scheduled, validated, and executed, and how to add new abilities without writing code.

Status: current as of 2026-07-03.

> **Related:** the round structure (Plan → Act → Resolve), enemy committed intents, and the
> ghost telegraph presentation live in `combat-round-and-telegraph.md`. This document owns the
> ability model itself: shapes, facing-relative aiming, queueing, validation, execution, and
> authoring.

---

## 1. Requirements

### 1.1 Functional requirements

**Shapes**

- R1. Exactly two ability shapes exist: **Line** and **Ring**.
- R2. **Line**: a straight row of cells, width 1, configurable length (min 1). The first cell is adjacent to the caster in the chosen direction; the line continues in that direction. Cells outside the battlefield truncate the line (it stops at the first out-of-bounds cell).
- R3. **Ring**: a hollow ring of cells at *exactly* the configured radius (min 1, default 1) around the caster. The caster's cell and all inner cells are excluded. Out-of-bounds cells are filtered out. A Ring has no direction.

**Targeting & input (PC) — aim = turn (facing-relative)**

- R4. Directional abilities fire **along the unit's facing** ("where the eyes look is where it fires"). There is **no per-ability stored direction**: the unit carries one `FacingDirection` and every directional ability in its queue uses it — turning re-points the **whole volley** at once.
- R5. An ability is aimed with a hold-release interaction: **press and hold** the ability hotkey (Q/W/E/R/T/Y, configurable in `InputConfig`), **aim** with the mouse — the aim input **rotates the unit** (a free `ChangeDirectionAction`) and the battlefield highlight follows the new facing — then **release** the key to confirm. A Ring ability highlights its cells immediately on press; mouse direction is ignored.
- R6. **Turning is free and unlimited**: changing facing costs no action and stays legal after the unit has acted, right up to executing the queue. The volley fires along the **final** facing. **Right-click** while holding cancels the aim.
- R7. Confirming an ability does **not** execute it immediately — it is added to the unit's **execution queue**. Scheduling an ability ends the unit's turn. A Line ability needs no chosen direction to confirm — the unit always has a facing.
- R8. **Aim & fire the volley with Enter (hold-to-aim / release-to-execute, D2).** **Holding Enter** enters a volley-aim gesture: the hero turns to face the **mouse cursor** (a free `ChangeDirectionAction`, exactly like ability aiming), so the whole queued volley and its telegraphs re-point along the new facing while held. **Releasing Enter submits the execution queue** — it fires along the **final** facing. **Right-click while holding aborts** the aim, so the release does not fire. Queue execution is itself a turn-ending action. (Turning while aiming is free and unlimited, per R6.)
- R9. Movement uses the same interaction pattern: hold **M**, aim with the mouse, release M to confirm. Movement executes immediately and ends the turn. (Movement details belong to the combat system document; listed here because the UX is shared.)

**Execution semantics**

- R10. Queued abilities execute **one by one** in their scheduled order (`ExecutionOrder`).
- R11. Affected cells are **recomputed at execution time** from the caster's *current* position and *current* facing (Line) or no direction (Ring). The cells highlighted during aiming are a preview, not a frozen target set. (Enemy **committed** intents are the deliberate exception — see `combat-round-and-telegraph.md`.)
- R12. An ability affects **all alive units** standing in its affected cells — enemies and allies alike. There is no target filtering by ownership.
- R13. The preview (highlight) and the execution must use the **same cell calculation code path** so they can never disagree.
- R13a. A **push** ability (`PushDistance > 0`, Line only) displaces every surviving struck unit away from the caster along the line direction after damage, farthest-first, stopping before the first invalid or occupied cell (`DisplacementResolver`). Corpses are not displaced.

**Effects**

- R14. An ability applies one of the implemented effect kinds to each unit in the area: damage, healing, a status effect, or damage + status effect (hybrid).
- R15. Status effect re-application resolves per the status's authored **stack rule** (`combat-status-effects.md` R5): **Refresh** (default — the duration resets), **StackToCap** (gains a stack up to the cap; the duration refreshes even at the cap), or **Ignore** (not re-applied while active).
- R16. Status effect triggers fire during execution: `OnHit` per damaged target (if still alive), HP-threshold triggers per target whose HP changed, and `OnAttack` once per execution for the caster of a damage ability.

**Cooldowns & queue rules**

- R17. Each ability has a cooldown in turns. Cooldowns are tracked per unit per ability (`AbilityInstance`), decremented at the start of each round, and **reset when the queue executes** (not when the ability is scheduled). An enemy's committed cast starts its cooldown at resolve.
- R18. An ability on cooldown cannot be scheduled.
- R19. The queue has a maximum size (`CombatConfig.MaxAbilityQueueSize`, currently 3, bound in `AreaInstaller`); scheduling beyond it is rejected.
- R20. There is no direction validation — facing always exists on the unit. Player actions are only accepted during the round's **Act phase** (`RoundPhase.PlayerAct`); free actions (turning, reorder) stay legal after the unit has acted, turn-ending actions do not.

**Data-driven authoring**

- R21. New abilities are defined as **ScriptableObject assets** — no code changes required for a new ability of an existing effect kind.
- R22. ScriptableObject definitions contain **data only**; all runtime logic lives in pure C# classes created by a factory.

**Part-granted abilities & passives (M1)**

- R23. A body part declares the combat abilities equipping it grants: **active** abilities (usable in combat) and **passive** abilities (standing modifiers). Authored directly on `PartDefinition` (`_activeAbilities`, `_passiveAbilities`).
- R24. The player unit's combat ability set is composed from its **currently equipped parts** at combat start, deduplicated. Equipped parts are the source of truth, so mutating the body changes the next combat's abilities. `HeroDefinition.Abilities` is a temporary fallback used only when no equipped part grants an active ability.
- R25. A **passive** ability is never queued or aimed: it references a `StatusEffectDefinition` (any kind — a stat-modifier, or e.g. a HoT for permanent regen) that is applied to the unit as a standing condition for the **whole combat** (infinite duration), and removed when combat ends. This is the duration-less case of the ONE modifier model (`combat-status-effects.md` R10).
- R26. A stat-modifier condition carries a **flat signed `Magnitude`** against a **`StatTarget`**: `IDamageSystem.CalculateFinalDamage` = `max(0, base + Σ attacker OutgoingDamage magnitudes + Σ target IncomingDamage magnitudes)` (each × stack count). Integer-only, lockstep-safe; the polarity is authored in the sign, never flipped by the Buff/Debuff classification.

### 1.2 Non-functional requirements

- N1. Cell calculation, validation, and execution are pure C# and unit-testable without Unity play mode (edit-mode tests exist for the shape calculator).
- N2. Domain state is immutable: executing an ability returns a new combat state; units are updated via `With*` copy methods.
- N3. All dependencies are wired through Zenject in `AreaInstaller`; no service locators.

---

## 2. Architecture

### 2.1 Layer map

| Layer | Responsibility | Key files (under `Assets/__Project/Scripts/Combat/`) |
|---|---|---|
| Domain (pure C#) | Ability model, shapes, cell math, facing geometry, displacement, queue entries | `Core/IAbility.cs`, `Core/Ability.cs`, `Core/AbilityShapeType.cs`, `Core/AbilityShapeData.cs`, `Core/AbilityInstance.cs`, `Core/ScheduledAbility.cs`, `Core/AbilityShapeCalculator.cs`, `Core/FacingGeometry.cs`, `Core/DisplacementResolver.cs`, `Core/IDisplacementAbility.cs`, `Core/DataDrivenAbilities.cs` |
| Data (ScriptableObjects) | Authoring-time configuration | `Data/Definitions/AbilityDefinition.cs` + subclasses, `Data/Factories/AbilityFactory.cs` |
| Execution (pure C#) | Validation and state transitions | `Execution/ActionValidator.cs`, `Execution/ActionExecutor.cs`, `Execution/AbilityExecutor.cs`, `Execution/DamageSystem.cs`, `Core/StatusEffects/StatusEffectDurations.cs` |
| Data (ScriptableObjects) | Passive ability authoring | `Data/Definitions/PassiveAbilityDefinition.cs` |
| Integration (pure C#) | Bridge from equipped parts → combat ability set | `Integration/IPartAbilityResolver.cs`, `Integration/PartAbilityResolver.cs`, `Integration/PartAbilitySet.cs`, `Integration/CharacterCombatInitializer.cs` |
| Application | Aiming presenter, input translation | `Player/CombatAbilityPresenter.cs`, `Input/AbilityInputHandler.cs` |
| Infrastructure | Raw input, highlight rendering, UI | `Input/PCInputController.cs`, `Battlefield/Controller/HexCellController.cs`, `View/AbilityPreview.cs` |

Dependencies point inward: input → presenter → domain/execution. The presenter and the executor both depend on `IAbilityShapeCalculator` — the single shared code path required by R13.

### 2.2 Core domain types

- **`AbilityShapeType`** — enum: `Line`, `Ring`.
- **`AbilityShapeData`** — immutable struct: `Type`, `LineLength`, `RingRadius`; created via `AbilityShapeData.ForLine(length)` / `ForRing(radius)`.
- **`IAbility` / `Ability`** — identity (`Id`, `Name`), `CooldownDuration`, `Shape`, `EffectType`. Effect payloads live on marker interfaces implemented by subclasses: `IDamageAbility.Damage`, `IHealAbility.HealAmount`, `IStatusEffectAbility.EffectToApply` + `EffectDuration`, `IDisplacementAbility.PushDistance`.
- **`Unit.FacingDirection`** — a `HexDirection` (E/NE/NW/W/SW/SE, default E) carried by the unit itself. The entire directional targeting state: there is no per-ability direction, no saved cells, no unit IDs (R4, R11). Changed via the free `ChangeDirectionAction`.
- **`FacingGeometry`** — pure facing → axial-offset conversion over `HexDirectionConfig` (`OffsetFor`, `Neighbor`); shared by execution, displacement, and the visual yaw.
- **`DisplacementResolver`** — pure push geometry (`ResolveDestination`): walks up to `PushDistance` cells along a direction, stopping before the first invalid or occupied cell. Shared by the executor and the outcome preview so the ghost's destination always matches execution.
- **`AbilityInstance`** — pairs an `IAbility` with `CurrentCooldown`; immutable (`DecrementCooldown()` / `ResetCooldown()` return new instances).
- **`ScheduledAbility`** — queue entry: `(IAbilityInstance, ExecutionOrder)` — deliberately direction-free.

### 2.3 Shape calculation

`IAbilityShapeCalculator.GetAffectedCells(shape, casterPosition, direction, isCellInBoundary)` (`Core/AbilityShapeCalculator.cs`):

- **Line**: looks up the axial offset for the direction in `HexDirectionConfig`, steps from the caster `LineLength` times, stops at the first cell failing the boundary predicate. Returns empty if `direction` is null.
- **Ring**: iterates the axial disc of radius `RingRadius`, keeps only cells at exact cube distance `RingRadius` (hollow — excludes center and inner cells), filters by the boundary predicate.

Callers pass the boundary predicate: the presenter uses battlefield validity for highlighting; the executor passes `ICombatState.IsPositionValid`.

Tests: `Assets/__Project/Tests/EditMode/AbilityShapeCalculatorTests.cs` (line per direction, ordering, boundary truncation, hollow-ring counts and exclusions).

### 2.4 Targeting flow (PC) — aim rotates the unit

```
PCInputController (MonoBehaviour, raw keys/mouse)
  │ OnAbilitySelected(index)      — hotkey pressed
  │ OnMovementDirectionChanged    — mouse moved while aiming (movement OR ability mode)
  │ OnAbilityConfirmed            — hotkey released (unless aborted)
  │ OnAbilityCancelled            — right-click while holding
  │ OnVolleyAimStarted            — Enter pressed (hold-to-aim begins)
  │ OnVolleyAimCancelled          — right-click while holding Enter (abort)
  │ OnExecuteQueueRequested       — Enter released (fires the queue, unless aborted)
  ▼
AbilityInputHandler (pure C#)
  │ converts world direction → HexDirection (DirectionToHexConverter.GetHexDirection)
  │ while volley-aim active, mouse → UpdateVolleyAim (turns the unit, re-points the whole queue)
  ▼
CombatAbilityPresenter (pure C#)
  SelectAbility(i)        highlight immediately: Ring cells, or the Line along the
                          unit's CURRENT facing (a facing always exists)
  UpdateAimDirection(d)   Line: submit a free ChangeDirectionAction (deduped — only when
                          the facing actually changes), re-highlight from the live unit's
                          new facing; Ring: ignore
  ConfirmAim()            submit ScheduleAbilityAction (no direction payload)
  ExecuteQueue()          submit ExecuteAbilityQueueAction (Enter-release path)
  BeginVolleyAim()        highlight the WHOLE queued volley along the current facing (Enter held)
  UpdateVolleyAim(d)      turn the unit (free ChangeDirectionAction, no ability selected) and
                          re-highlight the whole queue from the new facing
  EndVolleyAim()          clear the volley highlight (on execute or right-click cancel)
  CancelAbilitySelection()clear highlights and state
```

The presenter reads the **live** unit snapshot from `ICombatController.CombatState` for
position/facing (the injected `IUnit` reference is the initial snapshot). The unit's model
visually tracks its domain facing via `UnitFacingRotator` (a thin MonoBehaviour that slerps
the yaw on `OnStateChanged`), so facing is legible on the model.

Cancellation is tracked at the source: `PCInputController` sets an abort flag on right-click, so the subsequent key release emits nothing. `MobileInputController` and `JoystickInputController` declare the `OnAbilityConfirmed` event but never raise it — the hold-release flow is currently PC-only.

### 2.5 Scheduling, validation, execution

Actions flow through `ICombatController` → `ActionValidator` → `ActionExecutor`.

- **Validator gates** (all actions): the round must be in `RoundPhase.PlayerAct`; the unit must not be dead or stunned; a **turn-ending** action is rejected once the unit has acted, while **free** actions (`ChangeDirectionAction`, `ReorderAbilitiesAction`) stay legal — that is what makes turning free and unlimited (R6).
- **`ScheduleAbilityAction`** — validated against: ability exists on unit, not on cooldown, queue not full (R18–R19). Executed by appending a `ScheduledAbility` to the unit's `AbilityQueue` with `ExecutionOrder = queue.Count`. Carries an optional `FacingToSet` (used by the AI to turn-and-schedule in one action; `null` for the player). Ends the turn.
- **`ExecuteAbilityQueueAction`** — rejected if the queue is empty. Executes each `ScheduledAbility` in `ExecutionOrder` via `AbilityExecutor`, then resets cooldowns of every ability that was in the queue, then clears the queue. Ends the turn.
- **`ChangeDirectionAction`** — free; sets `Unit.FacingDirection` (always one of six valid values by construction).
- **`ReorderAbilitiesAction`** — supported by validator and executor. No UI currently drives it. (`RetargetAbilityAction` was **removed** with per-ability directions.)

**`AbilityExecutor.ExecuteAbility`** (per scheduled ability):

1. Read the **live** caster from the state; compute affected cells from its current position + current facing (R11) — this is why turning between scheduling and executing re-points the whole volley.
2. Delegate to `ExecuteAbilityAtCells` (also the committed-intent entry point — one damage/status/trigger/push code path for both): for each cell with an alive unit, apply damage and/or healing via `IDamageSystem`, apply the status effect (with stacking rules, R15).
3. Push displacement (R13a): surviving struck units are pushed away along the line direction, farthest-first, via `DisplacementResolver`.
4. Fire `OnHit` and HP-threshold triggers per affected target; fire `OnAttack` once for the caster of a damage ability (R16).
5. No units in the area → state returned unchanged (the queue execution still resets the cooldown).

Cooldown decrement happens at round start in `CombatController` (all ability instances decremented by 1 each round). Status-effect durations are advanced once per turn by `StatusEffectDurations.Tick`: a duration `> 0` ticks down and is removed at 0; a **negative duration means infinite** and is never decremented or removed (used by part passives).

### 2.6 Part-granted ability set & passives (M1)

The bridge from the modular character to combat lives in `Combat/Integration`:

```
CharacterCombatInitializer (combat start)
  │ reads the player's IModularCharacter.EquippedParts (slotId → partId)
  ▼
IPartAbilityResolver.Resolve(partIds)            (PartAbilityResolver, pure C#)
  │ looks each part up in IPartCatalog, collects PartDefinition.ActiveAbilities
  │ + PassiveAbilities, deduped by asset reference
  ▼
PartAbilitySet { ActiveAbilities, PassiveAbilities }
  │ active  → AbilityFactory.CreateAbilityInstance  → Unit.Abilities
  │ passive → StatusEffectFactory.CreateStatusEffect(modifier, duration: -1) → Unit.StatusEffects
  ▼
CharacterCombatComponent.InitializeForCombat(..., abilities, passiveEffects)
```

Because the set is resolved from the **live** equipped parts each time combat starts, an
overworld mutation swap automatically changes the next combat's ability set — the mutation
system does not push anything into combat (pull-at-init).

> **Layering note (debt):** `PartDefinition` lives in `CharacterSystem.Data` yet references
> `Combat.Data.Definitions` ability SOs. This is a deliberate, user-approved M1 coupling that
> violates the inward-only layering rule (CLAUDE.md §2); it is tracked in the ROADMAP. The M2
> part-driven-affinity rework revisited where this data lives and **kept the ability references on
> `PartDefinition`** (alongside the new mutation affinity/rarity) by user decision — single-asset
> authoring over strict layering — so this debt stands rather than being relocated.

---

## 3. Data-driven authoring (no code)

### 3.1 Definition assets

Base: `AbilityDefinition` (`Data/Definitions/AbilityDefinition.cs`) — data only, no logic. Fields:

| Field | Meaning |
|---|---|
| `_id`, `_name`, `_description` | Identity. `_id` must be unique among abilities a unit owns (lookup is by id). |
| `_shape` | `Line` or `Ring` |
| `_lineLength` | Line only, min 1 |
| `_ringRadius` | Ring only, min 1 |
| `_cooldownDuration` | Turns of cooldown after queue execution |
| `_effectType` | Informational classification (`AbilityEffectType`) |
| `_pushDistance` | Cells a struck unit is pushed away from the caster along the line direction; `0` = no push. **Line abilities only** (a Ring has no line direction to push along). Consumed by `DamageAbilityDefinition` / `HybridAbilityDefinition`. |
| `_icon`, `_animationTrigger` | Visuals. `_icon` also drives the overhead plan icons (see `combat-round-and-telegraph.md`); `_animationTrigger` is not consumed yet. |

Subclasses add the effect payload (each has its own `CreateAssetMenu` entry under **Create → Combat → Abilities**):

| Asset type | Menu entry | Extra fields | Runtime class |
|---|---|---|---|
| `DamageAbilityDefinition` | Damage Ability | `_damage` | `DataDrivenDamageAbility` |
| `HealAbilityDefinition` | Heal Ability | `_healAmount` | `DataDrivenHealAbility` |
| `StatusEffectAbilityDefinition` | Status Effect Ability | `_statusEffect` (a `StatusEffectDefinition` — field reference in `combat-status-effects.md` §3), `_durationOverride` (−1 = effect's default; the override reaches the applied instance) | `DataDrivenStatusEffectAbility` |
| `HybridAbilityDefinition` | Hybrid Ability | `_damage` + status effect fields | `DataDrivenHybridAbility` |
| `AbilityDefinition` (base) | Base Ability | — | `Ability` (no effect payload — not useful in play; exists as fallback) |

`AbilityFactory` (`Data/Factories/AbilityFactory.cs`) pattern-matches the definition type and constructs the runtime ability, building `AbilityShapeData` from the shape fields. A `StatusEffectAbilityDefinition`/`HybridAbilityDefinition` with no status effect assigned logs a warning and degrades gracefully (base/damage-only ability).

**Passive abilities** are a separate asset type (they are not aimed/queued):

| Asset type | Menu entry | Fields | Applied as |
|---|---|---|---|
| `PassiveAbilityDefinition` | Combat → Abilities → Passive Ability | `_id`, `_name`, `_description`, `_modifier` (a `StatusEffectDefinition` of any kind), `_icon` | The referenced status effect, applied to the unit at combat start with infinite duration (its authored duration is ignored) — the duration-less case of the one modifier model. |

### 3.2 Steps to add a new ability

1. **Create the asset**: right-click in the Project window → **Create → Combat → Abilities → (Damage | Heal | Status Effect | Hybrid) Ability**. Existing examples live in `Assets/__Project/Resources/Abilities/Data/` (`TestLineAbility.asset`, `TestRingAbility.asset`, `TestPushAbility.asset` — a Line that damages and shoves).
2. **Fill in fields**: unique id, name, shape (+ line length or ring radius), cooldown, effect payload, and (for a push ability) `_pushDistance > 0`. Keep the asset under `Resources/Abilities/Data/` — the plan-icon catalog indexes that folder by id.
3. **Grant it to a unit**: add the asset to the `Abilities` list on a `HeroDefinition` or `EnemyDefinition` asset.
4. (If using the global provider) add it to the `Ability Definitions` list on the `AreaInstaller` component in the Area scene — this feeds `IAbilityDataProvider`.
5. Hotkey mapping for the player is **positional**: ability at index *i* in the hero's list binds to `InputConfig.abilityKeys[i]` (Q/W/E/R/T/Y by default).

No script changes, no installer changes (beyond the inspector list), no recompile of logic.

### 3.3 Steps to add a passive ability

1. **Create (or reuse) a status effect**: **Create → Combat → Status Effects → Status Effect**; for a stat-modifier set `Type` to `Buff`/`Debuff`, pick the `Stat Target`, and author the **flat signed `Magnitude`** (e.g. `+5` outgoing for Empowered, `-5` incoming for Hardened — the sign is authored, never derived). Any other kind works too (a HoT passive = permanent regen). Full field reference: `combat-status-effects.md` §3.
2. **Create the passive**: **Create → Combat → Abilities → Passive Ability**; assign the status effect to `Modifier`. (The status effect's authored duration is ignored — passives last the whole combat.)

### 3.4 Steps to grant abilities via a body part

1. Open a `PartDefinition` asset (**Character System → Part**).
2. Add active ability assets to **Active Abilities** and/or passive ability assets to **Passive Abilities**.
3. Equip the part on the character (via the mutation choice or the assembly's default parts). On the next combat the unit's ability set is rebuilt from its equipped parts — no `HeroDefinition` edit needed.

---

## 4. Extension guide (code changes)

### 4.1 New effect kind (e.g. a shield/buff with its own payload)

1. **Marker interface** in `Core/` (e.g. `IShieldAbility { int ShieldAmount { get; } }`) — follows `IDamageAbility` precedent.
2. **Runtime class** in `Core/DataDrivenAbilities.cs` pattern: subclass `Ability`, implement the marker interface, take the payload through the constructor.
3. **Definition subclass** in `Data/Definitions/`: extend `AbilityDefinition`, add serialized payload fields + `CreateAssetMenu`. Data only — no logic.
4. **Factory case** in `AbilityFactory.CreateAbility`: add the pattern-match arm constructing the runtime class via `BuildShape(def)`.
5. **Executor handling** in `AbilityExecutor.ExecuteAbility`: add the per-target application inside the affected-cells loop (and trigger processing if the new effect should fire triggers).
6. Optionally extend `AbilityEffectType` and `AbilityPreview.BuildEffectsDescription` for UI text.

Validation, scheduling, queueing, cooldowns, input, and highlighting need **no changes** — they are shape/queue concerns, independent of effect kind.

### 4.2 New shape (if a third shape is ever needed)

1. Add the value to `AbilityShapeType` and a factory/fields on `AbilityShapeData`.
2. Add the computation branch in `AbilityShapeCalculator.GetAffectedCells` — this single change covers both preview and execution (R13).
3. Decide whether the shape is directional: if yes, mirror the Line branches in `AbilityExecutor` (facing → direction), `CombatAbilityPresenter` (`SelectAbility`/`UpdateAimDirection`), and the AI facing enumeration (`AIFacings.For` for the simulation AI, `SimpleRandomAI.GetFacingsForAbility`); if no, mirror the Ring branches.
4. Add shape fields to `AbilityDefinition` + `AbilityFactory.BuildShape`.
5. Add edit-mode tests in `AbilityShapeCalculatorTests`.

### 4.3 AI

AI decision makers emit the same `ScheduleAbilityAction`, with `FacingToSet` carrying the chosen facing (Line → one candidate per hex direction (6), Ring → `null`). Under the phase round the AI **decides at round start** — its decision becomes a committed, revealed `EnemyIntent` that resolves verbatim (see `combat-round-and-telegraph.md`). Since P2-4 the tactical scoring is **area-simulation-based**: candidates are evaluated through `IAbilityOutcomeCalculator` against the units actually standing in the affected cells (see `combat-enemy-ai.md`). Decision makers are seeded per enemy from the run seed (`LootSeed.Derive(runSeed, "combat-ai:{enemyId}")`), so same seed → same plans.

---

## 5. Known limitations / open points

These describe current behavior honestly; they are not requirements.

- **Cooldown starts at queue execution, not at scheduling.** An ability scheduled but not yet executed is still "available", so it can be scheduled again on a following turn before the queue runs. Pre-existing behavior, accepted for now.
- **A queued ability that hits nothing still consumes its cooldown** when the queue executes (cells recomputed at execution may contain no units).
- **Hold-release targeting is PC-only.** `MobileInputController` and `JoystickInputController` compile against the new events but never raise `OnAbilityConfirmed`; mobile/joystick ability confirmation is currently non-functional.
- **Queue reorder exists in the domain but has no UI.** (Retargeting is gone by design — the queue has one facing.)
- **Push is Line-only** (R13a): a Ring ability with `_pushDistance > 0` ignores it — there is no radial-push semantics yet. (ROADMAP)
- **Per-ability aiming is intentionally removed** (PO brief `combat-hero-facing.md`): a turn's volley is single-direction. "Turn as a queued step" (multi-directional volleys) is a deferred escape hatch, to revisit only if playtest shows a turn feels too constrained.
- **`MaxAbilityQueueSize` (3) and other `CombatConfig` values are hardcoded** in `AreaInstaller.InstallCombatConfigurations` rather than asset-driven.
- **Friendly fire is by design** (R12): a heal Line pointed at an enemy heals the enemy; a damage Ring hits adjacent allies. There is no ownership filtering anywhere in execution.
- **Stat targets cover outgoing/incoming damage only** (R26); a max-HP target is not wired (regen is modeled as HoT, not a stat). (ROADMAP)
- **`PartDefinition` references the Combat ability layer** — a deliberate M1 coupling that violates the inward-only layering rule (CLAUDE.md §2); the M2 part-driven-affinity rework kept it on `PartDefinition` by user decision, so it remains tracked in the ROADMAP.
- **`HeroDefinition.Abilities` is a temporary fallback**: used only when no equipped part grants an active ability (so the demo still runs before part-grants are authored). The long-term source of truth is the equipped parts.
