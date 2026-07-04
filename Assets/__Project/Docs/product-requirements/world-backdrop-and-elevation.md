# World Landscape Read (Routed Path · Elevation Tiers · Backdrop) — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-04
> Level: product-owner (what & feel). The code track owns the technical "how".
> This is the full brief for **P5-2** ("World backdrop + elevation tiers"), extended in the 2026-07-04
> discussion with a third axis — the **routed (weaving) path**.
> Inherits the decided art direction — do not restate it: `design/art/world-backdrop.md` (the three
> landscape axes), `design/art/render-look.md` §2 (muted base, flat low-poly, no outline → silhouette +
> value + reserved colour carry readability), and `design/art/site-dressing.md` §1 (gaps stay clean hops).
> The **character** of all three axes is authored per biome on the biome appearance config of
> `product-requirements/biome-visual-styles.md`.

## Goal

Make the traversal field read as a **natural landscape you route through** — not a straight, staged line
of platforms marching left to right. Three layout/visual axes, one felt goal (the world has depth,
horizon, and a sense of terrain):

1. **Routed path** — the sequence of platforms **weaves laterally** along a coherent, natural curve
   (skirting a peak, threading a valley), instead of sitting on one straight line.
2. **Elevation tiers** — platforms sit at **deliberate height levels**, so the field forms an undulating
   landscape rather than flat jitter.
3. **World backdrop + skyline** — a distant, hazed **biome horizon** behind the whole field.

**Hard boundary (all three):** this is **layout and read, never mechanic**. The **gaps stay clean hops**
(`site-dressing.md` §1); nothing here becomes traversal (no stepping up tiers, no bridges, no vertical
platforming, no climbing a landmark), the path **never forks or branches**, and it never backtracks in
screen-forward. It changes **where platforms sit and what sits behind them**, not the rules of movement,
combat, or the platform surface/shape.

## User stories

- As a player, the run feels like a **journey through a place** — the trail bends around a mountain,
  dips through a valley, and a hazed horizon sits behind it — not like platforms on a conveyor belt.
- As a player, I always read **which way is onward**: the path weaves, but forward is always forward; I'm
  never confused about where the run continues.
- As a player, when the trail curves **around a landform**, that landform sits **behind** the path and my
  hero stays **in the foreground, never hidden** by it.
- As a player, each **biome's landscape has its own character** — mountains switchback tightly around
  peaks, the desert makes long lazy arcs around dunes, the forest wanders gently.
- As a designer, I tune a biome's **whole landscape read** (how much the path weaves, how tall the tiers,
  which backdrop and routing landmarks) as **data on the biome appearance config**, and adding a biome is
  data-only.

## Functional requirements

### A. Routed path (lateral weave)

1. **The path weaves laterally.** Successive platforms are offset in **depth** (toward/away from the
   camera) along a **coherent, natural curve**, not a straight line and **not** per-platform random noise.
2. **Forward progress stays monotonic.** Screen-forward always advances — a later platform is never placed
   screen-behind an earlier one. The player always reads "forward = onward"; the weave is in depth, not a
   fold-back.
3. **Bounded corridor.** The lateral wander stays within an authored **corridor width** — the path
   breathes but never wanders off to the side unboundedly. (This **replaces** today's behaviour, where
   platforms drift sideways by an **unbounded per-platform random step** that reads as noise — see
   `design/art/world-backdrop.md` "Routed path" and `needs-code.md` 2026-07-04.)
4. **Two-layer character.** The curve is a gentle **low-frequency baseline wander** (one bend spans
   several platforms; the heading turns slowly, so it reads as a **trail with inertia**, not a zigzag),
   **plus** sparse, larger **feature arcs** that read as **routing around a specific landform**.
5. **Feature arcs are justified by a landmark.** Each feature arc has a **midground landmark** (a
   peak / dune / boulder / tree-clump) placed at the arc's apex on the **far side** of the path (away from
   the camera); the arc **bulges toward the camera** around it, so it reads as "the trail goes around
   that". The landmark is a **midground world object** — **not walkable, not on a platform cell** — sitting
   between the platforms and the far backdrop. Baseline wander needs no landmark.
6. **Foreground / readability invariant.** The **active platform and hero are always nearest the camera**
   and **never occluded** by a routing landmark; landmarks always sit **behind** the path. (No outline
   backs up readability — the silhouette must always be clear, `render-look.md` §2.)
7. **Camera is unchanged.** The existing **fixed isometric traversal angle** is kept; this feature adds
   **no** camera rotation or path-following turn. Because the view is already isometric, the depth weave
   reads as a winding trail on screen without any camera change.

### B. Elevation tiers

8. **Deliberate height levels.** Platforms sit at **authored elevation tiers** so the field reads as an
   undulating landscape (a valley, a rise toward a Site), not flat local jitter.
