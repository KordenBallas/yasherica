# Site & Camp Dressing Kits (Demo) — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track E · 3/4** — content that plugs into the seam. Depends on **Track E · 1/4**
> (`dressing-kit-binding-and-swap.md`) for the kit/swap contract + tone treatment, and on the
> already-decided **site dressing** design (`design/art/site-dressing.md`;
> `world-sites-and-landscape.md` dressing-theme) which owns *how* a cluster reads as one place.
> Inherits `design/art/render-look.md` §2. **Demo content only.**

## Goal

Give the **Settlement** and **Camp** sites their **first real dressing** — demo kits from the *RPG
Poly Pack - Lite* — so a village/city reads as a built-up place and a bandit camp reads as a camp,
across the cluster of islands the site occupies. This fills the site **dressing-theme** seam with
content; the connective method (aligned skyline, shared ground, gradient, gate) is already decided in
`site-dressing.md` and unchanged.

- **Village / City** ← RPG pack **houses/buildings** (the settlement building kit + skyline + a
  gate/threshold piece).
- **Camp** ← RPG pack **small props** — an **awning/tarp, sacks, a campfire, barrels** — the props
  that read "camp" when clustered.

## User stories

- As a player, a **village/city** reads as one continuous town across its platforms — houses whose
  rooflines line up into a skyline, a shared ground, a gate marking arrival — not a scatter of
  disconnected islands.
- As a player, a **bandit camp** reads as a camp — a tarp/awning, sacks and barrels around a campfire —
  a rough, thrown-together place distinct from a town.
- As a designer, I dress each site type by binding **one demo site-dressing kit** to its dressing
  theme, and can **swap it for a production kit** later by repointing one field.

## Functional requirements

### The demo site-dressing kits
1. **Settlement demo kit** (from the RPG pack): a **building/house kit** (the structures that form the
   skyline), a **ground overlay** (cobbles/packed street), **skyline pieces**, and a **gate/threshold**
   piece — the roles `site-dressing.md` §2/§5 expects for a Settlement. **Village and City share the
   kit** (city = more of it, denser core); the difference is the site's own capacity recipe/footprint,
   not a different kit.
2. **Camp demo kit** (from the RPG pack): a **prop kit** of small camp objects — **awning/tarp, sacks,
   campfire, barrels** — plus a **packed-dirt ground overlay** and a light **threshold** read. A camp
   has **no architecture-of-society**; its "one place" read comes from the clustered props + shared
   ground, not a skyline.
3. **Bound through the kit contract.** Each site type's dressing theme references its demo kit as a
   **whole kit** (Track E · 1/4); pack assets are reachable only through the kit, in the demo location.

### Reads-as-one-place (inherited, not re-specified)
4. **The connective method is already decided** (`site-dressing.md` §3–§5): aligned skyline / shared
   ground / density gradient / gate — **dressing, not geometry**; the **gaps stay clean hops** (no
   walkable bridges, no gap-crossing props). This brief supplies the **kit assets** those cues place;
   it adds no new connective rule.
5. **Muted, not native.** Both kits inherit the Track E · 1/4 tone treatment — desaturated/hazed under
   the biome key, below hero/enemy/loot. Do not use the pack's native saturation.
6. **Same structures, different biome materials.** A forest-village and a desert-village use the **same
   building kit** rendered in the biome's palette (`site-dressing.md` §2) — one demo kit serves the
   settlement across biomes; the biome layer recolours it.

### Placement discipline (frame the space, keep it playable)
7. **Group structures to the rim / rear, not the centre.** Cluster the houses, camp props, and other
   larger dressing toward the platform's **edges and its into-screen depth** — the organic decorative
   rim beyond the battlefield cells (`platform-hex-surface-and-shape.md`) — so the **hero's movement
   lane and the combat field stay clear**. Dressing **frames** a site platform; it does not crowd the
   space the player moves and fights in. On a **combat-capable** site platform the battlefield minimum
   is honoured (no structure eats the fight's room).
8. **Placed logically, not chaotically.** Structures read as a **coherent settlement/camp** — houses
   fronting a shared line, camp props gathered **around** the campfire — not scattered at random.
   Placement stays deterministic; the "one place" read comes from ordered grouping, not a dusting.

### Boundary with the bandit-camp enemies (keep them orthogonal)
9. **Camp *dressing* is not the camp *enemies*.** This brief owns only the **props** that make a camp
   look like a camp. The camp's humanoid bandits (boss + crew), their colour-coding, engagement, and
   the shady-offer content are **Track D** (`bandit-camp-humanoids.md`) and **P3-17**
   (`camp-shady-offer.md`) — a separate concern that shares the same Camp site. This brief changes
   neither.

## Content authoring rules (for the designer)
- Author **one Settlement demo kit** (building/skyline/ground/gate) and **one Camp demo kit**
  (awning/sacks/campfire/barrels/ground) from the RPG pack.
- **Bind** each to its site type's dressing theme by whole-kit reference; keep pack assets in the demo
  location only.
- Keep both **muted** (inherit the tone treatment); rely on the existing connective method for the
  "one place" read — do **not** add bridges or gap-crossing props.

## Acceptance criteria
- A **village/city** reads as one town across its cluster (aligned skyline + shared ground + gate);
  a **camp** reads as a camp (clustered awning/sacks/campfire/barrels on packed dirt).
- Structures are **grouped to the rim/rear** and read as a **coherent** settlement/camp (not scattered);
  the hero's **movement lane and the combat field stay clear**, battlefield minimum honoured.
- The **gaps stay clean hops**; no walkable or gap-crossing dressing.
- The same settlement kit **recolours per biome** (forest vs desert village share structures).
- Dressing is **muted** — hero/enemies/loot pop; camp bandits (Track D) are unaffected.
- **Swapping** a site's demo kit changes the look with **no code / no scene edit** (Track E · 1/4).
- **Same seed → same** site dressing.

## Out of scope / open points (do not build now)
- **Landmark dressing** (ruin / lair kits) — the packs don't target these; leave to a later kit
  (`site-dressing.md` §5 sketches the intent).
- **The bandit-camp enemies / shady offer** — Track D + P3-17, unchanged here.
- **Production site assets** and the **biome×site material matrix** — a later art pass (ROADMAP P5-3);
  the swap is guaranteed one-field by Track E · 1/4.
- **How skyline/gate pieces are placed to align across islands** — a generation/authoring concern the
  site seam owns (`site-dressing.md` §7); this brief supplies the pieces, not the placement rule.
- **Prop animation** (banners, smoke, market bustle) — later polish.
