# Modular Character System — Requirements & Design

This document describes the modular character system as implemented: how low-poly characters are assembled from interchangeable body-part meshes on a shared skeleton, how the two-tier socket system works, and how to add new skeletons, parts, and attachments without writing code.

Status: current as of 2026-06-13.

---

## 1. Requirements

### 1.1 Functional requirements

**Skeleton & parts**

- R1. One character = one shared skeleton (Generic, non-humanoid rig) instantiated from a rig prefab. All animation plays on this skeleton via a single `Animator` on the rig root; parts and attachments never have their own Animators.
- R2. Body parts are independent `SkinnedMeshRenderer` prefabs skinned to the shared skeleton. A part occupies exactly one named slot (Head, Torso, Arm L/R, Leg L/R, Tail, ...). Slots are data-driven: adding a slot = adding a `SlotDefinition` asset.
- R3. Any part is swappable at runtime. A swap instantiates the new part prefab under the rig root, remaps its `bones[]` and `rootBone` onto the shared skeleton's transforms by bone name, and destroys the old instance. The Animator is never touched, so the playing animation continues seamlessly (no T-pose, no state reset).
- R4. Multiple skeletons are supported. Each part declares its target `SkeletonDefinition`; equipping a part on the wrong skeleton is a validation error.

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
- R14b. `EquippedParts` — a snapshot `slotId → partId` map of the currently equipped parts. Backs combat's part-derived ability set (see ability-subsystem.md §2.6) and exact starting-part exclusion in the mutation choice.

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
             CharacterAssemblyState, BoneMapResolver, AssemblyValidator, records
  Data/      CharacterSystem.Data    — ScriptableObject definitions (config only),
             PartCatalog / AttachmentCatalog, DefinitionMapper (the only SO -> Core bridge)
  Runtime/   CharacterSystem.Runtime — plain C# orchestrators (CharacterAssemblyController,
             PartSwapExecutor, SocketMounter, ModularCharacterFactory) and exactly two
             MonoBehaviours: CharacterRig (bone registry) and ModularCharacter (API facade)
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
| `AssemblyValidator` | Stateless rules → `ValidationIssue` (severity + stable `ValidationIssueCode` + subject id + message). |

### 2.3 Runtime flow

`ModularCharacterFactory.Create(assembly, parent)`:

1. Validates the assembly definitions (`AssemblyValidator`); aborts on errors.
2. Instantiates the rig prefab, calls `CharacterRig.Initialize()` (caches every descendant transform by name — at that moment all descendants are bones), then verifies every bone in the `SkeletonDefinition` exists on the live rig.
3. Builds the per-character object graph (`CharacterAssemblyController` + `PartSwapExecutor` + `SocketMounter`) and mounts Tier-1 sockets via the first catalog rebuild.
4. Equips the assembly's parts through the normal `SwapPart` path and applies default attachments.
5. Adds the `ModularCharacter` facade and returns it.

`SwapPart` sequence (Animator-state-safe): validate part → instantiate part prefab under rig root → assign `renderer.bones[]` from the rig's name registry in the part's baked order and `rootBone` → disable the old renderer, then destroy the old instance (no double-geometry frame, no gap) → update assembly state → rebuild socket catalog → `SocketMounter` applies the added/removed diff (creates/destroys socket transforms, enforces the R9 attachment policy).

The Animator is never rebound: parts never add or remove bones (the rig prefab always contains the full bone superset, including tail/wing/ear chains), renderers are not animator bindings, and the Animator component is never disabled. Part prefabs carry whole-character `localBounds` so per-part culling cannot pop after the bone remap.

### 2.4 Data definitions (ScriptableObjects, config only)

| Asset | Contents |
|---|---|
| `SlotDefinition` | id, display name |
| `SocketDefinition` | id, parent bone name, local position/rotation/scale |
| `SkeletonDefinition` | id, rig prefab (bones + Animator + `CharacterRig`), authoritative bone-name list, Tier-1 sockets |
| `PartDefinition` | id, slot, target skeleton, part prefab (one SkinnedMeshRenderer), bone names in mesh-index order, contributed Tier-2 sockets, **granted active abilities + passive abilities** (Combat SOs — see note) |
| `AttachmentDefinition` | id, prefab, target socket id, extra local TRS offset |
| `CharacterAssemblyDefinition` | id, skeleton, one part per slot, optional default attachments |

`DefinitionMapper` converts definitions to Core records; TRS values never enter Core (they are consumed directly by `SocketMounter`). The part's granted abilities are **not** mapped into Core — `PartData` stays ability-agnostic; combat reads the ability SOs off `PartDefinition` directly (ability-subsystem.md §2.6).

> **Layering note (debt):** `PartDefinition`'s ability fields reference the `Combat.Data.Definitions` layer, so the CharacterSystem data layer depends on Combat. This is a deliberate, user-approved M1 coupling that breaks the inward-only layering rule (CLAUDE.md §2); it is tracked in the ROADMAP for the M2 part-driven-affinity rework, which will reconsider where this data lives.

