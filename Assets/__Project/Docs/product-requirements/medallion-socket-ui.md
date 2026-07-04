# Medallion Socket UI — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-03
> Level: product-owner (what & feel). The code track owns the technical "how".

## Goal

Give the socketed Part-Blank a **presentation that reads like building an organ**, and a better home in
the inventory. The socketed-blank **mechanic already ships** (socket artifacts into a blank → cross its
threshold → unseal a menu of variant mutations, `crafting-mutation-socketed-blanks.md`,
`mutation-choice-cards.md`), but its blanks sit as a **flat placeholder row of sockets on a rack to the
left of the cauldron** — visually weak and awkwardly placed.

We re-present a blank as a **medallion**: the body part pictured at the centre, its sockets arranged as
**gems evenly around the rim**, and the **rim itself a progress line** that fills as sockets fill and
**closes** when the blank is ready — and only **then** is the mutation offered. It makes the "socket it up,
watch it complete, then pull the mutation" loop tactile and legible.

This is a **presentation + one interaction-beat** change over the existing socketing model. It does
**not** change what a blank is, the trait/tier hiding rules, or the unseal variant menu.

## User stories

- As a player, I see each blank as a **medallion** with the body part at its heart and its sockets as
  **gems set around the rim** — I read "this organ needs N things set into it" at a glance.
- As a player, as I drop artifacts into sockets the **rim fills like a progress ring**, so I can see how
  close the organ is to being ready.
- As a player, when the **rim closes** (the organ is complete) the game **offers me the mutation** as a
  distinct beat — I choose to unseal it, rather than it happening the instant I drop the last piece.
- As a player, the medallion lives in a **clear, deliberate spot** in the pot/inventory view, not shoved
  to the side.
- As a designer, I don't author new data — a medallion is driven by the blank's existing socket count and
  fill state.

## Functional requirements

1. **Medallion layout.** A racked Part-Blank is shown as a **medallion**: the part at the centre, its
   sockets placed **symmetrically/evenly around the rim** (gem-like), with the number of gems = the
   blank's socket count.
2. **Rim as progress.** The **rim** reads as a **progress line** that fills as sockets are filled and is
   visibly **complete/closed** when the blank has reached its ready threshold.
3. **Confirm-before-unseal beat.** When the medallion completes, the mutation is **offered** as a
   **distinct action** ("the organ is ready — unseal it"), which the player triggers to open the variant
   menu. This **replaces today's auto-unseal on the last socket** (`mutation-subsystem.md` §6 noted this
   as the intended tweak).
4. **Commit rule unchanged in spirit.** Arranging/re-slotting stays free **until** the player confirms
   the unseal; unseal remains the **point of no return** (reagents consumed, one variant chosen, the rest
   lost) — the commit simply moves from "last drop" to "confirm".
5. **Feeds the existing variant menu.** Confirming the unseal opens the already-specced **mutation choice
   cards** (`mutation-choice-cards.md`); this brief owns the medallion + completion + confirm, not the
   card menu.
6. **Legibility rules hold.** Traits stay hidden; the medallion shows only the coarse cues already agreed
   — **archetype via colour, tier via glow** — and the cauldron voice stays the hint channel. No stat
   blocks on the medallion.
7. **Placement.** The medallion(s) sit in a **deliberate, legible position** in the pot/inventory view
   (resolving "shoved to the left of the cauldron"), coherent with the belly/cauldron framing.

## Content authoring rules (for the designer)

- None new. The medallion is driven by the blank's existing **socket count**, **fill state**, and
  **ready threshold**; archetype colour + tier glow reuse the shared artifact grammar.

## Acceptance criteria

- A blank renders as a **medallion** with the part centred and sockets as gems around the rim; gem count
  matches the blank's socket count.
- Filling sockets **advances the rim progress**; the rim reads as **complete** when the blank is ready.
- On completion the mutation is **offered as a separate confirm action**, not auto-triggered by the last
  drop; confirming opens the existing variant-card menu.
- Re-slotting is free before confirm; **confirming commits** (consumes reagents, one variant chosen).
- No traits/stats are shown; only archetype colour + tier glow; the medallion sits in its new deliberate
  position, not to the left of the cauldron.

## Out of scope / open points (do not build now)

- **The unseal variant menu** (the hand of mutation choice cards) — its own brief
  (`mutation-choice-cards.md`).
- **Partial-fill ready threshold** (< all sockets) — the design allows a threshold distinct from "all
  sockets full" (`crafting-mutation-socketed-blanks.md` FR7), but the rim MVP may equate "closed" with
  "all sockets filled"; a partial threshold is a later tuning option.
- **The stomach-interior backdrop** of the pot view — a separate art/stage item.
- Exact medallion art, gem/rim VFX, animation of the closing ring — tech-art / render-look.
- Implementation specifics (View/Presenter wiring) are the code track's call, not this brief.
