# Dressing-Kit Binding & Swap — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track E · 1/4** — the foundational brief of the Environment Dressing (Demo Kits) initiative;
> the other three (`biome-decoration-kits-demo.md`, `site-camp-dressing-kits-demo.md`,
> `world-backdrop-fill-demo.md`) are pure content that plugs into the contract fixed here.
> Inherits the decided art direction — do not restate it: `design/art/render-look.md` §2 (flat
> low-poly, no outline; biome = a muted key modulation of the master palette; saturation is scarce
> and reserved for gameplay), `design/art/site-dressing.md`, `design/art/world-backdrop.md`. Consumes
> the already-designed data seams: the biome **feature pool** (`biome-visual-styles.md` FR4), the
> site **dressing theme** (`world-sites-and-landscape.md`), and the **world backdrop**
> (`world-backdrop-and-elevation.md` §3).

## Goal

Give the world's decoration a **single, swappable binding unit** so that dressing the world is
**content authoring, never code or scene surgery**, and so the **demo** assets we are wiring now
(two Unity Asset Store packs) can be **replaced by production assets by repointing one field**.

Today each dressing seam (biome feature pool, site dressing theme, world backdrop) says only
"references a visual asset". This brief fixes **how** those references are bundled and swapped: a
**dressing kit** — a named, data-only bundle of visual-asset references that a seam points at **as a
whole**. Demo kits are built from the store packs and quarantined; a production pass authors a new
kit and repoints the seam. Nothing about a run's behaviour, generation, or determinism changes — a
kit only supplies **which meshes/materials** the already-designed seams place.

This is the **only genuinely new design decision** in the Environment Dressing track; the other three
briefs are content that fills the seams through this contract.

## User stories

- As a designer, I dress a biome / site / backdrop by pointing its config at a **dressing kit**, and
  I **swap the whole demo kit for a production kit by changing one reference** — no code, no scene
  edits, no per-mesh rewiring.
- As a designer, the **demo** (store-pack) assets are clearly **quarantined** in one place, so a
  production build can drop them without hunting references through scenes and prefabs.
- As a player, no matter which kit is bound, the decoration stays **muted and behind the gameplay** —
  the hero, enemies, and loot always **pop** (there is no outline to lean on).

## Functional requirements

### The dressing kit as the swap unit
1. **A dressing kit is a named, data-only asset** that bundles references to the visual assets
   (meshes, materials, and/or prefabs) a dressing seam consumes, grouped under the **role vocabulary**
   that seam already expects (a biome-feature kit lists feature entries; a site-dressing kit lists the
   building/prop/skyline/gate roles of `site-dressing.md`; a backdrop kit lists the distant-scatter
   roles). **One contract, three kit kinds** — the same binding, swap, and tone rules apply to all.
2. **Seams bind a kit by whole-kit reference.** A biome appearance config, a site dressing theme, and
   the world backdrop each reference **one kit** (per its kind), never individual meshes. **Swapping =
   repoint that one field.** Per-mesh binding is explicitly **out** — the store packs and future
   production sets are swapped as complete kits.
3. **Demo kits are quarantined.** Every kit built from a store pack lives in a clearly-marked **demo
   namespace/location**, so the demo assets are separable as a set from production content.

### The clean-swap invariant (this is the point of the brief)
4. **No store-pack asset may be hard-referenced from a scene, a prefab, or code.** A pack mesh/material
   is reachable **only** through a kit asset. This is what makes the swap a one-field change and lets a
   production build drop the demo packs cleanly (Asset Store demo content is licensed for testing, not
   for a distributed build — the swap contract is how we honour that without rework).
5. **Swapping or adding a kit is data-only** — authoring one kit asset and repointing the consuming
   config; no code change, no new scene wiring.

### Tone treatment (non-negotiable, inherited from the bible)
6. **Every kit's assets are pushed down the value/saturation hierarchy** under the biome key
   (desaturated / hazed toward muted), **never used at their native store saturation**. Store packs
   ship vivid materials; used raw they would fight the hero silhouette (`render-look.md` §2). The kit
   binding is the layer where the tone treatment is applied, so **any** kit — demo or production —
   inherits the muted base. Saturation stays reserved for gameplay accents (archetype hue, tier glow).

### Robustness
7. **Determinism is unchanged.** A kit only supplies *which* assets exist; **placement, density, and
   seeding stay owned by the consuming seam** (feature pool, site recipe, backdrop scatter). Same seed
   + same bound kit → same world.
8. **A missing/broken kit fails safe** — a seam with no bound (or an unresolvable) kit renders its
   **base layer only** (biome ground with no features, a site with no dressing, no backdrop) and never
   hard-crashes generation.

## Content authoring rules (for the designer)
- To dress a surface: author a **kit asset** of the matching kind, list its asset references under the
  seam's role vocabulary, and **point the biome/site/backdrop config at the kit**.
- To go production: author a **new kit** with production assets and **repoint the one field** — do not
  edit scenes or prefabs, and do not reference a pack asset from anywhere but a kit.
- Keep every kit **muted** (inherit `render-look.md`); do not rely on a pack's native materials — the
  binding applies the biome-key tone treatment.
- Keep demo kits in the **demo location**; never scatter pack references outside it.

## Acceptance criteria
- A biome/site/backdrop is dressed by binding **one kit reference**; **changing that reference** swaps
  the whole look with **no code and no scene edit**.
- **No store-pack asset** is referenced from any scene, prefab, or script — only from kit assets;
  removing the demo kit location leaves the game running on **base layers** (no dangling references,
  no crash).
- Bound decoration is **muted/hazed** and sits **below** hero/enemy/loot in the value hierarchy.
- **Same seed + same kit → same** world; swapping a kit changes only appearance, not placement.

## Out of scope / open points (do not build now)
- **The demo content itself** (which pack asset goes where) — the three population briefs
  (`biome-decoration-kits-demo.md`, `site-camp-dressing-kits-demo.md`, `world-backdrop-fill-demo.md`).
- **Production dressing assets** — a later art pass (ROADMAP P5); this brief only guarantees the swap
  is a one-field change when they arrive.
- **The muted→crisp / figure-ground shader spike** (how the tone treatment is achieved technically) —
  a render-look tech-art follow-up (ROADMAP P5-8), not this brief; here the treatment is a data-level
  requirement the kit binding must honour.
- **No new placement or generation behaviour** — kits feed the existing feature-pool / site-recipe /
  backdrop seams unchanged.
