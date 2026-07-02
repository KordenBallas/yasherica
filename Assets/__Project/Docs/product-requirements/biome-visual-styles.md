# Biome Visual Styles (Data-Driven) — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-02
> Level: product-owner (what & feel). The code track owns the technical "how".
> Inherits the decided art direction — do not restate it: `design/art/render-look.md` §2 (biome = a
> muted key modulation of the master palette; flat low-poly, no outline) and
> `design/art/site-dressing.md` §2 (biome = base layer; site dressing on top). Builds on
> `product-requirements/platform-hex-surface-and-shape.md` (features occupy whole hex cells; the
> battlefield minimum).

## Goal

Give each biome a **distinct visual identity, authored as data**. Today a platform wears a single
global material; there is no per-biome look. We add a **per-biome appearance config** — one asset per
biome (`LevelTheme`: Forest / Desert / Mountain / Cave), mirroring the existing per-biome **loot**
config — that defines the biome's ground, palette key, light, and its **characteristic features**, so
a designer can tune a biome's look and **add a new biome without code**, while the world keeps the
render-look bible's muted base so heroes, enemies, and loot still read.

This changes **how a biome looks** (and adds simple biome terrain); it does **not** change what
content a platform carries, nor combat damage rules.

## User stories

- As a player, each biome reads as a **distinct place** — a warm-green forest, an ochre desert, a
  cool grey mountain, a dark cave — through its ground, palette, light, and characteristic features,
  while the hero, enemies, and loot still **pop** against it.
- As a player, I occasionally cross **biome features** — trees, rocks, a river — and some **block a
  cell**, giving a little terrain to fight around.
- As a designer, I author a biome's **whole look** (ground/material, palette key, light, feature pool
  and density) as **one config asset**, and can **add a new biome** with no code change.
- As a designer, editing a biome config **visibly changes** that biome's look with no code change.

## Functional requirements

### Per-biome appearance config (data-driven)
1. **One appearance config per biome** (`LevelTheme`) defines its look: **ground material / mesh
   treatment**, **palette key**, **light / mood**, and a **feature pool** (below). It mirrors the
   existing per-biome loot config pattern (one asset per theme).
2. **Inherits the render-look bible** (`render-look.md` §2): the biome palette is a **muted key
   modulation** of the master palette (forest warm green-gold, desert sand-ochre, mountain cool
   grey-blue, cave dark cool), **flat low-poly, no outline**, kept muted so gameplay accents
   (hero/enemy/loot, archetype hue, tier glow) still read. Saturation is **not** spent on the biome.
3. **Biome is the base layer only** (`site-dressing.md` §2): ground / material / palette / light.
   **Site dressing** (structures, skyline, props) layers on top and is a **separate** concern.

### Biome features
4. **Feature pool.** The config lists which **biome features** may appear (e.g. forest trees/rocks,
   desert dunes/cacti, mountain boulders, cave stalagmites / a stream), each with its **look** and a
   **density / weight**. Placement is deterministic and on **whole hex cells** (platform brief).
5. **Decorative vs blocking.** Each feature is either **decorative** (visual only) or **blocking** —
   a blocking feature **occupies its cell and makes it unavailable** for movement/standing. **No
   cover / line-of-sight / hazard** rules; blocking is just "this cell is out". Blocking is a property
   of the feature.
6. **Respect the battlefield minimum.** On a combat-capable platform, blocking features must **not**
   reduce the playable field below the battlefield minimum (platform brief) — a fight always has room.

### Robustness
7. **Determinism.** The same run seed yields the same biome appearance and the same feature placement.
8. **Extensible.** Adding a **new biome** (a new config) or a **new feature** (a new pool entry) is
   **data authoring**, no code change.

## Content authoring rules (for the designer)
- Author **one appearance config per `LevelTheme`** (asset-menu alongside the biome loot config),
  holding: ground material/treatment, palette key, light, and the feature pool.
- Each **feature entry** carries: its visual asset, a **density/weight**, and a **decorative |
  blocking** flag.
- Keep the palette **muted** (inherit `render-look.md`); do not spend saturation on the biome — it is
  reserved for gameplay-meaningful accents.

## Acceptance criteria
- Each of the four biomes reads as a **distinct place** (ground/palette/light/features), with
  hero/enemies/loot still popping (muted base holds).
- **Biome features** appear at the authored density on **whole cells**; a **blocking** feature makes
  its cell **unavailable**; a **combat** platform keeps at least its battlefield minimum.
- **Editing a biome config** visibly changes that biome's look with **no code change**.
- **Adding a new biome / new feature** is data-only.
- **Same seed → same** biome look and feature placement.

## Out of scope / open points (do not build now)
- **Site dressing** (structures, skyline, props laid over the biome base) — a separate M5 item
  (`design/art/site-dressing.md`).
- **Which biome goes where along the run** (biome selection / placement, escalation through race
  homelands) — a generation / progression concern, not this brief.
- **Exact palette swatches** and the **flat-shading / figure-ground shader** — render-look follow-ups
  (tech-art), not this brief; here the biome look is data-authored and *inherits* the bible.
- **Full tactical terrain** — cover, line-of-sight, hazards — deferred (this brief is decorative +
  simple blocking only).
- **Biome loot** — already owned by the existing per-biome loot config; unchanged here.
- **No change** to combat damage rules (a blocking feature only removes a cell from play).
