# Modular Character System — Requirements & Design

This document describes the modular character system as implemented: how low-poly characters are assembled from interchangeable body-part meshes on a skeleton, how the two-tier socket system works, how **independent body plans** let a rare frame-changing part re-form the whole body on a different skeleton (P2-1, `product-requirements/body-plan-skeleton-swap.md`), and how to add new skeletons, body plans, parts, and attachments without writing code.

Status: current as of 2026-07-04.

---

## 1. Requirements

### 1.1 Functional requirements

**Skeleton & parts**

- R1. One character = one skeleton at a time (Generic, non-humanoid rig) instantiated from a rig prefab. All animation plays on this skeleton via a single `Animator` on the rig root; parts and attachments never have their own Animators.
- R2. Body parts are independent `SkinnedMeshRenderer` prefabs skinned against a skeleton's bind pose. A part occupies exactly one named slot (Head, Torso, Arm L/R, Leg L/R, Tail, Legs Cluster, ...). Slots are data-driven: adding a slot = adding a `SlotDefinition` asset.
- R3. *(revised by P2-1)* An ordinary same-frame part swap stays instant and Animator-safe: it instantiates the new part prefab under the rig root, remaps its `bones[]` and `rootBone` onto the current skeleton's transforms by bone name, and destroys the old instance — the playing animation continues seamlessly. Only the rare **body-plan change** (R19–R24) rebuilds the rig and rebinds the Animator.
- R4. *(revised by P2-1)* Multiple skeletons are supported. A part's `TargetSkeleton` is authoring provenance (which bind-pose family the mesh was baked against), **not** a hard gate: cross-frame fit is checked **structurally** — a part fits a skeleton iff every skinned bone and every contributed socket's parent bone resolves on it. The validator reports a target mismatch as a Warning; missing bones stay Errors.

**Body plans (P2-1 — supersedes the old single-fixed-superset reading of R1/R3/R10)**

- R19. A **base body plan** carries the common case: most parts belong to it and swap on it cheaply (R3). A part marked **`GovernsBodyPlan`** is a candidate to govern the whole frame: among equipped governors the highest **`BodyPlanPriority`** wins (ties break by ordinal part id); no governors → the base skeleton. The governing frame is a pure function of the equipped set — same body, same frame, always (FR3/FR11).
- R20. Installing/removing a governor **re-forms the body** on the winning frame: the old rig is torn down and a new rig is built on the governing skeleton under the same host root, re-equipping every surviving part. The new frame's own locomotion controller (`SkeletonDefinition.AnimatorController`) is bound, so the character keeps animating (fresh Animator idles at `Speed 0` — no T-pose).
- R21. Equipped ordinary parts that do **not** fit the new frame (structural check, R4) are **shed to the player's part inventory** (`IShedPartSink` → `PartInventoryModel`, inventory-subsystem.md) — never destroyed.
- R22. A frame change that would shed parts first shows a **confirm dialog** listing the parts that will come off; declining leaves the body, the blank, and the sockets untouched. A shed-nothing frame change needs no prompt.
- R23. A **losing governor stays equipped-but-dormant**: recorded on the body (it keeps its slot and its governance candidacy — removing the winner hands the frame to the next-highest) but contributes no renderer, sockets, abilities, or race markers. `EquippedParts` and all existing consumers keep "rendered body" semantics; dormant parts surface via `DormantParts`.
- R24. The frame-change transaction is **build-before-destroy**: the new body is fully staged on an inactive rig first (all fallible steps — validation, bone verification, equips — happen there); only a complete staged body replaces the live one. A failed build changes nothing.

**Sockets**

- R5. A socket is a named `Transform` parented to a specific bone with a local TRS offset, created at runtime from a `SocketDefinition`.
- R6. Tier-1 (skeleton) sockets always exist on every character of a skeleton: shoulders, palms, feet, back, tail base (placeholder set).
- R7. Tier-2 (part-contributed) sockets exist only while their part is equipped: e.g. heads contribute hat/ear sockets, Torso A contributes wing sockets, the tail part contributes a tail-tip socket.
- R8. `GetAvailableSockets()` returns the union of Tier-1 + currently contributed Tier-2 sockets; the union is rebuilt on every part swap.
- R9. When a swap removes a Tier-2 socket: if the incoming part contributes the same socket id, the socket transform is recreated and existing attachments are re-parented onto it; otherwise the attachments are destroyed (with an Info log).
- R10. Appendages that must deform (tail, wings, ears) are actual bones in the shared skeleton, skinned by part meshes — never socket props. Their tip sockets (e.g. tail-tip) sit on those bones and follow the animation.

