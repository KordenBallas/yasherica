# Mutation Subsystem — Requirements & Design

> The mutation subsystem is the M1 core loop: artifacts the player eats accumulate toward creature
> archetypes (Reptile, Insect, Aquatic, …), and those archetypes drive stage-up body-part mutations.
> The loop is now closed end-to-end: the authorable archetype set, the per-artifact archetype weights
> that feed it, the per-stage `MutationTally` that aggregates fed profiles, the `DigestionProgress`
> that tracks how close the stage is to a mutation, and the **stage-up mutation choice** that consumes
> the ready signal — scoring every candidate body part against the cumulative feed tally and offering
> the top-scoring options, swapping the chosen part on the live character, and resetting the tally +
> digestion for the next stage. Feeding the tally/digestion is wired through the Inventory feeding UI
> (see `inventory-subsystem.md`). Each body part carries its own archetype-affinity vector, rarity
> tier, and choice icon (on the CharacterSystem `PartDefinition`); the scoring ranks parts by how well
> their affinity matches what was fed, with a rarity bonus that ramps up as the player accumulates
> points (M2). Part-derived ability grants are wired: a swapped part changes the character's combat
> ability set, because combat rebuilds that set from the live equipped parts at combat start (see
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
    MutationCandidatePart.cs    — a scorable part (slot/part ids, affinity map, rarity tier, dominant id)
    MutationScoringParameters.cs— scoring tunables struct (rarity weight, unlock points/tier)
    IMutationOptionBuilder.cs / MutationOptionBuilder.cs — scores all candidate parts, returns the top-N
    IMutationCharacter.cs       — port to swap a body part on the live character (no MonoBehaviour in Core)
  Data/                         — ScriptableObject definitions + the only SO -> Core bridge
    Definitions/
      ArchetypeDefinition.cs    — one creature archetype axis (SO)
      ArchetypeWeight.cs        — one authored (archetypeId, weight) pair (serializable)
      MutationConfig.cs         — subsystem tunables (digestion threshold, max options, scoring) (SO)
    IArchetypeCatalog.cs / ArchetypeCatalog.cs — id -> definition lookup, fail-fast
    IMutationPartCatalog.cs / MutationPartCatalog.cs — builds candidate parts from the character part catalog + part icon
    ArtifactArchetypeMapper.cs  — ArchetypeWeight[] -> ArtifactArchetypeProfile
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

`PartDefinition` (CharacterSystem) owns the per-part mutation data — the `ArchetypeAffinity[]` vector,
the `MutationRarity` tier, and the `ChoiceIcon` — alongside its body/ability data. Hosting these
Mutation concepts on the character layer is a user-approved authoring convenience (M2); affinity and
rarity reference no Mutation type (id strings + a plain enum), so the Mutation Core stays decoupled.
The layering trade-off is recorded in §6 and the ROADMAP. `MutationPartCatalog` is the only bridge
from these authored parts into the UnityEngine-free `MutationCandidatePart` records.

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `ArtifactArchetypeProfile` | Immutable `archetypeId → weight` map for one artifact; `Create` applies the aggregation rules (R5); `Combine(profiles)` sums several profiles into one (cumulative feeding-tray readout) by re-applying `Create`; `WeightFor`, `IsEmpty`, `Empty`. |
| `IMutationTally` / `MutationTally` | Mutable live aggregate of the profiles fed during the **current mutation stage**; `Add(profile)` sums weights, `TotalFor`, `Dominant(count)` (top-N by weight, ordinal-id tie-break), `Reset()`, `OnChanged`. |
| `IDigestionProgress` / `DigestionProgress` | Per-stage count of digested artifacts vs. the `MutationConfig.DigestionThreshold`; `AddArtifact()` (+1), `Fed`, `Threshold`, `Normalized` (0..1), `IsReadyToMutate`, `Reset()`, `OnChanged`. Decides **when** a mutation is available; the tally decides **which** archetype it leans toward. |
| `ArchetypeWeight` | One authored contribution: `ArchetypeId`, `Weight`. Serialized on `ArtifactDefinition`. |
| `MutationOption` | Immutable offered swap: `SlotId`, `PartId`, `ArchetypeId` (the part's dominant-affinity archetype, for tint only), `DisplayName`. UnityEngine-free; the presenter resolves icon/tint at the view boundary. |
| `MutationCandidatePart` | A scorable part in UnityEngine-free terms: `SlotId`, `PartId`, `DisplayName`, `Affinity` (archetype id → weight), `RarityTier` (int; Common = 0), `DominantArchetypeId`. Built by the Data layer from a `PartDefinition`. |
| `MutationScoringParameters` | Scoring tunables struct (from `MutationConfig`): `RarityWeight`, `RarityUnlockPointsPerTier`. |
| `IMutationOptionBuilder` / `MutationOptionBuilder` | Scores every candidate part against the feed tally — `score = (affinity·tally) × (1 + RarityWeight·tier·unlock)` — excludes equipped parts and parts with no affinity to anything fed (score 0), and returns the top ≤`maxOptions` by descending score with an ordinal part-id tie-break. Deterministic, no LINQ. |
| `IMutationCharacter` | Port the choice uses to `SwapPart(slotId, partId)` and query the equipped part; keeps MonoBehaviours out of Core. |
| `IArchetypeCatalog` / `ArchetypeCatalog` | `archetypeId → ArchetypeDefinition` lookup; throws on empty/duplicate ids at construction. |
| `IMutationPartCatalog` / `MutationPartCatalog` | Builds `MutationCandidatePart[]` from the CharacterSystem `IPartCatalog` (summing duplicate affinity ids, dropping empty ids / non-positive weights, computing the dominant archetype), and serves the per-part `ChoiceIcon` via `TryGetIcon`. The only SO→Core bridge for candidate parts. |
| `ArtifactArchetypeMapper` | Pure adapter from `ArchetypeWeight[]` to `ArtifactArchetypeProfile`. |

### 2.3 Runtime flow

At install the `MutationInstaller` loads every `ArchetypeDefinition`, binds an `ArchetypeCatalog`,
loads `MutationConfig`, and binds the `MutationTally` and `DigestionProgress` (one each per
container). After all installers run, `MutationContentValidator` (NonLazy `IInitializable`) iterates
the artifact catalog and warns about any archetype weight whose id is empty or absent from the
archetype catalog, and iterates the character part catalog warning about any part affinity whose
archetype id is empty or unknown.

The live consumer path runs through the Inventory **feeding UI** (see `inventory-subsystem.md`): for
each artifact the player digests, the feeding presenter calls
`ArtifactArchetypeMapper.ToProfile(artifact.ArchetypeWeights)` → `IMutationTally.Add(profile)` and
`IDigestionProgress.AddArtifact()`. The tally is the model the feeding readout and the stage-up
mutation choice consult; the digestion progress is the "ready to mutate?" signal.

### 2.4 Stage-up mutation choice

`MutationChoicePresenter` (NonLazy, subscribed to `IDigestionProgress.OnChanged`) closes the loop:

1. **Trigger.** When `IsReadyToMutate` flips true (and a choice is not already showing), the presenter
   reads the cumulative feed tally (`IMutationTally.Totals`) and the full candidate-part set
   (`IMutationPartCatalog.AllCandidates`).
2. **Build options.** `MutationOptionBuilder.Build` scores every candidate against the tally —
   `score = (affinity·tally) × (1 + RarityWeight·tier·unlock)`, where `unlock` ramps from 0 to 1 as
   accumulated points approach `tier × RarityUnlockPointsPerTier` — excludes parts already equipped
   (`IMutationCharacter.TryGetEquippedPartId`, gathered once per candidate slot) and parts that score
   zero (no affinity to anything fed), then returns the top `MaxMutationOptions` by descending score
   with an ordinal part-id tie-break. If the result is **empty** (no unequipped part scores), the
   presenter logs and returns **without** resetting — the player keeps feeding.
3. **Present.** Each `MutationOption` becomes a `MutationChoiceViewData` (label from the option, tint
   from the part's dominant-affinity `ArchetypeDefinition.Tint`, icon from
   `IMutationPartCatalog.TryGetIcon`); the view shows the panel.
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
- `IMutationPartCatalog` → `MutationPartCatalog` via `BindInterfacesAndSelfTo` + `AsSingle`;
  constructor-injects the CharacterSystem `IPartCatalog` (bound by `CharacterSystemInstaller` in the
  same `SceneContext`) and builds its candidate parts from it. No part-set assets to load.
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
- `MutationContentValidator` via `BindInterfacesAndSelfTo` + `NonLazy`; it receives `IArtifactCatalog`,
  `IArchetypeCatalog`, and the CharacterSystem `IPartCatalog` to validate artifact weights and part
  affinities.
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
| `MaxMutationOptions` | int | How many mutation options to offer at a stage-up (fewer if too few parts score). | `3`; `[Min(1)]` |
| `RarityWeight` | float | How strongly a part's rarity tier multiplies its score once unlocked. | `0.5`; `[Min(0)]` |
| `RarityUnlockPointsPerTier` | float | Accumulated archetype points required per rarity tier before that tier is favoured. | `10`; `[Min(0)]` |

### Per-part mutation data on `PartDefinition`  (CharacterSystem — `Create → Character System/Part`)

The mutation affinity/rarity/icon live on the body part itself (see `character-system.md` for the
full `PartDefinition` reference). `MutationPartCatalog` reads every part from the CharacterSystem
`IPartCatalog`; no per-archetype part-set assets exist.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `ArchetypeAffinities` | `ArchetypeAffinity[]` | Per-archetype affinity (`ArchetypeId` + `Weight` 0..1) scored against the feed tally. Duplicate ids summed; empty ids / weights ≤ 0 dropped. | empty → part never offered (no affinity) |
| `Rarity` | `MutationRarity` | Rarity tier (`Common`…`Mythical`, ordinal 0..5); higher tiers are favoured once enough points accumulate. | `Common` |
| `ChoiceIcon` | Sprite | Icon shown on the choice button when this part is offered. | none |
| `DisplayName` | string | Friendly label on the choice button (general `PartDefinition` field). | empty → falls back to the asset name |

`MutationContentValidator` warns at startup if a part affinity's archetype id is empty or unknown.

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
   feeding UI reports "ready to mutate" — **Max Mutation Options** (how many choices a stage-up
   offers), and the scoring tunables **Rarity Weight** / **Rarity Unlock Points Per Tier** (how much
   rarity boosts a part's score, and how many accumulated archetype points unlock each tier). That's
   it; the `MutationInstaller` loads it automatically.

### Make a body part a mutation option (author its affinity, rarity & icon)

A part becomes a stage-up option purely by its own mutation data — there are no per-archetype set
assets. On the `PartDefinition` asset (see `character-system.md` for creating one):

1. Under **Mutation (part-driven affinity)** → **Archetype Affinities**, add an entry per archetype
   the part leans toward: `Archetype Id` (an existing `ArchetypeDefinition.Id`) and `Weight` (0..1).
   A part is only ever offered when something it has affinity for has been fed; mixed affinities are
   fine (e.g. reptile 0.8 / aquatic 0.2). Duplicate ids are summed; empty ids / weights ≤ 0 dropped.
2. Set **Rarity** — rarer tiers score higher, but only once the player has accumulated enough points
   (tuned by `MutationConfig.RarityUnlockPointsPerTier`).
3. Optionally set **Choice Icon** (shown on the choice button) and **Display Name** (the button's
   label; falls back to the asset name when blank).
4. No registration step: `MutationPartCatalog` reads every part from the part catalog automatically.
   `MutationContentValidator` warns at startup if an affinity's archetype id is empty or unknown.
   The choice-button tint comes from the part's highest-weight (dominant) archetype's `Tint`.

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
- `MutationOptionBuilderTests` — orders by affinity match against the tally; a rarer part wins with
  equal affinity; a lower-affinity rare part overtakes a common one as points accumulate; caps at
  `maxOptions`; excludes equipped parts; drops parts with no affinity to anything fed (score 0);
  ordinal part-id tie-break; deterministic; empty on null/empty tally, non-positive max, or null args.
- `MutationPartCatalogTests` — maps slot/part/rarity/affinity from a `PartDefinition`; dominant
  archetype is the highest weight; sums duplicate ids and drops empty ids / non-positive weights;
  `TryGetIcon` returns the authored icon; throws on a null part catalog.
- `MutationChoicePresenterTests` — not-ready shows nothing; ready flips → shows choices + visible;
  selection swaps then resets tally + digestion and hides; a failed swap resets nothing and stays
  visible; ready-but-no-scoring-options shows/resets nothing; already-equipped parts are excluded;
  further feeding while shown does not re-show.

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
  UI**); until then the choice is disabled (a startup warning, no crash). The shipped `*_B` body parts
  now carry placeholder affinity/rarity/icon data (migrated from the removed per-archetype part sets),
  so the `*_A → *_B` swaps remain reachable for `reptile`/`insect`/`aquatic`; `mammal`/`avian`
  affinities exist on parts but stay unreachable since no shipped artifact pushes them.
- **Layering trade-off (M2, accepted):** the per-part mutation data (`ArchetypeAffinity`,
  `MutationRarity`, `ChoiceIcon`) lives on the CharacterSystem `PartDefinition` for single-asset
  authoring, so Mutation concepts sit in the character layer (a §2 inward-only deviation, mirroring
  the accepted `PartDefinition → Combat` ability coupling). The Mutation Core stays decoupled
  (affinity is an id-string map, rarity an int tier). A future cleanup could host this on a
  Mutation-layer companion SO keyed by part id; deferred by user decision.
- `ArchetypeDefinition.Tint` tints the feeding readout and the choice buttons (the latter from each
  option's dominant-affinity archetype), but no other mutation UI uses it.
- No archetype currently has a `mammal`/`avian` artifact source in the shipped content; both are
  authorable and ready, just unused until more artifacts are added.