### 2.5 Zenject wiring

`CharacterSystemInstaller` (add to the scene's SceneContext) binds `IGameLogger` (IfNotBound), `IPartCatalog`, `IAttachmentCatalog` (serialized lists with `Resources.LoadAll` fallback from `Resources/CharacterSystem/Parts` and `.../Attachments`), and `IModularCharacterFactory`. Per-character object graphs are factory-constructed, not container-bound — they are transient per-entity state.

### 2.6 Naming conventions

- Bones: PascalCase with `.L`/`.R` side suffixes and numeric chain suffixes — `UpperArm.L`, `Tail.0`..`Tail.2`, `Wing.R`. Matching is exact and case-sensitive.
- Ids: lowercase dot-separated — slots `slot.head`, sockets `socket.palm.r` / `socket.tail.tip`, parts `part.torso.a`, attachments `attachment.hat`, skeletons `skeleton.placeholder`.
- Socket transforms are created as `Socket_<id>` GameObjects under their parent bone.

---

## 3. Placeholder assets & editor tooling

### 3.1 Generation

`Tools/Character System/Generate Placeholder Assets` (idempotent — regenerates `Assets/__Project/Resources/CharacterSystem/`) produces:

- `PlaceholderRig.prefab` — 27-bone hierarchy (spine chain, arms, legs, ears, wings, 3-bone tail) built at the origin with identity rotations, so `bone.worldToLocalMatrix` is directly each mesh's bindpose. Holds the Animator (with generated controller) and `CharacterRig`.
- `PlaceholderIdle.anim` / `PlaceholderRun.anim` / `PlaceholderLocomotion.controller` — two looping 2s clips of sampled quaternion `localRotation` curves: idle (spine sway, head nod, arm swings, ear twitches, wing flaps, phase-offset tail wag) and run (leg gait with counter-swinging arms and a spine bob). The controller is a 1D blend tree on a `Speed` float (idle @ 0, run @ 1), so `Speed` defaults to 0 and a freshly-instantiated character idles. Makes swap-continuity and appendage deformation visually verifiable. The movement-driven `Speed`/facing driver lives in the separate `character-locomotion.md` system.
- 13 part definitions (A/B variants per slot + tail) with generated skinned box meshes (one box per covered bone, full weight to that bone), part prefabs, and two URP Lit materials (blue A, orange B). Head A contributes hat + ear sockets; Head B only hat; Torso A contributes wing sockets; Torso B none; the tail part contributes the tail-tip socket.
- 14 socket definitions (8 Tier-1, 6 Tier-2), 7 slot definitions, 3 attachment definitions with primitive prefabs (hat, tail club, sword), and `PlaceholderAssembly_A`.

`Tools/Character System/Place Demo Character In Scene` drops a `CharacterSystemDemoBootstrap` wired to the generated assets. The bootstrap creates the character on Play and exposes context-menu actions: swap to B variants / back to defaults, attach/detach all listed attachments, log available sockets.

### 3.2 Assembly preview & inspectors

- `Tools/Character System/Assembly Preview` (`CharacterAssemblyPreviewWindow`): pick a skeleton or load an assembly, choose a part per slot, Build Preview — the preview is built through the same `ModularCharacterFactory` as runtime. "Show Sockets" draws socket gizmos (sphere + RGB axes + id label; Tier-2 in pink) for the preview and for any selected play-mode character. A validation panel lists `AssemblyValidator` issues.
- `PartDefinition` inspector: inline validation against the target skeleton + **Bake Bone Names From Prefab** (records `SkinnedMeshRenderer.bones` names in index order — the ingestion path for rigged FBX art).
- `SkeletonDefinition` inspector: inline validation + **Sync Bone Names From Rig Prefab**.

### 3.3 Importing real art later

A rigged FBX replaces the placeholder by: (1) creating a `SkeletonDefinition` pointing at its rig prefab (with `CharacterRig` added) and syncing bone names; (2) per part, creating a `PartDefinition`, assigning the part prefab, and baking bone names; (3) authoring sockets/slots/assemblies as assets. Requirement: all parts of one skeleton must be skinned against the same bind pose.

---

## 4. Tests

`Assets/__Project/Tests/EditMode/` (NUnit, pure C#, no Unity dependencies):

- `SocketCatalogTests` — Tier-1 union, Tier-2 add/remove on equip/unequip, swap diffs, same-id-different-part diffs, no-op rebuilds, duplicate-id determinism, ordering.
- `BoneMapResolverTests` — order preservation, missing-bone reporting by name, case sensitivity, empty/null lists.
- `AssemblyValidatorTests` — skeleton mismatch, missing bones, socket parent bone missing, duplicate slots/socket ids, clean-assembly zero issues.
- `CharacterAssemblyStateTests` — equip/replace/remove semantics, event payloads, open slots.

Unity-side classes (`PartSwapExecutor`, `SocketMounter`, factory) are verified manually via the demo bootstrap and assembly preview window.
