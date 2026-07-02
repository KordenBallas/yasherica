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
| [Quest Subsystem](quest-subsystem.md) | Requirements and design of the quest fragment, lifecycle, and rewards: offer/advance/complete/fail driven by Ink tags, fact effects gated against the quest's own footprint, progression-record bridge, and item rewards granted on completion |
| [Encounter Dialogue UI](encounter-dialogue-ui.md) | Requirements and design of the Hades-style encounter presentation: bottom-centre dialogue box with portrait + name, word-by-word line reveal (tap to complete), quest/attack/exit cards labelled with the job, and author-marked `[[ ]]` keyword highlighting — a presentation layer over the unchanged conversation engine |
| [Developer Tools](dev-tools.md) | The in-game developer overlay (editor / dev-build only): a key-toggled IMGUI panel with generic sections — quest statuses and director facts — built by a pure-C# presenter from live game state |
| [Logging](logging.md) | Per-system runtime logging: every line tagged with a `LogCategory`, a `LoggingConfig` SO with per-system + master verbosity ceilings (Off/Error/Warning/Info), gated through `IGameLogger`/`UnityGameLogger` so unrelated systems can be muted while testing a feature |
| [NPC Proximity Interaction](npc-proximity-interaction.md) | Approaching an NPC is deliberate: an **F** prompt + dialogue in an interaction radius for talkable NPCs, auto-battle in an aggro radius for hostile ones, always-visible `?`/`!` intent markers (derived from the placement-time casting, not an authored flag) and a name label above each NPC, two global radii config + a dev overlay |
| [Platform & Area Generation](platform-generation.md) | How island platforms are generated: a hex-composed top surface that IS the combat grid (one source of truth, never re-fitted), a non-walkable organic rim, per-content-kind size/shape profiles with a battlefield minimum (one `PlatformShapeConfig` SO), deterministic per run seed |

## Product requirement briefs

Verified, product-owner-level feature briefs (the intended behavior of a change, agreed before it
is built) live under [`product-requirements/`](product-requirements/README.md). They are the one
**exception** to the "as-implemented only" rule below: they describe intended behavior and are
handed to the code track. Once built, the as-implemented behavior is documented in the relevant
system doc above.

## Conventions

- One document per subsystem.
- Each document states requirements first, then describes the implementing design with file references.
- Do not document planned/unimplemented behavior except in a clearly marked "Known limitations / open points" section (the `product-requirements/` briefs above are the deliberate exception).
