# Biome Journey (Biome Selection Along the Run) — Requirements & Design

> The run's biome is no longer a hardcoded Forest: an authored, seeded, tier-climbing **biome
> journey** decides which biome (and escalation tier) is active for each planning window, in
> multi-window **stretches**. Crossing into a new stretch switches the live monster pool, loot
> table, landmark dressing, and world backdrop, and publishes the tier as a world fact.
> Status: current as of 2026-07-04. Brief: `product-requirements/biome-selection-along-the-run.md`.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** No hardcoded biome: the run's biome is selected from authored data
  (`BiomeProgressionConfig`).
- **R2** Every rotation biome carries an authored **escalation tier** — an ordering key only;
  nothing scales difficulty/tone off it (that is D19, out of scope).
- **R3** The run **climbs tiers**: the first stretch draws from the lowest authored tier, each
  subsequent stretch from the next tier up, clamping at the top (top-tier biomes keep serving —
  there is no run apex yet).
- **R4** Within a tier the biome is a **seeded weighted pick** over that tier's positive-weight
  entries, so different seeds route through different homelands (D18 divergence). When the tier
  offers an alternative, the previous stretch's biome is excluded so a boundary is a visible
  crossing; a single-biome tier legitimately repeats.
- **R5** The run travels in **stretches**: an authored `[min, max]` range of planning windows per
  biome (windows, not platforms — the planning atom; one window = `RunPacingConfig.WindowSize`
  platforms), rolled seeded per stretch.
- **R6** Crossing a stretch boundary switches the biome's content: the **monster pool** and **loot
  table** follow the live `ICurrentThemeProvider`; the **landmark dressing** and **world backdrop**
  are swapped by the entrypoint observer. Already-generated platforms keep their look.
- **R7** **Cave is excluded by data**: it has no entry in the authored config (weight 0 would also
  exclude). No code check anywhere.
- **R8** The current stretch's tier is published as the world fact **`run_escalation_tier`**
  (Int, global). Nothing consumes it yet — it is the D19 seam.
- **R9** **Determinism**: the journey rides its own random stream
  (`LootSeed.Derive(runSeed, "biome-journey")`), so the same run seed yields the same biome
  itinerary, and journey draws never perturb the shared narrative-slice stream (or vice versa).
- **R10** **Data-authored & extensible**: adding a biome to the rotation, re-tiering, re-weighting,
  or excluding one is an asset edit (§4); no code change.
- **R11** **Fail-safe**: a missing/empty/weightless config degrades to the pre-journey behavior —
  a fixed Forest tier-1 run — with a single warning.

### 1.2 Non-functional requirements

- **N1** Journey core is pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject (`AreaInstaller`); no service locators.
- **N3** `BiomeProgressionConfig` is a data-only ScriptableObject; `BiomeProgressionConfigMapper`
  is the only SO → Core bridge.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/LevelGeneration/Journey/        — pure C#, no UnityEngine
  BiomeProgressionEntry.cs              — one biome's tier/weight/stretch record
  BiomeProgressionSettings.cs           — immutable roster + CreateDefault() fail-safe
  BiomeStretch.cs                       — one resolved leg (theme, tier, window range)
  IBiomeJourney.cs / BiomeJourney.cs    — the seeded tier-climbing itinerary
  IBiomeStretchObserver.cs              — appearance-refresh seam (keeps LevelGeneration free of World.*)
  BiomeStretchDirector.cs               — applies a stretch: theme provider + tier fact + observer
Scripts/World/Biomes/Data/
  BiomeProgressionConfig.cs             — the SO (data only)
  BiomeProgressionConfigMapper.cs       — SO -> BiomeProgressionSettings (the only bridge)
Scripts/Core/DI/AreaInstaller.cs        — binds settings + IBiomeJourney (own seeded stream)
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `BiomeProgressionEntry` | Authored record: `Theme`, `EscalationTier` (≥1), `SelectionWeight` (≥0; 0 = excluded), `StretchMinWindows`/`StretchMaxWindows` (normalized, swap if inverted) |
| `BiomeProgressionSettings` | Immutable entry list; `CreateDefault()` = single Forest tier-1 entry (stretch 3–4) |
| `BiomeStretch` | Resolved leg: `Theme`, `EscalationTier`, `StretchIndex`, half-open window range `[FirstWindow, EndWindowExclusive)` |
| `IBiomeJourney` / `BiomeJourney` | `ForWindow(int)` — pull-based single source of truth; lazily extends + caches the stretch plan; queries are idempotent and order-independent (draws are consumed strictly in stretch order, so determinism is structural) |
| `IBiomeStretchObserver` | `OnBiomeStretchChanged(BiomeStretch)` — implemented by `AreaSceneEntrypoint` to swap dressing/backdrop |
| `BiomeStretchDirector` | `ApplyForWindow(int)` — on stretch change (incl. the first call): `ICurrentThemeProvider.SetTheme`, `SetInt(WorldFacts.RunEscalationTier, tier)`, notify observer, log. Idempotent within a stretch. **The single owner of theme/fact writes.** |

