# Platform & Area Generation — Requirements & Design

> How the world's island platforms are generated: each platform's top surface is composed of whole
> hex cells that ARE the combat grid (one source of truth, never re-fitted), wrapped in a
> non-walkable organic rim, sized and shaped by its content kind from one authored SO, fully
> deterministic per run seed. Covers the streaming layout path (`AreaGenerator` +
> `RunStreamingCoordinator`) and the combat-grid derivation.
> Status: current as of 2026-07-02.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

Source brief: `product-requirements/platform-hex-surface-and-shape.md` (verified 2026-07-02).

### 1.1 Functional requirements

- **R1** A platform's top surface is composed of **whole hex cells**; the combat grid is **derived
  from the same source** (no re-fit/re-snap at combat time). The walkable surface and the
  battlefield are identical.
- **R2** The tiling is **muted in traversal** (soft, implied) and **crisp in combat** (the cell
  boundaries read clearly) — an emphasis of the same ground, not a grid appearing from nowhere.
- **R3** **Every full hex cell lies on the top surface** — the playable interior is complete whole
  cells; combat is never clipped.
- **R4** Beyond the last full cell is a **non-playable decorative rim** giving the island its
  organic silhouette. The rim is dressing only — never walkable, never a combat cell.
- **R5** A platform's **extent and form come from its content kind** (Empty / Loot / Combat / NPC),
  each with a data-authored size/shape profile.
- **R6** A **combat-capable** platform (ambient monster, or a story whose combat slot is required)
  guarantees at least the **battlefield minimum** of whole cells.
- **R7** Non-combat platforms can be visibly smaller/narrower per their profile.
- **R8** **Determinism:** the same run seed produces the same platform shapes, sizes, and tiling.
- **R9** *(principle, seam only)* Biome features that affect the surface occupy **whole hex cells**
  and align to the grid. The data seam exists (`PlatformHexSurface.BlockedCells`); feature content
  itself belongs to the biome-appearance brief.

### 1.2 Non-functional requirements

