# Cauldron View — Stable Brew Layout & Event Physics — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track F · 3/3.** Depends on the spatial spine (F1, `cauldron-view-spatial-spine.md`).

## Goal

Stop the brew from **reshuffling every time you touch it**, and make artifacts **react physically to being
dropped in or pulled out** — so the pot feels like a tactile container of objects, not a list that
re-sorts itself on every change.

Today the bubble layout is recomputed **for the current count** on every add/remove
(`inventory-subsystem.md` R10, phyllotaxis spiral + a screen-plane separation pass). The product owner's
complaint: pick or drop one artifact and **all the others jump to new positions** — untouched items should
stay put. Separately, the crafting result **blocks the pot** until collected (R19), forcing an extra click.

This brief makes bubble positions **stable per artifact**, adds a **local physics reaction on
drop-in/removal**, and makes the **finished result flow into the brew automatically** when you start the
next craft. It changes interaction/feel over the shipped inventory; it does not change what an artifact is
or how fusion resolves.

## Decisions locked with the product owner (2026-07-05)

- **Hybrid: stable places + event physics.** In rest, everything holds still (untouched artifacts never
  move). Physicality lives on **events** — a dropped artifact splashes in and nudges neighbours, which then
  **settle back to their stable spots**.
- **Determinism is preserved where it lives today.** The *layout* (which stable spot an artifact holds)
  stays deterministic and testable; the *event reaction* (splash/settle nudge) is **view-layer juice**, not
  domain logic — it may be non-deterministic wobble that always resolves back to the deterministic rest
  positions.

## User stories

- As a player, when I pick one artifact out of the pot, **the others stay where they were** — I'm not
  hunting for the piece I meant to grab next.
- As a player, dropping an artifact into the brew feels physical: it **splashes in and the nearby bubbles
  bob**, then everything **settles** — the pot reacts to me.
- As a player, when a **fresh craft result is hovering** over the pot and I click a new artifact to start
  the next combine, the finished result **drops into the brew on its own** and my new pick takes the
  crafting slot — I don't have to collect it first.
- As a designer, I author nothing new — stable placement and the reactions are presentation/interaction over
  the existing inventory.

## Functional requirements

1. **Stable per-artifact placement.** Each artifact holds a **stable spot in the brew for as long as it
   stays in the pot**. Adding an artifact **takes a new/free spot** without moving the others; removing one
   **frees its spot** without re-sorting the rest. No global reshuffle on count change.
2. **Rest is still.** With no interaction, bubbles keep their calm idle life (the existing gentle drift is
   fine) but do **not** relocate. "Untouched artifacts stay in place" is the load-bearing acceptance point.
3. **Event physics — drop-in.** When an artifact enters the brew (dropped from a staging slot, or an
   auto-committed result, FR5), it **falls/splashes into its spot** and briefly **nudges nearby bubbles**,
   which then **settle back**. This is a short reaction, not perpetual motion.
4. **Event physics — removal.** When an artifact leaves the brew (picked to stage or socket), neighbours may
   give a small **settle bob** but keep their spots (they do not slide in to fill the gap). The gap simply
   becomes a free spot for the next add.
5. **Continuous result flow.** While a **fusion result hovers** above the pot, clicking a pot artifact to
   start the next combine **auto-commits the hovering result into the brew** (it drops in per FR3) and
   stages the newly-clicked artifact. This **replaces today's "new selections rejected until the result is
   collected"** (`inventory-subsystem.md` R19). Directly clicking the result to drop it still works.
6. **Non-overlap holds.** Bubbles still must **not visually overlap** (the current guarantee); the stable-
   spot scheme must keep that invariant as artifacts are added and removed.
7. **No behaviour change to outcomes.** Fusion inputs/outputs, socketing, and the contents of the inventory
   are unchanged — this is placement, reaction, and the collect-timing of the result only.

## Content authoring rules (for the designer)

- None new. Splash/settle strength, drift, and the reaction timing are **tunables**, not authored content.

## Acceptance criteria

- Picking or dropping one artifact leaves **every other bubble in its place** (no global reshuffle).
- A dropped-in artifact **splashes and nudges neighbours**, which **settle back** to their rest spots.
- With a result hovering, clicking a pot artifact **drops the result into the brew automatically** and
  stages the new pick — no separate "collect first" step; clicking the result directly still drops it too.
- Bubbles never overlap on screen through any sequence of adds/removes.
- No change to fusion/socketing outcomes or to the inventory contents.

## Out of scope / open points (do not build now)

- **The three-zone layout and the bubble = submerged rule** — F1 (`cauldron-view-spatial-spine.md`).
- **The liquid surface, surface-piercing, and fullness = fill-level** — F2
  (`cauldron-liquid-and-fullness.md`); a bubble's *vertical* break through the waterline is F2's, this
  brief owns its *in-plane* stable spot and the event reactions.
- **Full continuous physics simulation** of the brew (perpetual jostling) — deliberately **not** wanted;
  the reaction is event-scoped and settles to deterministic rest spots.
- Exact splash/settle animation curves and bubble idle feel — tech-art, code track.
- Implementation specifics (layout model, physics-vs-tween choice, View wiring) — the code track's call.
