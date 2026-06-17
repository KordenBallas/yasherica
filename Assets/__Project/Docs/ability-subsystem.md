# Ability Subsystem — Requirements & Design

Part of the **Combat System**. This document describes the ability subsystem as implemented: what abilities are, how they are targeted, scheduled, validated, and executed, and how to add new abilities without writing code.

Status: current as of 2026-06-10.

---

## 1. Requirements

### 1.1 Functional requirements

**Shapes**

- R1. Exactly two ability shapes exist: **Line** and **Ring**.
- R2. **Line**: a straight row of cells, width 1, configurable length (min 1). The first cell is adjacent to the caster in the chosen direction; the line continues in that direction. Cells outside the battlefield truncate the line (it stops at the first out-of-bounds cell).
- R3. **Ring**: a hollow ring of cells at *exactly* the configured radius (min 1, default 1) around the caster. The caster's cell and all inner cells are excluded. Out-of-bounds cells are filtered out. A Ring has no direction.

**Targeting & input (PC)**

- R4. An ability is aimed with a hold-release interaction: **press and hold** the ability hotkey (Q/W/E/R/T/Y, configurable in `InputConfig`), **aim** with the mouse (the battlefield highlights all affected cells live), **release** the key to confirm.
- R5. While a Line ability is held, the highlighted cells follow the mouse direction across all 6 hex directions. A Ring ability highlights its cells immediately on press; mouse direction is ignored.
- R6. **Right-click** while holding cancels the aim; releasing the key afterwards does nothing. Releasing a Line ability key with no valid direction chosen does nothing (no action submitted).
- R7. Confirming an ability does **not** execute it immediately — it is added to the unit's **execution queue**. Scheduling an ability ends the unit's turn.
- R8. Pressing **Enter** submits the execution queue for the player's unit. Queue execution is itself a turn-ending action.
- R9. Movement uses the same interaction pattern: hold **M**, aim with the mouse, release M to confirm. Movement executes immediately and ends the turn. (Movement details belong to the combat system document; listed here because the UX is shared.)

**Execution semantics**

- R10. Queued abilities execute **one by one** in their scheduled order (`ExecutionOrder`).
- R11. Affected cells are **recomputed at execution time** from the caster's *current* position, using the direction saved at scheduling time (Line) or no direction (Ring). The cells highlighted during aiming are a preview, not a frozen target set.
- R12. An ability affects **all alive units** standing in its affected cells — enemies and allies alike. There is no target filtering by ownership.
- R13. The preview (highlight) and the execution must use the **same cell calculation code path** so they can never disagree.

**Effects**

- R14. An ability applies one of the implemented effect kinds to each unit in the area: damage, healing, a status effect, or damage + status effect (hybrid).
- R15. Status effect application respects stacking: a stackable effect already present on the target gains a stack; a non-stackable effect already present is not re-applied.
- R16. Status effect triggers fire during execution: `OnHit` per damaged target (if still alive), HP-threshold triggers per target whose HP changed, and `OnAttack` once per execution for the caster of a damage ability.

**Cooldowns & queue rules**

- R17. Each ability has a cooldown in turns. Cooldowns are tracked per unit per ability (`AbilityInstance`), decremented at the start of each round, and **reset when the queue executes** (not when the ability is scheduled).
- R18. An ability on cooldown cannot be scheduled.
- R19. The queue has a maximum size (`CombatConfig.MaxAbilityQueueSize`, currently 3, bound in `AreaInstaller`); scheduling beyond it is rejected.
- R20. A Line ability cannot be scheduled without a direction; a Ring ability needs no direction. These are the only target validations.

**Data-driven authoring**

- R21. New abilities are defined as **ScriptableObject assets** — no code changes required for a new ability of an existing effect kind.
- R22. ScriptableObject definitions contain **data only**; all runtime logic lives in pure C# classes created by a factory.

**Part-granted abilities & passives (M1)**

