# Combat — Enemy Move-Destination Telegraph — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-03
> Level: product-owner (what & feel). The code track owns the technical "how".

## Goal

Let the player **see where an enemy will move** this round, on the board. Combat is "fast chess" — you
read committed intent and plan against it (`combat-turn-intent-phase.md`). Today an enemy's committed
move is shown only as a placeholder `»` glyph **above the unit**; the **destination cell is not drawn**
(`combat-round-and-telegraph.md` §6, a known gap). So the player can see *that* an enemy will move but
not *where to* — a hole in the plan-against-intent read that abilities and facing already fill.

We draw the enemy's **committed move destination on the hex board**, as part of the intent telegraph, so
the whole of an enemy's turn (where it steps, where it faces, what it fires) is readable before the
player commits.

This is a **telegraph/readability** change only. It does **not** change movement rules, AI, or the
turn structure.

## User stories

- As a player, when the enemies reveal their locked plans, I can **see the hex each enemy will move to**,
  not just an icon that says "it moves".
- As a player, I use that to **plan my turn** — step out of its path, meet it where it lands, or bait it.
- As a player, if the enemy's committed move ends up whiffing because the board changed (its target moved,
  the cell is taken), that reads consistently with the rest of the committed-intent model — the telegraph
  showed its **intended** destination.

## Functional requirements

1. When an enemy has a **committed move** in its revealed intent, the board shows its **destination cell**
   (and, clearly enough to follow, that this enemy is the one moving there) during the plan phase — not
   only the above-unit glyph.
2. The destination telegraph is **legible alongside** the existing ability ghost + facing cues: the player
   can read *step*, *facing*, and *ability* for the same enemy without them fighting for attention.
3. The telegraph reflects the **committed** move against the current board; it shows the **intended**
   destination even though the move may **whiff** at resolution if the board shifted (consistent with the
   committed-intent model — the telegraph is a promise of intent, not a guarantee of outcome).
4. Facing stays read **on the model** (`combat-hero-facing.md` FR6) — this brief adds only the
   **movement-destination** cue, and does **not** introduce an on-hex facing glyph.
5. The cue is shown for **enemy** committed moves (reading enemy intent); whether the player's own planned
   move gets the same on-board destination cue is a nice-to-have, not required here.

## Content authoring rules (for the designer)

- None new — this is a presentation cue over the existing committed-move data. No new content type.

## Acceptance criteria

- With an enemy that commits a move, its **destination hex is visibly marked** on the board during the
  plan phase, attributable to that enemy.
- The destination cue coexists **readably** with the enemy's ability ghost and model facing.
- If a committed move whiffs (board changed), behaviour is unchanged — the telegraph had shown the
  intended destination; no new correctness rule is introduced.
- Removing/decoupling this cue does **not** change movement, AI, or turn order — it is visual only.

## Out of scope / open points (do not build now)

- **Enemy orientation as an on-hex `>` glyph** — **cut**: facing is already legible via model rotation
  (`UnitFacingRotator`, `combat-round-and-telegraph.md` R8). We do not duplicate it on the hex.
- **Ability animations / ghost skeletal playback** — a separate already-scoped tech-art item
  (`_animationTrigger` authored but unconsumed; `combat-round-and-telegraph.md` §6).
- **Pathing visualisation** (the whole route, step-by-step) — only the **destination** is required; a
  full path line is optional polish, deferred.
- Exact VFX / shader for the destination marker — tech-art / render-look.
- Implementation specifics are the code track's call, not this brief.
