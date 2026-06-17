# Mutation Subsystem — Requirements & Design

> The mutation subsystem is the M1 core loop: artifacts the player eats accumulate toward creature
> archetypes (Reptile, Insect, Aquatic, …), and those archetypes drive stage-up body-part mutations.
> The loop is now closed end-to-end: the authorable archetype set, the per-artifact archetype weights
> that feed it, the per-stage `MutationTally` that aggregates fed profiles, the `DigestionProgress`
> that tracks how close the stage is to a mutation, and the **stage-up mutation choice** that consumes
> the ready signal — offering body-part options derived from the dominant archetype(s), swapping the
> chosen part on the live character, and resetting the tally + digestion for the next stage. Feeding
> the tally/digestion is wired through the Inventory feeding UI (see `inventory-subsystem.md`).
> Part-derived ability grants are now wired: a swapped part changes the character's combat ability
> set, because combat rebuilds that set from the live equipped parts at combat start (see
> ability-subsystem.md §2.6). Status: current as of 2026-06-17.
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
    MutationOption.cs           — one offered swap (slotId, partId, archetypeId, displayName)
    IMutationOptionProvider.cs  — archetypeId -> candidate options contract
    IMutationOptionBuilder.cs / MutationOptionBuilder.cs — picks 2-3 options from the dominant archetypes
    IMutationCharacter.cs       — port to swap a body part on the live character (no MonoBehaviour in Core)
  Data/                         — ScriptableObject definitions + the only SO -> Core bridge
    Definitions/
      ArchetypeDefinition.cs    — one creature archetype axis (SO)
      ArchetypeWeight.cs        — one authored (archetypeId, weight) pair (serializable)
      MutationConfig.cs         — subsystem tunables (digestion threshold, max options) (SO)
      ArchetypePartSetDefinition.cs — one archetype's body-part options (SO; inline MutationOptionEntry)
    IArchetypeCatalog.cs / ArchetypeCatalog.cs — id -> definition lookup, fail-fast
    IMutationOptionCatalog.cs / MutationOptionCatalog.cs — archetypeId -> options + part icon, fail-fast
    ArtifactArchetypeMapper.cs  — ArchetypeWeight[] -> ArtifactArchetypeProfile
    MutationOptionMapper.cs     — ArchetypePartSetDefinition -> MutationOption[]
  Infrastructure/
    ModularCharacterMutationAdapter.cs — IMutationCharacter over the scene's ModularCharacterVisual
  Presenter/
    MutationChoicePresenter.cs  — shows the choice when ready, applies the swap, resets the stage
  View/
    IMutationChoiceView.cs / MutationChoiceView.cs — thin choice-panel adapter
    MutationChoiceButton.cs / MutationChoiceViewData.cs — one choice button + its view DTO
  Application/
    MutationContentValidator.cs — startup authoring check (IInitializable)
Scripts/Core/DI/MutationInstaller.cs
Scripts/Editor/Mutation/MutationChoiceUISetup.cs — one-click in-scene choice panel + button prefab
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
| `MutationOption` | Immutable offered swap: `SlotId`, `PartId`, `ArchetypeId`, `DisplayName`. UnityEngine-free; the presenter resolves icon/tint at the view boundary. |
| `IMutationOptionProvider` / `IMutationOptionCatalog` | `archetypeId → MutationOption[]` (authored order); the catalog adds `TryGetIcon(partId)` and throws on empty/duplicate archetype ids. |
| `IMutationOptionBuilder` / `MutationOptionBuilder` | Picks ≤`maxOptions` options across the dominant archetypes in weight order, deduping shared parts (first archetype wins) and excluding already-equipped parts. Deterministic, no LINQ. |
| `IMutationCharacter` | Port the choice uses to `SwapPart(slotId, partId)` and query the equipped part; keeps MonoBehaviours out of Core. |
| `IArchetypeCatalog` / `ArchetypeCatalog` | `archetypeId → ArchetypeDefinition` lookup; throws on empty/duplicate ids at construction. |
| `ArtifactArchetypeMapper` | Pure adapter from `ArchetypeWeight[]` to `ArtifactArchetypeProfile`. |
| `MutationOptionMapper` | Pure adapter from `ArchetypePartSetDefinition` to `MutationOption[]` (the SO→Core bridge for options; icons stay in Data). |

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
mutation choice consult; the digestion progress is the "ready to mutate?" signal.

