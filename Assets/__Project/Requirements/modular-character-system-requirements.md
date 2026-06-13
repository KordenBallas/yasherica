# Requirements Prompt — Modular Low-Poly Character System (Unity)

## Objective
Implement a modular character system for the main character and any npc / enemy. I want all to share the same system. A low-poly 3D character is assembled from **interchangeable body-part meshes that all share a single skeleton**, plus a **hierarchical socket system** for attaching equipment and appendages. Any body part must be swappable **at runtime** without interrupting the currently playing animation, and anything attached to a socket must follow the skeleton automatically.

## Core Architecture
1. **Single shared skeleton (one rig).** Every body part is skinned to the same bone hierarchy with one shared bind pose. Use a **Generic (non-humanoid) rig**, because the character includes non-humanoid bones (tail, wings, ears) that Unity's Humanoid/Mecanim retargeting does not cover. All animation plays on this skeleton, never on individual meshes.
2. **Body parts as swappable `SkinnedMeshRenderer`s.** Each part is an independent mesh skinned to the shared skeleton. A swap = instantiate the new part, remap its `bones[]` and `rootBone` to the shared skeleton's transforms (matched by bone name), and keep the same `Animator`. Swapping a part must NOT reset or interrupt the current animation state.
3. **One `Animator` / `AnimatorController` per character**, living on the shared skeleton root.

## Body Part Slots (minimum)
Define each as a named slot; exactly one mesh occupies a slot at a time:
- Head
- Torso
- Arm Left / Arm Right
- Leg Left / Leg Right

The set of slots must be data-driven and extensible (easy to add new slots later).

## Socket System (two tiers)
A socket is a **named `Transform` parented to a specific bone** with a local position/rotation/scale offset. Anything parented to a socket inherits that bone's motion automatically.

### Tier 1 — Skeleton sockets (always present on every character)
- Shoulder Left / Shoulder Right
- Hand (palm) Left / Hand (palm) Right
- Foot Left / Foot Right
- Back (spine)
- Tail base
- Wings (optional, on back/torso)

### Tier 2 — Part-contributed sockets (provided by the equipped part)
A specific part may expose additional sockets only while it is equipped. Examples:
- A **head** exposes: Ear socket(s), Headwear/Hat socket.
- A specific **torso** exposes: Wing socket(s).
- A specific **tail** exposes: Tail-tip socket (e.g., for a club / mace).

**Requirement:** the system maintains one queryable set of available sockets = Tier-1 (skeleton) sockets + sockets contributed by all currently equipped parts. This set must be rebuilt/updated whenever a part is swapped (Tier-2 sockets appear and disappear with their part).

## Animated Appendages vs. Static Props
- **Static equipment** (hat, weapon, shield, ear meshes that don't deform) = prefab attached to a socket. Follows the bone; has no animation of its own.
- **Appendages that must deform/animate** (tail, wings, ears that wag/flap) must be **actual bones in the shared skeleton** (or sub-skeletal meshes that follow the skeleton), NOT socket-only props. Their socket(s) (e.g., tail-tip) then sit on those bones.

## Runtime API (minimum)
- `SwapPart(slotId, partId | mesh)` — replaces the mesh in a slot, remaps bones, preserves Animator state, refreshes available sockets.
- `AttachToSocket(socketId, prefab) -> handle` — instantiates and parents to the socket with the defined offset.
- `DetachFromSocket(socketId | handle)`.
- `GetAvailableSockets()` — returns Tier-1 + currently contributed Tier-2 sockets.
- All operations must be safe to call during gameplay while animations are playing.

## Data-Driven Definitions
Use `ScriptableObject`s:
- **PartDefinition** — slot, mesh, required bone names, list of contributed sockets.
- **SocketDefinition** — id, parent bone, local TRS offset.
- **AttachmentDefinition** — prefab, target socket id, local offset.
- A consistent **naming convention** for bones and sockets, plus validation that every referenced bone/socket actually exists on the target skeleton/part.

## Editor Tooling
- Inspector(s) to define and preview sockets both on the skeleton and on individual parts.
- Scene gizmos visualizing socket position and orientation.
- Validation warnings for: missing/renamed bones, duplicate socket ids, parts skinned to a mismatched skeleton.
- An assembly preview that lets a designer pick a part per slot and see the result in the editor.

## Performance & Art Constraints
- Low-poly assets with a **consistent scale and shared bind pose** across all parts.
- Optional: combine equipped `SkinnedMeshRenderer`s into a single skinned mesh at runtime to reduce draw calls (must preserve the shared bones).
- Keep one Animator/AnimatorController per character; avoid per-part animators.

## Acceptance Criteria
- A character can be assembled from parts both in the editor and at runtime.
- Swapping any body part keeps the current animation playing seamlessly (no T-pose flash, no state reset).
- Attaching/detaching to any Tier-1 or Tier-2 socket works, and the attachment follows the animation.
- Part-contributed (Tier-2) sockets become available/unavailable as their parts are equipped/removed.
- Tail, wings, and ears deform/animate together with the skeleton; their tip sockets follow correctly.

## Assumptions to Confirm
- **Generic (non-humanoid) rig** chosen over Unity Humanoid because of tail/wings/ears.
- **Runtime swapping** is required (not editor-only assembly).
- **Render pipeline** (Built-in / URP / HDRP) is left configurable — specify the target.
- Whether one shared skeleton serves a single archetype or must be reused across multiple character archetypes.
