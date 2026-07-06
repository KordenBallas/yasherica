# Environment Dressing — Requirements & Design

> The world's decoration layer behind one swappable **dressing-kit contract**: biome feature kits
> (ground + scattered features on platforms), site dressing kits (settlement skyline / camp props),
> and — later — backdrop kits, all bound **whole-kit** from the consuming configs so demo store-pack
> content swaps to production art by repointing one field. Realizes Track E (E1 kit contract, E2
> biome kits, E3 site/camp kits) of the 2026-07-05 Environment Dressing initiative; E4 (backdrop
> fill) is deferred, the contract is shaped for it.
> Status: current as of 2026-07-06.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.
>
> Briefs consumed: `product-requirements/dressing-kit-binding-and-swap.md` ·
> `product-requirements/biome-decoration-kits-demo.md` ·
> `product-requirements/site-camp-dressing-kits-demo.md` ·
> `product-requirements/biome-visual-styles.md` (the P1-2 placement model this builds).

---

## 1. Requirements

### 1.1 Functional requirements — the kit contract (E1)

- **R1** A dressing kit is a **named, data-only ScriptableObject** bundling visual-asset references
  under its seam's role vocabulary. One abstract base (`DressingKitDefinition`), one subclass per
  kit kind (biome-feature, site-dressing; a backdrop kind is additive later).
- **R2** Seams bind a kit **whole-kit**: a biome appearance config references one feature kit
  (`_featureKit`); a site's dressing theme id resolves to one site kit. **Swapping = repointing
  that one field** (or re-keying the theme id). Per-mesh binding does not exist.
- **R3** **Demo kits are quarantined** in `Resources/World/Dressing/Demo/`. Store-pack assets are
  reachable **only through kit assets** — no scene, prefab, or script references a pack asset. A
  living guard test enforces this (§5).
- **R4** **Bind-time tone treatment**: every kit material is rebuilt onto the project's URP Lit
  shader with its albedo lerped toward the biome's muted key (`_toneTint` × `_toneStrength`) and a
  slight value pull-down — pack materials are never used at native saturation (and their
  built-in-pipeline Standard shaders never reach a renderer raw, which would break under URP).
- **R5** **Determinism**: a kit supplies only *which* assets exist; placement, density, and seeding
  are owned by the consuming seam. Same seed + same bound kit → same world. Swapping a kit with the
  same entry count never shifts a draw (a different entry count is a different kit and may
  legitimately re-place).
- **R6** **Fail-safe base layer**: a biome with no bound kit, an unknown site theme id, an empty
  pool, or a broken entry yields the **empty plan** — base ground, no features, one warning per
  key, never a generation failure.

### 1.2 Functional requirements — biome features (E2, the P1-2 placement model)

- **R7** A biome feature kit lists a **ground material** and a **feature pool** — entries of
  (prefab, kind, weight, scale range) where kind ∈ **SmallDecorative | LargeDecorative | Blocking**.
- **R8** A **Blocking** feature is a tactical obstacle: it consumes its **whole cell**
  (`PlatformHexSurface.BlockedCells`, now live), one per cell, sparse (`_blockersPer100Cells`,
  pairwise spacing ≥ 2 cells), never on the protected set, never pushing free cells under the
  **battlefield minimum**, and never disconnecting the free-cell field (BFS guard).
- **R9** **Decorative features never consume cells** and are placed as **homogeneous clusters**
  (one entry per cluster — a copse, an outcrop, a tuft patch; 2–5 jittered members), several per
  cell allowed — decoration density is decoupled from obstacle density.
- **R10** **Rim/rear bias**: large decoration seeds rear (+Z, away from camera) and edge-biased; a
  fraction of large clusters anchor on the decorative rim strip. The **movement lane** (band around
  local Z = 0, `_laneHalfWidthCells`) and `CenterCell` + neighbors are protected from blockers.
- **R11** The combat grid, spawn placement, movement, and landing anchors all exclude blocked cells
  through the single `SurfaceHexGrid` / `PlatformAnchor` path; blocked cells remain ground/mesh
  (the obstacle prefab keeps its colliders, so out-of-combat free-move respects it too).