### 2.4 Stage-up mutation choice

`MutationChoicePresenter` (NonLazy, subscribed to `IDigestionProgress.OnChanged`) closes the loop:

1. **Trigger.** When `IsReadyToMutate` flips true (and a choice is not already showing), the presenter
   takes the dominant archetypes (`IMutationTally.Dominant(MutationConfig.MaxMutationOptions)`).
2. **Build options.** `MutationOptionBuilder.Build` walks those archetypes in weight order and each
   one's authored options in order, deduping a part shared by two archetypes (first wins) and
   excluding parts already equipped (`IMutationCharacter.TryGetEquippedPartId`), up to
   `MaxMutationOptions`. If the result is **empty** (the dominant archetypes have no new parts), the
   presenter logs and returns **without** resetting — the player keeps feeding.
3. **Present.** Each `MutationOption` becomes a `MutationChoiceViewData` (label from the option, tint
   from `ArchetypeDefinition.Tint`, icon from `IMutationOptionCatalog.TryGetIcon`); the view shows the
   panel.
4. **Apply.** On the player's pick, `IMutationCharacter.SwapPart(slotId, partId)` swaps the body part
   on the live character. A **failed** swap (e.g. the rig is not yet assembled) keeps the panel up and
   resets nothing. A **successful** swap hides the panel and then resets **both** the tally and the
   digestion (`Reset()`), starting the next stage.

The live character is reached through `ModularCharacterMutationAdapter`, which resolves the assembled
character from `ModularCharacterVisual.Character` lazily at call time. The equipped-part query reads
the character's live `IModularCharacter.EquippedParts` snapshot, so the choice excludes **every**
currently equipped part — including the character's *starting* parts, so a stage-1 choice never
re-offers a part the character already wears. The swap's effect on combat abilities is handled by
combat re-reading the equipped parts at combat start, not by the swap path (ability-subsystem.md
§2.6); the swap itself only changes the body.

### 2.5 DI wiring

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
- `IMutationOptionCatalog` → `MutationOptionCatalog` via `BindInterfacesAndSelfTo` + `AsSingle`;
  part-set definitions auto-load from `Resources/Mutation/PartSets` when the inspector list is empty.
- `IMutationOptionBuilder` → `MutationOptionBuilder` (`AsSingle`).
- `ModularCharacterVisual` resolved `FromComponentInHierarchy`; `IMutationCharacter` →
  `ModularCharacterMutationAdapter` (`AsSingle`).
- `IMutationChoiceView` → `MutationChoiceView` **instantiated from a prefab** via
  `FromComponentInNewPrefab`; the panel prefab auto-loads from `Resources/Prefabs/UI/MutationChoicePanel`
  (or an inspector override). This mirrors `InventoryInstaller`'s HUD-view binding and needs **no scene
  authoring**. If the prefab is missing the installer **logs a warning and skips** the view + presenter
  (the rest of the scene runs) — it never crashes the `SceneContext` over an unbuilt panel.
- `MutationChoicePresenter` via `BindInterfacesAndSelfTo` + `NonLazy` (so it subscribes to the ready
  signal at startup) — bound only when the panel prefab exists.
- `MutationContentValidator` via `BindInterfacesAndSelfTo` + `NonLazy`; it now also receives the raw
  part-set list and `IPartCatalog` (CharacterSystem) to validate options.
- `IGameLogger` is **not** bound here — `InventoryInstaller` provides the single `UnityGameLogger`
  for the shared `SceneContext` (re-binding `AsSingle` for the same concrete type trips Zenject 6).

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
| `MaxMutationOptions` | int | How many mutation options to offer at a stage-up (fewer if the dominant archetypes lack parts). | `3`; `[Min(1)]` |

### `ArchetypePartSetDefinition`  (asset menu: `Create → Mutation → Archetype Part Set`)

One asset per archetype, loaded from `Resources/Mutation/PartSets/` (or wired into the
`MutationInstaller` `_partSetDefinitions` list). Empty/duplicate archetype ids fail fast in
`MutationOptionCatalog`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `ArchetypeId` | string | Which `ArchetypeDefinition.Id`'s dominance offers these parts. | empty → fails fast |
| `Options` | `MutationOptionEntry[]` | Body-part options, tried in authored order. | — |