9. **Visual only.** Elevation is a **read**, never traversal — no stepping up a tier, no bridges, no
   vertical platforming; the **gaps stay clean hops** regardless of tier difference.

### C. World backdrop + skyline

10. **A distant biome horizon.** A **world-scale backdrop** sits behind the whole field — a distant,
    **hazed silhouette** of the biome's landforms (mountain peaks, dunes/mesas, a far treeline, cave far
    walls/openings), plus quiet **sky / cloud** bands.
11. **Biome-keyed and muted.** The backdrop is keyed to the biome and its palette key
    (`render-look.md` §2); it is **low-contrast, hazed, and lowest in the focus hierarchy** so the hero,
    enemies, and loot silhouettes still carry the eye.
12. **Distinct from the Site backdrop.** This is the **global** horizon the whole run reads against; the
    per-Site backdrop (`site-dressing.md` §3.4) is a **nearer** layer in front of it.

### D. Per-biome character & robustness

13. **Landscape character is per biome.** The character of all three axes — routed-path corridor width,
    baseline wavelength, feature-arc frequency + landmark kit, curvature; elevation tier set/amplitude;
    backdrop silhouette kit — is authored on the **per-biome appearance config**
    (`biome-visual-styles.md`) with a **sensible default**, so an unauthored biome still routes and reads
    sanely. Tuning or adding a biome's landscape is **data-only**.
14. **Determinism.** The same run seed yields the **same** route, tiers, landmark placement, and backdrop.

## Content authoring rules (for the designer)

- On the **per-biome appearance config** (`biome-visual-styles.md`), author the biome's **landscape
  character**: routed-path corridor width, baseline wavelength, feature-arc frequency, the **routing
  landmark kit** (which midground landforms justify a feature arc) and curvature; the **elevation tier**
  set/amplitude; and the **backdrop silhouette + sky** kit.
- **Routing landmarks** and **backdrop silhouettes** are **midground/background decoration** assets —
  flat low-poly, no outline, **muted**, low in the value hierarchy (`render-look.md` §1–§2). They are
  distinct from the on-platform biome **feature pool** (`biome-visual-styles.md` FR4–6), which places on
  hex **cells**; a routing landmark is **off the platforms**. The meshes themselves come from the
  decoration asset pipeline (P5-4) — this brief consumes the data path.
- Keep everything **muted**; do not spend saturation on the landscape — it is reserved for
  gameplay-meaningful accents.

## Acceptance criteria

- The traversal field reads as a **natural landscape route** — a weaving path, elevation, and a distant
  horizon — **not** a straight staged line.
- The path **weaves along a coherent curve**; **forward progress stays monotonic** (a later platform is
  never screen-behind an earlier one); the lateral wander stays within the **bounded corridor**.
- A **feature arc** reads as **routing around a landmark**; the landmark sits **behind** the path; the
  **hero is never occluded**; the camera keeps its fixed isometric angle.
- The old **unbounded random sideways drift is gone**, replaced by the coherent bounded curve.
- **Elevation** reads as terrain; **gaps stay clean hops**; no tier or route change ever becomes traversal
  or a fork.
- The **backdrop** reads as a distant, muted biome horizon that never competes with gameplay silhouettes,
  and sits **behind** any per-Site backdrop.
- Editing a biome's landscape config **visibly changes** its route character / tiers / backdrop with **no
  code change**; **adding a biome** is data-only.
- **Same seed → same** layout, tiers, landmark placement, and backdrop.

## Out of scope / open points (do not build now)

- **Path-following camera yaw** (the camera turns to follow the trail's bend) — deferred escalation. MVP
  keeps the **fixed isometric angle**. Revisit **only if** playtest reads the weave as "platforms sliding
  sideways" rather than "a trail winding forward".
- **Coupling the route to the real backdrop geometry** (the path threading the *actual* horizon
  silhouette) — deferred polish. MVP **fakes the causality**: the route is the source of truth and the
  landmark is placed to justify the bend (the player can't tell the difference).
- **Concrete backdrop silhouettes and routing-landmark meshes per biome** — an art/asset pass (decoration
  asset-gen pipeline, P5-4); this brief consumes the data path, it does not author the meshes.
- **Which biome goes where along the run**, and **escalation of the horizon** as the run climbs — a
  generation / progression concern (director D19), not this brief.
- **On-platform biome features** (trees/rocks on hex cells) — owned by `biome-visual-styles.md`; a routing
  landmark is a **different, off-platform** midground object.
- **Palette swatches** and the **figure-ground / flat-shading shader** — render-look follow-ups (tech-art).
- **Parallax / sky animation** (drifting clouds, depth movement) — later polish.
- **No change** to combat, movement rules, turn order, or the platform surface/shape.
