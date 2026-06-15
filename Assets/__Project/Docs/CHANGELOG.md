# Changelog

All notable changes to the project are recorded here. Format follows
[Keep a Changelog](https://keepachangelog.com/): newest first, grouped Added / Changed / Fixed /
Removed, each entry tagged with the affected system and (where useful) requirement ids.

Every functional change appends an entry **in the same change as the code** (CLAUDE.md §8.2).

## [Unreleased]

### Added
- **Inventory Subsystem:** feeding-mode presentation + one-click setup tool. Feeding now shows **3D
  feeding slots below the pot** (mirroring the crafting slots above) with the cumulative-archetype /
  digestion-progress / dominant-archetype readout and the **Feed** button on a screen-space
  `FeedingReadoutCanvas` anchored to the right of the pot; entering feeding mode **drops the stage
  camera** to reveal the slots below (Inventory R24a). Feeding-tray capacity is configurable
  (`InventoryConfig.FeedingSlotCount`, default 3) and enforced by `FeedingSession`; new
  `InventoryConfig` fields `FeedingSlotCount` / `FeedingCameraDrop` / `FeedingFramingDuration`;
  `InventoryStageView.SetFeedingFraming` performs the camera move, driven by `InventoryPresenter` on
  mode change. New editor tool **Tools → Inventory → Setup Feeding UI**
  (`Scripts/Editor/Inventory/FeedingUISetup.cs`, idempotent) builds the `FeedingArea` (tray anchor +
  readout canvas + wired `FeedingView`) on `InventoryStage.prefab` and the feed-mode toggle on
  `InventoryHud.prefab`, so the prefab setup is a menu click instead of manual authoring. Tests:
  `FeedingSessionTests` gains capacity coverage.
- **Inventory + Mutation Subsystems:** feeding / digestion UI (M1, Inventory R24–R26) — the open
  cauldron now has a **feeding mode** alongside crafting, toggled by a HUD button (`IInventoryModeState`
  arbitrates which mode owns a pot-bubble click; switching modes returns the other mode's staged
  items). In feeding mode, clicking pot bubbles fills a **feeding tray** (`IFeedingSession` /
  `FeedingSession`, pure C#: `TrySelect`/`TryUnselect`/`Consume`/`ReturnAll`), a readout panel shows
  the cumulative archetype weights of the tray plus this-stage digestion progress and the dominant
  archetype(s), and the **Feed** button digests the tray: each artifact maps via
  `ArtifactArchetypeMapper.ToProfile` into `IMutationTally.Add` and advances `IDigestionProgress`.
  New `IDigestionProgress` / `DigestionProgress` (`Mutation.Core`) counts artifacts fed this stage vs.
  the authored `MutationConfig.DigestionThreshold` (`IsReadyToMutate`, `Normalized`, `Reset`);
  `ArtifactArchetypeProfile.Combine` sums several profiles for the cumulative readout. New
  `MutationConfig` SO (*Create → Mutation → Mutation Config*, `Resources/Mutation/MutationConfig.asset`).
  New `FeedingPresenter` and `FeedingView` (+ `ArchetypeReadoutEntry` DTO); `InventoryHudView` gains a
  feed-mode toggle; `InventoryPresenter`/`CraftingPresenter` respect the mode and return their staged
  items on mode switch/close. Wired in `InventoryInstaller` (sessions, mode state, view, presenter)
  and `MutationInstaller` (config + digestion). Tests: `ArtifactArchetypeProfileCombineTests`,
  `DigestionProgressTests`, `FeedingSessionTests`. **Not wired yet:** nothing calls `Reset` on the
  tally/digestion and nothing acts on `IsReadyToMutate` — the stage-up mutation choice (next M1 step)
  drives that; remains on the ROADMAP.
- **Mutation Subsystem:** per-stage mutation tally (M1) — `IMutationTally` / `MutationTally`
  (`Mutation.Core`, pure C#) aggregates the archetype weights of artifacts fed during the current
  **mutation stage**: `Add(ArtifactArchetypeProfile)` sums per archetype, `TotalFor`,
  `Dominant(count)` returns the top-N archetypes (weight desc, ordinal-id tie-break), `OnChanged`
  notifies UI, and `Reset()` starts the next stage. Bound `AsSingle` in `MutationInstaller` (shared
  by the future feeding UI and stage-up mutation choice). The live consumer path is
  `ArtifactArchetypeMapper.ToProfile` → `MutationTally.Add`. Tests: `MutationTallyTests`. Not wired
  yet: nothing calls `Add` (feeding UI) or `Reset` (stage-up choice) — both remain on the ROADMAP.
- **Mutation Subsystem:** new subsystem and its first M1 step — the creature-archetype data surface
  (`mutation-subsystem.md`, Mutation R1–R5). Archetypes are an authorable set of `ArchetypeDefinition`
  ScriptableObjects (*Create → Mutation → Archetype*, auto-loaded from `Resources/Mutation/Archetypes`);
  shipped: reptile, insect, aquatic, mammal, avian. `ArtifactDefinition` gains an `_archetypeWeights`
  array (`ArchetypeWeight` = archetype id + weight) describing how strongly eating an artifact pushes
  the character toward each archetype; the seven shipped artifacts are populated. Pure
  `ArtifactArchetypeProfile` (aggregation: duplicate ids summed, empty/non-positive dropped, ids
  trimmed) with `ArtifactArchetypeMapper` as the SO→Core bridge; `ArchetypeCatalog` (fail-fast on
  empty/duplicate ids); `MutationContentValidator` warns at startup about unknown/empty archetype ids
  on artifacts. New `MutationInstaller` registered on the Area `SceneContext`. Tests:
  `ArtifactArchetypeMapperTests`, `ArchetypeCatalogTests`. The per-stage tally that consumes these
  weights is the follow-up entry above; the feeding UI and mutation choice remain on the ROADMAP.
- **Character Locomotion:** new subsystem driving movement-based animation and facing
  (`character-locomotion.md`). When the Hero moves it blends idle→run via a `Speed` float and yaws
  to face its travel direction along the shortest arc at a configured rate; idle holds the heading
  (Locomotion R1–R5). Pure `LocomotionSolver` (+ `LocomotionSolverTests`), `CharacterLocomotionView`/
  `CharacterLocomotionPresenter`, `LocomotionConfig` SO, and `CharacterLocomotionInstaller`.
  `CharacterMovementController` now exposes `ICharacterVelocityProvider`; `ModularCharacterVisual`
  exposes the assembled rig's `Animator`. Locomotion attaches to the **existing** `Hero.prefab`
  instance in `Area.unity` (already the cameras' `TrackingTarget` and `AreaSceneEntrypoint.characterTransform`):
  `Tools/Character System/Attach Modular Visual To Hero` adds + wires `ModularCharacterVisual` and
  `CharacterLocomotionView` on `Hero.prefab`, and `Tools/Character System/Setup Locomotion In Open
  Scene` registers `CharacterLocomotionInstaller` (+ `LocomotionConfig`) on the SceneContext. Neither
  creates/deletes/moves a Hero, so the camera follow and entry-platform placement stay intact.
- **Docs:** documentation discipline established — `Docs/` is the maintained source of truth, with a
  mandatory per-system template (`_TEMPLATE.md`) requiring a ScriptableObject Reference and an
  Adding Content section. Added `CHANGELOG.md` and `ROADMAP.md`.

### Changed
- **Character System:** placeholder animation tooling now generates a run clip (`PlaceholderRun.anim`)
  alongside the idle clip and a 1D blend-tree controller on a `Speed` parameter
  (`PlaceholderLocomotion.controller`, idle @ 0 / run @ 1) instead of the single-state idle
  controller. `Speed` defaults to 0, so existing characters still idle (Character System §3.1).
- **AI rules:** rewrote `CLAUDE.md` — fixed section numbering, made the AI workflow planning-first
  (plan + doc-impact statement before code), and made data-driven content extension a first-class
  rule.

### Fixed
- **Character Locomotion:** the run animation stopped playing because the assembled rig's Animator
  came up with no controller bound at runtime ("Animator is not playing an AnimatorController"),
  despite the rig prefab referencing a valid controller. `ModularCharacterVisual` now binds the
  controller in code at startup (a serialized `_animatorController`, else the placeholder controller
  from Resources), and `CharacterLocomotionView` skips writing `Speed` until a controller is bound (no
  warning spam).
- **Character Locomotion:** the run gait no longer reads as moving backward. `BuildRunClip` now has a
  `RunGaitSign` (−1) that mirrors the fore/aft swing of the legs/knees/arms/spine so the cycle reads
  forward for the −Z-forward placeholder once it is oriented forward; flip to +1 to reverse. Applied
  via `Rebuild Locomotion Controller` (run-clip content only; no rig regeneration).
- **Character Locomotion:** the character no longer moves backward. The placeholder model faces −Z
  (tail/back on +Z), but the solver aims the rig's +Z at the velocity, so the back led. Fixed by a
  data-driven `ModularCharacterVisual._localRotationEuler` (default `(0,180,0)`) that orients the
  model's front to the host's +Z — correcting both movement facing and forced turn-to-camera facing.
  Runtime-only; no rig regeneration. Real art facing +Z sets it to `(0,0,0)`.
- **Character Locomotion:** the run blend is now applied with a controller-only swap
  (`Tools/Character System/Rebuild Locomotion Controller`) that builds the run clip + blend-tree
  controller and assigns it onto the existing rig prefab via a `LoadPrefabContents`/`SaveAsPrefabAsset`
  round-trip, preserving the rig's root-GameObject fileID that `SkeletonDefinition._rigPrefab`
  references. Previously a full regenerate churned those fileIDs, so the skeleton's rig reference
  resolved to null and `ModularCharacterFactory` aborted ("assembly/skeleton/rig prefab is missing").
  `CharacterLocomotionView` now writes `Speed` only when the controller exposes it.
- **Editor tooling:** SceneContext installer registration used `SerializedObject.FindProperty("_installers")`,
  but the field is `_monoInstallers` (`[FormerlySerializedAs("_installers")]`, which `FindProperty`
  ignores) — it returned null and threw. Now registers through the public `Context.Installers` setter
  (`SceneLocomotionSetup`, `PlaceholderCharacterGenerator`).
- **Character System:** placeholder asset generation no longer wipes every definition to a blank
  shell when generation throws midway. `AssetDatabase.SaveAssets()` ran *after* the `try`, so any
  exception skipped it and left `SerializedObject`-configured definitions (parts, slots, sockets,
  skeleton, assembly) empty on disk — which then crashed `PartCatalog` ("empty id") at scene load.
  `SaveAssets`/`Refresh` and the rig cleanup now run in a `finally`.

### Removed
- _none yet_

<!--
Entry shape to copy:

## [Unreleased]
### Added
- **<System>:** <what changed> (<requirement id, e.g. Loot R7>).
-->
