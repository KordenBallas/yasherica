# Decoration Footprint & Edge-Fit — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-06
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track E · polish** on the biome-feature placement shipped in `environment-dressing.md`
> (E2 placement model) / `biome-visual-styles.md`. Extends placement only — the kit contract,
> tone treatment, density dials, and determinism are unchanged.

## Goal

Stop **wide decorations spilling past the platform edge** so props read as **sitting on the
platform**, not floating over the void. Today placement is point-based — a prop is dropped on a cell
centre (or a rim point) with no notion of its own width — so any prop whose horizontal size exceeds
its distance to the true edge overhangs. Teach placement to **account for a decoration's size**, and
keep the one overhang we actually want (tall framing props leaning over the rim) as a **deliberate,
opt-in style** rather than an accident that hits everything.

No mechanics change: this is placement discipline over the existing biome-feature pool.

## User stories

- As a player, scattered rocks, shrubs, tufts, and stands **read as resting on the platform** — no
  wide prop hangs half over the empty gap between platforms, so each island reads as a solid place.
- As a player, the occasional **tall tree or crag still leans its top/canopy out beyond the edge**
  where the world wants that framing — this reads as intentional silhouette, not a bug.
- As a designer, I can mark a prop's **rough size** so the world keeps it clear of the edge, and I
  can mark the few props that are **allowed to lean over** — both from the kit data, no code.

## Functional requirements

### Account for size when placing (the core fix)
1. **Each decoration carries a rough footprint (its horizontal size).** Placement uses it to keep a
   **keep-clear margin from the true platform edge** equal to that footprint (scaled by the prop's
   drawn scale): a prop may only land where it **fits fully within the platform silhouette**.
2. **Light authoring — sensible defaults by kind.** A designer should not have to measure every
   prop: the footprint **defaults from the prop's kind** (small decorative = small, large decorative
   = larger, a blocking obstacle already owns its whole cell), with an **optional per-prop override**
   for the odd outlier. Authoring stays "bind a kit", not "hand-tune every entry".
3. **Poke inward, don't drop.** When a chosen spot would overhang, the prop is **nudged inward** to
   where it fits rather than discarded — decoration **density and the clustered look are preserved**,
   the edge just "breathes" by exactly the prop's size. Only if a prop genuinely cannot fit is it
   skipped.
4. **Clusters respect the edge too.** The scattered members of a cluster (copse, outcrop, tuft
   patch) obey the same margin — a member that would jitter off the platform is pulled back in, so a
   cluster near the rim stays wholly on the platform (minus any intentional-overhang props, FR5).

### Keep the deliberate overhang (owner decision: conscious style)
5. **Framing props may lean over the edge — but only when flagged.** The existing rim-framing
   behaviour (tall props anchored at the decorative rim, leaning their top/canopy beyond the walkable
   edge) stays as an **intended style**. It applies **only to props explicitly marked as
   "may overhang"** (trees/crags authored for it); **every other, grounded prop obeys the footprint
   margin** of FR1. Overhang becomes an **opt-in the designer grants a few props**, never the default
   that catches wide rocks and sacks.
6. **Overhang stays a lean, not a launch.** Even a flagged framing prop keeps its **base/root on the
   platform** — it leans out from a foothold, it does not float free over the gap.

### Unchanged
7. **Determinism preserved.** Same seed + same kit → same world. Footprint is a **property of the
   kit data**, not a new random draw; nudging-inward is a deterministic adjustment of an
   already-seeded position. Swapping a kit with the same entry count still never shifts a draw.
8. **Everything else stands:** blocking obstacles still take a whole cell and stay sparse; the
   movement lane and battlefield minimum are untouched; the tone treatment and density dials are
   unchanged.

## Content authoring rules (for the designer)
- Leave footprint on its **kind default** for almost everything; set a **per-prop override** only
  for an outlier (an unusually wide rock, a broad bush).
- Grant the **"may overhang"** mark to the **few tall framing props** (trees, tall crags) where a
  canopy/top leaning over the rim reads well — nothing else.

## Acceptance criteria
- No **grounded** decoration (rock, shrub, tuft, sack, crate, cactus, stand) hangs past the platform
  silhouette — each reads as sitting on the platform.
- A near-rim **cluster** sits wholly on the platform; its members don't spill into the gap.
- The **few flagged framing props** (trees/crags) still lean their top/canopy over the edge, base on
  the platform — reading as deliberate silhouette.
- Decoration **density and the clustered look are preserved** — fixing the overflow does not thin
  the world or push everything to the middle.
- **Same seed → same** placement; **swapping** a kit (same entry count) still needs no code/scene
  edit.

## Out of scope / open points (do not build now)
- **Prop-vs-prop overlap / spacing.** This brief is about the **platform edge**, not props
  colliding with each other or with obstacles — that stays as-is.
- **Exact per-prop bounds from mesh geometry.** A rough authored/kind-derived footprint is enough
  for the demo; deriving a precise footprint automatically from each mesh is a possible later refinement.
- **Site dressing (structures/camp props).** Site kits place against their own platform-local rules
  (skyline band, gate, focal ring) and already sit inward by construction; if site props are seen to
  overhang, fold them under the same footprint rule in a follow-up — not part of this brief.
- **The world backdrop layer (E4).** The distant scatter behind the platforms is deliberately
  off-platform and unaffected.
