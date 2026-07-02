# World Sites — Requirements & Design

> Sites give the streaming world **shape**: a content beat pulls a contiguous multi-platform
> settlement or landmark (Camp/Village/City/Ruin/Lair) into being, filled by an authored capacity
> recipe in the shared `base·flavor` content vocabulary; everything else stays Wild. Builds on the
> world-content-density allocator (see `narrative-procedural.md` §2.6). PO brief:
> `product-requirements/world-sites-and-landscape.md`.
> Status: current as of 2026-07-03 — **phase 1 of 4** (domain model + reservation logic landed and
> tested; not yet bound in DI — the run still behaves exactly as before). §6 lists what each next
> phase adds.
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
A site Combat fill picks its enemy from the biome monster pool (flavor-filtered pools are §6).

`WorldContentDensitySettings` gains three dials (defaults preserve behavior until authored):
`AveragePlatformsPerAmbientSite` (default 14; 0 disables), `MinPlatformsBetweenSites` (default 6),
`WildQuestWeight` (default 40).

### 2.4 DI wiring

> **Not yet bound (phase 1).** Nothing constructs `SiteAwareSlotAllocator` at runtime yet; the
> planner still depends on `WorldContentAllocator` directly. Phase 3 swaps the planner to
> `IWorldSlotAllocator` in `NarrativeSliceInstaller`. Until then the run is behavior-identical.

---

## 3. ScriptableObject Reference

> **None yet (phase 1).** The `SiteFamilyDefinition` / `SiteDefinition` SO schema and the
> `WorldContentDensityConfig` field extension land in phase 2 (§6).

---

## 4. Adding Content

> **Not yet authorable (phase 1).** The asset-only recipes (*Add a site*, *Add a content flavor*,
> *Add a townsfolk story*, *Make a story demand a site*) arrive with the phase-2 SO schema and the
> phase-4 content pass (§6).

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `SiteBlockBuilderTests` (8) — footprint range; anchor-first; fill budget respected and clamped;
  connective remainder; anchor-only on an empty fill table; sequential stamps sharing instance and
  footprint; same-seed identical blocks; fills drawn from the table.
- `SiteAwareSlotAllocatorTests` (11) — **empty catalog is a bit-exact passthrough** of
  `WorldContentAllocator` (N2, incl. no roll consumed by `TryReserveSettlement`); ambient gate
  spacing measured from block end; disabled gate never triggers; block queue drains in order across
  calls; quest precedence over the ambient gate; `site:` tag hard request; unknown tag +
  zero-weight sites stay Wild; zero wild-weight always settles; empty-pool site combat downgrades
  to Empty keeping the stamp; same-seed identical site sequences; catalog channel split +
  `NpcFillFlavors` derivation.

Run outside Unity via the bundled-Roslyn workaround (`Temp/domaintests/run_sites.ps1`).

---

## 6. Known limitations / open points

Phased delivery (the PO brief's sequencing — each phase ships code + tests + docs together):

- **Phase 2 (next): SO schema + assets.** `SiteFamilyDefinition` (family-default recipe) +
  `SiteDefinition` (per-site override with explicit inherit toggles), `SiteCatalogMapper` (the one
  Data→Core bridge, family merge + validation), the three new `WorldContentDensityConfig` fields,
  catalog binding, and the authored Camp/Village/City/Ruin/Lair assets.
- **Phase 3: reservation goes live.** Planner depends on `IWorldSlotAllocator`; the `Npc` slot kind
  picks a flavor-tagged world-only story (and such stories are excluded from quest picks via
  `ISiteCatalog.NpcFillFlavors`); `TryReserveSettlement` stamps the quest platform;
  flavor-filtered monster pools (`Combat·bandit/guard/den-monster` via `EnemyDefinition.EnemyTags`,
  falling back to the unfiltered biome pool).
- **Phase 4: content landing.** Stamp/flavor flow onto `GraphNode` and into the loot roll context
  (`Loot·market/stash/chest/relic` as `BiasTags`); townsfolk chatter stories; acceptance histogram
  test.
- **Anchor-first ordering.** The anchor is always the block's first platform (its eligibility was
  verified in the triggering window). A density gradient toward a core is dressing-driven — the M5
  site-dressing item.
- **Site combat fills use the unfiltered biome pool** until the phase-3 flavored lookup.
- **Deferred schema fields** *(PO decision 2026-07-03)*: occupancy/passport gating, tier/altitude
  eligibility + tonal register, and biome compatibility are not authored — their consuming systems
  don't exist yet. The additive-defaults contract (R7) makes adding them later migration-free.
- **`NPC·quest-bearer` as a *fill* beat** (the design table's Camp "shady offer") is not supported:
  an NPC fill flavor is planner-matched by story tag, and quest semantics on a fill slot are
  undefined. Camps are authored without it; needs its own design pass if wanted.
- **Visual dressing** — the whole "reads as one place" pass (skyline, gate, shared palette,
  backdrop) is the M5 *Site dressing* ROADMAP item; this system only carries `SiteStamp` for it.