**Runtime API** (`IModularCharacter`, safe to call mid-animation)

- R11. `SwapPart(slotId, partId)` / `SwapPart(PartDefinition)` — replaces the part in a slot; validates, remaps bones, preserves Animator state, refreshes sockets. Returns false (with logged errors) on failure.
- R12. `AttachToSocket(socketId, prefab)` / `AttachToSocket(AttachmentDefinition)` — instantiates and parents under the socket; returns an `AttachmentHandle`.
- R13. `DetachFromSocket(handle)` / `DetachFromSocket(socketId)` — removes one attachment or everything on a socket.
- R14. `GetAvailableSockets()` / `GetSocketTransform(socketId)`.
- R14b. `EquippedParts` — a snapshot `slotId → partId` map of the currently equipped (rendered) parts. Backs combat's part-derived ability set (see ability-subsystem.md §2.6) and exact starting-part exclusion in the mutation choice. Dormant governors are excluded; they surface via `DormantParts` / `EquippedPartDefinitions`, and `EquipDormant(part)` records one (replacing any occupant of its slot).

### 1.2 Non-functional requirements

- R15. All bookkeeping logic (socket union, slot state, bone-name resolution, validation) is pure C# with no UnityEngine reference and is unit-tested.
- R16. Validation catches: missing/renamed bones, duplicate socket ids, parts targeting a mismatched skeleton, duplicate slots in an assembly, empty bone lists. The same `AssemblyValidator` runs at runtime (fail fast) and in editor tooling (inspector warnings).
- R17. Bone matching is exact (ordinal, case-sensitive). Bone-name lists in `PartDefinition` are ordered to match the mesh's bone indices/bindposes and are baked by tooling, never hand-ordered.
- R18. Runtime skinned-mesh combining is not implemented; the design keeps it possible (the controller owns the live per-slot renderer instances a future combiner would consume).

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/CharacterSystem/
  Core/      CharacterSystem.Core    — pure C#, no UnityEngine: SocketCatalog,
             CharacterAssemblyState, BoneMapResolver, AssemblyValidator,
             BodyPlanResolver, BodyPlanChangePlanner (+ plan/resolution records),
             IShedPartSink port, records
  Data/      CharacterSystem.Data    — ScriptableObject definitions (config only),
             PartCatalog / AttachmentCatalog, DefinitionMapper (the only SO -> Core bridge)
  Runtime/   CharacterSystem.Runtime — plain C# orchestrators (CharacterAssemblyController,
             PartSwapExecutor, SocketMounter, ModularCharacterFactory,
             BodyPlanSwapCoordinator) and MonoBehaviour adapters: CharacterRig
             (bone registry), ModularCharacter (API facade), ModularCharacterVisual
             (host→rig bridge)
  View/      CharacterSystem.View    — the body-plan confirm modal MVP triple
             (IBodyPlanConfirmView / BodyPlanConfirmView / BodyPlanConfirmPresenter)
