# Combat — Hero Facing & Directional Abilities — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-02
> Level: product-owner (what & feel). The code track owns the technical "how".
> Related: `vision.md` Pillar 4 + `world/overview.md` §2 ("anatomy telegraphs abilities"). Reshapes
> the aiming model shared by `combat-turn-intent-phase.md` and `combat-ability-ghost-telegraph.md`.

## Goal

Tie an ability's **direction to the unit's facing** — "where the eyes look is where it fires" —
instead of aiming and baking a direction into each queued ability. Facing is **free to change** (like
a chess repositioning) and **global**: the whole queued volley fires along the unit's **current
facing**, and turning the hero **re-points the entire queue** (and its telegraph ghosts) at once. This
makes the world's "anatomy telegraphs abilities" law a real mechanic, keeps aiming to a single
readable decision (which way do I face?), and makes turning the pivotal tactical move.

This changes **how directional abilities are aimed**; it does **not** change what an ability does.

## User stories

- As a player, my hero fires its directional abilities **where it is facing** — I aim by **turning the
  hero**, not by aiming each ability.
- As a player, **turning is free and unlimited** before I commit — I can try facings at no cost.
- As a player, when I turn, my **whole planned volley (and its ghosts) swings** to the new facing.
- As a player, **ring / all-around abilities** fire regardless of which way I face.
- As a player, I can **read an enemy's facing** to know where its committed attack will land.

## Functional requirements

### Facing drives direction
1. **Directional abilities fire relative to the unit's facing** — there is **no per-ability stored
   direction**; the unit carries a facing and all its directional abilities use it.
2. **Global facing (single orientation for the whole queue).** The queued volley executes along the
   unit's **current facing**; turning the hero **re-points the entire queue** and all its telegraph
   ghosts together. (There is one facing, not one-per-ability.)
3. **Ring / all-around abilities are unaffected** by facing.

### Free, visible, aim-by-turning
4. **Changing facing is free and unlimited** — no action/time cost — any number of times during the
   player's Act phase, up to executing the queue (the volley fires along the **final** facing).
5. **Aim = turn.** The existing direction-selection input **rotates the unit** instead of baking a
   direction into an ability.
6. **Facing is legible.** The unit's front / eyes clearly show which way it faces (anatomy
   legibility, Pillar 4).
7. **Facing is set independently of movement** — moving does not force a facing change; the player
   re-faces explicitly. *(Minor; can be refined in playtest.)*

### Enemies face too (ties the intent phase + ghost)
8. **An enemy's committed intent includes a committed facing** (`combat-turn-intent-phase.md`): its
   directional actions fire along that facing and are **locked** — they land where the enemy faced
   even if the player repositioned. Reading an enemy's facing = reading where its blow goes, and its
   ghost (`combat-ability-ghost-telegraph.md`) is drawn along that facing.

### Determinism
9. Determinism is unaffected (facing is a deliberate input, not randomized).

## Acceptance criteria
- **Turning the hero re-points the whole queued volley** and all of its telegraph ghosts.
- **Directional abilities fire along the current facing**; **ring abilities** fire all-around
  regardless of facing.
- **Facing changes cost nothing** and can be done any number of times before executing.
- The unit's facing is **visibly readable** on the model.
- An **enemy's committed action fires along its committed facing** (locked) regardless of where the
  player moved.

## Out of scope / open points (do not build now)
- **Per-ability independent aiming is intentionally removed** by the global-facing choice — a turn's
  volley is single-direction. **Multi-directional volleys via "turn as a queued step"** (Model B) are
  a **deferred escape hatch**, to revisit only if playtest shows a turn feels too constrained.
- **Turn animation / VFX** (how the model pivots, the ghost swing) — tech-art / render-look.
- **Facing ↔ movement coupling** nuance (auto-face on move, etc.) — refine in playtest.
- **No change** to what any individual ability does.