- **N1** Surface generation logic is pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject; no service locators.
- **N3** All dials live on one data-only ScriptableObject (`PlatformShapeConfig`).

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Combat/Battlefield/Core/
  HexMetrics.cs            — shared axial↔local hex math (pure C#)
  PlatformHexSurface.cs    — THE source of truth: cells + outline + rim (pure C#)
Scripts/Combat/Battlefield/Grid/
  SurfaceHexGrid.cs        — IHexGrid derived 1:1 from the surface
Scripts/LevelGeneration/Surface/
  PlatformSurfaceGenerator — deterministic blob growth + hole fill (pure C#)
  HexOutlineExtractor      — hex-union boundary loop (pure C#)
  PlatformRimBuilder       — jittered organic rim ring (pure C#)
  PlatformShapeSettings    — UnityEngine-free dials record
  PlatformContentKind(+Resolver) — profile key from the graph node
Scripts/LevelGeneration/Data/
  PlatformShapeConfig(.Mapper) — the SO + the only SO→Core bridge
Scripts/LevelGeneration/Area/
  AreaGenerator            — layout cursor + surface growth + GameObject assembly
Scripts/Platform/Visual/
  PlatformHexSurfaceMeshBuilder — mesh from the surface (Unity side)
  PlatformColliderBuilder  — floor + per-outline-segment wall colliders (unchanged)
Scripts/Core/DI/AreaInstaller.cs — binds PlatformShapeSettings; builds CombatConfig from it
```

Layering note: the hex vocabulary (`HexCoordinates`, `HexOrientation`, `HexMetrics`,
`PlatformHexSurface`) lives in `Combat.Battlefield` and is consumed by `LevelGeneration` — the
dependency arrows are `LevelGeneration → Combat.Battlefield` and `Platform → Combat.Battlefield`
(both pre-existing directions); combat never references LevelGeneration. Extracting a neutral
`Core.Hex` namespace is a ROADMAP item (a ~43-file rename sweep, deferred).

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `HexMetrics` | Axial↔local conversion (exact port of the legacy grid formulas), corner offsets, edge-aligned neighbor order, cube rounding |
| `PlatformHexSurface` | Immutable: sorted `Cells`, `HexSize`/`Orientation`, centroid `CenterOffset`, `CenterCell`, walkable `Outline`, `SubdividedOutline`+`RimRing` (index-aligned), empty `BlockedCells` seam (R9) |
| `PlatformSurfaceGenerator` | Seeded weighted blob growth to the profile's cell count (compactness 0–8), hole fill (R3), `guaranteedMinCells` floor (R6) |
| `HexOutlineExtractor` | Border segments (cell edges with no neighbor) stitched via quantized endpoints into one CCW loop |
| `PlatformRimBuilder` | Outline midpoint subdivision + outward offset `rimWidth·(1±jitter)` + tangential wobble (R4) |
| `PlatformShapeSettings` / `ShapeProfile` | Clamped dials record mapped from the SO |
| `PlatformContentKindResolver` | `GraphNode` → Empty/Loot/Combat/Npc; `Type == Combat` wins (covers story-with-required-combat) |
| `SurfaceHexGrid` | `IHexGrid` whose cells/positions come 1:1 from the surface; `IsCellInBoundary` = set membership; `WorldToHex` = exact inverse of cell placement |

### 2.3 Runtime flow

1. **Window planning** (`RunWindowPlanner` + the site-aware allocator, see
   `narrative-procedural.md` §2.6 and `world-sites.md`) yields `PlannedPlatform`s →
   `RunStreamingCoordinator.MapWindow` → `GraphNode`s (Type/ContentTypes/PrebuiltContent, plus
   `Site` — the world-sites `SiteStamp` a multi-platform site block stamps on its members, the seam
   the M5 site-dressing pass reads — and `ContentFlavor`, which biases the platform loot roll).
2. **`AreaGenerator.CreatePlatformFromNode`**: resolves the content kind, derives the per-platform
   PRNG (`LootSeed.Derive(runSeed, "platform-shape:{nodeId}")` → `DeterministicRandom` — its own
   stream, decoupled from the director's so shape draws never shift narrative picks), grows the
   `PlatformHexSurface` (battlefield-minimum floor when the kind is Combat), sets
   `PlatformVisual.Surface`, `TopBoundary` (= outline, final from birth), `Size` (= outline bbox for
   the layout cursor), and the cursor-based `Position` (height deviation drawn from the same
   per-platform stream; Perlin height seed derives from the run seed).
3. **`PlatformView`** builds the mesh via `PlatformHexSurfaceMeshBuilder` (per-cell shallow-dome
   tops — the R2 "muted" treatment: cell borders read as soft valleys; `CellInset` 0 = flat — plus
   the drooping rim strip, side skirt, and a mirrored concave-safe bottom), the `MeshCollider`, and
   `PlatformColliderBuilder` walls **on the walkable outline** — the rim lies beyond the walls,
   which is what makes it physically non-walkable (R4).
4. **Combat entry** (`CombatActiveState.OnEnter`): `ICombatController.InitializeBattlefield(
   platform.Visual.Surface, platform.Visual.Position)` → `Battlefield.Initialize(surface, center,
   hexConfig)` → `SurfaceHexGrid` — the grid's cells ARE the surface cells (R1); hex size and
   orientation ride on the surface. `BattlefieldView` spawns the crisp cell outlines
   (`Prefabs/HexagonOutline`) at `grid.GetCellPosition` — coinciding with the ground by
   construction. Character platform detection (`PlatformRegistry.GetPlatformAtPosition`)
   point-in-polygon tests the same outline (in platform-local space).

### 2.4 DI wiring

`AreaInstaller` binds the mapped `PlatformShapeSettings` `AsSingle` (inspector field
`_platformShapeConfig`, falling back to `Resources.Load("LevelGeneration/PlatformShapeConfig")`,
falling back to code defaults) and builds `CombatConfig` **from** those settings, so the tiling has
exactly one authored source. `AreaSceneEntrypoint` receives the settings by `[Inject]` and threads
them into the manually-constructed `AreaGenerator`.

---

## 3. ScriptableObject Reference

### `PlatformShapeConfig`  (asset menu: `Create → Level Generation → Platform Shape Config`)

Loaded from `Resources/LevelGeneration/PlatformShapeConfig.asset` (or wired on the scene's
`AreaInstaller`). Mapped by `PlatformShapeConfigMapper` (the only Data→Core bridge).

| Field | Type | Meaning | Default |
|---|---|---|---|
| `_hexCellSize` | float | Hex cell size (world units) — shared by ground AND combat grid | 2 |
| `_hexOrientation` | HexOrientation | Flat / Pointy — shared by ground AND combat grid | Flat |
| `_emptyProfile` | ShapeProfileData | Empty/traversal platforms: `_minCells`/`_maxCells`/`_compactness` | 2–4, c3 |
| `_lootProfile` | ShapeProfileData | Loot-only platforms | 3–5, c3 |
| `_combatProfile` | ShapeProfileData | Combat-capable platforms | 12–18, c6 |
| `_npcProfile` | ShapeProfileData | NPC (no forced fight) platforms | 4–7, c3 |
| `_battlefieldMinimumCells` | int | Whole-cell floor for every combat-capable platform (R6) | 12 |
| `_rimWidth` | float | Base outward width of the decorative rim | 1.2 |
| `_rimJitterPercent` | int 0–100 | Per-vertex rim irregularity (silhouette dial: tidy→ragged) | 35 |
| `_rimDropHeight` | float | How far the rim droops below the walkable top | 0.4 |
| `_platformThickness` | float | Extrusion below the top | 1 |
| `_gapBetweenPlatforms` | float | Layout gap between neighboring platforms | 2 |
| `_heightDeviation` | float | Max height deviation between consecutive platforms | 1.5 |
| `_cellInset` | float | Per-cell dome height → soft valley seams (R2); 0 = flat | 0.06 |

`_compactness` (0–8): growth-weight dial — 0 grows ragged/organic, 8 hugs the blob (round arena).

Referenced assets: none. (The crisp combat cell visual is the pre-existing
`Resources/Prefabs/HexagonOutline.prefab`, spawned by `BattlefieldView` — see gotchas.)

---

## 4. Adding Content

### Tune a content kind's platform size/shape

1. Open `Resources/LevelGeneration/PlatformShapeConfig.asset`.
2. Edit that kind's profile block (`_minCells`/`_maxCells` = size range in whole cells,
   `_compactness` = round vs ragged). Combat platforms never drop below
   `_battlefieldMinimumCells` regardless of the profile.
3. Enter play mode — platforms of that kind resize. No code change (R5 acceptance).

### Dial the island silhouette (rim)

1. Same asset: `_rimWidth` (how far the dressing extends), `_rimJitterPercent` (tidy → ragged),
   `_rimDropHeight` (how much the edge droops).
2. The rim is visual only — walkability and combat are untouched by any rim value.

### Change the tiling scale/orientation

1. Same asset: `_hexCellSize` / `_hexOrientation`. Ground and combat grid update **together** (one
   source of truth).
2. **Gotcha:** `Resources/Prefabs/HexagonOutline.prefab` (the crisp combat cell outline) is
   authored for size 2 — `HexCellView` scales by the battlefield's hex size at runtime, but check
   the line width still reads well after a large change.

**Authoring constraints / gotchas:**
- `_maxCells` below `_minCells` is clamped up to `_minCells`; all values are clamped to sane ranges
  by `PlatformShapeSettings`.
- An unedited (or missing) asset behaves identically to the code defaults — asserted by
  `PlatformShapeConfigMapperTests`.
- The `AreaSceneEntrypoint.seed` inspector field now only overrides the **height-noise** seed
  (0 = derive from the run seed); platform shapes always follow the run seed.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `HexMetricsTests` — formula parity with the legacy grids (the compatibility contract), corner
  radius, edge↔neighbor alignment, fractional round-trip.
- `PlatformSurfaceGeneratorTests` — same-seed exact replay (R8), profile range + battlefield-minimum
  floor (R6), edge-connectivity, hole-freeness (R3), compactness monotonicity, recentering,
  `CenterCell` validity.
- `HexOutlineExtractorTests` — single/multi-cell loops, CCW winding, no duplicate vertices.
- `PlatformRimBuilderTests` — index alignment, offset bounds, zero-jitter exactness, determinism.
- `PlatformShapeSettingsTests` / `PlatformShapeConfigMapperTests` — clamping, defaults parity.
- `PlatformContentKindResolverTests` — node→kind matrix incl. story-with-required-combat → Combat.
- `SurfaceHexGridTests` — grid cells ≡ surface cells, `HexToWorld`/`WorldToHex` round-trip (R1),
  membership-based boundary.

The pure-C# suites run outside Unity via the bundled-Roslyn workaround; verified green 2026-07-02
(32 pure + the Unity-side suites compile-checked). Unity-side behavior (mesh look, muted→crisp
feel, camera at arena scale) is verified manually in play mode.

---

## 6. Known limitations / open points

- **Regression — invisible wall inside the visible island.** The wall colliders sit on the walkable
  outline while the rim extends beyond it, so the hero stops with ground still visibly continuing
  (pre-rework walls coincided with the visual edge, commit `7201bb2`). The intended edge feel (rim
  reads as a drop-off vs the physical stop moving to the rim's outer ring) is undecided — ROADMAP.
- **Regression — units sink waist-deep in combat.** Unit placement reads `Battlefield.HexToWorld`
  directly and was implicitly calibrated against the legacy grids' accidentally doubled Y;
  `SurfaceHexGrid` (commit `9d2f909`) returns the true surface height, dropping units by the
  platform's height. Needs explicit grounding (surface top + feet/pivot offset) — ROADMAP.
- **Muted-tiling treatment is geometry-MVP.** Per-cell shallow domes read as soft valleys under a
  lit material; the real muted→crisp render treatment (shader/VFX emphasis on combat entry) is
  tech-art (ROADMAP, render-look bible).
- **`BlockedCells` is an unused seam.** R9 alignment is guaranteed by construction, but no biome
  feature content occupies cells yet (separate biome-appearance brief); nothing writes or reads
  `BlockedCells`.
- **Hole-fill may overshoot the drawn cell count** by the filled holes (rare at these sizes;
  deterministic; never below the minimum).
- **Combat-platform scale jumped** (~14–18 u across vs the old 3–6 u blobs — intended by the
  brief). Camera framing and `CombatEntryAnimator`/`ContentSpawner` behavior at arena scale need a
  play-mode pass (ROADMAP).
- **Content spawn points don't use `CenterCell` yet.** The character spawn does; `ContentSpawner`
  still places content relative to the platform center, which on a concave island can sit off-cell
  (ROADMAP).
- **`Core.Hex` extraction deferred.** The hex vocabulary stays in `Combat.Battlefield` (see §2.1);
  the namespace sweep is a ROADMAP debt item, as is lifting `IRandomSource`/`DeterministicRandom`
  out of `Narrative.Director.Core`.