`MutationOptionEntry` (inline):

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `SlotId` | string | `SlotDefinition.Id` the part fills (e.g. `slot.head`). | validated against the part's slot |
| `PartId` | string | `PartDefinition.Id` swapped in when chosen (e.g. `part.head.b`). | resolved via `IPartCatalog`; unknown → warning |
| `DisplayName` | string | Label on the choice button. | — |
| `Icon` | Sprite | Optional icon on the choice button. | none |

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### Add an archetype

1. `Create → Mutation → Archetype` to make an `ArchetypeDefinition` asset under
   `Resources/Mutation/Archetypes/`.
2. Set a unique `Id` (lower-case, stable), `DisplayName`, `Description`, and `Tint`.
3. That's it — the `MutationInstaller` auto-loads it. Duplicate or empty ids fail fast at startup.

### Tune digestion / choice count

1. Select `Resources/Mutation/MutationConfig.asset`.
2. Set **Digestion Threshold** — how many artifacts the player must feed in one stage before the
   feeding UI reports "ready to mutate" — and **Max Mutation Options** (how many choices a stage-up
   offers). That's it; the `MutationInstaller` loads it automatically.

### Add a body-part option set for an archetype

1. `Create → Mutation → Archetype Part Set` to make an `ArchetypePartSetDefinition` under
   `Resources/Mutation/PartSets/`. One asset per archetype.
2. Set `Archetype Id` to an existing `ArchetypeDefinition.Id`.
3. Under **Options**, add an entry per body part this archetype can grant: `Slot Id` and `Part Id`
   must match an existing `PartDefinition` (`Part Id`) and its slot (see `character-system.md`),
   plus a `Display Name` and optional `Icon`. Order matters — earlier entries are offered first.
4. The `MutationInstaller` auto-loads it. `MutationContentValidator` warns at startup if the
   archetype, part, or slot does not resolve.

### Wire the stage-up choice panel into the scene

Run **Tools → Mutation → Setup Stage-Up Choice UI** once: it builds two standalone prefabs under
`Resources/Prefabs/UI/` — `MutationChoicePanel.prefab` (a screen-space overlay canvas with the
`MutationChoiceView`) and the `MutationChoiceButton.prefab` it instantiates. The installer loads the
panel from `Resources` and instantiates it at runtime, so **no scene wiring is needed**. Until the
prefab exists the stage-up choice is simply disabled (one startup warning, no crash). Idempotent —
rerun to rebuild.

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
- `MutationOptionBuilderTests` — gathers across dominant archetypes in weight then authored order;
  caps at `maxOptions`; excludes equipped parts; dedupes a part shared by two archetypes (first wins);
  skips empty part ids; empty on no data / non-positive max / null args; deterministic across runs.
- `MutationChoicePresenterTests` — not-ready shows nothing; ready flips → shows choices + visible;
  selection swaps then resets tally + digestion and hides; a failed swap resets nothing and stays
  visible; ready-but-no-options shows/resets nothing; already-equipped parts are excluded; further
  feeding while shown does not re-show.

---

## 6. Known limitations / open points

**Terminology:** the character's mutation progression advances in discrete **stages** (Stage 1 →
mutate → Stage 2 → …). "Stage" is used here instead of "level" to avoid confusion with character
XP / combat / area-scene levels. The `MutationTally` and `DigestionProgress` accumulate within the
current stage; the stage-up choice resets both to start the next stage.

- **Part-derived abilities are granted via combat re-reading equipped parts** (M1, done). The stage-up
  choice swaps the body part; combat then rebuilds the unit's active + passive ability set from the
  live equipped parts at the next combat start (`PartAbilityResolver`; ability-subsystem.md §2.6).
  The swap path itself pushes nothing into combat — it is a pull-at-init.
- The stage-up choice panel prefab must be built once (run **Tools → Mutation → Setup Stage-Up Choice
  UI**); until then the choice is disabled (a startup warning, no crash). Default
  `ArchetypePartSetDefinition` assets now ship for all five archetypes under `Resources/Mutation/PartSets/`
  (placeholder `.a → .b` swaps); only `reptile`/`insect`/`aquatic` are reachable today since no shipped
  artifact pushes `mammal`/`avian`.
- `ArchetypeDefinition.Tint` now tints the feeding readout and the choice buttons, but no other
  mutation UI uses it.
- No archetype currently has a `mammal`/`avian` artifact source in the shipped content; both are
  authorable and ready, just unused until more artifacts are added.
