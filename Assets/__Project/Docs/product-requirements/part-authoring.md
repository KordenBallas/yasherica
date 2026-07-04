# Body-Part Authoring — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-03
> Level: product-owner (what & authoring rules). The code track owns the technical "how".
> Design background: `design/characters/model.md` (body-plan / skeleton-priority model).

## Goal

Make a **body part** a thing a designer **or an AI model** can author correctly on its own — a low-poly
mesh that snaps onto the hero's rig, fills the right slot, carries the right sockets, and can be
re-skinned for rarity. The hero is already assembled from modular parts on a shared rig
(`character-system.md`), and the hero's silhouette must read **visibly different from run to run**
(vision Pillar 1) — but there is **no formal authoring contract** for a part today, so producing a new
one (by hand or by generation) is guesswork.

This brief defines, at the authoring level, **what a valid part must satisfy** so it drops into the game
without hand-fixing: its **slot**, **size/proportion** budget, **socket & attach conventions**, its
**skeleton family**, and its **rarity re-texture** rules. It follows the accepted **independent
body-plans** model (`design/characters/model.md`): the hero can take on **different whole skeletons**, a
special part can **pull in its own skeleton**, and parts are **scoped to a skeleton family**.

This is an **authoring contract**. The runtime mechanics of swapping skeletons / handling incompatible
equipped parts are the code track's job (see `needs-code.md`); this brief is the rules an author follows.

## User stories

- As a designer, I can read **one spec** and produce a new head/tail/wing/arm/leg that **installs
  correctly** — right slot, right scale, sockets where they belong — without reverse-engineering existing
  parts.
- As a designer/AI generator, I know the **size and proportion budget** for each slot, so a generated part
  isn't wildly too big/small or misaligned when it snaps on.
- As a designer, I know which **skeleton family** a part belongs to, and how to mark a part that **requires
  its own skeleton** (a radically different body plan — a legless serpent, an extra pair of limbs).
- As a designer, I can give a part **rarity variants by re-texturing one mesh** (a common frog head is
  green; a legendary one wears a different texture set) **without** authoring a whole new part.
- As a designer, I can add **sub-parts on a part's own sockets** (ears/eyes/horns on a head's sockets),
  reusing the same contract one level down.

## Functional requirements

### Slot & fit
1. A part declares the **slot** it occupies (Head, Torso, Arm L/R, Leg L/R, Tail, Wings, …) and fills
   exactly that slot.
2. A part has a defined **size/proportion budget** per slot (a target scale + tolerance and an alignment
   origin), so a conforming mesh snaps on **in proportion** with the rest of the body and doesn't clip or
   float. The budget is **authored data**, expressed so a generator can hit it.

### Sockets & sub-parts
3. A part exposes its **sockets** (the two-tier model already shipped: a part like a head contributes
   sockets for ears/eyes/horns/hats). Socket **placement and naming** follow a **stable convention** so
   sub-parts attach predictably.
4. A **sub-part** (ear, eye, horn…) is authored by the **same contract** against a parent part's socket —
   slot = the socket, with its own size budget.

### Skeleton family & body plans
5. A part declares its **skeleton family** (which body plan it is built for). A part authored for the
   **base** family is valid on the base rig (the ~80% common case).
6. A part may be marked as **requiring its own skeleton** (it pulls a different whole body plan when
   equipped). Such a part carries the **priority** that decides which skeleton governs when parts disagree
   (`design/characters/model.md`).
7. A part authored for one family is **not assumed compatible** with another family — cross-family fit is
   **not** an authoring requirement (independent body-plans); the runtime handles incompatible equipped
   parts on a skeleton change (code track).

### Rarity re-texture
8. A single part mesh supports **multiple texture/material variants keyed by rarity** (Common…Mythical) —
   authoring a legendary version of an existing part is **re-texturing one mesh**, not a new mesh, unless
   the author *wants* a new mesh.
9. Rarity re-texture is **visual only** — it does not change the part's slot, size, sockets, or skeleton
   family.

### Alignment contract (the non-negotiable)
10. A part must respect the rig's **attach/bind conventions** for its family so it installs without manual
    editor fix-up. The **exact** bone/socket naming and bind-pose rules are the **binding technical
    contract** owned and documented by the code track's character system doc; this brief requires only
    that a part **conform to that contract** and that the contract be **published for authors/generators**.

## Content authoring rules (for the designer / AI generator)

- Pick the **slot**; hit the slot's **size/proportion budget** and alignment origin.
- Place **sockets** by the naming convention; author any **sub-parts** against those sockets by the same
  rules.
- Declare the **skeleton family**; if the part needs a new body plan, mark it **skeleton-changing** with a
  priority.
- Add **rarity variants** as texture/material sets over the one mesh where possible.
- Conform to the published **bind-pose / bone-naming** contract for the family — a part that violates it is
  an invalid part, not a runtime bug to absorb.

## Acceptance criteria

- A part authored to the spec **installs into its slot** in proportion and aligned, with **no manual
  editor fix-up**.
- A part's **sockets** accept sub-parts (ears/eyes) predictably by the naming convention.
- A **skeleton-changing** part, when equipped, governs the body plan per its priority (runtime behaviour
  per the code track), and a **base-family** part rides the base rig.
- A **rarity re-texture** of one mesh renders as a distinct-looking variant with identical slot/size/
  sockets/family.
- The **authoring contract is documented** clearly enough that a designer or an AI generator can produce a
  valid part from it alone.

## Out of scope / open points (do not build now)

- **The runtime skeleton-swap mechanics** — rebinding the rig, retargeting the Animator, and
  auto-unequipping incompatible equipped parts on a body-plan change — are the code track's job (see
  `needs-code.md`); this supersedes the shipped single-superset assumption (`character-system.md`
  R10/R3).
- **The race roster & per-race body plans / signature marker parts** — a separate parked design pass
  (`parking-lot.md`); this brief is the authoring contract, not the roster.
- **Procedural mesh generation tooling / the AI generation pipeline itself** — a production/tooling item,
  not this brief; this brief defines the target the pipeline must hit.
- Exact numeric size budgets, socket coordinates, and bind-pose bone names — the concrete values live in
  the code track's character system doc (the published contract), tuned in production.
