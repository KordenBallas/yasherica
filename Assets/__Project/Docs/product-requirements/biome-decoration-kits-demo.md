# Biome Decoration Kits (Demo) — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track E · 2/4** — content that plugs into the seam. Depends on **Track E · 1/4**
> (`dressing-kit-binding-and-swap.md`) for the kit/swap contract and the tone treatment, and on the
> already-verified **`biome-visual-styles.md`** (the per-biome appearance config + feature pool +
> ground) which owns the placement. Inherits `design/art/render-look.md` §2 and
> `design/art/biome-visual-styles` intent. **Demo content only** — see §Out of scope on production.

## Goal

Fill the **already-designed biome feature pool and ground** with the **first real content** — demo
kits built from two Unity Asset Store packs — so we can **see the world distribute decorations on
platforms** and validate the biome look. This is what turns `biome-visual-styles` from a data path
with no meshes into a visibly dressed Desert and Forest.

- **Desert biome** ← *Low-Poly Desert Environment Pack*: sand ground + rocks, cacti, dry shrubs.
- **Forest biome** ← *RPG Poly Pack - Lite*: grass ground + trees, plants/bushes, rocks, grass tufts.

No mechanics change: this is the biome-feature **content**, bound as demo **biome-feature kits** (Track
E · 1/4) and placed by the existing feature pool.

## User stories

- As a player, the **desert** reads as a desert — ochre sand underfoot, scattered rocks and cacti on
  and around the platforms — and the **forest** reads as a forest — green ground, trees, bushes, rocks
  — while the hero, enemies, and loot still pop against both.
- As a designer, each of the two biomes is dressed by binding **one demo biome-feature kit** to its
  appearance config; I can retune density/mix from the config and **swap the demo kit for a production
  kit later** by repointing one field.

## Functional requirements

### The two demo biome-feature kits
1. **Desert demo kit** (from the Desert pack): a **ground material** (sand/ochre) plus a **feature
   pool** of desert props — **rocks, cacti, dry shrubs** — each authored as a feature-pool entry with
   its density/weight and a **decorative | blocking** flag (per `biome-visual-styles.md` FR4–5).
2. **Forest demo kit** (from the RPG Poly Pack): a **ground material** (grass/green) plus a **feature
   pool** of forest props — **trees, plants/bushes, rocks, grass tufts** — each a feature-pool entry
   with density/weight and a decorative | blocking flag.
3. **Bound through the kit contract.** Each biome's appearance config references its demo kit as a
   **whole kit** (Track E · 1/4); the pack assets are reachable only through the kit and live in the
   demo location.

### Tone (inherited, not re-specified)
4. **Muted, not native.** Both kits inherit the **tone treatment** of Track E · 1/4 — desaturated /
   hazed under the biome key, below hero/enemy/loot in the value hierarchy. Do **not** use the packs'
   native vivid materials.

### Placement discipline (logical, sparse, obstacles are tactical)
5. **Placed logically, not chaotically.** Decoration reads as **coherent natural grouping** — rocks in
   an outcrop, trees in a copse, cacti in a stand — **not a uniform random dusting**. Placement stays
   deterministic (`biome-visual-styles.md` FR7) but should **cluster along a legible logic**, not
   scatter evenly across the field.
6. **Do not flood — obstacles are tactical.** A **blocking** feature is a **natural obstacle on the
   combat grid**, so blockers stay **few and deliberate** — something to fight *around*, never clutter.
   The battlefield minimum is a **floor, not a target**; density is tuned for the **sparse, breathing**
   look of the rare-world philosophy (`world-content-density.md`).
7. **Small decoration may cluster — several per cell.** A **blocking** feature takes its **whole cell**
   (it *is* the obstacle, one per cell). But **small decorative** props (pebbles, tufts, shrubs, twigs)
   are visual-only and **several may share one cell**, grouped, **without** each consuming a cell or
   blocking movement. So **decoration density and obstacle density are decoupled** — a cell can look
   dressed while staying fully playable. *(This extends the current one-feature-per-whole-cell model —
   see Out of scope.)*
8. **Keep the playable field open.** Cluster the larger/denser decoration toward the platform's **rim /
   rear** — the organic decorative rim beyond the battlefield cells
   (`platform-hex-surface-and-shape.md`) — leaving the hero's movement lane and the combat field clear.
   Dressing **frames** the space; it does not fill it.
9. **Sensible blocking split.** Author the obvious blockers as **blocking** (a big rock, a tall cactus,
   a tree trunk) and the small stuff as **decorative** (shrubs, grass tufts, pebbles).

## Content authoring rules (for the designer)
- Author **one demo biome-feature kit per dressed biome** (Desert, Forest), each holding a ground
  material and a feature pool of the pack's props with per-entry density + decorative|blocking.
- **Bind** each kit to its `LevelTheme` appearance config by whole-kit reference; keep pack assets in
  the demo location only.
- Keep it **muted** (inherit the tone treatment); tune density so the biome reads without crowding the
  platforms or the fight.

## Acceptance criteria
- **Desert** platforms carry sand ground + rocks/cacti/shrubs; **Forest** platforms carry grass
  ground + trees/bushes/rocks/tufts — at the authored density.
- Decoration reads as **coherent clusters** (outcrops, copses, stands), **not** an even random dusting.
- **Blockers are sparse and deliberate**; **small decorative props cluster (several per cell)** without
  blocking; larger dressing sits toward the **rim/rear** — the hero's movement lane and the combat field
  stay clear, and a **combat** platform keeps at least its battlefield minimum.
- Both biomes are **muted** — hero/enemies/loot still pop.
- **Swapping** a biome's demo kit for another kit changes the look with **no code / no scene edit**
  (Track E · 1/4).
- **Same seed → same** biome look and feature placement.

## Out of scope / open points (do not build now)
- **Placement-model extension (flagged to the code track).** FR5/FR7/FR8 (coherent **clustering**,
  **several small decorative props per cell**, **rim/rear grouping**) go beyond today's
  one-feature-per-whole-cell model in `biome-visual-styles.md` (FR4–5). This is a small extension the
  code track owns — decouple *decorative* placement (clustered, multi-per-cell, visual-only) from
  *blocking* placement (one obstacle per cell). Recorded in `design/needs-code.md`.
- **Mountain & Cave demo kits** — the two packs don't cover them; leave those biomes on their base
  layer for now (a later kit, demo or production, dresses them). Cave has no race yet and is out of
  the biome rotation anyway (`biome-selection-along-the-run.md`).
- **Production biome assets** — a later art pass; the swap is guaranteed one-field by Track E · 1/4.
- **Site dressing & the world backdrop** — separate Track E briefs (`site-camp-dressing-kits-demo.md`,
  `world-backdrop-fill-demo.md`).
- **New placement/terrain rules** (cover, LoS, hazards) — unchanged; `biome-visual-styles` stays
  decorative + simple blocking.