### 1.3 Functional requirements — site dressing (E3)

- **R12** A site dressing kit lists **role lists**: `_structures` (skyline houses, blocking),
  `_props` (small clutter, decorative), `_focalProps` (the camp fire, one blocking cell),
  `_gateProps` (threshold), plus a **ground overlay** material that overrides the biome ground on
  block platforms. Kits are matched by `SiteStamp.DressingThemeId` (the seam `world-sites.md`
  threaded; now live).
- **R13** The kit's **shape decides the style** (data, not site-id switches): structures ⇒
  settlement dressing (rear-third placement, house fronts on a **block-shared skyline band** with
  block-shared scale, yaw 180° facing camera); no structures but focal ⇒ camp dressing (fire on a
  rear-center cell, props ringed 1–1.5 cells around it, turned to face it).
- **R14** The **gate** appears only on the block's anchor platform (`Index == 0`), flanking the
  movement lane at the approach (min-X) edge.
- **R15** **Village and City share one kit** (`settlement-kit`); the size difference comes from the
  site's own footprint. Site dressing **replaces** biome features on site platforms (one voice per
  place). All placements are platform-local — no gap-crossing dressing is possible by construction.
- **R16** Site determinism rides the persisted stamp: block-shared draws derive from
  `site-dressing:{InstanceId}`, per-platform jitter from `site-dressing:{InstanceId}:{Index}` — a
  restored run replays identical dressing with **no save-format change**.

### 1.4 Non-functional requirements

- **N1** Planners and catalogs are pure C# (no UnityEngine), fully unit-tested headlessly.
- **N2** All wiring through Zenject (`AreaInstaller.InstallDressingBindings`); no service locators.
- **N3** Kit SOs are data-only; the single SO → Core bridge is `DressingKitMapper` (install time).
- **N4** Toned material variants are cached per (material, theme) and shared (`sharedMaterial`), so
  batching survives; dressing renderers cast no shadows.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/World/Dressing/
  Core/   — pure C#: FeatureKind, FeatureEntryData, FeaturePoolData, FeatureDensitySettings,
            SiteKitData, DressingRole, DressingPlacement, DressingPlanKind, PlatformDressingPlan,
            ProtectedCells, BlockedCellGuard, BiomeFeaturePlanner, SiteDressingPlanner,
            IEnvironmentDressingPlanner/EnvironmentDressingPlanner (the orchestrator seam),
            IBiomeFeaturePoolCatalog/BiomeFeaturePoolCatalog, ISiteDressingCatalog/SiteDressingCatalog
  Data/   — DressingKitDefinition (abstract base), BiomeFeatureKitDefinition,
            SiteDressingKitDefinition, DressingKitMapper (SO → Core), DressingKitLibrary (kit lookup
            for the spawner)
  View/   — EnvironmentDressingSpawner (thin adapter), ToneMaterialCache
Scripts/LevelGeneration/View/IEnvironmentDressingSpawner.cs   (the generator-facing seam, next to
                                                               IRouteLandmarkSpawner)