- R23. A body part declares the combat abilities equipping it grants: **active** abilities (usable in combat) and **passive** abilities (standing modifiers). Authored directly on `PartDefinition` (`_activeAbilities`, `_passiveAbilities`).
- R24. The player unit's combat ability set is composed from its **currently equipped parts** at combat start, deduplicated. Equipped parts are the source of truth, so mutating the body changes the next combat's abilities. `HeroDefinition.Abilities` is a temporary fallback used only when no equipped part grants an active ability.
- R25. A **passive** ability is never queued or aimed: it references a Buff/Debuff `StatusEffectDefinition` that is applied to the unit as a standing modifier for the **whole combat** (infinite duration), and removed when combat ends.
- R26. A standing Buff/Debuff modifier affects **outgoing damage**: `IDamageSystem.CalculateFinalDamage` scales an ability's base damage by the attacker's net `StatModifier` (sum over Buff/Debuff effects × stack count). Other stat targets (max-HP, defence) are not wired yet.

### 1.2 Non-functional requirements

- N1. Cell calculation, validation, and execution are pure C# and unit-testable without Unity play mode (edit-mode tests exist for the shape calculator).
- N2. Domain state is immutable: executing an ability returns a new combat state; units are updated via `With*` copy methods.
- N3. All dependencies are wired through Zenject in `AreaInstaller`; no service locators.

---

## 2. Architecture

### 2.1 Layer map