Selection algorithm (`BiomeJourney.AppendNextStretch`): eligible tiers = distinct authored tiers
with ≥1 positive-weight entry, ascending `T[0..K-1]`; stretch `s` draws from `T[min(s, K-1)]`;
weighted pick over the tier pool (previous biome excluded when an alternative exists — if the
exclusion empties the pool because of duplicate-theme authoring, it is undone); stretch length =
`min + NextInt(max − min + 1)` windows. Exactly two draws per stretch regardless of pool size.

### 2.3 Runtime flow

1. **Install** — `AreaInstaller.InstallWorldBiomeBindings()` maps the config
   (`BiomeProgressionConfigMapper.ToSettings`) and binds `IBiomeJourney` as a `BiomeJourney` with
   its own `DeterministicRandom(LootSeed.Derive(runSeed, "biome-journey"))`.
2. **Run setup** — `AreaSceneEntrypoint.GenerateArea()` reads the initial theme from
   `_biomeJourney.ForWindow(0)` and builds the route model, landmark spawner, backdrop, and
   `AreaGenerator` for it. It does **not** set the theme provider — it constructs a
   `BiomeStretchDirector` (observer: itself) and hands it to `RunStreamingCoordinator`.
3. **Per window** — the first statement of `RunStreamingCoordinator.GenerateNextWindow()` is
   `_biomeDirector.ApplyForWindow(_windowIndex)` — **before** `PlanWindow`, so the planner's
   allocators (monster pools) and the area generator's loot rolls (both read
   `ICurrentThemeProvider` live) see the active stretch. Window 0 is applied inside
   `coordinator.Begin()` before any theme read (`CurrentThemeProvider` throws on read-before-set).
4. **Stretch crossing** — the director switches the theme, publishes the tier fact, and notifies
   the entrypoint, which swaps the landmark dressing (`RouteLandmarkSpawner.ApplyBiome`) and
   rebuilds the backdrop for the new biome (skipped when the notified theme is already built —
   the window-0 no-op). Already-spawned landmarks and platforms keep their look: the country
   behind the player.

**Timing note:** the coordinator plans one window ahead of the player's feet (windows advance on
platform *exit*), so the theme/fact/backdrop switch fires when the **next** window is generated —
the player sees the new biome's platforms arrive ahead while finishing the old stretch's last
platform. The `run_escalation_tier` fact therefore leads the player's position by up to one window.

### 2.4 DI wiring

`AreaInstaller`: inspector field `_biomeProgressionConfig`, auto-loads from
`Resources/World/Biomes/BiomeProgressionConfig` when unset (missing → warning + fixed-Forest
default). Binds `BiomeProgressionSettings` (mapped) and `IBiomeJourney` `AsSingle`.
`BiomeStretchDirector` is constructed by the entrypoint (it needs the scene-object observer), not
container-bound. `ICurrentThemeProvider` stays bound in `LootInstaller`; the director is its only
writer now.

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

### `BiomeProgressionConfig`  (asset menu: `Create → World → Biome Progression`)

