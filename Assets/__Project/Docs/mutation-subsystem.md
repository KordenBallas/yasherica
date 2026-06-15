# Mutation Subsystem — Requirements & Design

> The mutation subsystem is the M1 core loop: artifacts the player eats accumulate toward creature
> archetypes (Reptile, Insect, Aquatic, …), and those archetypes drive stage-up body-part mutations.
> This document describes the **implemented surface so far** — the authorable archetype set, the
> per-artifact archetype weights that feed it, the per-stage `MutationTally` that aggregates fed
> profiles, and the `DigestionProgress` that tracks how close the stage is to a mutation. Feeding the
> tally/digestion is wired through the Inventory feeding UI (see `inventory-subsystem.md`); the
> remaining consumption step (the stage-up mutation choice that resets both) is planned and lives in §6.
> Status: current as of 2026-06-15.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** Creature archetypes are an **authorable set**, not a hard-coded enum: a designer adds an
  archetype by creating one ScriptableObject asset, never by editing code.
- **R2** Each artifact may contribute a **weight to one or more archetype axes**, expressing how
  strongly eating it pushes the character toward that archetype.
- **R3** Artifact archetype weights are authored on the existing `ArtifactDefinition`, so an artifact
  and its mutation contribution live in one asset.
- **R4** Authoring mistakes are surfaced, not silently swallowed: an artifact referencing an unknown
  archetype id, or an empty id, produces a startup warning.
- **R5** Aggregating an artifact's weights is deterministic and order-independent: duplicate ids are
  summed; empty ids and non-positive weights are dropped.

### 1.2 Non-functional requirements

- **N1** The aggregation/result type (`ArtifactArchetypeProfile`) is pure C# (no UnityEngine) and
  unit-tested.
- **N2** All dependencies wired through Zenject; no service locators.
- **N3** Archetypes are data-only ScriptableObjects; the only SO→Core bridge is the mapper.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Mutation/
  Core/                         — pure C#, no UnityEngine
    ArtifactArchetypeProfile.cs — immutable archetypeId -> weight snapshot + aggregation rules
                                  (Create from raw entries; Combine to sum several profiles)
    IMutationTally.cs           — per-stage archetype-weight accumulator contract
    MutationTally.cs            — live aggregate of fed profiles for the current mutation stage
    IDigestionProgress.cs       — per-stage "how close to mutating" contract
    DigestionProgress.cs        — count of artifacts fed this stage vs. the threshold
  Data/                         — ScriptableObject definitions + the only SO -> Core bridge
    Definitions/
      ArchetypeDefinition.cs    — one creature archetype axis (SO)
      ArchetypeWeight.cs        — one authored (archetypeId, weight) pair (serializable)
      MutationConfig.cs         — subsystem tunables (digestion threshold) (SO)
    IArchetypeCatalog.cs / ArchetypeCatalog.cs — id -> definition lookup, fail-fast
    ArtifactArchetypeMapper.cs  — ArchetypeWeight[] -> ArtifactArchetypeProfile
  Application/
    MutationContentValidator.cs — startup authoring check (IInitializable)
