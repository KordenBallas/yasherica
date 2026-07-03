# World Sites — Requirements & Design

> Sites give the streaming world **shape**: a content beat pulls a contiguous multi-platform
> settlement or landmark (Camp/Village/City/Ruin/Lair) into being, filled by an authored capacity
> recipe in the shared `base·flavor` content vocabulary; everything else stays Wild. Builds on the
> world-content-density allocator (see `narrative-procedural.md` §2.6). PO brief:
> `product-requirements/world-sites-and-landscape.md`.
> Status: current as of 2026-07-03 — **phase 3 of 4**: reservation is **live** — the planner runs on
> the site-aware allocator, sites appear in the streamed run (quest-pulled settlements + rare
> ambient landmarks, flavored combat picks, townsfolk Npc slots). Phase 4 (content landing on the
> platforms: stamp/flavor onto graph nodes, loot bias, townsfolk chatter stories) remains — §6.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

From the verified PO brief (`product-requirements/world-sites-and-landscape.md`):

### 1.1 Functional requirements

- **R1** Two independent axes describe the physical world: **Biome** (existing `LevelTheme`) ×
  **Site** (the settlement/structure layer). Any site can occur in any biome.
- **R2** **Content-first:** a Site is pulled into being by a settlement-scale content beat (a city
  quest NPC → a city; bandits → a camp). Ambient content needs no site — it is **Wild**, the
  world's majority; sites are rare.
- **R3** A Site occupies a **contiguous cluster of N platforms** (City 4–5, Village 2–3,
  Camp/Ruin/Lair 1–2), reserved together as one block — never one oversized platform.
