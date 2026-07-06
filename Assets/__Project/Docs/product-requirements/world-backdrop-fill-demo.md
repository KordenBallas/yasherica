# World Backdrop Fill (Demo) — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track E · 4/4** — content that plugs into the seam. Depends on **Track E · 1/4**
> (`dressing-kit-binding-and-swap.md`) for the kit/swap contract + tone treatment, and builds on the
> already-verified **`world-backdrop-and-elevation.md` §3** (the world/biome backdrop layer; its
> generation shipped 2026-07-04 — `world-landscape.md` — with the **meshes deferred**). Inherits
> `design/art/render-look.md` §2 and `design/art/world-backdrop.md` §3. **Demo content only.**

## Goal

**Fill the space behind the platforms now** — the distant low-poly landscape that gives the world
depth and a horizon (the Windblown read the owner asked for) — using demo meshes from the two store
packs. Today there is **no world-scale background** behind the traversal field; the backdrop layer is
designed but has no assets. This brief supplies them as a **backdrop kit** and builds the **distant
scatter** that renders them.

**Technique (decided):** the backdrop is **real 3D low-poly geometry scattered far behind the field**
(distant hills, dunes, mesas, tree-clumps, rock formations) — **not** a flat billboard/silhouette
layer. It reuses the same kind of pack meshes, reads with real parallax depth, and needs no separate
silhouette art.

## User stories

- As a player, when I look past the platforms I see a **landscape with a horizon** — dunes and mesas
  behind the desert, hills and a treeline behind the forest — so the world reads as a place with depth,
  not a flat field of floating islands.
- As a player, that background stays **quiet** — it never competes with the hero, enemies, or loot in
  front of me.
- As a designer, I supply the backdrop by binding **one demo backdrop kit per biome** and can **swap it
  for a production kit** later by repointing one field.

## Functional requirements

### The demo backdrop kits (per biome)
1. **A backdrop kit per dressed biome** listing the distant-scatter meshes:
   - **Desert** ← Desert pack: **dunes, mesas/rock formations, large cacti clusters**.
   - **Forest** ← RPG pack: **hills, tree-clumps, rock formations**.
2. **Bound through the kit contract** (Track E · 1/4) — the world backdrop references its biome's
   backdrop kit as a **whole kit**; pack meshes are reachable only through the kit, in the demo
   location. Swap = repoint one field.

### The distant scatter (the technique)
3. **Real 3D scatter, far behind the field.** The backdrop meshes are placed as **actual geometry** in
   the distance behind the platform field, at **low density**, to form the biome's horizon — hills/
   dunes/clumps, not a painted plane.
4. **Deterministic placement.** Same run seed → same backdrop. Placement is the backdrop layer's job
   (the kit only supplies the meshes).
5. **Biome-keyed and hazed.** The scatter wears the **biome key**, **heavily desaturated and hazed**,
   sitting at the **bottom of the focus hierarchy** (`world-backdrop.md` §3, render-look §2) — the
   quietest layer, further than the per-Site backdrop. It is the global horizon a Site backdrop reads
   *in front of*.

### Budget (cheap, atmosphere-first)
6. **Light in memory and performance.** The backdrop must **carry atmosphere while staying the cheapest
   layer in every sense, including cost** — a **small set of reused/shared meshes** at **low density**,
   far and simple, with **no per-frame cost that competes with combat**. Prefer **reuse over variety**:
   a handful of silhouettes repeated reads as a horizon; it should never be a dense, unique-mesh field.
   The technique for keeping it cheap (instancing, distance LOD/impostors, culling) is the code track's
   to choose — the requirement here is the **budget and the felt result**, not the method.

### Hard constraints (inherited)
7. **Background, never traversal.** The scatter is **behind** the field and **not walkable** — the
   **gaps stay clean hops**; the backdrop adds no geometry the player could expect to reach
   (`world-backdrop.md` §3, `site-dressing.md` §1).
8. **Must not occlude gameplay.** The scatter stays far and low-contrast so it never competes with or
   hides the hero/enemies/loot under the fixed isometric camera (there is no outline to lean on).

## Content authoring rules (for the designer)
- Author **one demo backdrop kit per dressed biome** (Desert, Forest) listing its distant-scatter
  meshes; keep pack meshes in the demo location only.
- **Bind** each kit to the world backdrop layer by whole-kit reference.
- Keep the scatter **heavily muted/hazed** (inherit the tone treatment) and **low density** — a
  horizon, not a crowd; it must stay the quietest layer.
- **Reuse a small set of meshes** (a few silhouettes repeated) rather than many unique ones — the
  backdrop must stay **cheap in memory and perf** while still selling the horizon.

## Acceptance criteria
- Behind the field, each dressed biome shows a **distant 3D landscape** (desert dunes/mesas; forest
  hills/treeline) that reads as a **horizon with depth**.
- The backdrop is **not walkable** and **never occludes** the hero/enemies/loot; the **gaps stay clean
  hops**.
- The scatter is the **quietest, most desaturated/hazed** layer — hero/enemies/loot pop.
- The backdrop stays **cheap** — a small set of reused meshes at low density, no per-frame cost that
  competes with combat — while still carrying the biome's atmosphere.
- **Swapping** a biome's demo backdrop kit changes the horizon with **no code / no scene edit**
  (Track E · 1/4).
- **Same seed → same** backdrop placement.

## Out of scope / open points (do not build now)
- **Mountain & Cave backdrops** — the packs don't cover them; leave those biomes without a backdrop
  for now (a later kit dresses them).
- **Escalation of the horizon** (does the backdrop shift as the run climbs backwater→courts→gods) —
  deferred with the escalation design (`world-backdrop.md` §5).
- **Coupling the routed path to the real backdrop silhouette** (the trail threading the *actual*
  landforms) and **path-following camera yaw** — both deferred (`world-backdrop.md` §5); the routed
  path keeps its own placed landmark and the fixed camera.
- **Parallax / sky animation** (drifting clouds, depth movement) — later polish.
- **Production backdrop assets** — a later art pass (ROADMAP P5-2 remainder); the swap is guaranteed
  one-field by Track E · 1/4.
