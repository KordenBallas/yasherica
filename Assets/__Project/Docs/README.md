# Project Documentation

This folder is the current, maintained documentation space for the project. Documents here describe systems **as implemented** — if code and a document disagree, the document is outdated and must be fixed.

Older documentation elsewhere in the repository may be stale; prefer this folder.

## Documents

| Document | Scope |
|---|---|
| [Ability Subsystem](ability-subsystem.md) | Requirements and design of combat abilities: shapes, targeting, scheduling queue, execution, data-driven authoring, extension guide |
| [Narrative Generation](narrative-generation.md) | Requirements and design of procedural level narrative: story/NPC pools and filtering, density config, combat-capability matching, rewards, dialogue composition, planned compatibility scoring |
| [Inventory Subsystem](inventory-subsystem.md) | Requirements and design of the magic pot inventory: container model, bubble visualization, belly camera zoom, combine crafting with data-driven artifacts/recipes, combat guard |
| [Loot Subsystem](loot-subsystem.md) | Requirements and design of data-driven artifact rewards: biome loot tables, deterministic per-run rolls, platform discovery pickups, enemy drops, quest grants, world pickup visuals |
| [Character System](character-system.md) | Requirements and design of the modular character: shared skeleton, swappable skinned body parts, two-tier sockets, runtime API, placeholder asset generation and preview tooling |
| [Character Locomotion](character-locomotion.md) | Requirements and design of movement-driven animation: run blend on a `Speed` parameter and shortest-arc facing toward the travel direction, wired between the movement controller and the assembled rig |

## Conventions

- One document per subsystem.
- Each document states requirements first, then describes the implementing design with file references.
- Do not document planned/unimplemented behavior except in a clearly marked "Known limitations / open points" section.
