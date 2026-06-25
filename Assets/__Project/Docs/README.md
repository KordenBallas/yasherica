# Project Documentation

This folder is the current, maintained documentation space for the project. Documents here describe systems **as implemented** — if code and a document disagree, the document is outdated and must be fixed.

Older documentation elsewhere in the repository may be stale; prefer this folder.

## Documents

| Document | Scope |
|---|---|
| [Ability Subsystem](ability-subsystem.md) | Requirements and design of combat abilities: shapes, targeting, scheduling queue, execution, data-driven authoring, extension guide |
| [Narrative Generation](narrative-generation.md) | **Superseded** by *Data-Driven Procedural Narrative*. Describes the deleted legacy generation path (story/NPC pools, density config, dialogue composition); kept for historical reference only |
| [Data-Driven Procedural Narrative](narrative-procedural.md) | Requirements and design of the recombinable narrative system: orthogonal fragments (archetype/dialogue/quest/enemy/story), casting layer, one unified namespaced fact store, precondition/effect machinery, run director, dialogue tag bridge, save boundary; vertical slice "The Toll at Razor Pass" |
| [Narrative Director — Requirements & Task Brief](narrative-director-requirements.md) | **Forward-looking design handoff (not as-implemented).** How the director should form the narrative (D1–D21: window/streaming, two clocks, reserved spine lane, two budgets, actor-as-continuity, threads-as-saga, passport gating, divergence/escalation/memory) + the Priority-1 task scope (actor/faction-scoped eligibility + recurring-actor casting). Folds into *Data-Driven Procedural Narrative* as built |
| [Inventory Subsystem](inventory-subsystem.md) | Requirements and design of the magic pot inventory: container model, bubble visualization, belly camera zoom, combine crafting with data-driven artifacts/recipes, combat guard |
| [Loot Subsystem](loot-subsystem.md) | Requirements and design of data-driven artifact rewards: biome loot tables, deterministic per-run rolls, platform discovery pickups, enemy drops, quest grants, world pickup visuals |
| [Character System](character-system.md) | Requirements and design of the modular character: shared skeleton, swappable skinned body parts, two-tier sockets, runtime API, placeholder asset generation and preview tooling |
| [Character Locomotion](character-locomotion.md) | Requirements and design of movement-driven animation: run blend on a `Speed` parameter and shortest-arc facing toward the travel direction, wired between the movement controller and the assembled rig |
| [Mutation Subsystem](mutation-subsystem.md) | Requirements and design of the M1 mutation data surface: authorable creature-archetype set and per-artifact archetype weights that feed the (planned) level-up mutation loop |
| [Character Progression](character-progression.md) | Requirements and design of the per-run progression record: quests (active/completed/failed), NPCs encountered, key choices, and condition evaluation that gates run-state-dependent content (e.g. reward slots) |

## Conventions

- One document per subsystem.
- Each document states requirements first, then describes the implementing design with file references.
- Do not document planned/unimplemented behavior except in a clearly marked "Known limitations / open points" section.