Scripts/Core/DI/AreaInstaller.cs — InstallDressingBindings()
```

Consuming-seam extensions: `BiomeAppearanceDefinition` gained the dressing binding + dials
(`_featureKit`, `_toneTint`, `_toneStrength`, `_blockersPer100Cells`, `_decorClustersPer100Cells`,
`_laneHalfWidthCells` — see `world-landscape.md` §3). `PlatformHexSurface.BlockedCells` went live
(ctor-supplied + `IsBlocked` + `WithBlockedCells`; `SurfaceHexGrid` and `PlatformAnchor` skip
blocked cells — see `platform-generation.md`).

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `PlatformDressingPlan` | One platform's deterministic outcome: kind, kit key, theme, blocked cells, placements. `Empty` = the fail-safe base layer. |
| `DressingPlacement` | One instance: role, entry index, platform-local X/Z, yaw, scale. |
| `FeaturePoolData` / `FeatureEntryData` | The pure feature pool, index-aligned with the kit asset's list (a broken entry keeps its slot at weight 0 — indices never shift). |
| `SiteKitData` | Pure site-kit shape: per-role entry counts. |
| `FeatureDensitySettings` | The biome's placement dials (blockers/100 cells, clusters/100 cells, lane half-width in cell units). |
| `BiomeFeaturePlanner` | E2 placement model: scored+jittered blocker picks under the guards; homogeneous decorative clusters; rim/rear bias. |
| `SiteDressingPlanner` | E3 placement model: skyline band + shared scale (instance stream), gate on anchor, camp focal + facing ring. |
| `EnvironmentDressingPlanner` | The one seam `AreaGenerator` calls: routes site vs wild, owns the seed streams, warns once per unresolved key. |
| `ProtectedCells` / `BlockedCellGuard` | The never-block invariants shared by both planners (lane ∪ center+neighbors; spacing, connectivity BFS, battlefield-minimum cap). |

### 2.3 Runtime flow

1. `AreaGenerator.CreatePlatformFromNode` grows the surface, then calls
   `IEnvironmentDressingPlanner.Plan(nodeId, node.Site, kind, surface, battlefieldMin)`.
2. The orchestrator routes: site stamp → site kit by theme id (site dressing replaces biome
   features there); wild → the live `ICurrentThemeProvider.CurrentTheme`'s feature kit. Nothing
   bound → `PlatformDressingPlan.Empty`.
3. Blocked cells fold into the surface **before** the mesh/grid consume it
   (`surface.WithBlockedCells(plan.BlockedCells)`), so the combat grid, spawns, movement, and
   landings are right by construction.
4. `CreatePlatformGameObject` asks `IEnvironmentDressingSpawner.ResolveGroundMaterial(plan)` (site
   overlay wins over biome ground wins over the default grey) and passes it into
   `PlatformView.SetConfig`; after `Initialize`, `Spawn(plan, platformTransform)` instantiates each
   placement under a `Dressing` child (platform-local), applies toned material variants to every
   renderer, and strips colliders on decorative placements. Two precedence rules keep the ground
   honest: a **bound kit always yields a kit-carrying plan** even with zero placements (a
   featureless platform still wears the biome/site ground), and a **dressed ground suppresses the
   legacy per-platform debug color variation** (`AreaGeneratorConfig.colorVariation` — the tint
   used to overwrite the material color; the Area scene now ships with it off).
5. Restore replays the same `AppendPlatforms` with persisted node ids + site stamps → identical
   plans (seed streams are per-node / per-instance, order-independent across windows).

Seed streams (`LootSeed.Derive` off the run seed): `biome-features:{nodeId}` ·
`site-dressing:{instanceId}` (block-shared draws) · `site-dressing:{instanceId}:{index}`.

### 2.4 DI wiring

`AreaInstaller.InstallDressingBindings()`: loads all kit SOs from `Resources/World/Dressing`
(recursive — `Demo/` rides in); binds `IBiomeFeaturePoolCatalog` + `ISiteDressingCatalog` (mapped by
`DressingKitMapper` from the same biome-appearance list `InstallWorldBiomeBindings` resolved),
`IEnvironmentDressingPlanner`, `DressingKitLibrary`, `ToneMaterialCache`, and
`IEnvironmentDressingSpawner`. `AreaSceneEntrypoint` threads planner + spawner into the
`AreaGenerator` ctor (optional params, null = no dressing — the `landmarkSpawner` pattern).
Duplicate kit ids / theme ids: first wins, warned.

---

## 3. ScriptableObject Reference

### `BiomeFeatureKitDefinition` (asset menu: `Create → World → Dressing → Biome Feature Kit`)

Loaded from `Resources/World/Dressing/` (any subfolder; demo kits live in `Demo/`). Bound from
`BiomeAppearanceDefinition._featureKit`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_kitId` | string | Stable id (logs + the spawner's kit lookup) | required, unique |
| `_groundMaterial` | Material | Platform top material, toned at bind time | empty = keep default ground |
| `_features` | list | The feature pool; **entry order is the stable index — append, don't reorder** | |
| `_features[i]._prefab` | GameObject | The feature mesh (pack prefab allowed — kits are the only legal home) | null = slot kept, never drawn (warned) |
| `_features[i]._kind` | FeatureKind | `SmallDecorative` / `LargeDecorative` / `Blocking` | SmallDecorative |
| `_features[i]._weight` | int ≥ 0 | Draw weight among same-kind entries | 1; 0 = never drawn |
| `_features[i]._scaleMin/Max` | float | Uniform per-instance scale range | 0.8–1.2 |

### `SiteDressingKitDefinition` (asset menu: `Create → World → Dressing → Site Dressing Kit`)

Loaded from `Resources/World/Dressing/`. Matched by theme id, not by direct reference.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_kitId` | string | Stable id for logs | required |
| `_dressingThemeId` | string | Matched against `SiteDefinition._dressingThemeId` (e.g. `settlement-kit`, `camp-kit`) | required, unique (first wins) |
| `_structures` | list of GameObject | Skyline houses — **blocking**, whole-cell | empty = not a settlement |
| `_props` | list of GameObject | Small clutter (sacks/barrels/crates) — decorative | |
| `_focalProps` | list of GameObject | The camp fire pieces, **stacked on one blocking cell** | empty = no focal |
| `_gateProps` | list of GameObject | Threshold pieces (anchor platform only, ≤ 2 placed) | |
| `_groundOverlayMaterial` | Material | Site ground (street/packed dirt), overrides biome ground | empty = biome ground |

### `BiomeAppearanceDefinition` — dressing extension (owned by `world-landscape.md`; listed here for the binding)

| New field | Type | Meaning | Default |
|---|---|---|---|
| `_featureKit` | BiomeFeatureKitDefinition | **The whole-kit binding** — swap = repoint this field | null = base layer |
| `_toneTint` | Color | The muted biome key the tone treatment lerps toward | grey-green |
| `_toneStrength` | float 0–1 | Lerp amount toward the tint | 0.45 |
| `_blockersPer100Cells` | float | Blocking-obstacle density (floor-not-target, capped by guards) | 4 |
| `_decorClustersPer100Cells` | float | Decorative-cluster density | 10 |
| `_laneHalfWidthCells` | float | Protected lane half-width, in hex-size units | 1.1 |

Authored demo content (the quarantine, `Resources/World/Dressing/Demo/`):
`Demo_BiomeFeatureKit_Desert` (*Low Poly Desert Environment* rocks/cacti + RPGPP dry tufts, sand
ground) · `Demo_BiomeFeatureKit_Forest` (*RPG Poly Pack - Lite* trees/bushes/rocks/tufts/flowers,
grass ground) · `Demo_SiteKit_Settlement` (`settlement-kit`: buildings 01–05, banners+fences gate,
crates/barrels/well/wagon, street ground) · `Demo_SiteKit_Camp` (`camp-kit`: wood-hanger campfire
focal + stones + log, awnings/sacks/barrels/crates ring, packed-dirt ground) · four flat muted URP
ground materials (`Demo_Ground_*`). Mountain/Cave bind no kit (base layer, by design).

---

## 4. Adding Content

### Dress a biome (bind or author a feature kit)

1. `Create → World → Dressing → Biome Feature Kit`; set `_kitId`, optional `_groundMaterial`, and
   the feature entries (prefab + kind + weight + scale range). Keep pack prefabs referenced **only
   from kit assets**; put demo-pack kits under `Resources/World/Dressing/Demo/`.
2. Open the biome's `BiomeAppearanceDefinition` (`Resources/World/Biomes/`) and point
   `_featureKit` at the kit; tune `_toneTint`/`_toneStrength` and the density dials.
3. Enter play mode: platforms of that biome carry the ground + clustered features; blockers read as
   sparse obstacles the fight moves around.

### Add a feature entry to an existing kit

1. **Append** the entry to `_features` (never reorder/insert — entry order is the deterministic
   index; reordering re-rolls existing worlds' picks).
2. Pick its kind honestly: a big rock / tall cactus / tree trunk = `Blocking`; shrubs, tufts,
   pebbles = `SmallDecorative`; the in-between = `LargeDecorative`.

### Dress a site type

1. `Create → World → Dressing → Site Dressing Kit`; set `_dressingThemeId`, fill the role lists
   (structures for a settlement; focal + props for a camp), optional ground overlay.
2. Set the same theme id on the `SiteDefinition`'s `_dressingThemeId`
   (`Resources/World/Sites/…`). Several sites may share one kit (Village + City both say
   `settlement-kit`).
3. Enter play mode and walk into the site block: anchor platform carries the gate; houses front a
   shared line across the block; a camp's props ring its fire.

### Swap a demo kit for production (the one-field swap)

1. Author the production kit asset (same kind, same role vocabulary; matching entry **count** keeps
   existing worlds' draws stable).
2. Repoint the binding: the biome's `_featureKit` field, or re-key the site kit's
   `_dressingThemeId`. **No scene, prefab, or code edit.** Deleting
   `Resources/World/Dressing/Demo/` afterwards must leave zero dangling references —
   `DemoPackQuarantineTests` proves it.

### Add a new kit kind (e.g. the E4 backdrop kit)

1. New subclass of `DressingKitDefinition` (data-only) + a `DressingKitMapper` mapping + a binding
   field on the consuming config. The binding/swap/tone/fail-safe rules of §1.1 apply unchanged.

**Authoring constraints / gotchas:** never reference a pack asset outside a kit (the quarantine
test fails the build); never reorder `_features`; a null prefab entry is kept at weight 0 and
warned, not dropped (index stability); duplicate `_kitId`/`_dressingThemeId` — first wins, warned;
kit materials need no manual muting — the bind-time treatment tones (and URP-converts) them.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/` (pure suites run headless via the bundled
Roslyn workaround — 29 dressing tests green 2026-07-06, plus the hexsurface/combat regressions):

- `BiomeFeaturePlannerTests` — same-seed identity; blockers off the protected set; battlefield
  minimum under hostile density; pairwise spacing; connectivity; decor-vs-blocker decoupling; rear
  bias statistic; empty/null pool + zero density fail-safes.
- `SiteDressingPlannerTests` — same-seed replay; block-shared scale; rear placement + camera-facing
  fronts; structure cells blocked with guards; gate only on the anchor, approach side; camp focal
  stack + facing ring geometry; wild/empty-kit fail-safes; placements within platform bounds.
- `EnvironmentDressingPlannerTests` — site-vs-wild routing; missing-catalog fail-safe;
  order-independent per-node replay; instance-shared draws across a block.
- `PlatformHexSurfaceBlockedCellsTests` — `WithBlockedCells` immutability + filtering; grid
  exclusion; anchor skip.
- `DressingKitMapperTests` (in-editor) — SO→Core mapping, null-prefab slot keeping, duplicate
  first-wins, unbound-biome absence.
- `DemoPackQuarantineTests` (in-editor, file IO) — harvests every pack GUID and asserts zero
  references from `__Project` outside the demo quarantine (the R3 living guard).

Verified manually (play mode, user): tone/density feel, skyline read across a block, campfire
read, "muted but biomes read" — density and tone are data dials on the biome asset.

---

## 6. Known limitations / open points

- **Tone ≠ true desaturation.** The bind-time treatment is a multiply-tint toward the biome key on
  a URP Lit rebuild; real figure-ground desaturation of textured albedo is the **P5-8** shader
  spike (ROADMAP).
- **Site platforms skip biome features** (one voice per place, KISS). A light biome pass under the
  site dressing is a possible later layer.
- **Skyline alignment tolerates route weave**: platforms of one block share the band in local
  space; the route's few units of Z-weave between neighbors reads fine at demo scale (site-aware
  tier flattening is already a ROADMAP item).
- **Mountain & Cave feature kits and Ruin/Lair site kits are unauthored** — those surfaces stay on
  the base layer (the packs don't cover them). Data-only to add.
- **E4 backdrop kit kind is deferred** — the contract is shaped for it (one more
  `DressingKitDefinition` subclass + a binding field); the distant-scatter layer itself is Track E4.
- **Variant-prefab references were hand-computed** (nested-prefab fileID = source ^ instance): if
  any camp/settlement entry shows as `None` in the inspector, re-drag the prefab onto the slot —
  data-only fix, the spawner warns and skips broken entries at runtime.
- **Prop animation** (banner sway, fire light/smoke) — later polish; the demo campfire is unlit
  geometry by owner decision (composed hanger + stones + log).