Loaded from `Resources/World/Biomes/BiomeProgressionConfig` (or wired into the `AreaInstaller`
inspector field). One asset per project — the run rotation roster.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_biomes` | `List<Entry>` | One entry per biome in the rotation | empty = fixed Forest fallback |
| `Entry._theme` | `LevelTheme` | Which biome this entry puts into the rotation | — |
| `Entry._escalationTier` | int (≥1) | Ordering key: the run climbs from the lowest authored tier upward | 1 |
| `Entry._selectionWeight` | int (≥0) | Weight within the tier's seeded pick; **0 = excluded** without deleting the entry | 1 |
| `Entry._stretchMinWindows` | int (≥1) | Minimum stretch length in planning windows | 3 |
| `Entry._stretchMaxWindows` | int (≥1) | Maximum stretch length (swapped if authored inverted) | 4 |

References no other assets. The biome's **look / monsters / loot** come from its existing
per-theme configs (`BiomeAppearanceDefinition` in `Resources/World/Biomes`,
`BiomeMonsterPoolDefinition` in `Resources/Combat/MonsterPools`, `BiomeLootDefinition` in
`Resources/Loot/Biomes`) — this config only decides **which biome is active** for a stretch.

Current authored data (PO-approved 2026-07-04): Forest tier 1 · Mountain tier 2 · Desert tier 2
(all weight 1, stretch 3–4 windows); **Cave has no entry**. Mountain+Desert sharing tier 2 is what
makes journeys diverge by seed today; the lore's "desert sits high" re-tiering is a data edit once
a fourth biome exists.

Related asset: the fact key **`Fact_RunEscalationTier`** (`Resources/Narrative/Facts/`,
`_key: run_escalation_tier`, Int/Global/World) is registered in `DemoFactKeyRegistry` — the fact
store is fail-closed, so deleting it from the registry silently drops the tier publishes (warned).

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### Add a biome to the rotation

1. Make sure the biome's per-theme content assets exist (each is optional but recommended):
   appearance (`Create → World → Biome Appearance` → `Resources/World/Biomes/`), monster pool
   (`Create → Combat → Enemies → Biome Monster Pool` → `Resources/Combat/MonsterPools/`), loot
   table (`Create → Loot → Biome Loot` → `Resources/Loot/Biomes/`).
2. Open `Resources/World/Biomes/BiomeProgressionConfig` and add an entry: pick the `LevelTheme`,
   set its escalation tier, selection weight, and stretch min/max windows.
3. Enter play mode: runs whose climb reaches that tier can now route through the biome.

### Exclude a biome from the rotation

- Delete its entry, **or** set its `_selectionWeight` to 0 (keeps the authored numbers for later).
  This is how Cave is excluded today (no entry).

### Re-tier / re-weight a biome

- Edit `_escalationTier` (moves it earlier/later in the climb) or `_selectionWeight` (more/less
  likely within its tier). Data only; existing seeds will produce different journeys — that is
  expected and confined to the journey stream (story/enemy picks are unaffected).

### Tune stretch lengths

- Edit `_stretchMinWindows`/`_stretchMaxWindows` per biome. One window =
  `RunPacingConfig.WindowSize` platforms (4 today), so 3–4 windows ≈ 12–16 platforms.

**Authoring constraints / gotchas:** duplicate `LevelTheme` entries — first authored wins
(warning); inverted min/max — swapped silently; a tier whose entries are all weight-0 is not an
eligible tier; a biome without a monster pool asset downgrades its ambient-combat slots to Empty
(allocator warning), and without a loot/appearance asset falls back to code defaults — author all
three for a legible crossing.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/` (pure suites verified green via the bundled
Roslyn runner, 15/15, 2026-07-04):

- `BiomeJourneyTests` — same seed → identical journey; seeds diverge within a shared tier; first
  stretch = lowest tier; climbs one tier per stretch and clamps at top (non-contiguous tiers);
  stretch lengths in authored range and contiguous tiling; unlisted theme never appears (Cave);
  zero-weight never picked; no immediate repeat when the tier offers an alternative; single-biome
  top tier repeats without error; empty settings → Forest fallback + one warning; `ForWindow`
  idempotent and query-order-independent.
- `BiomeStretchDirectorTests` — first window sets theme + publishes tier fact + notifies; repeat
  windows of a stretch write nothing; stretch change switches theme/fact/notification; null
  observer safe.
- `BiomeProgressionConfigMapperTests` (SO-touching; compile-checked, runs in-editor) — null config
  → Forest default; field mapping; inverted range normalized; duplicate theme first-wins;
  zero-weight passes through.

Verified manually in play mode (thin adapters): the entrypoint observer (backdrop rebuild +
landmark re-dress on crossing), coordinator hook ordering, `AreaGenerator` live loot theme.

---

## 6. Known limitations / open points

- **Route/landscape *shape* is frozen to the entry biome.** `RunRouteModel` keeps the first
  biome's `BiomeLandscapeSettings` (corridor width, weave, tier step) for the whole run — swapping
  mid-run would discontinuously jump `Sample(x)` (a Z/Y teleport at the boundary) and desync the
  landmark scan cursor. Landmark dressing, backdrop, monsters, and loot DO follow the stretch. A
  piecewise, blended multi-biome route belongs to the M5 transition-art pass. *(ROADMAP: World &
  Environment)*
- **Backdrop swap is a hard cut** (destroy + rebuild on crossing). Gate/skyline/blend transition
  art is the M5 site-dressing / world-backdrop item (out of scope per the brief).
- **Mountain/Desert monster pools are placeholders** — they reuse the two Forest demo enemies so
  ambient combat doesn't vanish outside Forest. Real per-biome rosters are the ambient-monster
  content item (P1-13). *(ROADMAP: Data-Driven Procedural Narrative)*
- **Nothing consumes `run_escalation_tier` yet** — published only (the D19 escalation seam:
  difficulty/tone/density scaling by tier is the separate Escalation design thread).
- **No cross-run persistence** of the journey (rides the general save/load work, R14/P2-2); the
  journey's own `DeterministicRandom.State` is serializable when that lands.
- **Platform ground/material look does not change per biome** — that is the biome-visual-styles
  brief (P1-2), which extends `BiomeAppearanceDefinition`; the journey already switches whichever
  appearance exists.