| Layer | Responsibility | Key files (under `Assets/__Project/Scripts/Combat/`) |
|---|---|---|
| Domain (pure C#) | Ability model, shapes, cell math, queue entries | `Core/IAbility.cs`, `Core/Ability.cs`, `Core/AbilityShapeType.cs`, `Core/AbilityShapeData.cs`, `Core/AbilityTarget.cs`, `Core/AbilityInstance.cs`, `Core/ScheduledAbility.cs`, `Core/AbilityShapeCalculator.cs`, `Core/DataDrivenAbilities.cs` |
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
- **`IAbility` / `Ability`** — identity (`Id`, `Name`), `CooldownDuration`, `Shape`, `EffectType`. Effect payloads live on marker interfaces implemented by subclasses: `IDamageAbility.Damage`, `IHealAbility.HealAmount`, `IStatusEffectAbility.EffectToApply` + `EffectDuration`.
- **`AbilityTarget`** — immutable struct holding only `HexDirection? Direction`. `AbilityTarget.ForDirection(d)` for Line, `AbilityTarget.None()` for Ring. This is the entire saved targeting state — there are no saved cells or unit IDs (R11).
- **`AbilityInstance`** — pairs an `IAbility` with `CurrentCooldown`; immutable (`DecrementCooldown()` / `ResetCooldown()` return new instances).
- **`ScheduledAbility`** — queue entry: `(IAbilityInstance, AbilityTarget, ExecutionOrder)`.

### 2.3 Shape calculation

`IAbilityShapeCalculator.GetAffectedCells(shape, casterPosition, direction, isCellInBoundary)` (`Core/AbilityShapeCalculator.cs`):

- **Line**: looks up the axial offset for the direction in `HexDirectionConfig`, steps from the caster `LineLength` times, stops at the first cell failing the boundary predicate. Returns empty if `direction` is null.
- **Ring**: iterates the axial disc of radius `RingRadius`, keeps only cells at exact cube distance `RingRadius` (hollow — excludes center and inner cells), filters by the boundary predicate.

Callers pass the boundary predicate: the presenter uses battlefield validity for highlighting; the executor passes `ICombatState.IsPositionValid`.

Tests: `Assets/__Project/Tests/EditMode/AbilityShapeCalculatorTests.cs` (line per direction, ordering, boundary truncation, hollow-ring counts and exclusions).

### 2.4 Targeting flow (PC)

```
PCInputController (MonoBehaviour, raw keys/mouse)
  │ OnAbilitySelected(index)      — hotkey pressed
  │ OnMovementDirectionChanged    — mouse moved while aiming (movement OR ability mode)
  │ OnAbilityConfirmed            — hotkey released (unless aborted)
  │ OnAbilityCancelled            — right-click while holding
  │ OnExecuteQueueRequested       — Enter
  ▼
AbilityInputHandler (pure C#)
  │ converts world direction → HexDirection (DirectionToHexConverter.GetHexDirection)
  ▼
CombatAbilityPresenter (pure C#)
  SelectAbility(i)        Ring: highlight cells immediately; Line: wait for direction
  UpdateAimDirection(d)   Line: recompute + highlight from caster position; Ring: ignore
  ConfirmAim()            build AbilityTarget, submit ScheduleAbilityAction; Line with
                          no direction → silent cancel (R6)
  ExecuteQueue()          submit ExecuteAbilityQueueAction (Enter path)
  CancelAbilitySelection()clear highlights and state
```

Cancellation is tracked at the source: `PCInputController` sets an abort flag on right-click, so the subsequent key release emits nothing. `MobileInputController` and `JoystickInputController` declare the `OnAbilityConfirmed` event but never raise it — the hold-release flow is currently PC-only.

### 2.5 Scheduling, validation, execution

Actions flow through `ICombatController` → `ActionValidator` → `ActionExecutor`.

- **`ScheduleAbilityAction`** — validated against: ability exists on unit, not on cooldown, queue not full, Line has a direction (R18–R20). Executed by appending a `ScheduledAbility` to the unit's `AbilityQueue` with `ExecutionOrder = queue.Count`. Ends the turn.
- **`ExecuteAbilityQueueAction`** — rejected if the queue is empty. Executes each `ScheduledAbility` in `ExecutionOrder` via `AbilityExecutor`, then resets cooldowns of every ability that was in the queue, then clears the queue. Ends the turn.
- **`ReorderAbilitiesAction` / `RetargetAbilityAction`** — supported by validator and executor (reorder the queue / replace a queue entry's `AbilityTarget`). No UI currently drives them.

**`AbilityExecutor.ExecuteAbility`** (per scheduled ability):

1. Compute affected cells from the caster's **current** position + saved direction (R11).
2. For each cell with an alive unit: apply damage and/or healing via `IDamageSystem`, apply the status effect (with stacking rules, R15).
3. Fire `OnHit` and HP-threshold triggers per affected target; fire `OnAttack` once for the caster of a damage ability (R16).
4. No units in the area → state returned unchanged (the queue execution still resets the cooldown).

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
| `_icon`, `_animationTrigger` | Visuals |

Subclasses add the effect payload (each has its own `CreateAssetMenu` entry under **Create → Combat → Abilities**):

| Asset type | Menu entry | Extra fields | Runtime class |
|---|---|---|---|
| `DamageAbilityDefinition` | Damage Ability | `_damage` | `DataDrivenDamageAbility` |
| `HealAbilityDefinition` | Heal Ability | `_healAmount` | `DataDrivenHealAbility` |
| `StatusEffectAbilityDefinition` | Status Effect Ability | `_statusEffect` (a `StatusEffectDefinition`), `_durationOverride` (−1 = effect's default) | `DataDrivenStatusEffectAbility` |
| `HybridAbilityDefinition` | Hybrid Ability | `_damage` + status effect fields | `DataDrivenHybridAbility` |
| `AbilityDefinition` (base) | Base Ability | — | `Ability` (no effect payload — not useful in play; exists as fallback) |

`AbilityFactory` (`Data/Factories/AbilityFactory.cs`) pattern-matches the definition type and constructs the runtime ability, building `AbilityShapeData` from the shape fields. A `StatusEffectAbilityDefinition`/`HybridAbilityDefinition` with no status effect assigned logs a warning and degrades gracefully (base/damage-only ability).

**Passive abilities** are a separate asset type (they are not aimed/queued):

| Asset type | Menu entry | Fields | Applied as |
|---|---|---|---|
| `PassiveAbilityDefinition` | Combat → Abilities → Passive Ability | `_id`, `_name`, `_description`, `_modifier` (a Buff/Debuff `StatusEffectDefinition`), `_icon` | The referenced status effect, applied to the unit at combat start with infinite duration (its authored duration is ignored). |

### 3.2 Steps to add a new ability

1. **Create the asset**: right-click in the Project window → **Create → Combat → Abilities → (Damage | Heal | Status Effect | Hybrid) Ability**. Existing examples live in `Assets/__Project/Resources/Abilities/Data/` (`TestLineAbility.asset`, `TestRingAbility.asset`).
2. **Fill in fields**: unique id, name, shape (+ line length or ring radius), cooldown, effect payload.
3. **Grant it to a unit**: add the asset to the `Abilities` list on a `HeroDefinition` or `EnemyDefinition` asset.
4. (If using the global provider) add it to the `Ability Definitions` list on the `AreaInstaller` component in the Area scene — this feeds `IAbilityDataProvider`.
5. Hotkey mapping for the player is **positional**: ability at index *i* in the hero's list binds to `InputConfig.abilityKeys[i]` (Q/W/E/R/T/Y by default).

No script changes, no installer changes (beyond the inspector list), no recompile of logic.

### 3.3 Steps to add a passive ability

1. **Create a Buff/Debuff status effect**: **Create → Combat → Status Effects → Status Effect**; set `Type` to `Buff` or `Debuff` and `Stat Modifier` to the percentage (e.g. `0.2` for +20% outgoing damage; debuffs are negated automatically).
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
3. Decide whether the shape is directional: if yes, mirror the Line branches in `ActionValidator.ValidateAbilityTarget`, `CombatAbilityPresenter` (`SelectAbility`/`UpdateAimDirection`/`ConfirmAim`), and the AI target enumeration (`SimpleRandomAI`, `TacticalAI`, `ConfigurableTacticalAI` — `GetTargetsForAbility`); if no, mirror the Ring branches.
4. Add shape fields to `AbilityDefinition` + `AbilityFactory.BuildShape`.
5. Add edit-mode tests in `AbilityShapeCalculatorTests`.

### 4.3 AI

AI decision makers schedule abilities through the same `ScheduleAbilityAction` path. Target enumeration is shape-driven: Line → one candidate per hex direction (6), Ring → single `AbilityTarget.None()`. Scoring is currently payload-based (damage/heal amounts), not area-simulation-based.

---

## 5. Known limitations / open points

These describe current behavior honestly; they are not requirements.

- **Cooldown starts at queue execution, not at scheduling.** An ability scheduled but not yet executed is still "available", so it can be scheduled again on a following turn before the queue runs. Pre-existing behavior, accepted for now.
- **A queued ability that hits nothing still consumes its cooldown** when the queue executes (cells recomputed at execution may contain no units).
- **Hold-release targeting is PC-only.** `MobileInputController` and `JoystickInputController` compile against the new events but never raise `OnAbilityConfirmed`; mobile/joystick ability confirmation is currently non-functional.
- **Queue reorder/retarget actions exist in the domain but have no UI.**
- **`MaxAbilityQueueSize` (3) and other `CombatConfig` values are hardcoded** in `AreaInstaller.InstallCombatConfigurations` rather than asset-driven.
- **Friendly fire is by design** (R12): a heal Line pointed at an enemy heals the enemy; a damage Ring hits adjacent allies. There is no ownership filtering anywhere in execution.
- **Passive standing modifiers affect only outgoing damage** (R26). `StatModifier` has no stat-target dimension, so max-HP, defence, healing, etc. are not yet modified by passives. (ROADMAP)
- **`PartDefinition` references the Combat ability layer** — a deliberate M1 coupling that violates the inward-only layering rule (CLAUDE.md §2); the M2 part-driven-affinity rework kept it on `PartDefinition` by user decision, so it remains tracked in the ROADMAP.
- **`HeroDefinition.Abilities` is a temporary fallback**: used only when no equipped part grants an active ability (so the demo still runs before part-grants are authored). The long-term source of truth is the equipped parts.
