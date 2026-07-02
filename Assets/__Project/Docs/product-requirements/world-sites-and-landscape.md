# World Sites & Landscape — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-01
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Builds on** the [World Content Density](world-content-density.md) brief (prerequisite).
> Fuller design background: `design/world/sites-and-landscape.md` + `design/world/content-kinds.md`.

## Goal

The density brief made the world *rare and breathing*, but every platform is still a uniform island.
This brief gives the world **shape**: **content decides how much world it needs and how that world
looks** — never the reverse. A city NPC pulls a **city** into being (a cluster of platforms dressed
as a city that reads as one place); a bandit raid pulls a **camp**; a monster den a **lair**. Most of
the world stays open **wilderness** the hero crosses fast; a settlement is a **rare, large event**
("civilisation at last"). Everything is authored from data and must stay revisable later without
breaking existing content.

## User stories

- As a player, I cross mostly open wilderness, and arriving at a settlement (camp / village / city)
  feels like a real **place** and a real **event** — not just another platform.
- As a player, a city reads as **one continuous town spanning several platforms** — a shared skyline
  and look, a sense of arriving and leaving — even though I'm hopping clean gaps between islands.
- As a player, a **city is busier** than a village — more townsfolk, a market to loot, maybe guards —
  while the **quest itself stays rare**.
- As a player, **wild places** (a ruin, a monster lair) are where I find relics/chests and fight
  tougher beasts, with **no townsfolk**.
- As a designer, I add or revise a site — or a kind of content — by **authoring data (one asset)**,
  and **revising an attribute months later doesn't force me to touch every existing site**.

## Functional requirements

### Two axes — Biome × Site
1. The physical world is described by two independent axes: **Biome** (existing — ground, palette, a
   race's homeland) and **Site** (new — the settlement/structures laid on top). A city can occur in
   any biome; a city-in-forest and a city-in-desert share the Site, differ in Biome. (Biome-locking a
   site is deferred, §out-of-scope.)

### Content-first — sites emerge from content
2. A **Site is pulled into being by a settlement-scale content beat** (a city NPC → a city; a raid →
   a camp). Ambient content (monsters, loot) needs no site — it is **Wild**. Because quests are rare
   (density brief), **Sites are rare**: the world is mostly wilderness, and a settlement is a rare,
   large event.

### A Site is a cluster that reads as one place
3. A Site occupies a **contiguous cluster of N platforms** (city ~4–5, village ~2–3, camp 1–2),
   reserved together. It is **not** one oversized platform.
4. A Site **reads as one place** via **dressing, not geometry** — a **shared skyline/look +
   ground/palette**, a **density gradient** toward a core, and a **legible arrival** (a gate/threshold
   at the boundary **plus** the dressing ramp). The **gaps stay clean hops** (no walkable bridges, no
   gap-crossing props); traversal stays **fast island-hopping** — connected islands, not continuous
   terrain. Art direction: `design/art/site-dressing.md`.

### Sites concentrate content (capacity recipe)
5. Content is **clustered in Sites, spaced by Wild**. Each Site fills its footprint by a **recipe**:
   - an **anchor beat** — the content that *is the Site's reason to exist* (city → its quest NPC; camp
     → its bandits; ruin → a relic/chest; lair → a monster). Usually one, occasionally two;
   - a **weighted fill** — how many of the remaining platforms carry a secondary beat, drawn from a
     weighted table of content kinds;
   - a **connective / breath remainder** — the rest is dressing and traversal, never dead space.

### One shared content vocabulary
6. All content — density budgets *and* site recipes — is named in **one vocabulary**: **4 base kinds**
   (**Empty**, **Loot**, **Combat**, **NPC**) × an **open set of flavor tags**. **Quest-bearing** and
   **hostile** are **derived** from an NPC beat (per the [NPC Proximity](npc-proximity-interaction.md)
   brief), **not** separate kinds — quest-rarity is a low weight on the "quest-bearer" NPC flavor,
   ordinary "townsfolk" a separate, more common flavor. **Corpse-loot** is the outcome of a Combat
   beat on the separate combat/mutation channel — never a placed Loot beat.

