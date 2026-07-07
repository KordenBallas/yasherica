# Cauldron View — Stable Brew Layout & Event Physics — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track F · 3/3.** Depends on the spatial spine (F1, `cauldron-view-spatial-spine.md`).
> **Reference:** `references/cauldron-view-reference.png` (shared Track F board, annotated in F1) shows the
> target brew — several artifacts each holding a distinct, stable bubble spot.

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

## Decision revised with the product owner (2026-07-07) — gravity settle on removal

The 2026-07-05 "frozen absolute spots" rule went one step too far: by pinning every spot forever, pulling an
artifact from the **middle or bottom** left its bubble spot empty while the bubbles above it **hung over the
void** — which breaks the very "tactile container of objects" goal this brief opens with. The original FR4
("neighbours do not slide in to fill the gap") was a **precaution** against the old global phyllotaxis
reshuffle, and it over-corrected.

The fix distinguishes **two different motions** the earlier draft conflated:

- **Arbitrary re-sort (still forbidden).** Touch one → *all* bubbles teleport to unrelated new positions.
  This is the thing F3 was created to kill; it stays killed.
- **Coherent gravity settle (now wanted).** Remove one → **only the bubbles that were resting above it**
  (its column) settle **down** to close the gap; everything beside and below it stays put. Like pulling a
  marble from a jar. This is not the reshuffle we hated — it *is* what makes the pot read as a real
  container.

**Locked (2026-07-07):**
- **Hybrid, not a live simulator.** Rest positions stay **deterministic and testable** (packed bottom-up);
  on removal the column above **tweens/falls** into the freed positions. No rigidbody sim, no perpetual
  jostling — the fall resolves to the deterministic rest layout and stops.
- **Bubbles sink, they do not float.** Consistent with F2's fill-bottom-up / rising-waterline: artifacts are
  heavy, they rest as a pile on the bowl floor and the gap collapses **downward**.
- **Accepted cost:** grabbing from the very bottom visibly settles the whole column above it by one place.
  The "untouched artifacts never move" acceptance point is **softened** to "untouched artifacts never move
  **unless they were resting on top of the removed one**".

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

1. **Stable packed order, volume filled bottom-up.** Bubbles are a **pile packed from the bottom up**, and
   an artifact's position is a **stable place in that packing order** for as long as it stays in the pot (the
   position is deterministic and testable, not a per-count phyllotaxis recompute). The stack grows with the
   count (this is what F2's fullness/waterline reads — the topmost bubbles sit just under the rising
   waterline). Adding an artifact **takes the next free spot on top** without disturbing the pile below it.
   Removing one lets **gravity close the gap** (FR4). No **arbitrary** global reshuffle on count change — the
   only motion is the coherent downward settle of FR4.
2. **Rest is still.** With no interaction, bubbles keep their calm idle life (the existing gentle drift is
   fine) but do **not** relocate. "Untouched artifacts stay in place" is the load-bearing acceptance point.
3. **Event physics — drop-in.** When an artifact enters the brew (dropped from a staging slot, or an
   auto-committed result, FR5), it **falls/splashes into its spot** and briefly **nudges nearby bubbles**,
   which then **settle back**. This is a short reaction, not perpetual motion.
4. **Event physics — removal (gravity settle).** When an artifact leaves the brew (picked to stage or
   socket), the bubbles that were **resting above it settle down** to close the gap — a short **fall/settle**
   to the new deterministic rest positions, so no bubble is left hanging over a void. Only the **column above**
   the removed spot moves; bubbles **beside and below** it keep their places. The settle resolves and stops
   (no perpetual jostling). When the topmost bubble is the one removed, nothing settles (nothing was above it).
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

- Picking or dropping one artifact causes **no arbitrary reshuffle** — every bubble **except the column that
  was resting above the removed one** stays in place; that column **settles down** to close the gap.
- Pulling an artifact from the **middle or bottom** never leaves bubbles **hanging over an empty spot** — the
  gap collapses downward.
- A dropped-in artifact **splashes and nudges neighbours**, which **settle back** to their rest spots.
- With a result hovering, clicking a pot artifact **drops the result into the brew automatically** and
  stages the new pick — no separate "collect first" step; clicking the result directly still drops it too.
- Bubbles never overlap on screen through any sequence of adds/removes.
- No change to fusion/socketing outcomes or to the inventory contents.

## Out of scope / open points (do not build now)

- **The three-zone layout and the bubble = submerged rule** — F1 (`cauldron-view-spatial-spine.md`).
- **The liquid surface and fullness = fill-level** — F2 (`cauldron-liquid-and-fullness.md`); bubbles are
  **fully submerged** (none pierce the surface) and the waterline sits above the topmost bubble. This brief
  owns the **stable spot within the volume** and the event reactions; F2 owns the surface/waterline itself.
- **Full continuous physics simulation** of the brew (rigidbodies in a bowl, perpetual jostling) —
  deliberately **not** wanted, and the 2026-07-07 gravity-settle does **not** introduce it: the drop-in
  splash, the removal settle, and the column fall are all **event-scoped** and resolve to the deterministic
  rest layout, then stop.
- Exact splash/settle animation curves and bubble idle feel — tech-art, code track.
- Implementation specifics (layout model, physics-vs-tween choice, View wiring) — the code track's call.
