# Races & Passport — Requirements & Design

> The world's peoples as data, and "the world reads what you are" as a built rule: a small roster of
> races (one asset each), a race tag on every body part, and a derived per-race **acceptance tier**
> (0 outsider / 1 tolerated / 2+ kin = count of that race's tagged parts equipped) exposed as the
> narrative fact `faction.<raceId>.reads_as_tier`. Consumes the brief
> `product-requirements/race-roster-and-passport.md`; design background in `design/narrative/races.md`.
> Status: current as of 2026-07-04.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** A starting roster of three races exists as data — Ibex (Mountain), Lizard (Desert),
  Fox (Forest) — each holding a stable id, display name, home biome (`LevelTheme`), and belonging
  colour. One `RaceDefinition` asset per race.
- **R2** Every body part carries a race tag: a race id string on `PartDefinition`, empty = kindless.
  The hero's starting parts (the `PlaceholderAssembly_A` set) are kindless. A part's race is
  independent of the abilities it grants.
- **R3** Adding a race or re-tagging a part is data authoring only — no code change. The roster is
  not hard-coded to three; the fact key, calculator, and projector are race-id-agnostic.
- **R4** Acceptance tier per race = count of that race's tagged parts currently equipped, clamped to
  0 = outsider / 1 = tolerated ("one of us, but a freak") / 2+ = kin. Any tagged part counts — there
  is no special marker slot.
- **R5** No hard conflict between races: parts never forbid each other. Wearing a fox part and a
  lizard part makes the hero tier-1 to both; scarcity comes from the finite slot budget.
- **R6** The tier is exposed as a per-race narrative fact — `faction.<raceId>.reads_as_tier` (Int),
  subject = race id — that story/dialogue preconditions gate on (e.g. "reads as Fox ≥ 1"). This
  replaces the retired `world.reads_as_frogfolk` bool.
- **R7** The tier updates as the body changes: every successful part swap recomputes and rewrites
  all roster tiers before the next encounter reads them. The Hub's starting-part install (O1,
  `hub-staging.md`) yields the 1-marker "tolerated freak" through this **unchanged** projection —
  no passport-side code knows about the Hub.
- **R8** Determinism: the same equipped body always yields the same per-race tiers.

### 1.2 Non-functional requirements

