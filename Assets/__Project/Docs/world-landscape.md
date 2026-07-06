# World Landscape Read — Requirements & Design

> How the traversal field reads as a natural landscape you route through (P5-2): the platform
> sequence weaves laterally along a bounded, seeded curve with sparse feature arcs justified by
> midground routing landmarks; platforms sit on deliberate elevation tiers; a distant hazed biome
> horizon sits behind the whole run. All three axes are **layout and read, never mechanic** — gaps
> stay clean hops, the path never forks, the camera keeps its fixed isometric angle.
> Status: current as of 2026-07-04.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

Source brief: `product-requirements/world-backdrop-and-elevation.md` (verified 2026-07-04).
Art direction: `design/art/world-backdrop.md`, `design/art/render-look.md` §2.

### 1.1 Functional requirements

- **R1 (Routed path)** Successive platforms are offset in **depth (world Z)** along a coherent
  curve: a **low-frequency baseline wander** (one bend spans several platforms — a trail with
  inertia, not a zigzag) plus sparse, larger **feature arcs**.
- **R2 (Monotonic forward)** Screen-forward (world X) always advances; a later platform is never
  placed screen-behind an earlier one.
- **R3 (Bounded corridor)** The lateral wander stays within an authored corridor half-width.
  This **replaces** the old unbounded per-platform random depth step (the mislabeled
  `_heightDeviation` dial, removed).
- **R4 (Landmark-justified arcs)** Each feature arc bulges **toward the camera** around a
  **midground landmark** placed at the arc's apex on the **far side** of the path. The landmark is
  a world object off the platforms — never walkable, never on a hex cell.
- **R5 (Readability invariant)** The hero is never occluded by a routing landmark; landmarks always
  sit behind the path (enforced structurally: landmark offset ≥ worst-case local path swing).
- **R6 (Camera unchanged)** The fixed isometric traversal angle is kept; no camera rotation or
  path-following yaw.
- **R7 (Elevation tiers)** Platforms sit at authored **height levels** (tier count × tier step)
  following a low-frequency swell — an undulating landscape read. Visual only: no stepping between
  tiers, no bridges; gaps stay clean hops regardless of tier difference.
- **R8 (World backdrop)** A distant, **hazed biome horizon** (two ridge silhouette layers + sky
  gradient band) sits behind the whole field, keyed to the biome, muted and lowest in the focus
  hierarchy. It reads as infinitely distant and sits behind any future per-Site backdrop. The
  hero-anchored ridge rig here is the *painted-plane* horizon; a nearer, **world-fixed 3D scatter**
  layer (dunes/hills between the platforms and the ridges) is the environment-dressing **E4**
  backdrop kit — see `environment-dressing.md` §1.5 (it renders in front of these ridges).
- **R9 (Per-biome character)** The character of all three axes is authored per biome on the
  **biome appearance config** (`BiomeAppearanceDefinition`); an unauthored biome falls back to sane
  code defaults. Tuning or adding a biome's landscape is data-only.
- **R10 (Determinism)** The same run seed yields the same route, tiers, landmark placement, and
  backdrop.

### 1.2 Non-functional requirements

