# Combat — Initiative, Turn-Order Queue & Facing Input — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Part of the "Bandit Camp & Combat Legibility II" initiative (Track D, brief 2 of 3).**
> **Builds on** Track C — `combat-round-and-telegraph.md` (Plan → Act → Resolve, locked enemy intent),
> [Combat Hero Facing](combat-hero-facing.md) (facing drives direction; the whole volley fires along the
> **current** facing; turning is free), and [Combat Enemy Intent Phase](combat-turn-intent-phase.md)
> (resolution order). Advances the deferred **initiative-order** follow-up (`combat-round-and-telegraph.md`
> §6) and adds the on-screen order read + the aim/fire control scheme.

## Goal

Combat today resolves in a **fixed order** — the player's actions always land first, then the enemies' —
and the player has **no on-screen read of the order of play**. And aiming (turning the hero) uses the
generic direction input with **no committed "now fire" gesture**. This brief gives the fight a **legible
order** and a **clear control loop**:

- **The one who started the fight goes first.** If the player attacks, the player leads; if the fight came
  from a hostile NPC (through dialogue, or an ambush with no dialogue), the **enemy leads**.
- **A turn-order strip** in the top-right shows **who acts in what order**, so the round reads at a glance.
- **Aim by holding, fire by releasing.** Holding **Enter** turns the hero toward the **mouse cursor**
  (re-pointing the whole queued volley, per the shipped global-facing model); **releasing Enter executes the
  queue** along the final facing.

This changes **who leads a round**, adds an **order read-out**, and pins the **aim/fire input**. It does
**not** change what any ability does.

## User stories

- As a player, when **I** start the fight (I chose to attack), **I act first**; when a bandit jumps me (from
  a talk that turned, or an ambush), the **enemy acts first** — the aggressor gets the opening move.
- As a player, I can see a **horizontal order strip in the top-right** telling me who acts next and in what
  sequence, so I always know whose move it is.
- As a player, I aim by **holding Enter and moving the mouse** — my hero **turns toward the cursor**, and I
  can see my whole planned volley swing to the new facing.
- As a player, I **release Enter to commit** — the queued volley fires along the direction I ended on. Aiming
  is free right up until I let go.

## Functional requirements

### Initiative — the initiator leads

1. **Who acts first in a fight is decided by who started it:**
   - **Player-initiated** (the player chose to attack — e.g. picked the attack action against an NPC, or
     otherwise triggered the fight themselves) → **the player leads the first round**.
   - **Enemy-initiated** → **the enemy side leads the first round**. This covers **both** a fight that came
     **out of a dialogue** (the NPC turns on the player) **and** an **ambush with no dialogue** (a hostile
     engagement that starts a fight directly).
2. Initiative is decided **at the moment the fight starts**, from **who caused it**, and sets the **leader of
   the opening round**. (Whether the lead alternates or persists across later rounds is a combat-design
   detail for the code track — the felt requirement is that **the opening move belongs to the initiator**.)
3. Initiative **reorders who resolves first**, replacing today's always-player-then-enemies rule for the
   opening round; it does **not** change what actions do, the plan/commit model, or determinism (same seed +
   same initiator → same order).

### Turn-order queue UI

4. Combat shows a **turn-order / action-order strip**: a readable sequence of **who acts and in what order**
   this round (the player and each live enemy).
5. The strip is **horizontal**, anchored in the **top-right corner** of the screen. *(This is a **demo
   placement** — final HUD layout is out of scope.)*
6. The strip **reflects the current order**, including **who leads** per the initiative rule, and **updates as
   the round progresses** (whose turn is current / already acted reads clearly; units removed from the fight
   drop out of the strip).
7. The strip is a **read-out only** — it does not add a new way to act; it surfaces the order the round
   already follows.

### Aim & fire input (hold-to-turn, release-to-execute)

8. **Holding Enter enters an aim gesture**: while Enter is held, the **hero turns to face the mouse cursor** —
   i.e. it faces the **cell / direction the cursor points at**. This drives the **existing global facing**
   (`combat-hero-facing.md`): the hero's model turns, and the **whole queued volley and its telegraphs
   re-point** with it.
9. **Turning while aiming is free and continuous** — moving the cursor with Enter held re-points the hero any
   number of times at no cost, up to release (consistent with the shipped free-facing rule).
10. **Releasing Enter executes the queued volley** — the queue fires along the **final** facing (the direction
    the hero ended on at release). Aiming and firing are **one gesture**: hold to aim, release to commit.
11. The hero's facing is **visibly readable** on the model throughout the aim (the shipped facing-legibility
    rule) so the player sees where the volley will fire before releasing.

## Content authoring rules (for the designer)

- **None new.** Initiative, the order strip, and the aim/fire input are **presentation + turn-flow** over the
  existing combat model and the existing abilities. No new content type; no per-ability or per-enemy authoring.

## Acceptance criteria

- **Player-started fight → player acts first; enemy-started fight (dialogue-turned or ambush) → enemy acts
  first**, visible in the order strip.
- A **horizontal turn-order strip** sits in the **top-right**, lists the round's actors **in order**, shows
  **who leads**, and **updates** as units act / leave.
- **Holding Enter turns the hero toward the mouse cursor**, and the **whole queued volley + telegraphs swing**
  to the new facing; the model shows the facing.
- **Releasing Enter fires the queue** along the final facing.
- **Same seed + same initiator → same order**; abilities are unchanged.

## Out of scope / open points (do not build now)

- **Speed / stat-based initiative** (a fast unit leaping ahead of the initiator) — this brief ties the opening
  move to **who started the fight** only; a per-unit speed/initiative stat is a later enhancement.
- **Multi-round initiative policy** (does the lead alternate, persist, or re-roll each round) — a combat-design
  detail; the required guarantee is the **opening** round's leader.
- **Final HUD design / theming of the order strip** — the top-right horizontal strip is a **demo** placement;
  polished layout, portraits, and styling are a later UI pass.
- **Rebindable / gamepad aim-and-fire input** — this brief pins the **Enter hold-to-aim / release-to-execute**
  scheme for the demo; input remapping is out of scope. *(Gamepad aim/fire has since landed via
  `cross-device-input-foundation.md` — the same gesture on RT + left stick, see `Docs/input-foundation.md`;
  player-facing remapping stays out of scope.)*
- **The animation of an action as it resolves** (making an enemy's turn readable-not-instant, the ability
  playing an animation) — owned by the initiative's third brief
  ([Ability Animation & Enemy Action Read](combat-ability-animation.md)).
- **No change** to what any individual ability does, to the plan/commit model, or to determinism.
