# Character Locomotion — Requirements & Design

> Movement-driven animation and facing for the modular character: when the Hero moves it blends
> from idle into a run clip and yaws to face its travel direction. Sits between the movement
> controller (input) and the assembled rig (visual).
> Status: current as of 2026-06-13.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** While the character's planar speed exceeds a threshold it plays a **run** animation,
  blended from idle by a `Speed` float (0 = idle, 1 = full run) normalized against the mover's max
  speed. Below the threshold `Speed` collapses toward 0 (idle).
- **R2** The character **yaws to face its movement direction**, turning at a configured rate
  (degrees/sec) along the shortest arc — not an instant snap.
- **R3** When the character is **not moving** it keeps its current heading (no snap) and the
  locomotion view leaves rotation alone, so forced facing (turn-to-camera via `ICharacterFacing`)
  is not fought while idle.
- **R4** Facing follows the **actual world-space velocity**. Movement maps input directly to world
  axes (`input.x→+X`, `input.y→+Z`), so this is correct under the isometric camera with no
  camera-relative remapping.
- **R5** The run blend is just an animator parameter — the Animator is **never rebound**, so part
  swaps stay seamless mid-run (see `character-system.md` R3).

### 1.2 Non-functional requirements

- **N1** Locomotion logic (`LocomotionSolver`) is pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject; no service locators.
- **N3** Tuning is a data-only `LocomotionConfig` ScriptableObject.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Character/Locomotion/
  LocomotionSolver, LocomotionState   — pure C#, no UnityEngine (Core)
  LocomotionAnimatorParameters        — shared parameter-name constant
  ICharacterVelocityProvider          — input/source contract
  ILocomotionView                     — output contract
  CharacterLocomotionView             — thin MonoBehaviour adapter (View)
  CharacterLocomotionPresenter        — ITickable glue (Presenter)
  LocomotionConfig                    — data-only ScriptableObject (Data)
Scripts/Core/DI/CharacterLocomotionInstaller.cs
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `LocomotionState` | Immutable result: `NormalizedSpeed` (0..1), `YawDegrees` (0 = +Z), `IsMoving`. |
| `LocomotionSolver` | Pure logic: normalizes planar speed, applies the move threshold, and steps a retained yaw toward `Atan2(vx, vz)` capped by `turnDegreesPerSecond * dt` along the shortest (wrap-safe) arc. Holds only the current yaw. |

### 2.3 Runtime flow

Each frame `CharacterLocomotionPresenter.Tick` reads `ICharacterVelocityProvider.PlanarVelocity`
and `MaxPlanarSpeed`, calls `LocomotionSolver.Evaluate(vx, vz, maxSpeed, Time.deltaTime)`, then
pushes the result to the view: `SetMotionSpeed(NormalizedSpeed)` always, and `SetFacingYaw(YawDegrees)`
only while `IsMoving`. `CharacterMovementController` is the provider (exposes the per-frame movement
intent `moveDir * moveSpeed`). `CharacterLocomotionView` (on the Hero root) lazily caches the
assembled rig's `Animator` from `ModularCharacterVisual.Animator`, writes the `Speed` parameter, and
yaws the host root (the parented rig inherits the rotation).

### 2.4 DI wiring

