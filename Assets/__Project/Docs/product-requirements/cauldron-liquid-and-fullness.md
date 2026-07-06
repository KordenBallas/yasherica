# Cauldron View — Liquid You Can Feel & Fullness — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track F · 2/3.** Depends on the spatial spine (F1, `cauldron-view-spatial-spine.md`).

## Goal

Make the brew **read as a real liquid the artifacts are inside**, and make the pot's **fill level** the
one honest cue for **how full the inventory is**.

Today the liquid is a flat sine-displaced sector mesh with a pulsing emissive glow
(`inventory-subsystem.md` R9), and the artifacts **float in bubbles above it** rather than *in* it — the
product owner's read is "there's no sense they're in a liquid; no surface to be under". Separately, the
pot never changes with how much you carry, so a nearly-empty inventory and a stuffed one look identical.

This brief (a) gives the liquid a **believable stylised surface** and makes **bubbles pierce it**, and
(b) drives the **liquid level from the artifact count**. It is presentation over the shipped cauldron; it
changes no crafting or inventory logic.

## Decisions locked with the product owner (2026-07-05)

- **Fullness = liquid level only. The cauldron model is a fixed size — never resized in code, no
  discrete size swaps.** The prior "model grows with fullness" idea is dropped in favour of the fill line
  doing the work (stable footprint, no reflow).
- **Fill level is snapshotted on open** and holds for the session, so it does **not** slide up and down as
  you craft/consume within one open view (that would be distracting).
- **Bubbles pierce the surface** — an artifact sits **partly above, partly below** the waterline, which is
  the strongest "it is *in* the liquid" cue.
- **Stylised low-poly liquid, not photoreal.** It must not betray the render-look bible (flat low-poly,
  **no outline**, facets are the texture — `design/art/render-look.md` §1). A crisp bright waterline edge,
  not glossy refraction.

## User stories

- As a player, the brew **looks and behaves like liquid**: a clear surface, a rim waterline, artifacts
  half-sunk in it — not sprites hovering in a jar.
- As a player, I can tell at a glance **how full my pot is** — a nearly-empty inventory sits as a shallow
  pool; a hoard brims near the rim.
- As a player, the fill level **holds steady while I work** in one open session; it doesn't jitter every
  time I pull or drop an item.
- As a designer, I author nothing new — the surface and the fill mapping are presentation over the
  existing brew and artifact count.

## Functional requirements

1. **Believable stylised surface.** The liquid reads as a **low-poly translucent surface** with a
   **legible waterline** (a crisp bright edge/meniscus where it meets the pot wall and where objects
   break it), keeping the boil/emissive-glow life it has today. It stays within the flat-low-poly,
   no-outline treatment — **no glossy/refractive realism**.
2. **Bubbles pierce the surface.** Each in-brew artifact's bubble sits **partly above and partly below the
   waterline** (a visible cut line across it), so it reads as **suspended in the liquid**, not floating on
   top of or above it. The submerged part may be tinted/dimmed by the brew; the emerged part stays clear
   and clickable.
3. **Fullness drives the liquid level.** The **height of the liquid** is a function of **how many
   artifacts are in the inventory** — few = a shallow pool low in the bowl, many = brimming near the rim —
   between a configurable **minimum** (never bone-dry) and **maximum** (never overflowing) level.
4. **Level snapshots on open.** The fill level is **computed when the view opens** and **held for that
   session**; crafting/consuming/adding artifacts within the same open view does **not** re-slide the
   level. It re-evaluates on the next open.
5. **Cauldron model is fixed.** The cauldron mesh is **not** scaled or swapped by fullness or at any time
   — a single authored size. (The "prettier model" is the separate Track F art item; this brief assumes
   whatever mesh is current.)
6. **Muted, in-hierarchy.** The liquid is a **reserved, gameplay-meaningful accent** (like tier-glow):
   it may carry more life/colour than the muted stage, but it must not out-shout the artifacts, medallions,
   or cards that carry the eye (`render-look.md` §2 focus hierarchy).
7. **No behaviour change.** Fill level and surface are read-only presentation; the inventory count, crafting
   resolution, and socketing are untouched.

## Content authoring rules (for the designer)

- None new. Minimum/maximum fill levels, the count→level curve, and surface/waterline look are **tunables**
  (config + tech-art), not authored content.

## Acceptance criteria

- The brew reads as liquid with a clear surface and rim waterline; artifacts sit **half-submerged**, cut by
  the waterline, not hovering above it.
- Opening the inventory with few artifacts shows a **low** pool; with many, a **brimming** one; the level
  stays between the configured min and max.
- The level is **stable for the whole open session** and only re-evaluates on the next open.
- The cauldron mesh is never resized or swapped.
- The liquid never fights the artifacts/medallions/cards for attention; render stays flat-low-poly,
  no-outline.

## Out of scope / open points (do not build now)

- **The three-zone layout and the bubble = submerged rule** — F1 (`cauldron-view-spatial-spine.md`).
- **Where bubbles sit and how they react physically to drop-in/removal** — F3
  (`cauldron-brew-layout-and-physics.md`); this brief owns the *surface & level*, not per-bubble placement.
- **The upgraded, prettier cauldron mesh** — the Track F art item (this brief assumes the current mesh).
- **The stomach-interior backdrop** behind the diorama — P5-5 / `render-look.md` §5.
- Exact shader/tech-art for the surface, waterline foam, and submerged tint — tech-art spike, code track.
- Implementation specifics (mesh/shader/View wiring) — the code track's call.
