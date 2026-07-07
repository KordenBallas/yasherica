# Inventory Subsystem — Requirements & Design

The **magic pot inventory**: the beast (main character) carries a magic cauldron, and that cauldron is the inventory. The cauldron is presentation-only — a detached 3D diorama rendered as a screen-space overlay, not an object in the game world. This document describes the subsystem as implemented: the container model, the cauldron/bubble visualization, the combine-above-the-pot crafting flow with **two-tier no-failure fusion** (signature recipes + emergent trait grammar), and how to add new artifacts, traits, recipes, and fusion rules without writing code. The same open screen also hosts the Mutation subsystem's **medallion ribbon** under the cauldron (blank socketing via drag & drop — `mutation-subsystem.md` §2.3); the former feeding/digestion mode was **removed** with the Socketed Blanks cutover (2026-07-02).

The **Cauldron View** presentation pass (**Track F**, 2026-07-06) reshaped the look-and-feel over these unchanged mechanics: a vertical **three-zone spine** (crafting top · brew centre · medallion ribbon bottom) with one **bubble = suspended in the liquid** rule (§1.1 R27–R29), a **translucent liquid surface + waterline** whose **fill level reads the artifact count** (F2, R30), **stable bottom-up bubble spots** with drop-in/settle **event physics** and **gravity settling on removal** (F3, R10/R31), and a **stomach-interior backdrop** seam (F5). Two interaction beats changed with it: the crafting result now **flows into the brew on the next combine** instead of being rejected until collected (R19), and the socketing rack's placement moved from left of the cauldron to the ribbon under it (F1). Nothing about the container, crafting outcome, or fusion changed.

Status: current as of 2026-07-06.

---

## 1. Requirements

### 1.1 Functional requirements

**Container**

- R1. The inventory is **container-based, not slot-based**: an unordered multiset of artifact instances. Duplicate artifacts are allowed; there is no capacity limit.
- R2. Every artifact in the world is an **instance** (unique `InstanceId`) of an authored **definition** (`DefinitionId`, e.g. `fire`). The 7 starting artifact types are: bacteria, fire, lizard, rock, snake, virus, water (icons in `Resources/Artifacts/UI/Icons/`).
- R3. Until a loot system exists, the inventory is seeded at startup from `InventoryConfig.StartingInventory` (dev seed). Future integration point: `Platform/Content/LootContent.cs` (stub).

**Opening & presentation**