- **N1** Core logic (`RaceData`, `RaceRoster`, `RaceAcceptanceCalculator`) is pure C# and unit-tested.
- **N2** All wiring through Zenject (`AreaInstaller` + `NarrativeSliceInstaller`); no service locators.
- **N3** `RaceDefinition` is a data-only ScriptableObject; SO → Core crossing happens only in
  `RaceRosterMapper`.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/World/Races/
  Core/          — RaceData, IRaceRoster/RaceRoster, RaceAcceptanceCalculator (pure C#)
  Data/          — RaceDefinition (SO), RaceRosterMapper (the only SO -> Core bridge),
                   RaceIdAttribute (inspector drop-down marker)
  Integration/   — RacePassportProjector (single fact writer), RacePassportBinder (body -> projector)
Scripts/Editor/World/RaceIdDrawer.cs — draws [RaceId] strings as a drop-down over the authored races
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `RaceData` | One race: id, display name, home biome. Colour stays on the SO (no Unity types in Core). |
| `IRaceRoster` / `RaceRoster` | Id-keyed catalog in stable authored order; duplicate ids first-wins + warn. |
| `RaceAcceptanceCalculator` | Pure projection: equipped race ids → per-race tier, clamped at `KinTier` (2). Kindless/unknown ids never count. |
| `RacePassportProjector` | The **single writer** of `reads_as_tier`: equipped part ids → race tags (via `IPartCatalog`) → calculator → `SetInt` for **every** roster race, so an un-worn race drops back to 0. |
| `RacePassportBinder` | `IInitializable`/`IDisposable`; subscribes to the hero's `ModularCharacterVisual.CharacterAssembled` + `IModularCharacter.PartsChanged` and calls `Recompute`. |

### 2.3 Runtime flow

1. `AreaInstaller` maps the `RaceDefinition` assets into `IRaceRoster`.
2. The hero's rig assembles (`ModularCharacterVisual.Start`) → `CharacterAssembled` fires →
   `RacePassportBinder` recomputes: hero starts kindless → every tier written 0 (which also equals
   the registry default, so pre-assembly reads are already correct — B4).
3. A mutation equips a part (`CharacterAssemblyController.SwapPart`) → `PartsChanged` fires →
   projector rewrites all roster tiers synchronously — fresh before the next encounter (R7).
4. Story selection (`RunWindowPlanner` → `PreconditionEvaluator`) gates on the fact with a literal
   race-id subject token: `{namespace: Faction, subject: fox, key: reads_as_tier, op: Gte, value: 1}`.
   The demo `frog_marsh` thread is the built example (§4.3).

Only the hero's bound visual is watched; the mutation preview rig's clone controllers never reach
the fact store.

### 2.4 DI wiring

- `AreaInstaller.InstallRaceBindings()` — `IRaceRoster` from the inspector list, falling back to
  `Resources.LoadAll<RaceDefinition>("World/Races")`. An empty roster is a valid raceless world.
- `NarrativeSliceInstaller.InstallRacePassport()` — `RacePassportProjector` (`AsSingle`) +
  `RacePassportBinder` (`BindInterfacesAndSelfTo`, `NonLazy`). Depends on `IFactStore` (same
  installer), `IPartCatalog` (`CharacterSystemInstaller`), `ModularCharacterVisual`
  (`MutationInstaller`) — one shared Area-scene container.
- Fact key: `Resources/Narrative/Facts/Fact_ReadsAsTier.asset`, listed in `DemoFactKeyRegistry`;
  curated ref `FactionFacts.ReadsAsTier` in `TypedFacts.cs` (drift-checked at startup).

---

## 3. ScriptableObject Reference

### `RaceDefinition`  (asset menu: `Create → World → Race`)

Loaded from `Resources/World/Races/` (or the `AreaInstaller` `_raceDefinitions` list).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_raceId` | string | Stable id: the part race-tag value and the `reads_as_tier` fact subject | lowercase, no spaces; empty = asset skipped + warning |
| `_displayName` | string | UI name (e.g. "Ibex-folk") | empty = falls back to asset name |
| `_homeBiome` | `LevelTheme` | The biome this race calls home | Ibex=Mountain, Lizard=Desert, Fox=Forest; Cave has no race yet |
| `_belongingColor` | `Color` | Belonging hue for the quest card grammar (a Part-Blank reward's race colour). | white; consumed by Track H via `BelongingTintCatalog` (P0-3·b) |

### Race tag on `PartDefinition`  (existing asset menu: `Create → Character System → Part`)

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_raceId` | string | Race this part reads as for the passport | empty = kindless; drawn as a drop-down (`[RaceId]` + `RaceIdDrawer`) listing `(kindless)` + every authored race id |

### Fact key `Fact_ReadsAsTier`  (`FactKeyDefinition`, in `DemoFactKeyRegistry`)

| Property | Value |
|---|---|
| Key | `reads_as_tier` — namespace `Faction`, scope `PerFaction` (subject = race id), type Int, default 0 |
| Written by | `RacePassportProjector` only |
| Read by | any authored precondition, e.g. `faction.fox.reads_as_tier >= 1` |

---

## 4. Adding Content

### 4.1 Add a race (data only, no code)

1. `Create → World → Race` under `Resources/World/Races/`, named `Race_<Name>`.
2. Fill `_raceId` (lowercase stable id), `_displayName`, `_homeBiome`, `_belongingColor` (§3).
3. Done — the roster auto-loads, the projector starts publishing `faction.<id>.reads_as_tier`
   (no new fact asset needed: the one `reads_as_tier` key covers every race via its subject), and
   the id appears in every part's race drop-down.

### 4.2 Tag a body part to a race

1. Open the `PartDefinition` asset (`Resources/CharacterSystem/Parts/…`).
2. Pick the race in the **Race (passport marker)** drop-down (or `(kindless)` to clear).
3. Any tagged part counts toward that race's tier while equipped — no other wiring.

Current demo tagging: `Part_Head_B`/`Part_ArmR_B` → ibex, `Part_Torso_B`/`Part_ArmL_B` → lizard,
`Part_LegL_B`/`Part_LegR_B` → fox; the whole starting `_A` set is kindless.

### 4.3 Gate a story / dialogue on a tier

Author a precondition on the `StoryTemplate` (`_preconditions`):

```yaml
- _namespace: 2        # Faction
  _subjectToken: fox   # the race id, as a literal
  _key: reads_as_tier
  _op: 3               # Gte (4 = Lt for the closed-door variant)
  _value: {_valueType: 1, _intValue: 1}
```

Built example — the demo `frog_marsh` thread: `DemoStory_MarshPool` (hint, `Lt 1`),
`DemoStory_FrogElderClosed` (`Lt 1`), `DemoStory_FrogElderOpen` (`Gte 1`, offers the errand quest).
Equipping one fox-tagged part flips the marsh from closed to open.

**Authoring constraints / gotchas:** a part `_raceId` that matches no race asset warns at recompute
and never counts (typo-proof, the drawer shows it as `<id> (missing)`); duplicate `_raceId` race
assets keep the first authored; the tier clamps at 2 — a precondition `Gte 3` can never pass.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `RaceRosterMapperTests` — null/empty roster warns, field mapping, authored order, empty-id skip,
  duplicate-id first-wins, display-name fallback.
- `RaceAcceptanceCalculatorTests` — 0/1/2/3 parts → 0/1/2/2; two races both tier-1 (R5); kindless
  and unknown ids ignored (+warn); determinism (R8); null roster.
- `RacePassportProjectorTests` — writes every roster race each pass, tier drops to 0 on swap-away,
  unknown part id tolerated, and the gating integration test: a real `FactStore` +
  `PreconditionEvaluator` flip `faction.fox.reads_as_tier >= 1` from false to true after one fox part.

Verified manually in play mode: the marsh elder closed → open flip after a fox-part mutation
(`RacePassportBinder` + `CharacterAssembled`/`PartsChanged` plumbing are Unity-side).

---

## 6. Known limitations / open points

- **Belonging colour is consumed by Track H** (P0-3·b): `BelongingTintCatalog` merges it with the
  artifact `RewardFamilyDefinition` colours for the quest-offer card + quest log tint (⚠ gameplay-untested).
- **No un-equip path**: the assembly controller only swaps parts, so tiers move on swap; a future
  remove/un-equip surface must also raise `PartsChanged`.
- **Exposure / betrayal** (trust flipping to horror on deeper mutation, `heresy_exposed`) is design
  only — not built.
- **Per-race questlines, NPC casts, signature enemies, marker part meshes / body-plans** are the
  downstream content step (P2-1 and the design track), not this system.
- **Cave has no starting race**; the roster accepts a fourth race as pure data when designed.
- The mutation blank's `SpeciesArchetypeId` (what a *blank* reads as) and the part race tag are
  separate axes today; reconciling them is a ROADMAP item.