Scripts/Core/DI/CharacterSystemInstaller.cs
Scripts/Editor/CharacterSystem/       — YashericaEditor assembly (editor-only)
```

This is a visual/gameplay infrastructure system, not UI, so classic MVP does not literally apply; the layering keeps its spirit: testable pure-C# domain, thin MonoBehaviours, constructor-injected plain classes.

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `SocketInfo` | Immutable socket description (id, parent bone, tier, source part id). Full-field equality so a socket whose contributing part changed diffs as removed + re-added. |
| `SocketCatalog` | The queryable Tier-1 + Tier-2 union. `Rebuild(skeleton, equippedParts)` recomputes it and raises `Changed(added, removed)` — all diff logic lives here, testable. On duplicate ids the first occurrence wins deterministically (validator flags the collision). |
| `CharacterAssemblyState` | Slot id → equipped `PartData`; `Equip` returns the replaced part; slots are open (unknown slot ids just create entries). |
| `BoneMapResolver` | Resolves a part's ordered bone-name list against a skeleton's bone set; reports missing names. |
| `AssemblyValidator` | Stateless rules → `ValidationIssue` (severity + stable `ValidationIssueCode` + subject id + message). Since P2-1, `SkeletonMismatch` is a **Warning** (provenance note); structural fit is enforced by the `MissingBone`/`SocketParentBoneMissing` Errors. |
| `BodyPlanResolver` | Which frame governs an equipped set: highest `BodyPlanPriority` among `GovernsBodyPlan` parts, ties by ordinal part id, none → base skeleton. Stateless, order-independent (FR3/FR11). |
| `BodyPlanChangePlanner` | The one pure decision function of an install: computes the prospective body (incoming part replaces its slot occupant), resolves the governing frame, and classifies every part **Active / Dormant / Shed** against it. Kinds: `InstantSwap` (cheap path) / `DormantInstall` / `FrameChange` / `Incompatible`. `Fits(part, skeleton)` is the single structural-fit test. Shed/active lists sort by slot id for deterministic plans and stable confirm-dialog order. |
| `IShedPartSink` | Port for "shed parts go to the player's inventory" — implemented by the Inventory layer (`PartInventorySink`), so this system never references Inventory. |

### 2.3 Runtime flow

`ModularCharacterFactory.Create(assembly, parent)`:

1. Validates the assembly definitions (`AssemblyValidator`); aborts on errors.
2. Instantiates the rig prefab, calls `CharacterRig.Initialize()` (caches every descendant transform by name — at that moment all descendants are bones), then verifies every bone in the `SkeletonDefinition` exists on the live rig.
3. Builds the per-character object graph (`CharacterAssemblyController` + `PartSwapExecutor` + `SocketMounter`) and mounts Tier-1 sockets via the first catalog rebuild.
4. Equips the assembly's parts through the normal `SwapPart` path and applies default attachments.
5. Adds the `ModularCharacter` facade and returns it.

`SwapPart` sequence (Animator-state-safe): validate part → instantiate part prefab under rig root → assign `renderer.bones[]` from the rig's name registry in the part's baked order and `rootBone` → disable the old renderer, then destroy the old instance (no double-geometry frame, no gap) → update assembly state → rebuild socket catalog → `SocketMounter` applies the added/removed diff (creates/destroys socket transforms, enforces the R9 attachment policy) → raise `PartsChanged`.

Within one frame the Animator is never rebound: same-frame swaps never add or remove bones, renderers are not animator bindings, and the Animator component is never disabled. Part prefabs carry whole-character `localBounds` so per-part culling cannot pop after the bone remap. *(A body-plan change is the deliberate exception: it replaces the rig — and therefore the Animator — wholesale; see §2.3b.)*

### 2.3b Body-plan change flow (P2-1)

Every gameplay install request routes through **`BodyPlanSwapCoordinator`** (pure C#, singleton; the assembly controller owns *one body on one rig*, the coordinator owns transitions *between* bodies):

1. `RequestInstall(slotId, partId)` reads the live body (`EquippedPartDefinitions` + `DormantParts`) off the hero's `ModularCharacterVisual`, maps everything to Core records, and asks `BodyPlanChangePlanner` for a plan. Skeletons are looked up through the SO graph itself (base assembly's skeleton + each governor's `TargetSkeleton`) — no separate skeleton catalog.
2. `InstantSwap` → plain `SwapPart` (the ~80% path, untouched). `DormantInstall` → `EquipDormant` (state-only). `Incompatible` → rejected, nothing offered/changed.
3. `FrameChange` with a non-empty shed list → `IBodyPlanConfirmPrompt.Request(summary)` (the modal lists the target frame + shed part names); the request returns `PendingConfirmation` and resolves later via the `InstallResolved(bool)` event. Declining changes nothing. A shed-nothing change skips the prompt (R22). With no prompt bound (demo scenes) the change auto-confirms with a logged warning.
4. Execution is **build-before-destroy** (R24): a staging GameObject is created inactive under the host; `ModularCharacterFactory.Create(skeleton, activeParts, dormantParts, …)` builds the complete new body there (this overload is all-or-nothing — any equip failure aborts and returns null, live body untouched). On success: reparent the new rig to the host, `ModularCharacterVisual.ReplaceCharacter(...)` (re-points `Character`/`Animator`, binds the frame's `AnimatorController`, re-fires `CharacterAssembled`), destroy the old rig (its sockets/attachments die with it — R9-style), and hand the shed part ids to `IShedPartSink`.
5. Downstream consumers self-heal through existing seams: `RacePassportBinder` re-binds on the re-fired `CharacterAssembled`; combat reads `visual.Character.EquippedParts` at combat start; `CharacterLocomotionView` re-resolves its cached Animator by reference-comparing against `ModularCharacterVisual.Animator` each call; camera/physics/registry live on the host root and never notice. `TastedFormsRecorder` (`CharacterSystem.Integration`, Area-bound, same subscription discipline as the passport binder) rides the same events: every part the hero carries — equipped or dormant, on any install path — is marked `world.<partId>.arena_tasted` (Meta horizon), the passive tasted-forms catalog the Arena draft board reads (`arena-mode.md` §2.9); preview clones never reach the fact store.

The mutation unseal is the (only) in-run gameplay trigger: `IMutationCharacter.RequestSwapPart` returns `Applied | PendingConfirmation | Rejected`, the card presenter commits the unseal only on an applied/confirmed install, and unseal offers are pre-filtered through `CanInstall` so a card that cannot be installed on the current body is never shown (mutation-subsystem.md).

**Run-start install (O1, `hub-staging.md`):** `StartingPartApplier` (`CharacterSystem.Integration`, Area-bound beside `HeroBodyRestorer`, same wait-for-`CharacterAssembled` discipline) installs the Hub-chosen starting part on **fresh runs only**, exactly once, via plain `SwapPart` — the Hub offer is pre-filtered to non-frame-changers, so no coordinator hop or shed-confirm is possible. `PartsChanged` then drives the passport (the 1-marker tolerated freak) and the tasted recorder through their existing binders; missing/failing part ids degrade to a bare launch (FR14 tolerance).

### 2.4 Data definitions (ScriptableObjects, config only)

| Asset | Contents |
|---|---|
| `SlotDefinition` | id, display name |
| `SocketDefinition` | id, parent bone name, local position/rotation/scale |
| `SkeletonDefinition` | id, **display name** (confirm-dialog label; falls back to the id), rig prefab (bones + Animator + `CharacterRig`), authoritative bone-name list, Tier-1 sockets, **`AnimatorController`** (this frame's locomotion controller, bound whenever the frame governs the body — per-frame gait) |
| `PartDefinition` | id, **display name** (friendly UI label; falls back to the asset name when blank), slot, target skeleton, part prefab (one SkinnedMeshRenderer), bone names in mesh-index order, contributed Tier-2 sockets, **granted active abilities + passive abilities** (Combat SOs — see note), **mutation data: `TraitAffinities` (`TraitAffinity[]`: trait id + 0..1 weight, scored against the socketed reagents at unseal), `Rarity` (`MutationRarity` enum, `Common`…`Mythical`), `ChoiceIcon` (Sprite)** — see mutation-subsystem.md, **`RaceId` (string race tag, empty = kindless; drawn as a drop-down over the authored races — the passport marker, see races-passport.md)**, **body-plan data: `GovernsBodyPlan` (bool — the rare frame-changing flag; while equipped, the part's `TargetSkeleton` is a candidate to govern the body) + `BodyPlanPriority` (int — higher wins among equipped governors, ties by ordinal part id)** |
| `AttachmentDefinition` | id, prefab, target socket id, extra local TRS offset |
| `CharacterAssemblyDefinition` | id, skeleton, one part per slot, optional default attachments |

`DefinitionMapper` converts definitions to Core records; TRS values never enter Core (they are consumed directly by `SocketMounter`). The part's granted abilities are **not** mapped into Core — `PartData` stays ability-agnostic; combat reads the ability SOs off `PartDefinition` directly (ability-subsystem.md §2.6).

> **Layering note (debt):** `PartDefinition`'s ability fields reference the `Combat.Data.Definitions` layer, so the CharacterSystem data layer depends on Combat. This is a deliberate, user-approved M1 coupling that breaks the inward-only layering rule (CLAUDE.md §2); it is tracked in the ROADMAP.
>
> **Mutation data (M2, accepted; trait-based since Socketed Blanks):** `PartDefinition` also carries the per-part trait affinity, rarity, and choice icon, so the Mutation subsystem can score parts as unseal variants without companion assets. `TraitAffinity` references traits by id string (the Inventory layer's `TraitDefinition.Id`) and `MutationRarity` is a plain enum, so this adds no type dependency on the Mutation/Inventory layers — but the *concepts* of trait affinity and rarity sit in the character layer. This was a deliberate user decision (single-asset authoring over strict layering); see mutation-subsystem.md §6 and the ROADMAP.
>
> **Race tag (races-passport.md):** `RaceId` follows the same id-string decoupling — the value matches a `RaceDefinition._raceId`, kindless when empty. The `[RaceId]` attribute only drives the inspector drop-down (`RaceIdDrawer`); the race system reads the plain string off the catalog, and `PartData` stays race-agnostic (the tag is not mapped into Core).

The controller raises `PartsChanged` after every successful swap or dormant install (surfaced through `IModularCharacter`), and `ModularCharacterVisual` raises `CharacterAssembled` after every rig assembly — once from `Start` and again after every body-plan change — the seam cached-reference holders (e.g. the race passport projector, races-passport.md §2.3) re-bind on.

> **Demo role tint (bandit-camp brief, 2026-07-05):** `IDemoRoleTintApplier` / `DemoRoleTintApplier`
> tints an assembled rig via `MaterialPropertyBlock` (`_BaseColor` + `_Color` over every child
> renderer), so shared part materials are never mutated — the same placeholder mesh reads green on a
> villager and maroon on a bandit at once. The colour is authored where the role lives:
> `NpcArchetype._demoTint` (applied by `NpcContent.SpawnModularVisual`) and
> `EnemyDefinition._demoTint` (applied by `EnemyVisualSpawner`). **Alpha 0 = untinted** (the default,
> so unedited assets keep their authored look). This is an explicit demo affordance pending real
> per-faction art. Shipped tints: villagers/frogfolk green, barn raider + camp crew lighter maroon,
> camp boss deep maroon.

### 2.5 Zenject wiring

`CharacterSystemInstaller` (add to the scene's SceneContext) binds `IPartCatalog`, `IAttachmentCatalog` (serialized lists with `Resources.LoadAll` fallback from `Resources/CharacterSystem/Parts` and `.../Attachments`), `IModularCharacterFactory`, `IDemoRoleTintApplier` (the demo role tint), the hero's `ModularCharacterVisual` (`FromComponentInHierarchy` — moved here from MutationInstaller; Mutation and the races integration resolve it cross-installer), the `BodyPlanSwapCoordinator`, and the confirm modal (`IBodyPlanConfirmView` from `Resources/Prefabs/UI/BodyPlanConfirmPanel` + `BodyPlanConfirmPresenter` as `IBodyPlanConfirmPrompt`; a missing prefab degrades to auto-confirm with a warning, never a scene crash). The coordinator's prompt and `IShedPartSink` are `[InjectOptional]` — the sink is bound by `InventoryInstaller` (inventory-subsystem.md). Per-character object graphs are factory-constructed, not container-bound — they are transient per-entity state.

### 2.6 Naming conventions

- Bones: PascalCase with `.L`/`.R` side suffixes and numeric chain suffixes — `UpperArm.L`, `Tail.0`..`Tail.2`, `Wing.R`. Matching is exact and case-sensitive.
- Ids: lowercase dot-separated — slots `slot.head`, sockets `socket.palm.r` / `socket.tail.tip`, parts `part.torso.a`, attachments `attachment.hat`, skeletons `skeleton.placeholder`.
- Socket transforms are created as `Socket_<id>` GameObjects under their parent bone.

---

## 3. Placeholder assets & editor tooling

### 3.1 Generation

`Tools/Character System/Generate Placeholder Assets` regenerates `Assets/__Project/Resources/CharacterSystem/` from the per-frame data tables in `PlaceholderFrameLibrary` (base biped + serpent + spider). It is idempotent **and in-place**: existing assets are updated at their paths, so GUIDs and hand-authored content fields (display names, abilities, traits, race tags, body-plan priorities) survive a regeneration — scene/prefab/blank references never break. Only clips/controllers are rebuilt from scratch (they are re-assigned to the rig prefab and skeleton definition in the same run). Per frame it produces:

- `{Frame}Rig.prefab` — the frame's bone hierarchy built at the origin with identity rotations, so `bone.worldToLocalMatrix` is directly each mesh's bindpose. Holds the Animator (with the frame's generated controller) and `CharacterRig`. Base = the historical 27-bone biped (spine chain, arms, legs, ears, wings, 3-bone tail); **serpent** = base minus the 6 leg bones plus `Tail.3..5`; **spider** = base minus the legs plus 8 radial `SpiderHip.k`/`SpiderTip.k` chains under `Pelvis`. **Invariant: bones shared across frames keep identical names and rest TRS**, so base parts survive on any frame that still carries their bones — cross-frame fit stays purely structural.
- `{Frame}Idle.anim` / `{Frame}Run.anim` / `{Frame}Locomotion.controller` — looping 2s clips of sampled quaternion `localRotation` curves built from the frame's sway table: biped idle/run gait, serpent sway/slither (a lateral wave travelling down the tail chain), spider bob/scuttle (alternating radial-leg steps). Each controller is the same 1D blend tree on `Speed` (idle @ 0, run @ 1) and is assigned to both the rig prefab and `SkeletonDefinition.AnimatorController`. The movement-driven `Speed`/facing driver lives in the separate `character-locomotion.md` system.
- `{Frame}Skeleton.asset` + the frame's part definitions with generated skinned box meshes (one box per covered bone, full weight to that bone) and part prefabs. Base: 13 A/B parts as before. Serpent: `part.spine.serpent` (slot.tail, **governs, priority 20**, boxes down Pelvis + Tail.0..5). Spider: `part.legs.spider` (**new slot `slot.legs.cluster`**, governs, priority 10, hub + 8 radial leg boxes). Both frame-changers seed trait affinities + Rare rarity so unseal scoring surfaces them.
- Shared across frames: 14 socket definitions (8 Tier-1, 6 Tier-2 — legless frames pick a Tier-1 subset without the foot sockets), 8 slot definitions (incl. `slot.legs.cluster`), 3 attachment definitions with primitive prefabs (hat, tail club, sword), and three preview assemblies (`PlaceholderAssembly_A` / `_Serpent` / `_Spider`).
- Equip triggers for the demo frames are hand-authored blanks (not generator output): `Resources/Mutation/Blanks/Blank_SerpentSpine.asset` (slot.tail, reptile) and `Blank_SpiderCluster.asset` (slot.legs.cluster, insect).

`Tools/Character System/Place Demo Character In Scene` drops a `CharacterSystemDemoBootstrap` wired to the generated assets. The bootstrap creates the character on Play and exposes context-menu actions: swap to B variants / back to defaults, attach/detach all listed attachments, log available sockets.

`Tools/Character System/Build Body-Plan Demo Scene` builds and saves `Assets/__Project/Scenes/BodyPlanDemo.unity`: a small walled platform, the Hero prefab (movement + modular visual + locomotion — WASD to walk, so each frame's gait is visible), and the **part-selection dev console** — one TMP dropdown per authored slot listing every catalog part (frame-changers tagged `[frame]`, a dormant occupant `(dormant)`). Picks route through the real `BodyPlanSwapCoordinator`, so ordinary parts swap instantly while a frame-changing pick opens the confirm-and-shed modal, and shed parts land in the part-stash readout (the demo installer binds the same stash trio as the Area scene). MVP triple: `BodyPlanDemoConsoleView` (thin, clones a row template) + `BodyPlanDemoConsolePresenter` (pure C#, re-renders on every body change; a rejected/declined pick re-renders the dropdowns back to the actual body) + `BodyPlanDemoInstaller` (logger, character registry, stash, console — the character system and locomotion come from their own installers on the same SceneContext).

### 3.2 Assembly preview & inspectors

- `Tools/Character System/Assembly Preview` (`CharacterAssemblyPreviewWindow`): pick a skeleton or load an assembly, choose a part per slot, Build Preview — the preview is built through the same `ModularCharacterFactory` as runtime. "Show Sockets" draws socket gizmos (sphere + RGB axes + id label; Tier-2 in pink) for the preview and for any selected play-mode character. A validation panel lists `AssemblyValidator` issues.
- `PartDefinition` inspector: inline validation against the target skeleton + **Bake Bone Names From Prefab** (records `SkinnedMeshRenderer.bones` names in index order — the ingestion path for rigged FBX art).
- `SkeletonDefinition` inspector: inline validation + **Sync Bone Names From Rig Prefab**.

### 3.3 Importing real art later

A rigged FBX replaces the placeholder by: (1) creating a `SkeletonDefinition` pointing at its rig prefab (with `CharacterRig` added) and syncing bone names; (2) per part, creating a `PartDefinition`, assigning the part prefab, and baking bone names; (3) authoring sockets/slots/assemblies as assets. Requirement: all parts of one skeleton must be skinned against the same bind pose.

---

## 3b. Adding Content (asset-only recipes)

**Add a body part (ordinary, base frame)** — unchanged: create a `PartDefinition` (`Character System/Part`), assign slot / target skeleton / part prefab, bake bone names, author sockets/abilities/mutation data/race tag. No code.

**Add a body plan (frame)** — data only:
1. Author the frame's rig prefab: bone hierarchy + `Animator` + `CharacterRig`, built at origin facing −Z. Bones the frame shares with other frames MUST keep the same names and rest TRS (that is what lets base parts ride along).
2. Create a `SkeletonDefinition` (`Character System/Skeleton`): id, display name (shown in the confirm dialog), rig prefab, "Sync Bone Names From Rig Prefab", pick its Tier-1 sockets, and assign its locomotion `AnimatorController` (same `Speed` 1D-blend convention).
3. Author the frame's parts as ordinary `PartDefinition`s targeting the new skeleton.
4. Nothing registers anywhere: the frame is reachable as soon as some frame-changing part's `TargetSkeleton` points at it.

**Add a frame-changing part** — data only: author a `PartDefinition` whose `TargetSkeleton` is the frame it pulls in, tick **`GovernsBodyPlan`**, set **`BodyPlanPriority`** (higher wins; ties break by ordinal part id), give it mutation data (traits/rarity/icon) so unseals can offer it, and (optionally) author a blank for its slot (`Mutation/Part Blank`) as the equip trigger. Installing it in play re-forms the body; nothing else to wire.

**Add an attachment / socket / slot** — unchanged (`Character System/Attachment` / `Socket` / `Slot` assets).

**Give a role a demo tint** — data only: set `_demoTint` (alpha 1) on the role's `NpcArchetype`
(world NPCs) or `EnemyDefinition` (combat enemies). Alpha 0 (the default) means untinted. No code,
no material edits — the tint rides a `MaterialPropertyBlock` over the shared placeholder materials.

---

## 4. Tests

`Assets/__Project/Tests/EditMode/` (NUnit, pure C#, no Unity dependencies):

- `SocketCatalogTests` — Tier-1 union, Tier-2 add/remove on equip/unequip, swap diffs, same-id-different-part diffs, no-op rebuilds, duplicate-id determinism, ordering.
- `BoneMapResolverTests` — order preservation, missing-bone reporting by name, case sensitivity, empty/null lists.
- `AssemblyValidatorTests` — skeleton mismatch is Warning-only (P2-1), missing bones stay Errors, socket parent bone missing, duplicate slots/socket ids, clean-assembly zero issues.
- `CharacterAssemblyStateTests` — equip/replace/remove semantics, event payloads, open slots.
- `BodyPlanResolverTests` — no/single/competing governors, priority + ordinal tie-break, input-order independence, winner-absent fallback, broken governor ignored, repeat determinism (FR3/FR11).
- `BodyPlanChangePlannerTests` — all four change kinds; serpent-on-legged-base sheds exactly both legs; frame-changers never shed (dormant instead); governance falls to the dormant governor when the winner's slot is replaced; orphaned contributed-socket parent ⇒ shed; shed-nothing change needs no confirmation; stable slot-ordered lists under input shuffle; unknown governing skeleton ⇒ incompatible; `Fits` positive/negative.

Unity-side classes (`PartSwapExecutor`, `SocketMounter`, factory, `BodyPlanSwapCoordinator`'s commit path, the confirm modal) are verified manually via the demo bootstrap, assembly preview window, and the serpent/spider acceptance script (see the PRD's acceptance criteria).