- R4. A HUD button (bottom-right) opens the inventory. While open, a close button (top-right) and the **UI Cancel action** close it — `UI/Cancel` from the shared `GameActions.inputactions` (Escape / right-click / gamepad east button; wired to `InventoryHud.prefab`'s `_cancelAction` by the input foundation — it was silently unwired before, see `input-foundation.md`).
- R5. The inventory is a **UI layer only**: the cauldron diorama lives on a detached `InventoryStage` parked far below the playfield (world y = −50) on the `InventoryFocus` layer, rendered screen-centered by a dedicated **URP overlay camera** stacked on the main camera. It is never attached to the character and never part of the game world.
- R6. Opening zooms the base camera to the beast's torso: a dedicated Cinemachine camera (priority 30) follows the `BellyCameraAnchor` on the hero root every frame while active, so the framing stays correct as the hero turns; closing returns to whichever camera (isometric or combat) was active before. The character stays visible behind the overlay.
- R7. On open the character **smoothly rotates to face the camera** (yaw-only, smoothstep); on close the original facing is restored.
- R8. The cauldron is a **low-poly cross-section**: a procedurally generated lathe mesh (sphere-slice belly, rim lip, ~200° sweep) whose open front exposes the interior and the wall thickness at the cut planes. It is the **top zone's fixed anchor** for the brew — sized generously (bowl radius 0.68, depth 0.6) so a full inventory fits below the rim; the mesh is never resized in code (F2).
- R9. The liquid **boils continuously and reads as a real body of liquid** (F2): a **translucent low-poly surface** at the session's fill height (displaced by two crossed traveling sine waves, emissive glow pulsing), a **cut-away curtain** filling the pot's open wedge from floor to waterline (shaped to the bowl's inner profile, so submerged bubbles read *through* the liquid), and a **bright waterline band** at the meniscus (`M_PotWaterline`). Small bubbles rise from the surface and a looping **steam** plume rises above it while the inventory is open.
- R10. Artifacts are suspended inside **bubbles** on **stable spots** in the brew (F3): a deterministic **bottom-up lattice** of fixed-radius spots (`BrewSpotLatticeBuilder`) fills the bowl's volume in rows, clipped to the inner wall at each height; rows share one X grid, so spots align into **vertical columns**. Each artifact **holds its spot** for as long as nothing below it *in its own column* changes — adding one takes the lowest free spot, so **untouched bubbles never reshuffle**; removing one lets **gravity close the gap column-scoped** (FR4, owner revision 2026-07-07): only the bubbles that were **resting above the removed one** — its column — fall down to close the gap; everything **beside and below** it stays put (like pulling a marble from a jar; no arbitrary re-sort ever). Spots carry a small deterministic **depth** wobble (local Z) for parallax under the perspective stage camera, and **never overlap on screen** by construction (row/column spacing = bubble diameter × margin). The bubble shell is rendered translucent (fixed shell alpha; artifact tints color it without making it opaque) so the artifact inside stays visible; a bubble is bare (shell hidden) when it is *not* in the brew (R28).
- R11. Bubbles drift slowly around their spots: a small-amplitude quasi-circular wander (two incommensurate sine frequencies on local X/Y), out of sync per bubble (phase offset), so the pot reads as calm simmering. **Event physics** ride on top (F3): a bubble dropped into the brew **falls in and splashes**, kicking a damped-spring offset on nearby bubbles that **settles back** to the rest spots; a removal gives neighbours a small settle bob; a settling glide (R10) moves a bubble to its new lower spot through the same spring, never a teleport. All motion is event-scoped — the brew is still at rest.
- R11a. Bubble and craft-result clicks are routed by an explicit raycast from the stage camera (`StageDragRouter`, renamed from `StageClickRouter`). Press-and-release below a pixel threshold is a click (`IStageClickable`); pressing a pot bubble and moving past the threshold **drags** it — the bubble follows the pointer on its camera-distance plane and can be released onto an `IArtifactDropTarget` (a gem socket on a medallion in the ribbon, see `mutation-subsystem.md` §2.3), else it snaps back into the pot. Clicks over visible UI are ignored (UI wins), and routing is inert while the stage camera is disabled. Beware fullscreen raycast-target graphics from other subsystems: the dialogue UI's modal dim used to stay active (and invisible to the eye behind other canvases) after dialogues ended, silently eating all stage clicks — `DialogueView` now toggles the dim together with its panel.
- R12. Artifact sprites must read as **3D objects**: each artifact renders as a stack of lit, alpha-clipped quads (`M_ArtifactQuad`) — the rear layers sit slightly behind the front one and darken progressively — plus a slight per-instance yaw. Layer count/spacing/darkening are configurable.
- R13. Player movement input is disabled while the inventory is open and restored on close.

**Crafting**

- R14. Clicking a bubble moves its artifact out of the inventory into a **crafting slot** above the pot.
- R14a. Clicking a **staged artifact returns it to the pot** (unstage). Staged items are clickable at all times while staged.
- R15. When **N** artifacts are staged (`InventoryConfig.ItemsToCombine`, default 2), the session enters an observable **Crafting** state and a **merge animation** plays: the staged bubbles travel from their slots to the result anchor (`MergeDuration`). The combine is resolved against the recipe book only when the animation completes (presenter handshake; the domain itself has no timing).
- R15a. Clicking a merging artifact **during the Crafting state cancels the combine**: the clicked artifact returns to the pot, the remaining selection stays staged, and no recipe is consumed. New pot selections are rejected while Crafting.
- R16. Combine resolution is **two-tier and never fails**: (a) **signature** — an authored `RecipeDefinition` whose inputs match order-independently and multiset-exactly (`fire+water` equals `water+fire`; `fire+fire` is distinct from `fire`) wins; (b) otherwise the **emergent grammar** derives a result from the inputs' **function traits**: the trait union is shaped by authored `FusionRuleDefinition`s (combine / amplify / transmute), and the authored artifact best matching the resulting trait+tier target is produced (inputs excluded so a combine yields something new; deterministic — same inputs, same result).
- R17. **Resolution**: the staged artifacts are consumed, a steam burst plays, and the result artifact **pops in above the pot with an overshoot bounce** (`ResultPopDuration`, curve authored on `CraftingSlotsView`). Clicking the result drops it into the pot (inventory) with a shrink-and-drop animation. The domain reports whether the result was a signature or an emergent fusion (`OnCraftSucceeded(result, isSignature)`); the presentation does not differentiate them yet (ROADMAP polish item).
- R18. *(Removed 2026-07-02 — there is no failure path; the former dark-puff/last-item-return rule is gone.)*
- R19. **Continuous result flow** (F3): while a result hovers above the pot, clicking a pot artifact to start the next combine **auto-commits the hovering result into the brew** (it drops in and takes a spot) and stages the new pick; clicking the result directly still drops it too. *(Replaced the pre-Track-F rule "new selections rejected until the result is collected".)*
- R20. Closing the inventory returns all staged artifacts and any uncollected result to the pot; a merge in progress is aborted without resolving.

**Cauldron View — spatial spine & presentation (Track F)**

- R27. **Three-zone vertical spine** (F1): the open view reads top-to-bottom as **crafting zone (top) · brew/cauldron (centre) · medallion ribbon (bottom)**, all one continuous interior. The crafting zone and ribbon **anchor to the frame**, not to the liquid, so they never jump as the fill level changes.
- R28. **Bubble = "in the brew"** (F1): an artifact wears a bubble **if and only if it is suspended in the cauldron liquid**. Staged artifacts, the hovering fusion result, and artifacts socketed into a medallion render **bare** (shell hidden). The bubble doubles as a state signal — "this is in my pot" vs "this is staged / socketed".
- R29. **Medallion ribbon placement** (F1): the socketed Part-Blanks are presented as a **horizontal ribbon of medallions under the cauldron** (was: a rack left of it). The medallion's own presentation is the Mutation subsystem's job (`mutation-subsystem.md` §2.3, F4); this subsystem only owns the ribbon's position in the spine.
- R30. **Fullness = fill level** (F2): the liquid height is a function of **how many artifacts are in the inventory** — few = a shallow pool, many = brimming — between a configured minimum (never bone-dry) and maximum (never overflowing), and **always above the topmost bubble**. The level is **snapshotted when the view opens** and held for the session (it does not slide as you craft/consume within one open view); it re-evaluates on the next open. The cauldron mesh is never resized (R8).
- R31. **Stable brew + column gravity** (F3): bubble spots are stable per artifact (R10); adds take the lowest free spot on top of the pile without disturbing it; a removal drops **only the column that was resting above the removed bubble** by one place each (keeping the column's order) — bubbles beside and below never move, so no bubble is ever left hanging over a void, every column stays bottom-packed, and the fill level (R30) follows the real bubble pile. Placement is deterministic and testable; the splash/settle/glide reactions are non-deterministic view juice that always resolves back to the rest spots.
- R32. **Stomach-interior backdrop** (F5): a muted screen-space backdrop sits behind the diorama (no scene swap) as a designer-authored art seam — a plain quad + swappable material, present but placeholder.

**Feeding / digestion**

- R24–R26. *(Removed 2026-07-02 — the feeding mode, tray, readout, and digestion flow are deleted;
  mutations are obtained on the medallion ribbon instead, on the same screen with no mode
  switch. See `mutation-subsystem.md`.)*

**Combat guard**

- R21. The inventory **cannot be opened during combat** (HUD button disabled); if combat starts while the inventory is open, it force-closes.

**Data-driven authoring**

- R22. Artifacts and recipes are **ScriptableObject assets** — adding either requires no code changes (see §4).
- R23. ScriptableObjects contain **data only**; all logic lives in pure C# classes.

### 1.2 Non-functional requirements

- N1. The domain layer (`Inventory.Core`) has **no UnityEngine references** and is covered by edit-mode tests (`Tests/EditMode/`): container, fusion resolution (signature matching, emergent grammar, artifact selection), crafting state machine (incl. the deferred Crafting state, cancel-by-unstage, and continuous result flow), brew spot lattice + stable-spot assignment/settling, liquid fill math, bubble drift math, cauldron profile math, liquid wave math.
- N2. MVP: views are thin MonoBehaviour adapters behind interfaces; presenters are pure C# (`IInitializable`/`IDisposable`); the view never touches the model.
- N3. All wiring is in Zenject installers (`InventoryInstaller`, one binding in `AreaInstaller`); no service locators.
- N4. All tunables (combine count, bubble layout/bobbing numbers, animation and camera timings) live in `InventoryConfig` — no magic numbers in code.

---

## 2. Architecture

### 2.1 Layer map

| Layer | Responsibility | Key files |
|---|---|---|
| Domain (pure C#) | Container, crafting state machine, two-tier fusion resolution (recipe matching + emergent trait grammar), brew spot lattice / stable-spot assignment / liquid fill / drift / cauldron profile / liquid wave math | `Scripts/Inventory/Core/`: `ArtifactInstance.cs`, `InventoryModel.cs`, `CraftingSession.cs`, `RecipeBook.cs`, `ArtifactTraitProfile.cs`, `TraitFusionRule.cs`, `TraitFusionRuleSet.cs`, `EmergentFusionCalculator.cs`, `ArtifactByTraitSelector.cs`, `FusionResolver.cs`, `FusionSettings.cs`, `BrewSpot.cs`, `BrewLatticeSettings.cs`, `BrewSpotLatticeBuilder.cs`, `BrewLayoutModel.cs`, `LiquidFillSettings.cs`, `LiquidFillCalculator.cs`, `ICauldronGeometry.cs`, `BubbleDriftCalculator.cs`, `CauldronProfileCalculator.cs`, `LiquidWaveCalculator.cs` |
| Data (ScriptableObjects) | Authoring-time configuration + SO→Core bridges | `Scripts/Inventory/Data/Definitions/`: `ArtifactDefinition.cs`, `RecipeDefinition.cs`, `TraitDefinition.cs`, `TraitAxis.cs`, `FusionRuleDefinition.cs`, `InventoryConfig.cs`; `Scripts/Inventory/Data/`: `ArtifactCatalog.cs`, `TraitCatalog.cs`, `ArtifactTraitIndex.cs`, `RecipeBookBuilder.cs`, `FusionRuleSetBuilder.cs` |
| Application | Open/close orchestration, crafting flow, startup content validation | `Scripts/Inventory/Presenter/InventoryPresenter.cs`, `CraftingPresenter.cs`; `Scripts/Inventory/Application/ArtifactContentValidator.cs` |
| Infrastructure | HUD, inventory stage, cauldron/liquid meshes, bubbles, steam, puffs, stage click/drag routing | `Scripts/Inventory/View/`: `InventoryHudView.cs`, `InventoryStageView.cs`, `PotView.cs`, `BubbleView.cs`, `StageDragRouter.cs`, `IStageClickable.cs`, `IArtifactDropTarget.cs`, `CraftingSlotsView.cs`, `CauldronView.cs` (also `ICauldronGeometry`) + `CauldronMeshBuilder.cs`, `ILiquidSurfaceView.cs` + `LiquidSurfaceView.cs` + `LiquidSurfaceMeshBuilder.cs` + `LiquidBandMeshBuilder.cs` (curtain/waterline) (the medallion-ribbon views live in `Scripts/Mutation/View/` — see `mutation-subsystem.md` §2.3) |
| Shared infrastructure | Belly camera + anchor, character facing, movement lock, combat flag, logging | `Scripts/Core/Camera/CameraService.cs` (belly camera follow, output camera), `IBellyAnchorProvider.cs`/`BellyAnchorMarker.cs`, `Scripts/Character/ICharacterFacing.cs`/`CharacterFacingController.cs`, `IMovementInputLock.cs`, `Scripts/Combat/Integration/CombatActivityTracker.cs`, `Scripts/Core/Logging/IGameLogger.cs` |

Dependencies point inward: views raise events → presenters call domain → domain raises events → presenters update views through interfaces (`IInventoryHudView`, `IPotView`, `ICraftingSlotsView`). Presenters hand views plain DTOs (`BubbleViewData`, `ArtifactViewData`) with resolved sprites/tints, so views never see domain objects.

### 2.2 Core domain types

- **`ArtifactInstance`** — immutable: `InstanceId` (unique, generated by `InventoryModel`), `DefinitionId`.
- **`IInventoryModel` / `InventoryModel`** — multiset container: `Add(definitionId)`, `Return(instance)`, `Remove(instanceId)`, `CreateDetachedInstance(definitionId)` (for craft results not yet collected); events `OnItemAdded`/`OnItemRemoved`.
- **`IRecipeBook` / `RecipeBook`** — dictionary keyed by the sorted input-id multiset; `TryMatch(ids, out outputId)`. Built from assets by `RecipeBookBuilder` (skips malformed recipes with warnings). The **signature layer** of fusion resolution.
- **`ArtifactTraitProfile`** — immutable snapshot of one artifact's function: normalized trait ids (substance + property merged; the grammar is axis-agnostic) + tier. Deterministic ordinal ordering.
- **`IArtifactTraitSource` / `ArtifactTraitIndex`** *(Data)* — the SO→Core bridge: every authored artifact as a trait profile, ordinal by definition id; `TryGetProfile(defId)`.
- **`TraitFusionRule` / `TraitFusionRuleSet`** — authored grammar steps in pure terms (required / added / removed trait ids + tier delta), held in the one deterministic application order (ordinal rule id; duplicate ids fail fast). Built from assets by `FusionRuleSetBuilder`.
- **`EmergentFusionCalculator`** — computes the emergent target profile: trait union → rules in order (each at most once, cascading within one combine) → tier = max input tier + rule deltas + `AmplifyTierBonus` per trait duplicated across inputs.
- **`ArtifactByTraitSelector`** — deterministically picks the authored artifact best expressing a target profile (`overlap·W − offTarget·W − tierDistance·W`, ordinal tie-break); inputs are excluded unless that would empty the pool. Total over a non-empty pool — this is what structurally guarantees no-failure.
- **`IFusionResolver` / `FusionResolver`** — the combine pipeline (R16): signature first, else calculator + selector; returns `FusionResult(outputId, isSignature)`.
- **`FusionSettings`** — grammar tunables fed from `InventoryConfig` (amplify bonus, selector weights).
- **`ICraftingSession` / `CraftingSession`** — state machine `Idle → Selecting → Crafting → ResultReady`; implements R14–R20. Staging the Nth item enters `Crafting` and raises `OnCraftingStarted(stagedItems)`; the combine resolves only when `ResolveCraft()` is called (by the presenter, after the merge animation), and returns false outside `Crafting` so a stale animation callback after a cancel is a no-op. Resolution goes through `IFusionResolver` and **always succeeds**. `TryUnstage(instanceId)` returns a staged item to the inventory from `Selecting` or `Crafting` — from `Crafting` it cancels the pending combine (the count drops below N, so it cannot re-trigger). **Continuous result flow (F3, R19):** `TrySelect` while a result hovers (`ResultReady`) **auto-collects the result into the inventory first** and then stages the new pick — but only after the pick's own inventory check passes, so an invalid selection never silently drops the result. Events: `OnItemStaged`, `OnCraftingStarted`, `OnCraftSucceeded(result, isSignature)`, `OnItemUnstaged`, `OnResultCollected`, `OnSessionCleared`.
- **`BrewSpot` / `BrewLatticeSettings` / `BrewSpotLatticeBuilder`** — the deterministic **stable-spot lattice** (F3): `Build(CauldronProfileSettings, BrewLatticeSettings)` → an ordered `BrewSpot[]` filling the bowl's volume **bottom-up** in rows (each row a centre-out spread over one shared X grid — so spots align into **vertical columns**, each spot carrying its `Row`/`Column` — clipped to the inner wall radius at its height, plus a bounded depth wobble), with a few configured overflow rows above the rim. Spot order **is** the fill order; a column's rows are contiguous (the bowl's unimodal width guarantees no holes for the settle to fall through).
- **`BrewLayoutModel`** — **stable per-artifact placement** over the lattice: `Sync(instanceIds)` / `TryOccupy` / `Release` keep each artifact on its spot; a new artifact takes the lowest free spot; a removal settles **column-scoped** (`SettleColumn`, FR4): only the removed bubble's column re-packs onto its lowest spots keeping its order — every other column stays put, and every column stays bottom-packed by induction. `HighestOccupiedY` is the top of the pile — the fill calculator reads it so the waterline covers the bubbles. Pure state; the glide/splash is view juice.
- **`LiquidFillSettings` / `LiquidFillCalculator`** — deterministic **fullness** (F2): `Calculate(itemCount, highestBubbleTopY, LiquidFillSettings)` → the waterline height — a linear count curve between min/max, raised when needed so it always clears the topmost bubble, clamped to max (the documented overfill edge).
- **`ICauldronGeometry`** — the port exposing the authored bowl `CauldronProfileSettings` (implemented by `CauldronView`), so the lattice and the liquid surface derive from the one silhouette instead of duplicating size tunables.
- **`CauldronProfileCalculator`** — deterministic: `CauldronProfileSettings` (bowl radius/depth, wall thickness, rim width, segments) → `CauldronProfile`: index-paired outer/inner `ProfilePoint` polylines (sphere-slice belly + rim lip), so the mesh builder can cap the cross-section cut planes with simple quad strips. `InnerRadiusAtHeight(settings, height)` gives the inner wall radius at any height (clamped into the bowl) — the lattice and the liquid curtain/surface shape themselves by it.
- **`LiquidWaveCalculator`** — deterministic boil height field: `SampleHeight(x, z, time, LiquidWaveSettings)` = two crossed traveling sine waves, bounded by the configured amplitude.
- **`BubbleDriftCalculator`** — deterministic idle bubble motion: `SampleOffset(time, phase, BubbleDriftSettings, out x, out y)` = per-axis sines with an irrational frequency ratio and a quarter-turn phase offset, so each bubble wanders a slowly precessing quasi-circle (bounded per axis by the amplitude) instead of bobbing on a line.

### 2.3 Presenters

- **`InventoryPresenter`** — owns the open/close flow: combat & camera-transition guards → movement lock → `ICharacterFacing.FaceTowards(ICameraService.OutputCameraPosition)` → `ICameraService.SetBellyAnchor(IBellyAnchorProvider.BellyAnchor)` + `SwitchToBellyCamera` → `IInventoryStageView.SetStageActive(true)` → **brew refresh + fill snapshot** → HUD button swap. `RefreshBubbles` syncs the `BrewLayoutModel` to the current inventory and hands `IPotView` one bubble per artifact on its stable spot (F3); `SnapshotFillLevel` computes the session's waterline via `LiquidFillCalculator` (reading `BrewLayoutModel.HighestOccupiedY`) and pushes it to `ILiquidSurfaceView.SetFillHeight` **once, on open** (F2, R30). Subscribes to inventory events for live re-layout (the fill level does **not** re-slide within a session); seeds the starting inventory; closes on combat start (R21). Close calls `CraftingSession.ReturnAll()` (R20) — blank sockets deliberately keep their contents (incubation persists across open/close; `mutation-subsystem.md` §2.3) — deactivates the stage, `SwitchToPreviousCamera`, `RestoreFacing`, and unlocks movement.
- **`CraftingPresenter`** — routes `IPotView.OnBubbleClicked` → `session.TrySelect` and `ICraftingSlotsView.OnStagedItemClicked` → `session.TryUnstage`; renders staged slots. Merge handshake: `OnCraftingStarted` → `slotsView.PlayMergeAnimation()` → view raises `OnMergeCompleted` → `session.ResolveCraft()`. Resolution → success puff + result pop-in (signature and emergent results present identically for now); unstage → staged slots rebuild (which aborts a running merge, so a cancelled craft never resolves); result click → `TryCollectResult` → drop animation.
- **`ArtifactContentValidator`** *(Application, `IInitializable`, NonLazy)* — startup warnings for trait-authoring mistakes: broken/misplaced (wrong-axis) trait references, trait-less artifacts, fusion rules pushing toward traits no authored artifact carries (unreachable outputs).

### 2.4 Zenject wiring

`Scripts/Core/DI/InventoryInstaller.cs` (added to the Area scene's `SceneContext`):

- `InventoryConfig` instance (validated non-null), `IArtifactCatalog`, `IRecipeBook`, `ITraitCatalog`, `IArtifactTraitSource` → `ArtifactTraitIndex`, `TraitFusionRuleSet` (via `FusionRuleSetBuilder`), `FusionSettings` (from config). Definitions auto-load from `Resources/Artifacts/{Definitions,Recipes,Traits,FusionRules}` when the inspector lists are empty.
- `IInventoryModel`, `EmergentFusionCalculator`, `ArtifactByTraitSelector`, `IFusionResolver` → `FusionResolver`, `ICraftingSession` (with `ItemsToCombine`), and `BrewLayoutModel` (`FromMethod`: builds the lattice from `ICauldronGeometry.ProfileSettings` + the config's `BrewLatticeSettings`, so layout and the authored bowl silhouette never drift apart).
- **Part inventory (P2-1, §2.6):** `IPartInventoryModel` → `PartInventoryModel`, `CharacterSystem.Core.IShedPartSink` → `PartInventorySink`, and (when `Resources/Prefabs/UI/PartInventoryPanel` exists) `IPartInventoryView` `FromComponentInNewPrefab` + `PartInventoryPresenter` NonLazy — a missing prefab only disables the readout, never the stash.
- `ArtifactContentValidator` via `BindInterfacesTo` + `NonLazy`.
- `IInventoryHudView` (scene instance or instantiated from `Resources/Prefabs/UI/InventoryHud.prefab`); `IPotView`/`ICraftingSlotsView`/`IInventoryStageView`/`ICauldronGeometry` (→ `CauldronView`)/`ILiquidSurfaceView` (→ `LiquidSurfaceView`) (on the InventoryStage scene instance) and `IMovementInputLock`/`ICharacterFacing`/`IBellyAnchorProvider` (on the hero scene instance) via `FromComponentInHierarchy`.
- Both presenters (`InventoryPresenter`, `CraftingPresenter`) via `BindInterfacesAndSelfTo` + `NonLazy`.
- `IGameLogger` → `UnityGameLogger` with `IfNotBound` (first logger abstraction in the project; other subsystems may adopt it).
- The fusion bindings (`EmergentFusionCalculator`, `TraitFusionRuleSet`, `FusionSettings`, `IArtifactTraitSource`) are also consumed by the Mutation subsystem's socketing (same `SceneContext`); see [Mutation Subsystem](mutation-subsystem.md).

`AreaInstaller` additionally binds `CombatActivityTracker` (`BindInterfacesAndSelfTo`); `Platform/States/CombatActiveState.cs` sets it on combat enter/exit.

### 2.4b Part inventory — the stash for shed body parts (P2-1)

The player's stash of body parts that are owned but not on the body. Today it is filled by exactly
one source: parts **shed by a body-plan change** (character-system.md R21 — a legless frame orphans
the legs; they return here, never destroyed). Distinct from the artifact inventory by design: parts
carry no per-instance state, so the model is a plain multiset of part definition ids.

- **`IPartInventoryModel` / `PartInventoryModel`** *(Core, pure C#)* — `Add(partId)`, `PartIds`
  (insertion order, duplicates allowed), `OnPartAdded`. No take/remove API yet — the
  re-install-from-inventory flow is a deferred ROADMAP item.
- **`PartInventorySink`** *(Integration)* — implements the character system's
  `CharacterSystem.Core.IShedPartSink` port over the model; the only bridge between the two layers
  (the character system never references Inventory).
- **`PartInventoryPresenter` / `IPartInventoryView` / `PartInventoryView`** — MVP readout: on every
  addition the presenter aggregates the multiset into name×count rows (friendly names via the
  CharacterSystem `IPartCatalog` — the same pragmatic cross-read `MutationPartCatalog` uses) and
  pushes them to a small corner panel (`Resources/Prefabs/UI/PartInventoryPanel.prefab`, bottom-right,
  hidden while empty). Read-only: no interactions until the re-install flow ships.

### 2.5 Scene & prefab setup

- **`Resources/Prefabs/InventoryStage.prefab`** — the detached diorama, every object on layer `InventoryFocus` (8), composed as the **three-zone spine** (F1): root (`InventoryStageView` + `PotView`), `StageCamera` (perspective FOV 40 at local z −2.6, slight downward tilt, URP **Overlay** render type, culling mask = `InventoryFocus`, `StageDragRouter` routes clicks and drags; disabled until the inventory opens), `StageLight` (directional, lights only the stage layer), `Cauldron` (`CauldronView` builds the lathe mesh at runtime + serves `ICauldronGeometry`, bowl radius 0.68 / depth 0.6, `M_PotBody`), `LiquidSurface` (`LiquidSurfaceView` builds and animates the translucent top surface `M_PotLiquid` + child `SurfaceBubbles`, and at runtime parents two child meshes under the cauldron — the cut-away **LiquidCurtain** and the bright **Waterline** with `M_PotWaterline`), `Steam` (looping plume, started/stopped by `PotView.SetPotFocused`), `BubbleContainer` (at the bowl origin — the brew centre), `CraftingSlotsAnchor` **above** the pot (`Slot1`, `Slot2`, `ResultAnchor`, the `PuffSuccess` steam burst) with `CraftingSlotsView`, a `MedallionRibbon` **under** the cauldron carrying the medallion ribbon (`BlankRackView` + three anchors spread along X + the disabled medallion-entry/socket templates) — see `mutation-subsystem.md` §2.3 — and a `StomachBackdrop` quad (F5) behind the diorama with `M_StomachBackdrop`.
- **`Resources/Prefabs/UI/InventoryHud.prefab`** — `InventoryHudView` with the open/close buttons.
- **`Hero.prefab`** — `BellyCameraAnchor` child (local `(0, 0.18, 2.85)`, yaw 180) carrying `BellyAnchorMarker`; `CharacterFacingController` on the root. The old `BellyPotRig` is gone — no pot geometry lives on the character.
- **`Resources/Prefabs/UI/Bubble.prefab`** — transparent shell + lit artifact quad (front layer of the pseudo-3D stack; rear layers are cloned at runtime) + `SphereCollider` + `BubbleView`; layer `InventoryFocus` (8). Reused for pot bubbles, staged items, and the result.
- **`Area.unity`** — `InventoryStage` prefab instance at `(0, −50, 0)`; Main Camera excludes layer `InventoryFocus` from its culling mask and stacks `StageCamera` as a URP overlay; the Directional Light also excludes the stage layer (the `StageLight` owns stage lighting); `BellyCamera` (CinemachineCamera, lens `OrthographicSize` 0.65 — the ortho Main Camera ignores anchor distance, so this value alone sets the belly zoom; driven by `CameraService`, which also holds the Main Camera as `_outputCameraTransform`); `EventSystem` + `InputSystemUIInputModule`; `InventoryInstaller` registered in `SceneContext`. While the stage camera is disabled the click router is inert (and bubble colliders are off), so bubbles cannot be clicked when the inventory is closed.
- Materials in `Resources/Artifacts/Materials/` (Track F added `M_PotWaterline` and `M_StomachBackdrop`; `M_PotLiquid` is now a translucent URP-Lit surface).

---

## 3. Crafting flow (sequence)

1. HUD open click → `InventoryPresenter`: lock movement, turn the hero toward the camera, belly camera in, stage overlay on (cauldron appears screen-centered with the hero visible behind, steam starts), show bubbles.
2. Bubble click → `CraftingPresenter` → `CraftingSession.TrySelect`: item leaves the inventory (bubble disappears), appears in a slot above the pot. Clicking a staged item returns it to the pot (`TryUnstage`).
3. Second selection enters the Crafting state: the two staged bubbles travel toward the result anchor (merge animation). Clicking either bubble while they travel cancels the craft (clicked item returns to the pot, the other stays staged). When the travel completes, the combine resolves:
   - **Signature match** → the authored recipe's output; **no match** → the emergent grammar's output (best authored artifact for the combined trait+tier target). Either way the inputs are consumed, a steam burst plays, and the result pops in at the result anchor with an overshoot bounce. Click it → it drops into the pot and becomes a bubble; or just start the next combine (click a pot artifact) and the hovering result **flows into the brew on its own** (R19, continuous result flow).
4. Close (button/Escape/combat start) → a merge in progress is aborted, staged items and any uncollected result return to the pot, stage overlay off, camera, facing, and movement restore.

---

## 4. Adding content without code

**New artifact**: create an `ArtifactDefinition` via *Create → Inventory → Artifact* in `Resources/Artifacts/Definitions/`; set a unique `Id`, display name, icon sprite, and bubble tint. Fill its **function**: `Substance Traits` (what it is made of), `Property Traits` (what it does) — both reference `TraitDefinition` assets — and `Tier` (0 = raw find; higher = crafted/refined). **Binding authoring rule:** the artifact's name and look must telegraph its traits (traits are never shown in a stats panel). Duplicate or empty artifact ids fail fast at startup (`ArtifactCatalog`).

**New trait**: create a `TraitDefinition` via *Create → Inventory → Trait* in `Resources/Artifacts/Traits/`; set a unique `Id`, display name, and `Axis` (**Substance** = material, **Property** = behavior; the runtime grammar is axis-agnostic — the axis exists for authoring clarity and validation). Shipped vocabulary: substance `stone, water, fire, chitin, rot`; property `sharp, heavy, toxic, focusing, fiery`.

**New signature recipe**: create a `RecipeDefinition` via *Create → Inventory → Recipe* in `Resources/Artifacts/Recipes/`; reference input artifact definitions (order irrelevant, duplicates allowed) and one output. The first authored recipe for a given input combination wins. Signatures are rare highlights; the emergent grammar covers everything else — never author an exhaustive pair table.

**New fusion rule**: create a `FusionRuleDefinition` via *Create → Inventory → Fusion Rule* in `Resources/Artifacts/FusionRules/`; set a unique `Rule Id` (also the deterministic application order, ordinal), the `Required Traits` (all must be present in the combined inputs), `Added Traits` (the emergent output), optional `Removed Traits` (transmute — consumed by the reaction), and a `Tier Delta`. Rules cascade within one combine in rule-id order. Shipped rules: `heat_hardens` (fiery+stone → chitin), `rot_spreads` (rot+water → toxic). The startup validator warns when a rule pushes toward a trait no authored artifact carries.

**Fusion tuning**: `Resources/Configs/InventoryConfig.asset` — `Amplify Tier Bonus` (tier bump per trait duplicated across inputs), `Trait Overlap Weight` / `Trait Mismatch Weight` / `Tier Proximity Weight` (how the emergent output artifact is selected).

**Tuning**: `Resources/Configs/InventoryConfig.asset` — items-to-combine, fusion grammar weights (see above), starting inventory, **Brew Layout (stable spots)** (bubble radius, spot spacing margin, edge padding, floor clearance, depth jitter, overflow rows), **Brew Event Physics** (drop-in height/duration, splash radius/impulse, removal bob, settle spring stiffness/damping), **Liquid Fullness** (min/max fill height, artifacts-at-full-pot, headroom), bubble drift (amplitude/frequency), pseudo-3D artifact layers (count/spacing/darkening), liquid boil (wave amplitude/frequency/scale/secondary weight, emission pulse), character facing duration, camera/merge/result-pop/drop timings. The result pop-in curve lives on `CraftingSlotsView` in the stage prefab. Cauldron geometry (bowl radius/depth, wall thickness, rim, segments, sweep arc) is authored on `CauldronView`, and the liquid surface tessellation / curtain / waterline thickness + material on `LiquidSurfaceView`, in the stage prefab; the bubble shell opacity (`_shellAlpha`) on `BubbleView` in `Bubble.prefab`; the belly zoom on the `BellyCamera` lens in the scene. Growing the bowl on `CauldronView` automatically widens the brew lattice and raises capacity (the lattice reads the geometry); nudge `artifactsAtFullPot` and the max fill height to match.

Sample signature set shipped: fire+water→snake, fire+rock→lizard, water+bacteria→virus, rock+bacteria→lizard. Any other pair resolves emergently (e.g. fire+bacteria: traits `fire, fiery, rot, toxic` → best trait match in the authored pool).

---

## 5. Tests

Edit-mode tests in `Assets/__Project/Tests/EditMode/`:

- `InventoryModelTests` — add/remove/return semantics, unique ids, duplicate definitions, events.
- `PartInventoryModelTests` — insertion order, duplicate accumulation, event payloads, null/empty guard, empty default (the P2-1 shed-part stash, §2.4b).
- `RecipeBookTests` — order independence, multiset exactness, disambiguation, no-match.
- `ArtifactTraitProfileTests` — normalization (drop empty, dedupe, ordinal order), tier clamp, `Has`.
- `TraitFusionRuleSetTests` — ordinal rule ordering, duplicate-id fail-fast, malformed-rule validation.
- `EmergentFusionCalculatorTests` — trait union, max-tier base, amplify on duplicates, rule add/remove/delta, forward-only cascade in ordinal order, tier floor.
- `ArtifactByTraitSelectorTests` — overlap scoring, off-target penalty, tier proximity, ordinal tie-break, input exclusion + full-pool fallback, empty source.
- `FusionResolverTests` — signature-first, emergent non-input selection, rule-steered targets, identity fallback on an empty trait source.
- `CraftingSessionTests` — state transitions (incl. the deferred `Crafting` state), combine-at-N via `ResolveCraft`, **signature and emergent resolution both consume inputs and produce a result (no fail path)**, unstage from `Selecting` and cancel from `Crafting` (incl. the stale-resolve no-op and re-trigger after cancel), result collection, `ReturnAll` (incl. from `Crafting`), **continuous result flow** (select-while-`ResultReady` auto-commits the result and stages the pick; an invalid pick keeps the result hovering; the auto-committed result can complete the next combine).
- `BrewLatticeTests` — settings validation, sequential spot indices, bottom-up order, floor/wall containment, submerged-vs-overflow row limit, screen-plane non-overlap, **vertical column alignment** (one X per column, one Y per row) and **per-column row contiguity**, bounded depth jitter, determinism, and `InnerRadiusAtHeight` (widens floor→belly, clamps rim overshoot).
- `BrewLayoutModelTests` — lowest-free-spot occupancy, idempotent re-occupy, **column-scoped gravity settle on removal** (a mid-pile removal drops only the column above it while the bubbles beside keep their spots; a removal with nothing above it moves nobody; removing the bottom of a tall column drops the whole column by one and the fullness top follows), exhausted lattice, `Sync` reconciliation (release stale → settle their columns → stack new on top), `HighestOccupiedY`.
- `LiquidFillCalculatorTests` — settings validation, empty-pot minimum, monotonic rise with count, full-pot maximum + clamp beyond, waterline-always-covers-topmost-bubble, max-clamp-wins overfill edge.
- `BubbleDriftCalculatorTests` — still at zero amplitude, per-axis bounded by amplitude, determinism, temporal variation, phase desync, quasi-circular (non-collinear) path.
- `CauldronProfileCalculatorTests` — paired outer/inner counts, wall-thickness inset, rim height/lip, belly-wider-than-mouth silhouette, validation, determinism.
- `LiquidWaveCalculatorTests` — flat at zero amplitude, bounded by amplitude, spatial and temporal variation, determinism.