`CharacterLocomotionInstaller` (added to the scene's SceneContext): resolves `ICharacterVelocityProvider`
and `ILocomotionView` `FromComponentInHierarchy`, builds `LocomotionSolver` from `LocomotionConfig`
(serialized field, else `Resources/CharacterSystem/LocomotionConfig`, else defaults), and registers
`CharacterLocomotionPresenter` via `BindInterfacesAndSelfTo(...).AsSingle().NonLazy()` so Zenject
ticks it.

The player is the existing `Hero.prefab` instance already in `Area.unity` — it is the Cinemachine
cameras' `TrackingTarget` and `AreaSceneEntrypoint.characterTransform`, and the prefab carries
`CharacterController` + `CharacterMovementController`, `CharacterFacingController`, the
`BellyCameraAnchor`/`BellyAnchorMarker`, and `ModularCharacterVisual`. Three editor commands enable
locomotion without disturbing that wiring: `Tools/Character System/Attach Modular Visual To Hero`
bakes the modular model **and** the `CharacterLocomotionView` onto `Hero.prefab` (the scene instance
inherits both, so the camera/entrypoint references stay valid); `Tools/Character System/Rebuild
Locomotion Controller` builds the run clip + 1D blend-tree controller (`Speed`) and assigns it onto
the existing `PlaceholderRig.prefab` via a `LoadPrefabContents`/`SaveAsPrefabAsset` round-trip that
preserves the rig's root-GameObject fileID (so `SkeletonDefinition._rigPrefab` keeps resolving — a
full asset regeneration would churn that fileID and break the link); and `Tools/Character System/Setup
Locomotion In Open Scene` registers `CharacterSystemInstaller` + `CharacterLocomotionInstaller` (and
the `LocomotionConfig`) on the SceneContext. None of them create, delete, or move a Hero. The
`CharacterLocomotionView` writes `Speed` only when the active controller exposes that float parameter,
so a rig still on an idle-only controller degrades to facing-only without log spam.

---

## 3. ScriptableObject Reference

### `LocomotionConfig`  (asset menu: `Create → Character System → Locomotion Config`)

Loaded from `Resources/CharacterSystem/LocomotionConfig` (or wired into `CharacterLocomotionInstaller._config`).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `MoveThresholdSpeed` | float | Planar speed (units/sec) below which the character is idle: no run blend, no facing update. | `0.1` |
| `TurnDegreesPerSecond` | float | How fast the character yaws toward its movement direction. | `720` (half a turn / 0.25 s) |

The max speed used to normalize the blend is **not** in this config — it comes from the mover
(`ICharacterVelocityProvider.MaxPlanarSpeed`, i.e. `CharacterMovementController.moveSpeed`).

---

## 4. Adding Content

### Tune locomotion feel

1. Select the `LocomotionConfig` asset (or create one via `Create → Character System → Locomotion Config`
   under `Resources/CharacterSystem/`).
2. Adjust `MoveThresholdSpeed` (idle deadzone) and `TurnDegreesPerSecond` (snappier vs. floaty turns).
3. Enter Play and move — no code or recompile needed.

### Change the run gait (placeholder art)

The run/idle clips and the blend-tree controller are code-authored. Edit
`PlaceholderAnimationBuilder.BuildRunClip` (bone curves) and re-run
`Tools/Character System/Generate Placeholder Assets`. The controller is a 1D blend tree on `Speed`
(idle @ 0, run @ 1) named `PlaceholderLocomotion.controller`.

### Retarget to real art

Replace the rig prefab's controller with one exposing a float `Speed` parameter feeding an
idle↔run blend (see `character-system.md` §3.3 for skeleton/part import). No runtime changes are
needed as long as the parameter name matches `LocomotionAnimatorParameters.Speed`.

---

## 5. Tests

Edit-mode suite in `Assets/__Project/Tests/EditMode/`:

- `LocomotionSolverTests` — speed normalization + clamp, move threshold, cardinal-direction yaw
  (+Z→0, +X→90, −X→−90, −Z→±180), rate-limited shortest-arc turning, wrap-around across ±180,
  and idle holding the previous heading.

Unity-side classes (`CharacterLocomotionView`, `CharacterLocomotionPresenter`, installer) are
verified manually via `Tools/Character System/Place Moving Hero In Scene` and Play.

---

## 6. Known limitations / open points

- The run clip is a procedural placeholder; there is no walk tier (the blend is idle↔run only).
- `Speed` is the raw normalized speed with no acceleration smoothing, so it tracks input
  instantly; add damping (e.g. `SetFloat` damp time) if the blend looks abrupt.
- `MaxPlanarSpeed` reflects movement **intent** (`moveDir * moveSpeed`), not actual displacement,
  so the character also "runs in place" when pushing into a wall.
- While idle the solver retains its last movement yaw and does not resync from forced facing, so a
  turn-to-camera pose is not preserved as the new heading once movement resumes.
- The Hero is composed by an editor menu, not shipped as a prefab.
