# Inventory Subsystem — Requirements & Design

The **magic pot inventory**: the beast (main character) carries a magic cauldron, and that cauldron is the inventory. The cauldron is presentation-only — a detached 3D diorama rendered as a screen-space overlay, not an object in the game world. This document describes the subsystem as implemented: the container model, the cauldron/bubble visualization, the combine-above-the-pot crafting flow, the feeding/digestion flow that drives the Mutation subsystem, and how to add new artifacts and recipes without writing code.

Status: current as of 2026-06-15.

---

## 1. Requirements

### 1.1 Functional requirements

**Container**

- R1. The inventory is **container-based, not slot-based**: an unordered multiset of artifact instances. Duplicate artifacts are allowed; there is no capacity limit.
- R2. Every artifact in the world is an **instance** (unique `InstanceId`) of an authored **definition** (`DefinitionId`, e.g. `fire`). The 7 starting artifact types are: bacteria, fire, lizard, rock, snake, virus, water (icons in `Resources/Artifacts/UI/Icons/`).
- R3. Until a loot system exists, the inventory is seeded at startup from `InventoryConfig.StartingInventory` (dev seed). Future integration point: `Platform/Content/LootContent.cs` (stub).

**Opening & presentation**

- R4. A HUD button (bottom-right) opens the inventory. While open, a close button (top-right) and the **UI Cancel action (Escape)** close it.
- R5. The inventory is a **UI layer only**: the cauldron diorama lives on a detached `InventoryStage` parked far below the playfield (world y = −50) on the `InventoryFocus` layer, rendered screen-centered by a dedicated **URP overlay camera** stacked on the main camera. It is never attached to the character and never part of the game world.
- R6. Opening zooms the base camera to the beast's torso: a dedicated Cinemachine camera (priority 30) follows the `BellyCameraAnchor` on the hero root every frame while active, so the framing stays correct as the hero turns; closing returns to whichever camera (isometric or combat) was active before. The character stays visible behind the overlay.
- R7. On open the character **smoothly rotates to face the camera** (yaw-only, smoothstep); on close the original facing is restored.
- R8. The cauldron is a **low-poly cross-section**: a procedurally generated lathe mesh (sphere-slice belly, rim lip, ~200° sweep) whose open front exposes the interior and the wall thickness at the cut planes.
- R9. The liquid **boils continuously**: the surface mesh is displaced by two crossed traveling sine waves, the emissive glow pulses, small bubbles rise from the surface, and a looping **steam** particle plume rises above the liquid while the inventory is open.
- R10. Artifacts float inside **bubbles** on the liquid. The more artifacts, the smaller/closer the bubbles get (phyllotaxis spiral layout, radius shrinks with `1/(1 + falloff·√count)`, min/max radius configurable). Bubbles are also distributed in **depth** (deterministic plastic-ratio offsets along local Z, bounded by the pot's interior half depth), giving parallax and size variation under the perspective stage camera. Bubbles **never overlap on screen**: a second layout pass shrinks the shared radius until the minimum pairwise XY distance clears the bubble diameter plus a perspective safety margin (hard floor: the configured min radius). The bubble shell is rendered translucent (fixed shell alpha; artifact tints color it without making it opaque) so the artifact inside stays visible.
- R11. Bubbles drift slowly around their layout spots: a small-amplitude quasi-circular wander (two incommensurate sine frequencies on local X/Y), out of sync per bubble (phase offset), so the pot reads as calm simmering rather than active bobbing.
- R11a. Bubble and craft-result clicks are routed by an explicit raycast from the stage camera (`StageClickRouter`). Clicks over visible UI are ignored (UI wins), and routing is inert while the stage camera is disabled. Beware fullscreen raycast-target graphics from other subsystems: the dialogue UI's modal dim used to stay active (and invisible to the eye behind other canvases) after dialogues ended, silently eating all stage clicks — `DialogueView` now toggles the dim together with its panel.
- R12. Artifact sprites must read as **3D objects**: each artifact renders as a stack of lit, alpha-clipped quads (`M_ArtifactQuad`) — the rear layers sit slightly behind the front one and darken progressively — plus a slight per-instance yaw. Layer count/spacing/darkening are configurable.
- R13. Player movement input is disabled while the inventory is open and restored on close.

**Crafting**

- R14. Clicking a bubble moves its artifact out of the inventory into a **crafting slot** above the pot.
- R14a. Clicking a **staged artifact returns it to the pot** (unstage). Staged items are clickable at all times while staged.
- R15. When **N** artifacts are staged (`InventoryConfig.ItemsToCombine`, default 2), the session enters an observable **Crafting** state and a **merge animation** plays: the staged bubbles travel from their slots to the result anchor (`MergeDuration`). The combine is resolved against the recipe book only when the animation completes (presenter handshake; the domain itself has no timing).
- R15a. Clicking a merging artifact **during the Crafting state cancels the combine**: the clicked artifact returns to the pot, the remaining selection stays staged, and no recipe is consumed. New pot selections are rejected while Crafting.
- R16. Recipe matching is **order-independent and multiset-exact**: `fire+water` equals `water+fire`; `fire+fire` is distinct from `fire`. Success requires an authored recipe with exactly those inputs.
- R17. **Success**: the staged artifacts are consumed, a bright steam burst plays, and the result artifact **pops in above the pot with an overshoot bounce** (`ResultPopDuration`, curve authored on `CraftingSlotsView`). Clicking the result drops it into the pot (inventory) with a shrink-and-drop animation.
- R18. **Failure** (no matching recipe): a visually distinct (dark) steam burst plays, **only the last selected artifact returns to the pot** — its bubble visibly drops from the merge point into the liquid — and the surviving selections glide back to their slots (`FailReturnDuration`).
- R19. While a result is waiting above the pot, new selections are rejected until it is collected.
- R20. Closing the inventory returns all staged artifacts and any uncollected result to the pot; a merge in progress is aborted without resolving.

**Feeding / digestion**

- R24. The open inventory has two **modes**, toggled by a HUD button: **Crafting** (default, R14–R20) and **Feeding**. A pot-bubble click belongs to whichever mode is active, so a single click is never ambiguous. Switching modes returns any items the other mode had staged to the pot.
- R25. In feeding mode, clicking a bubble moves its artifact into a **feeding tray** above the pot (any number of items, no recipes); clicking a tray item returns it to the pot. A readout panel shows the **cumulative archetype weights** of the current tray selection, the **digestion progress** this stage, and the **dominant archetype(s)** accumulated this stage.
- R26. The **Feed** button digests the tray: each artifact maps via `ArtifactArchetypeMapper.ToProfile` into the Mutation subsystem's per-stage tally (`IMutationTally.Add`) and advances digestion (`IDigestionProgress.AddArtifact`); the digested artifacts are consumed (they already left the inventory when selected). See [Mutation Subsystem](mutation-subsystem.md).

**Combat guard**

- R21. The inventory **cannot be opened during combat** (HUD button disabled); if combat starts while the inventory is open, it force-closes.

**Data-driven authoring**

- R22. Artifacts and recipes are **ScriptableObject assets** — adding either requires no code changes (see §4).
- R23. ScriptableObjects contain **data only**; all logic lives in pure C# classes.

### 1.2 Non-functional requirements

- N1. The domain layer (`Inventory.Core`) has **no UnityEngine references** and is covered by edit-mode tests (`Tests/EditMode/`): container, recipe matching, crafting state machine (incl. the last-item-return failure rule, the deferred Crafting state, and cancel-by-unstage), bubble layout math, bubble drift math, cauldron profile math, liquid wave math.
- N2. MVP: views are thin MonoBehaviour adapters behind interfaces; presenters are pure C# (`IInitializable`/`IDisposable`); the view never touches the model.
- N3. All wiring is in Zenject installers (`InventoryInstaller`, one binding in `AreaInstaller`); no service locators.
- N4. All tunables (combine count, bubble layout/bobbing numbers, animation and camera timings) live in `InventoryConfig` — no magic numbers in code.

---

## 2. Architecture

### 2.1 Layer map

| Layer | Responsibility | Key files |
|---|---|---|
| Domain (pure C#) | Container, crafting state machine, feeding tray, inventory mode, recipe matching, bubble layout / drift / cauldron profile / liquid wave math | `Scripts/Inventory/Core/`: `ArtifactInstance.cs`, `InventoryModel.cs`, `CraftingSession.cs`, `FeedingSession.cs`, `InventoryMode.cs`, `InventoryModeState.cs`, `RecipeBook.cs`, `BubbleLayoutCalculator.cs`, `BubbleDriftCalculator.cs`, `CauldronProfileCalculator.cs`, `LiquidWaveCalculator.cs` |
| Data (ScriptableObjects) | Authoring-time configuration | `Scripts/Inventory/Data/Definitions/`: `ArtifactDefinition.cs`, `RecipeDefinition.cs`, `InventoryConfig.cs`; `Scripts/Inventory/Data/`: `ArtifactCatalog.cs`, `RecipeBookBuilder.cs` |
| Application | Open/close orchestration, crafting flow, feeding flow | `Scripts/Inventory/Presenter/InventoryPresenter.cs`, `CraftingPresenter.cs`, `FeedingPresenter.cs` |
| Infrastructure | HUD, inventory stage, cauldron/liquid meshes, bubbles, steam, puffs, stage click routing, feeding tray/readout | `Scripts/Inventory/View/`: `InventoryHudView.cs`, `InventoryStageView.cs`, `PotView.cs`, `BubbleView.cs`, `StageClickRouter.cs`, `CraftingSlotsView.cs`, `FeedingView.cs`, `CauldronView.cs` + `CauldronMeshBuilder.cs`, `LiquidSurfaceView.cs` + `LiquidSurfaceMeshBuilder.cs` |
| Shared infrastructure | Belly camera + anchor, character facing, movement lock, combat flag, logging | `Scripts/Core/Camera/CameraService.cs` (belly camera follow, output camera), `IBellyAnchorProvider.cs`/`BellyAnchorMarker.cs`, `Scripts/Character/ICharacterFacing.cs`/`CharacterFacingController.cs`, `IMovementInputLock.cs`, `Scripts/Combat/Integration/CombatActivityTracker.cs`, `Scripts/Core/Logging/IGameLogger.cs` |

Dependencies point inward: views raise events → presenters call domain → domain raises events → presenters update views through interfaces (`IInventoryHudView`, `IPotView`, `ICraftingSlotsView`). Presenters hand views plain DTOs (`BubbleViewData`, `ArtifactViewData`) with resolved sprites/tints, so views never see domain objects.

### 2.2 Core domain types

- **`ArtifactInstance`** — immutable: `InstanceId` (unique, generated by `InventoryModel`), `DefinitionId`.
- **`IInventoryModel` / `InventoryModel`** — multiset container: `Add(definitionId)`, `Return(instance)`, `Remove(instanceId)`, `CreateDetachedInstance(definitionId)` (for craft results not yet collected); events `OnItemAdded`/`OnItemRemoved`.
- **`IRecipeBook` / `RecipeBook`** — dictionary keyed by the sorted input-id multiset; `TryMatch(ids, out outputId)`. Built from assets by `RecipeBookBuilder` (skips malformed recipes with warnings).
- **`IInventoryModeState` / `InventoryModeState`** — shared, observable current `InventoryMode` (`Crafting`/`Feeding`); `SetMode`, `OnModeChanged`. Both the crafting and feeding presenters read it so each ignores pot-bubble clicks that belong to the other mode (R24).
- **`IFeedingSession` / `FeedingSession`** — the feeding tray (R25/R26): `TrySelect(instanceId)` moves an inventory item into the tray (removing it from the inventory, like crafting staging), `TryUnselect` returns it, `Consume()` empties the tray and returns the snapshot **without** re-adding it (the caller digests it), `ReturnAll()` returns everything to the inventory; `Tray`, `OnTrayChanged`. No mutation dependency — turning tray items into archetype contributions is the presenter's job.
- **`ICraftingSession` / `CraftingSession`** — state machine `Idle → Selecting → Crafting → ResultReady`; implements R14–R20. Staging the Nth item enters `Crafting` and raises `OnCraftingStarted(stagedItems)`; the combine resolves only when `ResolveCraft()` is called (by the presenter, after the merge animation), and returns false outside `Crafting` so a stale animation callback after a cancel is a no-op. `TryUnstage(instanceId)` returns a staged item to the inventory from `Selecting` or `Crafting` — from `Crafting` it cancels the pending combine (the count drops below N, so it cannot re-trigger). Events: `OnItemStaged`, `OnCraftingStarted`, `OnCraftSucceeded`, `OnCraftFailed(returnedItem)`, `OnItemUnstaged`, `OnResultCollected`, `OnSessionCleared`.
- **`BubbleLayoutCalculator`** — deterministic: `(count, potHalfWidth, potHalfHeight, potHalfDepth, BubbleLayoutSettings)` → `BubblePlacement[]` (3D position incl. plastic-ratio depth offset, radius, bob phase). Two passes: phyllotaxis placement with the falloff radius, then a separation pass that shrinks the radius (and re-places) until no two bubbles overlap in the screen plane.
- **`CauldronProfileCalculator`** — deterministic: `CauldronProfileSettings` (bowl radius/depth, wall thickness, rim width, segments) → `CauldronProfile`: index-paired outer/inner `ProfilePoint` polylines (sphere-slice belly + rim lip), so the mesh builder can cap the cross-section cut planes with simple quad strips.
- **`LiquidWaveCalculator`** — deterministic boil height field: `SampleHeight(x, z, time, LiquidWaveSettings)` = two crossed traveling sine waves, bounded by the configured amplitude.
- **`BubbleDriftCalculator`** — deterministic idle bubble motion: `SampleOffset(time, phase, BubbleDriftSettings, out x, out y)` = per-axis sines with an irrational frequency ratio and a quarter-turn phase offset, so each bubble wanders a slowly precessing quasi-circle (bounded per axis by the amplitude) instead of bobbing on a line.

### 2.3 Presenters

- **`InventoryPresenter`** — owns the open/close flow: combat & camera-transition guards → movement lock → `ICharacterFacing.FaceTowards(ICameraService.OutputCameraPosition)` → `ICameraService.SetBellyAnchor(IBellyAnchorProvider.BellyAnchor)` + `SwitchToBellyCamera` → `IInventoryStageView.SetStageActive(true)` → bubble layout → HUD button swap. Subscribes to inventory events for live re-layout; seeds the starting inventory; closes on combat start (R21). Owns the feed-mode toggle (R24): on open it shows the toggle and forces `Crafting`; `OnFeedModeToggled` flips `IInventoryModeState` between `Crafting`/`Feeding`. Close calls `CraftingSession.ReturnAll()` **and** `FeedingSession.ReturnAll()` (R20), resets the mode to `Crafting`, hides the toggle, deactivates the stage, `SwitchToPreviousCamera`, `RestoreFacing`, and unlocks movement.
- **`FeedingPresenter`** — drives the feeding flow (R25/R26): routes `IPotView.OnBubbleClicked` → `FeedingSession.TrySelect` (only while in `Feeding` mode) and `IFeedingView.OnTrayItemClicked` → `TryUnselect`; on tray change rebuilds the tray view and the cumulative readout (`ArtifactArchetypeProfile.Combine` over each tray item's profile); on `OnFeedClicked` calls `Consume()` then `IMutationTally.Add` + `IDigestionProgress.AddArtifact` per artifact; refreshes the digestion bar and dominant-archetype panel from the tally/digestion `OnChanged` events; shows/hides the feeding UI with the mode and returns the tray when leaving `Feeding`. Resolves archetype display names/tints from `IArchetypeCatalog` into `ArchetypeReadoutEntry` DTOs.
- **`CraftingPresenter`** — routes `IPotView.OnBubbleClicked` → `session.TrySelect` and `ICraftingSlotsView.OnStagedItemClicked` → `session.TryUnstage`; renders staged slots. Ignores pot-bubble clicks unless in `Crafting` mode and returns its staged items when the mode leaves `Crafting` (R24). Merge handshake: `OnCraftingStarted` → `slotsView.PlayMergeAnimation()` → view raises `OnMergeCompleted` → `session.ResolveCraft()`. Success → success puff + result pop-in; failure → fail puff + `PlayCraftFailure(returnedId)` (the returned bubble drops into the liquid and also reappears via the inventory event; survivors glide back to their slots); unstage → staged slots rebuild (which aborts a running merge, so a cancelled craft never resolves); result click → `TryCollectResult` → drop animation.

### 2.4 Zenject wiring

`Scripts/Core/DI/InventoryInstaller.cs` (added to the Area scene's `SceneContext`):

- `InventoryConfig` instance (validated non-null), `IArtifactCatalog`, `IRecipeBook` (definitions auto-load from `Resources/Artifacts/Definitions` and `Resources/Artifacts/Recipes` when the inspector lists are empty).
- `IInventoryModel`, `ICraftingSession` (with `ItemsToCombine`), `IFeedingSession`, `IInventoryModeState`, `BubbleLayoutCalculator`.
- `IInventoryHudView` (scene instance or instantiated from `Resources/Prefabs/UI/InventoryHud.prefab`); `IPotView`/`ICraftingSlotsView`/`IFeedingView`/`IInventoryStageView` (on the InventoryStage scene instance) and `IMovementInputLock`/`ICharacterFacing`/`IBellyAnchorProvider` (on the hero scene instance) via `FromComponentInHierarchy`.
- All three presenters (`InventoryPresenter`, `CraftingPresenter`, `FeedingPresenter`) via `BindInterfacesAndSelfTo` + `NonLazy`.
- `IGameLogger` → `UnityGameLogger` with `IfNotBound` (first logger abstraction in the project; other subsystems may adopt it).
- The feeding flow also resolves `IMutationTally`, `IDigestionProgress`, and `IArchetypeCatalog` from the `MutationInstaller` (same `SceneContext`); see [Mutation Subsystem](mutation-subsystem.md).

`AreaInstaller` additionally binds `CombatActivityTracker` (`BindInterfacesAndSelfTo`); `Platform/States/CombatActiveState.cs` sets it on combat enter/exit.

### 2.5 Scene & prefab setup

- **`Resources/Prefabs/InventoryStage.prefab`** — the detached diorama, every object on layer `InventoryFocus` (8): root (`InventoryStageView` + `PotView`), `StageCamera` (perspective FOV 35 at local z −1.6, URP **Overlay** render type, culling mask = `InventoryFocus`, `StageClickRouter` raycasts bubble clicks; disabled until the inventory opens), `StageLight` (directional, lights only the stage layer), `Cauldron` (`CauldronView` builds the lathe mesh at runtime, `M_PotBody`), `LiquidSurface` (`LiquidSurfaceView` builds and animates the sector mesh, `M_PotLiquid`, child `SurfaceBubbles` particle system), `Steam` (looping plume, started/stopped by `PotView.SetPotFocused`), `BubbleContainer`, `CraftingSlotsAnchor` (`Slot1`, `Slot2`, `ResultAnchor`, `PuffSuccess`/`PuffFail` steam bursts) with `CraftingSlotsView`, and `FeedingView` (a `FeedingTrayAnchor` parent for tray bubbles, reusing `Bubble.prefab`, plus a uGUI readout panel: cumulative-archetype label, digestion progress bar (`Image` type Filled), dominant-archetype label, and the **Feed** button).
- **`Resources/Prefabs/UI/InventoryHud.prefab`** — `InventoryHudView` with the open/close buttons plus a **feed-mode toggle** button and an optional active-state indicator (R24).
- **`Hero.prefab`** — `BellyCameraAnchor` child (local `(0, 0.18, 2.85)`, yaw 180) carrying `BellyAnchorMarker`; `CharacterFacingController` on the root. The old `BellyPotRig` is gone — no pot geometry lives on the character.
- **`Resources/Prefabs/UI/Bubble.prefab`** — transparent shell + lit artifact quad (front layer of the pseudo-3D stack; rear layers are cloned at runtime) + `SphereCollider` + `BubbleView`; layer `InventoryFocus` (8). Reused for pot bubbles, staged items, and the result.
- **`Area.unity`** — `InventoryStage` prefab instance at `(0, −50, 0)`; Main Camera excludes layer `InventoryFocus` from its culling mask and stacks `StageCamera` as a URP overlay; the Directional Light also excludes the stage layer (the `StageLight` owns stage lighting); `BellyCamera` (CinemachineCamera, lens `OrthographicSize` 0.65 — the ortho Main Camera ignores anchor distance, so this value alone sets the belly zoom; driven by `CameraService`, which also holds the Main Camera as `_outputCameraTransform`); `EventSystem` + `InputSystemUIInputModule`; `InventoryInstaller` registered in `SceneContext`. While the stage camera is disabled the click router is inert (and bubble colliders are off), so bubbles cannot be clicked when the inventory is closed.
- Materials in `Resources/Artifacts/Materials/`.

---

## 3. Crafting flow (sequence)

1. HUD open click → `InventoryPresenter`: lock movement, turn the hero toward the camera, belly camera in, stage overlay on (cauldron appears screen-centered with the hero visible behind, steam starts), show bubbles.
2. Bubble click → `CraftingPresenter` → `CraftingSession.TrySelect`: item leaves the inventory (bubble disappears), appears in a slot above the pot. Clicking a staged item returns it to the pot (`TryUnstage`).
3. Second selection enters the Crafting state: the two staged bubbles travel toward the result anchor (merge animation). Clicking either bubble while they travel cancels the craft (clicked item returns to the pot, the other stays staged). When the travel completes, the combine resolves:
   - **Match** → inputs consumed, bright steam burst, result item pops in at the result anchor with an overshoot bounce. Click it → it drops into the pot and becomes a bubble.
   - **No match** → dark steam burst, the last-clicked item visibly drops from the merge point into the pot (bubble reappears), the first glides back to its slot and stays staged.
4. Close (button/Escape/combat start) → a merge in progress is aborted, staged items and any uncollected result return to the pot, stage overlay off, camera, facing, and movement restore.

---

## 4. Adding content without code

**New artifact**: create an `ArtifactDefinition` via *Create → Inventory → Artifact* in `Resources/Artifacts/Definitions/`; set a unique `Id`, display name, icon sprite, and bubble tint. Optionally fill **Archetype Weights** (`_archetypeWeights`) — each entry references an `ArchetypeDefinition` by id and a weight describing how strongly eating this artifact pushes the character toward that creature archetype; duplicate ids are summed and empty/non-positive entries are ignored. See [Mutation Subsystem](mutation-subsystem.md) for the archetype set and how the weights are consumed. Duplicate or empty artifact ids fail fast at startup (`ArtifactCatalog`).

**New recipe**: create a `RecipeDefinition` via *Create → Inventory → Recipe* in `Resources/Artifacts/Recipes/`; reference input artifact definitions (order irrelevant, duplicates allowed) and one output. The first authored recipe for a given input combination wins.

**Feeding contribution of an artifact**: feeding reuses an artifact's **Archetype Weights** (see above and [Mutation Subsystem](mutation-subsystem.md)) — no separate feeding field. An artifact with no archetype weights still counts as one digested artifact toward the stage threshold but adds nothing to the archetype tally. Tune how many artifacts a stage needs via `Resources/Mutation/MutationConfig.asset` (`DigestionThreshold`). The feeding tray, readout panel, and feed-mode toggle are authored in the `InventoryStage`/`InventoryHud` prefabs (§2.5); no code is needed to add artifacts to the feeding flow.

**Tuning**: `Resources/Configs/InventoryConfig.asset` — items-to-combine, starting inventory, bubble radius/falloff/padding, bubble drift (amplitude/frequency), pseudo-3D artifact layers (count/spacing/darkening), liquid boil (wave amplitude/frequency/scale/secondary weight, emission pulse), character facing duration, camera/merge/fail-return/result-pop/drop timings. The result pop-in curve lives on `CraftingSlotsView` in the stage prefab. Cauldron geometry (bowl radius/depth, wall thickness, rim, segments, sweep arc) and liquid surface tessellation are authored on `CauldronView`/`LiquidSurfaceView` in the stage prefab; the bubble interior extents (`_potInteriorHalfExtents`, `_potInteriorHalfDepth`) on `PotView`; the bubble shell opacity (`_shellAlpha`) on `BubbleView` in `Bubble.prefab`; the belly zoom on the `BellyCamera` lens in the scene.

Sample recipe set shipped: fire+water→snake, fire+rock→lizard, water+bacteria→virus, rock+bacteria→lizard. Any other pair fails (demonstrates the fail path).

---

## 5. Tests

Edit-mode tests in `Assets/__Project/Tests/EditMode/`:

- `InventoryModelTests` — add/remove/return semantics, unique ids, duplicate definitions, events.
- `RecipeBookTests` — order independence, multiset exactness, disambiguation, no-match.
- `CraftingSessionTests` — state transitions (incl. the deferred `Crafting` state), combine-at-N via `ResolveCraft`, success consumption, **failure returns only the last selected item**, unstage from `Selecting` and cancel from `Crafting` (incl. the stale-resolve no-op and re-trigger after cancel), result collection, `ReturnAll` (incl. from `Crafting`).
- `FeedingSessionTests` — `TrySelect` removes from inventory and adds to the tray (raising `OnTrayChanged`); unknown id rejected; `TryUnselect` returns to inventory; `Consume` clears the tray without returning items; empty-tray `Consume` is empty; `ReturnAll` returns every tray item; null inventory throws.
- `BubbleLayoutCalculatorTests` — count, ellipse bounds, depth bounds and per-bubble depth variation (zero half depth ⇒ flat), screen-plane non-overlap for counts 2–30, single-bubble formula radius, radius monotonicity and clamping, determinism, phase desync.
- `BubbleDriftCalculatorTests` — still at zero amplitude, per-axis bounded by amplitude, determinism, temporal variation, phase desync, quasi-circular (non-collinear) path.
- `CauldronProfileCalculatorTests` — paired outer/inner counts, wall-thickness inset, rim height/lip, belly-wider-than-mouth silhouette, validation, determinism.
- `LiquidWaveCalculatorTests` — flat at zero amplitude, bounded by amplitude, spatial and temporal variation, determinism.