- **N1** Route/tier/landmark/backdrop math is pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject / constructor injection; no service locators.
- **N3** All per-biome dials live on one data-only ScriptableObject.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/LevelGeneration/Route/            (pure C#, UnityEngine-free)
  BiomeLandscapeSettings.cs   — immutable clamped per-biome dials record + CreateDefault()
  RunRouteModel.cs            — THE route function: Sample(x) + GetLandmarksInRange(from, to)
  RouteSample.cs              — one answer: LateralZ + TierY + TierIndex
  LandmarkSpec.cs             — one landmark: X/Z/Y + kit index + scale
  BackdropSilhouetteModel.cs  — seeded bounded ridge-line heights per backdrop layer
Scripts/World/Biomes/
  IBiomeAppearanceCatalog.cs / BiomeAppearanceCatalog.cs — theme → appearance asset lookup
Scripts/World/Biomes/Data/
  BiomeAppearanceDefinition.cs — the per-biome SO (landscape section; P1-2 extends it later)
  BiomeAppearanceMapper.cs     — the only SO→Core bridge (null → CreateDefault)
Scripts/World/Landscape/                  (Unity views/adapters)
  RouteLandmarkSpawner.cs      — IRouteLandmarkSpawner: kit prefab or procedural placeholder
  PlaceholderSilhouetteMeshBuilder.cs — peak/dome/ridge-strip/sky-quad meshes, vertex-color haze
  WorldBackdropBuilder.cs      — assembles the backdrop rig (2 ridge layers + sky band)
  WorldBackdropView.cs         — thin MonoBehaviour: anchors the rig to the hero on X/Z
Scripts/LevelGeneration/View/
  IRouteLandmarkSpawner.cs     — the seam AreaGenerator calls (keeps Route UnityEngine-free)
Scripts/Core/DI/AreaInstaller.cs — binds IBiomeAppearanceCatalog (Resources/World/Biomes fallback)
```

Dependency flow: `World.Landscape → World.Biomes + LevelGeneration.Route`;
`AreaGenerator → LevelGeneration.Route` (pure) + `IRouteLandmarkSpawner` (interface). The route
model reuses `LootSeed.Derive` + `DeterministicRandom` (the same seeded-stream vocabulary as the
platform surface).

### 2.2 The route function

The whole route is a **pure function of forward distance** `x` (the layout-cursor space), seeded
per run — so it streams window-by-window with zero lookahead and same-seed replay is structural.

- **Baseline wander:** `z = A·(0.7·sin(2πx/λ + φ1) + 0.3·sin(2πx/2.6λ + φ2))` — two seeded
  incommensurate sinusoids; bounded by construction, slope-bounded (inertia).
- **Feature arcs:** forward space is partitioned into fixed **slots**; each slot draws (from its
  own derived stream) occurrence, apex, half-length, depth, kit index, and scale. The arc is a
  C¹-smooth `cos²` bump toward the camera (`RunRouteModel.TowardCameraSign = -1`, one constant).
  The apex window + half-length cap keep an arc's support inside its slot, so `Sample(x)` only
  consults the slot containing `x`.
- **Corridor invariant:** the settings constructor caps `A ≤ CorridorHalfWidth − ArcDepth`, so
  `|baseline + arc| ≤ CorridorHalfWidth` always (R3).
- **Far-side invariant:** `LandmarkOffset` is floored at `2A + clearance`, so a landmark is always
  behind the trail across its whole arc (R5).
- **Elevation tiers:** a low-frequency normalized swell quantized to `TierCount` levels ×
  `TierStep`; the default wavelength keeps consecutive platforms within one tier of each other.
- **Backdrop ridges:** per layer, two seeded sinusoids + small per-sample jitter, normalized to
  `[0, BackdropRidgeAmplitude]`.

### 2.3 Runtime flow

1. `AreaSceneEntrypoint.GenerateArea` reads the run's **initial theme from the biome journey**
   (`IBiomeJourney.ForWindow(0)` — see `biome-journey.md`; the fixed-Forest hardcode is gone),
   resolves that theme's `BiomeAppearanceDefinition` from `IBiomeAppearanceCatalog`, maps it to
   `BiomeLandscapeSettings`, derives the route seed
   (`LootSeed.Derive(runSeed, "landscape-route")`, overridable by the inspector `seed` field), and
   builds `RunRouteModel` + `RouteLandmarkSpawner`, threading both into `AreaGenerator`. On each
   later **biome-stretch crossing** the entrypoint (as `IBiomeStretchObserver`) re-resolves the new
   biome's appearance, calls `RouteLandmarkSpawner.ApplyBiome` (landmarks placed from then on wear
   the new kit/tint/shape; spawned ones keep theirs), and rebuilds the backdrop. The **route model
   itself keeps the entry biome's settings** for the whole run (§6).
2. `AreaGenerator.CalculatePlatformPosition` places each platform at
   `(cursorX, Sample.TierY, Sample.LateralZ)` — X marches with the cursor (R2), Z weaves (R1/R3),
   Y is the tier (R7).
3. After each appended window, `AreaGenerator.SpawnPendingLandmarks` scans the half-open forward
   span the window covered (`_landmarkScanX → _cursorX`, persisted like the cursor) and hands the
   specs to `IRouteLandmarkSpawner` — each landmark spawns exactly once while streaming, parented
   under the Area root so `Clear()` removes them.
4. `RouteLandmarkSpawner` instantiates an authored kit prefab when the biome provides one,
   otherwise a procedural placeholder silhouette (peak for Mountain/Cave, dome for Forest/Desert)
   tinted with the biome's muted landmark tint. No colliders, no shadows.
5. `AreaSceneEntrypoint.CreateWorldBackdrop` builds the backdrop rig once per biome stretch
   (rebuilt on a stretch crossing; a hard cut — transition art is M5)
   (`WorldBackdropBuilder`: far + near ridge strips from `BackdropSilhouetteModel`, sky gradient
   quad, all unlit vertex-color), rotated to the camera yaw and with every part **lowered** by
   `distance × tan(pitch)` (both from `CameraConfig.IsometricRotation`) so the horizon lands in
   the sky band of the tilted orthographic view (see the §3 scale note);
   `WorldBackdropView` re-anchors it to the hero each `LateUpdate` (X/Z instantly, Y
   slow-smoothed so tier climbs re-center the horizon while jump arcs average out) — the
   "infinitely distant" screen-stable read without parallax machinery.

### 2.4 DI wiring

`AreaInstaller.InstallWorldBiomeBindings` binds `IBiomeAppearanceCatalog` `AsSingle` from the
inspector list, falling back to `Resources.LoadAll<BiomeAppearanceDefinition>("World/Biomes")`;
missing assets are fine (code defaults per theme). `AreaSceneEntrypoint` receives the catalog and
`CameraConfig` by `[Inject]` and constructs the route model, spawner, and backdrop manually —
the same pattern as `AreaGenerator` itself.

---

## 3. ScriptableObject Reference

### `BiomeAppearanceDefinition`  (asset menu: `Create → World → Biome Appearance`)

One asset per `LevelTheme`, loaded from `Resources/World/Biomes/` (or wired on the scene's
`AreaInstaller`). Mapped by `BiomeAppearanceMapper` (the only Data→Core bridge); Unity-typed
fields (kits, tints) are read by the view layer directly off the SO. The environment-dressing pass
(2026-07-06) extended this asset with the biome's **dressing binding** — `_featureKit` (whole-kit),
`_toneTint`/`_toneStrength` (bind-time tone treatment), `_blockersPer100Cells` /
`_decorClustersPer100Cells` / `_laneHalfWidthCells` (placement dials), and (E4) `_backdropKit`
(whole-kit) + `_backdropScatterPer100Units` (distant-scatter density) — all documented in
`environment-dressing.md` §3, which owns those fields.

| Field | Type | Meaning | Default |
|---|---|---|---|
| `_theme` | LevelTheme | Which biome this asset describes | Forest |
| `_corridorHalfWidth` | float | Max lateral excursion of the route from the run axis (R3) | 14 |
| `_baselineWavelength` | float | Forward length of one baseline bend (several platforms) | 90 |
| `_baselineAmplitude` | float | Lateral amplitude of the wander (auto-capped to corridor − arc depth) | 5 |
| `_arcSlotLength` | float | Forward length of one feature-arc slot (≤ 1 arc per slot) | 140 |
| `_arcChancePercent` | int 0–100 | Chance a slot carries a feature arc | 60 |
| `_arcDepth` | float | Max extra toward-camera bulge of an arc | 7 |
| `_arcHalfLengthMin/Max` | float | Arc half-length range (max auto-capped to 0.3 × slot) | 25 / 42 |
| `_landmarkOffset` | float | How far behind the path a landmark sits (floored at 2·amplitude + 4) | 14 |
| `_landmarkScaleMin/Max` | float | Uniform landmark scale range (landmarks are big midground landforms) | 12 / 18 |
| `_landmarkKit` | List\<GameObject\> | Landmark prefabs; **empty = procedural placeholder** (P5-4 seam) | empty |
| `_landmarkTint` | Color | Muted tint for placeholder landmark silhouettes | grey-green |
| `_tierCount` | int | Number of elevation levels (R7) | 4 |
| `_tierStep` | float | Vertical distance between adjacent tiers | 2.5 |

Scale note: the traversal camera is a tight tilted **orthographic** window (~25×14 world units)
over platforms ~8–16 units across, and ortho has no perspective convergence — a point at ground
height `D` units away along the camera axis projects `0.5·D` **above** screen centre, forever. Two
consequences are baked into the system: every backdrop part is **lowered** by
`distance × tan(cameraPitch)` (after which its on-screen height depends only on its authored base
offset, never its distance), and a routing landmark is only in frame at offsets ≲ 14 — which is
why the baseline amplitude stays gentle (the no-occlusion floor is `2·amplitude + 4`) and the
**feature arcs** (unconstrained by that floor — they bulge toward the camera) carry the felt
turns. Landmark bases sit at a fixed low height, deliberately not coupled to the local tier, for
the same reason.
| `_tierWavelength` | float | Forward length of one elevation swell | 300 |
| `_silhouetteKit` | List\<Mesh\> | Backdrop ridge meshes; **empty = procedural ridge strips** (P5-4 seam) | empty |
| `_backdropRidgeAmplitude` | float | Peak height of the backdrop ridge line | 8 |
| `_backdropRidgeWavelength` | float | Forward length of one ridge undulation | 60 |
| `_skyTopTint` / `_skyHorizonTint` | Color | Sky gradient band colors | muted blue-greys |
| `_hazeTint` | Color | Haze color the ridge layers fade toward | muted grey |

Authored assets: `BiomeAppearance_Forest/Desert/Mountain/Cave.asset` (distinct characters:
mountains switchback tightly with tall tiers, the desert makes long lazy arcs in a wide corridor,
the forest wanders gently, caves run a tight low corridor).

---

## 4. Adding Content

### Tune a biome's landscape character

1. Open `Resources/World/Biomes/BiomeAppearance_<Theme>.asset`.
2. Routed path: `_corridorHalfWidth` (how far the trail may stray), `_baselineWavelength` /
   `_baselineAmplitude` (bend length / sway), `_arcSlotLength` + `_arcChancePercent` +
   `_arcDepth` (how often and how strongly the trail routes around a landmark).
3. Elevation: `_tierCount` × `_tierStep` (how tall the terrain reads), `_tierWavelength`
   (how fast it swells).
4. Backdrop: `_backdropRidgeAmplitude`/`_backdropRidgeWavelength` (jagged peaks vs long dunes),
   sky/haze tints (keep them muted — saturation is reserved for gameplay accents).
5. Enter play mode — the run's layout and horizon change. No code change (R9 acceptance).

### Add a biome's landscape appearance

1. `Create → World → Biome Appearance` in `Resources/World/Biomes/`, set `_theme`.
2. Author the dials per above. A theme without an asset uses code defaults — the run still
   routes and reads sanely.

### Swap in real landmark / backdrop meshes (the P5-4 asset pass)

1. Add landmark prefabs (flat low-poly, muted, no outline — `render-look.md` §1–§2) to
   `_landmarkKit`; the spawner picks per-arc deterministically (kit index modulo list size) and
   applies the arc's scale draw.
2. Add ridge silhouette meshes to `_silhouetteKit`; the backdrop builder tiles them along each
   ridge layer instead of the procedural strips.
3. No code change; empty lists fall back to the procedural placeholders.

**Authoring constraints / gotchas:**
- All numeric dials are clamped to safe values by `BiomeLandscapeSettings` (corridor and far-side
  invariants are enforced there, not by the inspector).
- An unedited (or missing) asset behaves identically to the code defaults — asserted by
  `BiomeAppearanceMapperTests`.
- The `AreaSceneEntrypoint.seed` inspector field overrides the **route** seed
  (0 = derive from the run seed); platform shapes always follow the run seed.
- Duplicate `_theme` assets: first authored wins, warning logged (`BiomeAppearanceCatalog`).

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `RunRouteModelTests` — same-seed exact replay (R10), corridor bound over dense sweeps (R3),
  lateral slope under the analytic inertia bound (R1), arcs bulge toward the camera only (R4),
  landmarks behind the local trail across the whole arc (R5), windowed landmark scan ≡ whole-span
  scan (streaming, no dupes/misses), tier grid membership + ≤ 1 tier step per platform pitch (R7).
- `BiomeLandscapeSettingsTests` — clamping (corridor/amplitude/arc caps, landmark-offset floor,
  tier floors) and defaults parity.
- `BiomeAppearanceMapperTests` — null → defaults; a fresh unedited SO maps identically to the code
  defaults.
- `BackdropSilhouetteModelTests` — determinism per seed/layer, layers differ, heights within
  `[0, amplitude]` (R8/R10).

The pure-C# suites run outside Unity via the bundled-Roslyn workaround; verified green 2026-07-04
(route + settings + backdrop suites, 23/23; mapper suite compile-checked). Unity-side behavior
(landmark/backdrop look, occlusion invariant, per-biome feel) is verified manually in play mode.

---

## 6. Known limitations / open points

- **Placeholder visuals.** Landmarks and backdrop ridges are procedural muted silhouettes until
  the decoration asset pipeline (P5-4) authors real meshes; the SO kit lists are the swap-in seam.
  The placeholder material is `Sprites/Default` (unlit, vertex-color) — fine for the read, not a
  final shading decision (render-look tech-art pass).
- **Toward-camera sign is a constant.** `RunRouteModel.TowardCameraSign = -1` assumes the fixed
  isometric rig looks from the −X/−Z side; if a camera pass ever changes that, flip the one
  constant.
- **Route shape is frozen to the entry biome.** The biome journey (`biome-journey.md`) swaps
  landmark dressing and the backdrop per stretch, but `RunRouteModel` keeps the first biome's
  `BiomeLandscapeSettings` (corridor/weave/tier character) for the whole run — swapping mid-run
  would discontinuously jump `Sample(x)` and desync the landmark scan cursor. A piecewise blended
  multi-biome route belongs to the M5 transition-art pass (ROADMAP).
- **Route is site-blind.** A site block spanning a tier step or an arc apex may fight the future
  "reads as one place" dressing; `GraphNode.Site` is available if site-aware flattening is needed
  (ROADMAP).
- **No parallax / sky animation.** The backdrop is rigidly hero-anchored (screen-stable); drifting
  clouds and depth movement are deferred polish (brief out-of-scope).
- **Path-following camera yaw** is a parked escalation — revisit only if playtest reads the weave
  as "platforms sliding sideways" (brief out-of-scope).
- **Route ↔ real backdrop coupling** is faked by construction (the landmark is placed to justify
  the bend); threading the path through the actual horizon silhouette is deferred polish.
- **Tier step vs the jump animation.** A 1.5–2.2 u tier step changes the hop arc's vertical reach;
  if a step reads oddly in play, tune `_tierStep` down (data-only).