- **R4** Each Site fills its footprint by a **capacity recipe**: guaranteed **anchor beat(s)** (the
  site's reason to exist, emitted first) + a **weighted fill** budget drawn from a table of content
  kinds + a **connective Empty remainder** (never dead space).
- **R5** All content is named in **one vocabulary**: 4 base kinds (**Empty/Loot/Combat/NPC**) × an
  open set of flavor tags. Quest-bearing and hostile are **derived** from the NPC beat (the
  `NpcIntentResolver` model), never separate kinds. Corpse-loot is a Combat outcome on the separate
  loot channel — never a placed Loot beat.
- **R6** Committed sites: **Camp, Village, City** (family Settlement), **Ruin, Lair** (family
  Landmark); **Wild** is the no-site baseline, not a family member.
- **R7** **Extensibility is first-class:** a new site or flavor is one authored asset, no code;
  recipes are a **family default + per-site override**; attributes are additive/optional with
  defaults (a new attribute leaves every existing site valid); recipes reference content
  kinds/tags, never literal content assets.
- **R8** A quest may stay **Wild** (a lone wanderer): the quest-channel roll weighs "no settlement"
  (`WildQuestWeight`) against the quest-anchored sites' trigger weights. A story can **demand** a
  site with a `site:<id>` story tag *(PO decision 2026-07-03)*.
- **R9** A site's **trigger channel derives from its first anchor beat** — NPC anchor → the quest
  roll; Combat/Loot anchor → a rare, spaced ambient roll. Camp (family Settlement, anchor
  `Combat·bandit`) therefore triggers on the ambient channel *(PO decision 2026-07-03)*.
- **R10** **Determinism:** the same run seed produces the same sites, footprints, and content mix.

### 1.2 Non-functional requirements

- **N1** Site logic is pure C# (no UnityEngine), unit-tested outside Unity.
- **N2** The Wild allocation path is byte-for-byte unchanged: with no sites authored, the world
  behaves exactly as the density brief shipped it (a passthrough, proven by regression test).
- **N3** Visual dressing (skyline/gate/shared palette) is **out of scope** — this system only
  threads the `SiteStamp` data seam the M5 site-dressing pass will read.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/World/Sites/
  Core/                 — pure C# site domain (no UnityEngine)
  Data/                 — SiteDefinition/SiteFamilyDefinition SOs + SiteCatalogMapper (the only SO -> Core bridge)
Scripts/Narrative/Director/Core/
  IWorldSlotAllocator.cs      — the planner's allocation seam (site-aware)
  SiteAwareSlotAllocator.cs   — wraps the untouched WorldContentAllocator
```

Dependency flow is one-way: the director consumes `World.Sites.Core`; the site domain knows nothing
about the allocator or planner (it references only `IRandomSource`, a shared utility already
roadmapped for extraction to a neutral namespace).

### 2.2 Core domain types (`World.Sites.Core`)

| Type | Responsibility |
|---|---|
| `ContentBaseKind` | The 4-kind stable spine: Empty / Loot / Combat / Npc. |
| `ContentBeat` | One `base·flavor` beat: a base kind + open flavor string (empty = unflavored). |
| `WeightedBeat` | A fill-table entry: beat + integer draw weight. |
| `SiteTriggerChannel` | Quest (NPC-anchored) vs Ambient (combat/loot-anchored) — derived, R9. |
| `SiteStamp` | A platform's site membership: siteId, run-unique instanceId, index-in-block, footprint, dressingThemeId. `SiteStamp.Wild` = default. |
| `SiteDefinitionData` | The immutable effective site record (post family-merge): footprint range, trigger weight, anchor beats, fill budget + table, dressing theme; derives its trigger channel from anchor[0]. |
| `SiteSlot` | One built block slot: `ContentBeat` + `SiteStamp`. |
| `ISiteCatalog` / `SiteCatalog` | The run's authored sites split by channel (ordered by site id for determinism) + `NpcFillFlavors` (union of NPC fill flavors — the planner's quest-pick exclusion set). |
| `SiteBlockBuilder` | Builds one site instance: rolls footprint, emits anchors first, rolls the fill budget (clamped to footprint − anchors), draws fills from the weighted table, pads with connective Empty; stamps every slot. |

### 2.3 The allocation seam (`Narrative.Director.Core`)

`IWorldSlotAllocator` is the planner-facing seam:

- `SlotAllocation AllocateSlot(bool questAvailable)` — as before, plus site-block slots.
- `SiteStamp TryReserveSettlement(IReadOnlyList<string> anchorStoryTags)` — called by the planner
  once per landed quest slot; decides Wild vs settlement (R8) and reserves the block.

`SiteAwareSlotAllocator` wraps the **untouched** `WorldContentAllocator` (N2):

1. **Pending queue first.** A reserved block's remaining slots drain before any new draw — this is
   how a 4–5 platform city **spans window boundaries** (the allocator is run-scoped, the same
   pattern as the quest-spacing counter).
2. **Inner draw.** Wild slots pass through bit-exact. A Quest result returns untouched — the
   planner decides settlement-vs-wild via `TryReserveSettlement` (quest precedence: the rarer beat
   wins the slot).
3. **Ambient site gate.** Mirrors the quest gate: a hard `MinPlatformsBetweenSites` spacing
   (measured from the previous block's end) + a 1-in-`AveragePlatformsPerAmbientSite` seeded roll,
   then a trigger-weighted pick among ambient-channel sites. On a hit the block's **shape is
   resolved fully at trigger time** from the shared director stream (R10) and the anchor slot
   supersedes the inner ambient draw.

`SlotAllocation` carries two new (defaulted) fields: `Flavor` (the beat's flavor tag) and `Site`
(the `SiteStamp`); `WorldSlotKind` gains `Npc` — a non-quest NPC beat (e.g. `NPC·townsfolk`) the
planner fills by flavor, never by the quest path. `PlannedPlatform` carries the same two fields.

**Flavored combat picks:** `IBiomeMonsterPoolCatalog` pools are tagged entries
(`MonsterPoolEntry` = enemy id + its `EnemyDefinition.EnemyTags`, mapped by
`BiomeMonsterPoolMapper`). A site Combat beat draws from `GetPool(theme, flavor)` (case-insensitive
tag match); when no enemy carries the tag the allocator falls back to the unfiltered pool (the
fight matters more than its flavor — warned once per flavor), and an entirely empty pool downgrades
the slot to Empty, keeping the stamp.

**Planner (`RunWindowPlanner`) integration:** the planner depends on `IWorldSlotAllocator` +
`ISiteCatalog`. Eligible stories are partitioned: a story tagged with any `NpcFillFlavors` entry is
**ambient colour** — it fills site `Npc` slots by flavor (unused stories preferred; a small chatter
pool may repeat with a fresh actor rather than leaving the site platform dead; none at all degrades
to Empty, warned once per flavor) and **never satisfies a Quest slot** (`questAvailable` counts
quest-eligible stories only). On a landed quest the planner calls
`TryReserveSettlement(story.StoryTags)` and stamps the quest platform as the block's anchor.
Townsfolk platforms plan as ordinary `Story` encounters, so downstream (casting at window mapping,
`NpcIntentResolver`) derives `Plain` naturally — no quest slot filled, no forced combat.

`WorldContentDensitySettings` gains three dials (defaults preserve behavior until authored):
`AveragePlatformsPerAmbientSite` (default 14; 0 disables), `MinPlatformsBetweenSites` (default 6),
`WildQuestWeight` (default 40).

### 2.4 DI wiring

`NarrativeSliceInstaller` (`Scripts/Core/DI/NarrativeSliceInstaller.cs`):
- `ISiteCatalog` — built once at install by `SiteCatalogMapper.ToCatalog(_siteDefinitions, logger)`;
  the inspector list falls back to `Resources.LoadAll<SiteDefinition>("World/Sites")` (the project's
  auto-load convention). Family assets ride in through each site's `_family` reference.
- `SiteBlockBuilder` — bound `AsSingle` (stateless).
- The three site dials ride the existing `WorldContentDensitySettings` binding (mapped from
  `WorldContentDensityConfig`).

- `IWorldSlotAllocator` → `SiteAwareSlotAllocator` wrapping the still-bound
  `WorldContentAllocator`, sharing the director's `IRandomSource` stream (replay determinism);
  the `RunWindowPlanner` binding consumes it plus `ISiteCatalog`.

---

## 3. ScriptableObject Reference

### `SiteFamilyDefinition`  (asset menu: `Create → World → Sites → Site Family`)

Loaded indirectly — referenced by each `SiteDefinition._family`. Authored assets:
`Resources/World/Sites/Families/{SettlementFamily,LandmarkFamily}.asset`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_familyId` | string | Stable family id (`settlement` / `landmark`) | recorded on the mapped site |
| `_defaultAnchorBeats` | List<ContentBeatEntry> | The family's default anchor beat(s) — kind + flavor. The **first** anchor's kind decides the trigger channel (Npc → quest roll; Combat/Loot → ambient roll) | Settlement: `NPC·quest-bearer`; Landmark: `Combat·den-monster` |
| `_defaultFillBudgetMin/Max` | int | Default per-instance fill-budget roll range | Settlement 1–1; Landmark 0–1 |
| `_defaultFillTable` | List<WeightedBeatEntry> | Default weighted fill table (kind + flavor + weight). **Never list corpse-loot** — it is a Combat outcome | Settlement: townsfolk 3 / scattered 1; Landmark: den-monster 2 / scattered 1 |

### `SiteDefinition`  (asset menu: `Create → World → Sites → Site Definition`)

Loaded from `Resources/World/Sites/` (or the `NarrativeSliceInstaller` "World Sites" list).
Authored assets: `Camp / Village / City / Ruin / Lair`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_siteId` | string | Stable id — referenced by `site:<id>` story tags and the dressing pass | empty = skipped (warned) |
| `_displayName` | string | Designer-facing label | unused by the engine |
| `_family` | SiteFamilyDefinition | The family whose defaults this site inherits | null + no anchor override = skipped (warned) |
| `_footprintMin/Max` | int | Contiguous platform-count range the block reserves | City 4–5, Village 2–3, Camp/Ruin/Lair 1–2 |
| `_triggerWeight` | int | Relative weight among same-channel sites when a trigger lands | 0 = only via a `site:` tag |
| `_dressingThemeId` | string | Dressing-theme key stamped on every block platform (M5 seam) | inert today |
| `_overrideAnchorBeats` + `_anchorBeats` | bool + List<ContentBeatEntry> | Toggle **on** = replace the family anchors (e.g. Camp → `Combat·bandit`) | off = inherit |
| `_overrideFillBudget` + `_fillBudgetMin/Max` | bool + int | Toggle **on** = the site's own budget roll range (e.g. City 2–3) | off = inherit |
| `_overrideFillTable` + `_fillTable` | bool + List<WeightedBeatEntry> | Toggle **on** = the site's own weighted fill table (e.g. City: townsfolk 5 / market 3 / guard 2) | off = inherit |

`ContentBeatEntry` = `_kind` (`Empty/Loot/Combat/Npc`) + `_flavor` (open string).
`WeightedBeatEntry` = the same + `_weight` (relative, ≥0).

**Validation (mapper, warn + skip):** missing `_siteId`; duplicate `_siteId` (first asset wins); no
effective anchor beat (no family and no override — a site needs a reason to exist).

### `WorldContentDensityConfig` — site dials (extension)

The one world-fullness asset (`Resources/Narrative/WorldContentDensityConfig.asset`) gains:

| Field | Type | Meaning | Default |
|---|---|---|---|
| `_averagePlatformsPerAmbientSite` | int | ~1 ambient-channel site per this many platforms; **0 disables ambient sites** | 14 |
| `_minPlatformsBetweenSites` | int | Hard minimum platforms between one block's end and the next site | 6 |
| `_wildQuestWeight` | int | Weight of "no settlement" in the quest-channel roll (the lone wanderer); rolls against the quest sites' `_triggerWeight`s | 40 (≈40% wild vs Village 40 / City 20) |

---

## 4. Adding Content

### Add a site

1. (Optional) pick or author its family: `Create → World → Sites → Site Family` under
   `Resources/World/Sites/Families/` — id + default anchors + default fill budget/table.
2. `Create → World → Sites → Site Definition` under `Resources/World/Sites/`; set `_siteId`,
   `_family`, footprint range, `_triggerWeight`, `_dressingThemeId`.
3. Override only the delta: toggle `_overrideAnchorBeats` / `_overrideFillBudget` /
   `_overrideFillTable` and fill the overriding values; everything left off inherits the family.
4. Remember: the **first anchor's kind** picks the trigger channel — an `Npc` anchor makes it a
   quest-pulled settlement; `Combat`/`Loot` makes it an ambient landmark-style trigger.
5. No code, no installer edit — the asset auto-loads from `Resources/World/Sites`.

### Revise a whole family

Edit the family asset's defaults — every site of that family that doesn't override the field shifts
with it (e.g. make all Settlements fill one extra beat by raising the family budget).

### Make a story demand a site

Add a `site:<id>` entry to the `StoryTemplate`'s story tags. When that story is picked for a landed
quest slot, the site is reserved as a hard request (no roll). An unknown id warns and stays Wild.

### Add a combat flavor (e.g. a new `Combat·pack` beat)

1. Add the flavor string to the site's fill table / anchor (`_kind: Combat`, `_flavor: pack`).
2. Tag at least one `EnemyDefinition` in the biome's monster pool with the same string in
   `_enemyTags`. Matching is case-insensitive. Untagged flavor → the fight still lands from the
   unfiltered pool (warned once).

### Add a loot flavor / a townsfolk story

> **Phase 4 (§6).** Loot `BiasTags` consumption and townsfolk chatter stories land next; a
> townsfolk beat authored today degrades to Empty (warned) until a story carries the
> `townsfolk` tag.

**Authoring constraints / gotchas:** a duplicate `_siteId` is skipped (first wins); a site with no
family and no anchor override is skipped; corpse-loot must never appear in a fill table (it is the
outcome of a Combat beat); `NPC·quest-bearer` is an **anchor**, not a fill flavor (see §6).

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `SiteBlockBuilderTests` (8) — footprint range; anchor-first; fill budget respected and clamped;
  connective remainder; anchor-only on an empty fill table; sequential stamps sharing instance and
  footprint; same-seed identical blocks; fills drawn from the table.
- `SiteAwareSlotAllocatorTests` (13) — **empty catalog is a bit-exact passthrough** of
  `WorldContentAllocator` (N2, incl. no roll consumed by `TryReserveSettlement`); ambient gate
  spacing measured from block end; disabled gate never triggers; block queue drains in order across
  calls; quest precedence over the ambient gate; `site:` tag hard request; unknown tag +
  zero-weight sites stay Wild; zero wild-weight always settles; flavored combat picks the tagged
  enemy / falls back unfiltered; empty-pool site combat downgrades to Empty keeping the stamp;
  same-seed identical site sequences; catalog channel split + `NpcFillFlavors` derivation.
- `BiomeMonsterPoolCatalogTests` (6) — unfiltered lookup; case-insensitive flavor filter; empty
  flavor falls through; unmatched flavor returns empty (caller owns the fallback); unknown theme;
  ids-only constructor stays untagged.
- `RunWindowPlannerTests` (+3 site cases) — a townsfolk Npc fill picks the flavor-tagged story with
  a fresh actor and the site stamp (repeating a one-story chatter pool rather than dying); ambient
  colour stories never satisfy Quest slots; a missing chatter pool degrades the fill to Empty
  keeping the stamp. The 16 pre-site planner tests run **unchanged over the site-aware allocator**
  (passthrough proof at the planner level).
- `SiteCatalogMapperTests` (7) — family inheritance; override toggles replace only their delta;
  trigger channel derives from the overridden anchor (Camp → Ambient despite family Settlement);
  no-anchor / missing-id / duplicate-id skipped; `NpcFillFlavors` from effective fill tables;
  null/empty input → empty catalog. *(Unity edit-mode — exercises the SO layer.)*

The pure-C# suites run outside Unity via the bundled-Roslyn workaround
(`Temp/domaintests/run_sites.ps1` + `run_planner.ps1`); the mapper suite needs the editor's
edit-mode runner.

---

## 6. Known limitations / open points

Phased delivery (the PO brief's sequencing — each phase ships code + tests + docs together):

- **Phase 4 (next): content landing.** Stamp/flavor flow onto `GraphNode` and into the loot roll
  context (`Loot·market/stash/chest/relic` as `BiasTags`); townsfolk chatter stories + Ink; enemy
  flavor tags on the forest pool; acceptance histogram test. Until it lands: townsfolk Npc slots
  degrade to Empty (no story carries the tag yet — warned), site loot rolls the plain biome table,
  and flavored combat picks fall back to the unfiltered pool (no enemy is tagged yet — warned).
- **Anchor-first ordering.** The anchor is always the block's first platform (its eligibility was
  verified in the triggering window). A density gradient toward a core is dressing-driven — the M5
  site-dressing item.
- **Deferred schema fields** *(PO decision 2026-07-03)*: occupancy/passport gating, tier/altitude
  eligibility + tonal register, and biome compatibility are not authored — their consuming systems
  don't exist yet. The additive-defaults contract (R7) makes adding them later migration-free.
- **`NPC·quest-bearer` as a *fill* beat** (the design table's Camp "shady offer") is not supported:
  an NPC fill flavor is planner-matched by story tag, and quest semantics on a fill slot are
  undefined. Camps are authored without it; needs its own design pass if wanted.
- **Visual dressing** — the whole "reads as one place" pass (skyline, gate, shared palette,
  backdrop) is the M5 *Site dressing* ROADMAP item; this system only carries `SiteStamp` for it.
