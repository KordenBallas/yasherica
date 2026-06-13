# <System Name> — Requirements & Design

> One-paragraph summary: what this system does and where it sits in the game.
> Status: current as of YYYY-MM-DD.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

Number every requirement (`R1`, `R2`, …) so other docs, the CHANGELOG, and tests can reference them.

- **R1** …
- **R2** …

### 1.2 Non-functional requirements

- **N1** Core logic is pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject; no service locators.
- **N3** Data definitions are data-only ScriptableObjects.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/<System>/
  Core/         — pure C#, no UnityEngine
  Data/         — ScriptableObject definitions + mappers (the only SO -> Core bridge)
  Application/  — use cases / presenters / services
  View/         — MonoBehaviour adapters
Scripts/Core/DI/<System>Installer.cs
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| … | … |

### 2.3 Runtime flow

Describe the main sequence (creation, the key operation, teardown) in enough detail to extend safely.

### 2.4 DI wiring

Which installer, what it binds, where assets auto-load from when inspector fields are empty.

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

One subsection per SO type the system owns. A designer must be able to author content from this
section alone.

### `XxxDefinition`  (asset menu: `Create → <System> → Xxx`)

Loaded from `Resources/<System>/Xxx/` (or: wired into `<System>Installer` list field).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `Id` | string | Stable identity; referenced by … | empty = excluded |
| … | … | … | … |

Referenced assets this SO points at: e.g. Ink JSON, icon `Sprite`, prefab, mesh — list each and what
it must contain (e.g. "Ink file must define a `start` knot").

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

Asset-only recipes. No code changes. One numbered recipe per content type this system exposes.

### Add a <content type> (e.g. ability / artifact / biome / NPC / story / body part)

1. Create the referenced assets (icon / Ink JSON / prefab / mesh / material).
2. `Create → <System> → Xxx` to make the `XxxDefinition` asset; fill every field per §3.
3. Place it under `Resources/<System>/Xxx/` **or** add it to the `<System>Installer` list in the scene.
4. (If applicable) wire cross-system references by id (e.g. `RewardDefinition.ItemId == ArtifactDefinition.Id`).
5. Validate: run the relevant editor tool / preview window / enter play mode and confirm.

**Authoring constraints / gotchas:** list the validation rules that will reject bad content
(mismatched ids, missing bones, combat story without a combat-capable NPC, etc.).

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `XxxTests` — what is covered.

State which Unity-side classes are verified manually (and how: demo bootstrap, preview window).

---

## 6. Known limitations / open points

Bullet each gap. Every bullet here should have a matching ROADMAP item (CLAUDE.md §8.3). Mark
forward-looking designs explicitly:

> **Planned design (NOT implemented).** …