### Committed sites & flavors (this brief)
7. **Sites** — Settlements: **Camp**, **Village**, **City**; Landmarks: **Ruin**, **Lair**; plus
   **Wild** (the no-site baseline).

   | Site | Family | Footprint | Anchor | Fill feel |
   |---|---|---|---|---|
   | **Camp** | Settlement | 1–2 | bandits (often hostile) | a loot stash; sometimes a shady offer |
   | **Village** | Settlement | 2–3 | a quest / a plain situation | a townsfolk; a little loot |
   | **City** | Settlement | 4–5 | a rare quest NPC | townsfolk-heavy + a market (loot) + a guard fight |
   | **Ruin** | Landmark | 1–2 | a relic / chest | a lurking monster; a little loot |
   | **Lair** | Landmark | 1–2 | a tougher monster | a second monster; a little loot |

   - **Settlements** are faction-occupied → **passport-gated** and **quest-bearing**; their satire
     register grows with the run's altitude. **Landmarks** are unoccupied / monster-held → **no
     passport**, find/fight-bearing (the world's naturalistic loot sources live here).
8. **Committed content flavors** — Loot: *scattered* (simple wild), *stash*, *market*, *chest*,
   *relic*. Combat: *wild-beast*, *den-monster* (tougher), *bandit*, *guard*. NPC: *quest-bearer*
   (rare), *townsfolk*. (The list is meant to grow by authoring.)

### Extensibility & maintainability (a first-class requirement)
9. **New sites and new content flavors are added by authoring data — no code change.**
10. **Attributes are additive and optional with defaults**: introducing a new site/recipe attribute
    later must leave **every existing site valid** (nothing to migrate).
11. **Family default + per-site override**: a site inherits its **family** (Settlement / Landmark)
    baseline and overrides only its delta; **revising a family default shifts all its sites** in one
    edit.
12. **Recipes reference content kinds/tags, never specific content assets** — the world stays
    procedural and non-catalog (no authored "this city always has the blacksmith").
13. **Determinism holds** — the same run seed produces the same world layout, sites, and content mix.

## Content authoring rules (for the designer)
- Author each **site** as one asset filling the site schema: **footprint** range, **family**,
  **occupancy/faction**, **capacity recipe**, **tier/altitude** eligibility + tonal register, **biome
  compatibility**, **dressing theme**.
- A **capacity recipe** = **anchor beat(s)** + a **weighted fill table** of content-kind references +
  a **connective remainder**; authored as a **family default** the site overrides.
- Name content by the **base·flavor** vocabulary (a base kind, refined by a flavor when it matters).
- **Never** list corpse-loot in a recipe — it comes from the Combat beat itself.
- Settlement occupancy sets which faction's passport opens it; landmarks are not passport-gated.

## Acceptance criteria
- Traversing a run, **most platforms are Wild**; settlements are **rare**, each spans the right number
  of platforms, and reads as **one dressed place with an arrival**.
- A **city** shows more townsfolk / market-loot / guards than a **village**; the **quest** in each
  stays rare.
- A **ruin** yields relic/chest loot with a lurking monster; a **lair** is a tougher fight; **neither
  has townsfolk**.
- A designer **adds a new site or content flavor by authoring one asset, no code change**; changing a
  **family-default recipe shifts all sites of that family**; **adding a new site attribute leaves
  existing sites valid**.
- **Same seed → same world.**

## Delivery sequencing (recommended)
- Comes **after** the World Content Density brief (its prerequisite).
- The **site vocabulary + capacity recipes + content-kinds catalog** (the *what fills where*) can land
  **before** the full multi-platform **visual** spanning + connective-dressing art (the *looks like
  one place*).

## Out of scope / open points (do not build now)
- **Beta site backlog:** hamlet / outpost, grove / shrine, crater (meteorite), and **biome-locked**
  site variants (e.g. a merfolk city in water only).
- **The Order's Seat (apex throne)** — whether it is a special tier-gated Site in this vocabulary or a
  hand-authored climax outside it — **open**, deferred to beta.
- **Monster difficulty escalation** by altitude — flat for now.
- **Site-dressing art direction is decided** — `design/art/site-dressing.md` (aligned skyline + shared
  ground/palette + density gradient + gate/threshold; **clean gaps, no walkable bridges**; biome-base ×
  site-dressing layering). Still deferred: the broader **render-look / palette bible** and final asset
  production.
- **The engine "how"** — reserving a multi-platform Site block across the director's planning window,
  and the exact balance numbers (recipe weights, footprint tuning) — the code track's call.