Scripts/Core/DI/MutationInstaller.cs
```

`ArtifactDefinition` (Inventory) owns the authored `ArchetypeWeight[]`; it references archetypes only
by string id, so Inventory does not depend on the archetype *catalog*, only on the small
`ArchetypeWeight` data type.

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `ArtifactArchetypeProfile` | Immutable `archetypeId → weight` map for one artifact; `Create` applies the aggregation rules (R5); `Combine(profiles)` sums several profiles into one (cumulative feeding-tray readout) by re-applying `Create`; `WeightFor`, `IsEmpty`, `Empty`. |
| `IMutationTally` / `MutationTally` | Mutable live aggregate of the profiles fed during the **current mutation stage**; `Add(profile)` sums weights, `TotalFor`, `Dominant(count)` (top-N by weight, ordinal-id tie-break), `Reset()`, `OnChanged`. |
| `IDigestionProgress` / `DigestionProgress` | Per-stage count of digested artifacts vs. the `MutationConfig.DigestionThreshold`; `AddArtifact()` (+1), `Fed`, `Threshold`, `Normalized` (0..1), `IsReadyToMutate`, `Reset()`, `OnChanged`. Decides **when** a mutation is available; the tally decides **which** archetype it leans toward. |
| `ArchetypeWeight` | One authored contribution: `ArchetypeId`, `Weight`. Serialized on `ArtifactDefinition`. |
| `IArchetypeCatalog` / `ArchetypeCatalog` | `archetypeId → ArchetypeDefinition` lookup; throws on empty/duplicate ids at construction. |
| `ArtifactArchetypeMapper` | Pure adapter from `ArchetypeWeight[]` to `ArtifactArchetypeProfile`. |

### 2.3 Runtime flow

At install the `MutationInstaller` loads every `ArchetypeDefinition`, binds an `ArchetypeCatalog`,
loads `MutationConfig`, and binds the `MutationTally` and `DigestionProgress` (one each per
container). After all installers run, `MutationContentValidator` (NonLazy `IInitializable`) iterates
the artifact catalog and warns about any archetype weight whose id is empty or absent from the
archetype catalog.

The live consumer path runs through the Inventory **feeding UI** (see `inventory-subsystem.md`): for
each artifact the player digests, the feeding presenter calls
`ArtifactArchetypeMapper.ToProfile(artifact.ArchetypeWeights)` → `IMutationTally.Add(profile)` and
`IDigestionProgress.AddArtifact()`. The tally is the model the feeding readout and the stage-up
mutation choice consult; the digestion progress is the "ready to mutate?" signal. **What is not
wired yet:** nothing *calls* `Reset` on either — the stage-up mutation choice drives that, and
`IsReadyToMutate` is surfaced but not yet consumed to trigger a mutation. See §6.

### 2.4 DI wiring

`Scripts/Core/DI/MutationInstaller.cs` (a `MonoInstaller` added to the Area scene's `SceneContext`):

- `IArchetypeCatalog` → `ArchetypeCatalog`; archetype definitions auto-load from
  `Resources/Mutation/Archetypes` when the inspector list is empty.
- `MutationConfig` instance bound via `BindInstance`; loaded from `Resources/Mutation/MutationConfig`
  when the inspector field is empty (missing config fails fast).
- `IMutationTally` → `MutationTally` via `BindInterfacesAndSelfTo` + `AsSingle` (shared by the
  feeding UI and the stage-up mutation choice). Not `NonLazy`: it has no startup side effect and is
  created when its first consumer resolves.
- `IDigestionProgress` → `DigestionProgress` via `BindInterfacesAndSelfTo` + `AsSingle`, constructed
  with `MutationConfig.DigestionThreshold`.
- `MutationContentValidator` via `BindInterfacesAndSelfTo` + `NonLazy` (so validation always runs).
- `IGameLogger` → `UnityGameLogger` with `IfNotBound` (shared with the other installers).

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

### `ArchetypeDefinition`  (asset menu: `Create → Mutation → Archetype`)

Loaded from `Resources/Mutation/Archetypes/` (or wired into the `MutationInstaller` list field).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `Id` | string | Stable identity referenced by `ArchetypeWeight.ArchetypeId`. | empty id fails fast in `ArchetypeCatalog` |
| `DisplayName` | string | Human-readable name for UI. | — |
| `Description` | string | Flavor / designer note. | `[TextArea]` |
| `Tint` | Color | Accent colour for later mutation UI. | white |

### `ArchetypeWeight`  (serialized inline on `ArtifactDefinition._archetypeWeights`)

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `ArchetypeId` | string | Which `ArchetypeDefinition.Id` this weight contributes to. | empty → ignored + warning |
| `Weight` | float | Strength of the contribution. | `1`; ≤ 0 ignored |

Shipped archetypes: `reptile`, `insect`, `aquatic`, `mammal`, `avian`.

### `MutationConfig`  (asset menu: `Create → Mutation → Mutation Config`)

Single asset loaded from `Resources/Mutation/MutationConfig.asset` (or wired into the
`MutationInstaller` `_config` field).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `DigestionThreshold` | int | Artifacts that must be fed in a stage before `IDigestionProgress.IsReadyToMutate` is true. | `5`; `[Min(1)]` |

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### Add an archetype

1. `Create → Mutation → Archetype` to make an `ArchetypeDefinition` asset under
   `Resources/Mutation/Archetypes/`.
2. Set a unique `Id` (lower-case, stable), `DisplayName`, `Description`, and `Tint`.
3. That's it — the `MutationInstaller` auto-loads it. Duplicate or empty ids fail fast at startup.

### Tune digestion

1. Select `Resources/Mutation/MutationConfig.asset`.
2. Set **Digestion Threshold** — how many artifacts the player must feed in one stage before the
   feeding UI reports "ready to mutate". That's it; the `MutationInstaller` loads it automatically.

### Set an artifact's archetype weights

1. Select an `ArtifactDefinition` under `Resources/Artifacts/Definitions/`.
2. Under **Mutation → Archetype Weights**, add an entry per archetype the artifact should push
   toward; set `Archetype Id` to an existing `ArchetypeDefinition.Id` and a `Weight`.
3. Leave the list empty for artifacts that grant no mutation pull (e.g. `fire`, `rock`).

**Authoring constraints / gotchas:** an `ArchetypeId` that matches no archetype, or is empty,
is skipped at runtime and logged by `MutationContentValidator`; duplicate ids on one artifact are
summed; weights ≤ 0 are ignored.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `ArtifactArchetypeMapperTests` — null/empty input → empty profile; weights preserved; duplicate
  ids summed; empty/zero/negative entries dropped; ids trimmed; null entries skipped.
- `ArtifactArchetypeProfileCombineTests` — null/empty sequence → empty; weights summed across
  profiles; duplicate ids across profiles summed; null and empty profiles skipped.
- `ArchetypeCatalogTests` — `TryGet`/`Contains`/`All`; throws on empty id, duplicate id, and null list.
- `DigestionProgressTests` — throws on non-positive threshold; empty on construct; `AddArtifact`
  increments and raises `OnChanged`; `Normalized` clamps to 1 past threshold; `IsReadyToMutate` flips
  at threshold; `Reset` clears and raises `OnChanged` only when non-empty.
- `MutationTallyTests` — empty on construction; `Add` accumulates and sums across calls; null/empty
  profiles are no-ops that don't raise `OnChanged`; `TotalFor` of unknown/empty/null id → 0;
  `Dominant` orders by weight desc with ordinal-id tie-break and clamps `count`; `Reset` clears and
  raises `OnChanged` only when it was non-empty.

---

## 6. Known limitations / open points

> **Planned design (NOT implemented).** The archetype data surface, the per-stage `MutationTally`,
> the `DigestionProgress`, and the feeding UI that fills them exist today; the stage-up mutation
> choice that consumes them does not.

**Terminology:** the character's mutation progression advances in discrete **stages** (Stage 1 →
mutate → Stage 2 → …). "Stage" is used here instead of "level" to avoid confusion with character
XP / combat / area-scene levels. The `MutationTally` and `DigestionProgress` accumulate within the
current stage; `Reset()` on each starts the next stage.

- The feeding UI now fills the tally (`Add`) and the digestion progress (`AddArtifact`), but nothing
  **resets** them yet and nothing acts on `IsReadyToMutate`: there is **no stage-up mutation choice**
  calling `Reset` or turning the ready signal into an actual body-part mutation. See ROADMAP
  "Mutation Subsystem" for the next M1 step.
- `ArchetypeDefinition.Tint` now tints the feeding readout entries, but no other mutation UI uses it.
- No archetype currently has a `mammal`/`avian` artifact source in the shipped content; both are
  authorable and ready, just unused until more artifacts are added.
