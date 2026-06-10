# Project Documentation

This folder is the current, maintained documentation space for the project. Documents here describe systems **as implemented** — if code and a document disagree, the document is outdated and must be fixed.

Older documentation elsewhere in the repository may be stale; prefer this folder.

## Documents

| Document | Scope |
|---|---|
| [Ability Subsystem](ability-subsystem.md) | Requirements and design of combat abilities: shapes, targeting, scheduling queue, execution, data-driven authoring, extension guide |
| [Narrative Generation](narrative-generation.md) | Requirements and design of procedural level narrative: story/NPC pools and filtering, density config, combat-capability matching, rewards, dialogue composition, planned compatibility scoring |

## Conventions

- One document per subsystem.
- Each document states requirements first, then describes the implementing design with file references.
- Do not document planned/unimplemented behavior except in a clearly marked "Known limitations / open points" section.
