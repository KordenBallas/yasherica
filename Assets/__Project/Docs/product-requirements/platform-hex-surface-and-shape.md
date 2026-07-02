# Platform — Hex Surface, Natural Edges & Content-Aware Shape — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-02
> Level: product-owner (what & feel). The code track owns the technical "how".
> Related: `vision.md` §4 (connected islands, Windblown hops), Pillar 4 (fast chess / anatomy
> legibility); `design/art/site-dressing.md` (islands read as one place); builds toward the
> `world-sites-and-landscape.md` and `world-content-density.md` briefs (which own multi-platform
> footprints and the content mix, respectively).

## Goal

Make a platform's **top surface and its combat grid one and the same thing**. Today a platform is a
random-sized organic **blob** and the hex battlefield is a **separate** grid fitted *inside* that
blob when a fight starts — two representations that only meet at combat time. We want the surface to
be **built from hex cells** so the combat grid is literally the ground it always stood on, the world
reads as a natural place (not a board game), the silhouette stays an **organic island**, and the
platform's **size and shape come from its content** instead of being random.

This changes **how a platform looks and how big it is**, and **removes the mismatch** between the
walkable surface and the combat grid. It does **not** change combat rules or what content does.

## User stories

- As a player, when a fight starts on a platform the **combat grid matches the ground exactly** — the
  tiles were always there; nothing snaps in or shifts.
- As a player, walking the world I see **natural, organic islands** — the hex tiling is **subtle**
  under grass/rock, not a hard honeycomb — and when combat begins the **cells read crisply** so I can
  plan my "fast chess".
- As a player, a **combat platform feels like it has room to fight**, while a loot pickup or an empty
  traversal step is a **small, quick** island — the size tells me what a platform is for.
- As a player, **biome features** (a river, a rock/mountain) sit **on the grid**, so an obstacle in
  combat lines up with what I saw on the ground — never a confusing half-blocked tile.
- As a designer, I control **how big each kind of content's platform is** from data, with no code
  change, and the **same run seed always produces the same platforms**.

## Functional requirements

### One surface (hex-composed)
1. A platform's **top surface is composed of whole hex cells**; the **combat grid is derived from the
   same source** (one source of truth), not re-fitted at combat time. The walkable surface and the
   battlefield are identical.
2. **Muted in traversal, crisp in combat.** Out of combat the tiling is **visually soft** (natural
   ground dressing), the hex structure only implied. On entering combat the **cell boundaries /
   highlight read clearly**. It is an **emphasis of the same ground**, not a grid appearing from
   nowhere.

### Natural edges (whole interior + organic rim)
3. **Every full hex cell lies on the top surface** — the playable interior is complete whole cells,
   so combat is never clipped.
4. The **silhouette is organic**: beyond the last full hex there is a **non-playable decorative rim**
   (overhang / grass / rock) that gives the island its natural, irregular outline (the look today's
   blob has). The rim is dressing only — never walkable, never a combat cell.

### Content-aware size & shape
5. A platform's **extent and form are chosen from its content kind**, not random. Each content kind
   (Empty / traversal, Loot, Combat, NPC) has its own **size/shape profile** authored as data.
6. A **combat-capable** platform (content that can lead to a fight) **guarantees at least the
   battlefield minimum** — enough whole hex cells meeting the combat requirements — so a fight always
   has room.
7. **Non-combat** platforms (loot-only, empty/traversal, NPC with no fight) can be **visibly smaller
   / narrower**, per their profile — a quick step, not a fighting arena.
8. **Determinism holds:** the same run seed produces the same platform shapes, sizes, and tiling.

### Biome-feature alignment (principle only)
9. Biome features that affect the surface (river banks, a mountain/rock footprint) **occupy whole hex
   cells and align to the grid**, so any feature that becomes a combat obstacle **matches the cells
   exactly** — never a partial/ambiguous tile. *(How features look per biome, and which appear, is a
   separate biome-appearance brief — see out of scope.)*

## Content authoring rules (for the designer)
- **Per-content-kind size/shape profile** (Empty / Loot / Combat / NPC): a size range and shape
  character, authored as data. Editing a profile changes those platforms' size with no code change.
- **Battlefield minimum** for combat-capable content is a data value; combat platforms are sized to
  meet or exceed it.
- **Rim thickness / irregularity** (the organic edge) is a tunable so the island silhouette can be
  dialed from tidy to ragged.
- These profiles are **single-platform**; multi-platform Site footprints (a city spanning several
  platforms) are authored in the Sites system, not here.

## Acceptance criteria
- A platform's top surface is **visibly built from hex cells**, and when combat begins the combat
  grid **coincides exactly** with those cells (no re-snap, no offset, no mismatch).
- In traversal the tiling reads as **soft/natural**; in combat the **cells read crisply**.
- **Every full hex cell** is on the surface; the outline is **organic**, with a non-walkable rim
  beyond the last full cell.
- A **combat-capable** platform always has at least the configured **battlefield minimum** of whole
  cells; **loot/empty** platforms are visibly smaller — matching the configured profiles.
- **Editing a content-kind size profile** visibly changes platform sizes with **no code change**.
- A biome feature that blocks movement/combat sits on **whole cells** and never yields a half-blocked
  combat tile.
- **Same seed → same platforms** (shape, size, tiling).

## Out of scope / open points (do not build now)
- **Biome-specific appearance** — materials/palette of the surface, which features appear per biome,
  and their look — is a **separate biome-appearance brief** (roadmap p.3). This brief owns *hex
  alignment* of features, not their styling.
- **Multi-platform Site footprints** — a camp/village/city occupying a cluster of platforms that
  reads as one place — belongs to `world-sites-and-landscape.md`, not here.
- **The exact render treatment** of "muted → crisp in combat" (shader/VFX for cell edges and
  highlight) is tech-art / the render-look bible; this brief sets the *intent*, not the shader.
- **Monster/altitude escalation** affecting platform size — deferred (can ride director D19 later).
- **No change** to combat rules, turn order, or what any content does once it starts.
- Implementation specifics (mesh construction, grid data, how the surface and grid share a source)
  are the code track's call.
