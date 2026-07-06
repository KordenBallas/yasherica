# Combat — Ability Animation, Animated Ghost & Enemy Action Read — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Part of the "Bandit Camp & Combat Legibility II" initiative (Track D, brief 3 of 3).**
> **Builds on** Track C — `combat-round-and-telegraph.md`: the ghost telegraph (translucent full-outcome
> playback on queue-submit + hover-replay), the overhead plan icons, and the paced enemy resolve.
> **Resolves** the deferred *"ghost caster is a static clone — `AnimationTrigger` authored but unconsumed"*
> follow-up (§6) and **advances** the *move-intent presentation* gap (`»`-glyph only) — see also
> [Combat Move-Destination Telegraph](combat-move-destination-telegraph.md).

## Goal

The combat readability layer is in place — a **ghost** previews an ability's outcome, and every unit shows
its **plan as icons** — but the actions themselves are **frozen and instant**: the ghost is a **static
translucent clone** (no motion), and an enemy's committed action **snaps** rather than playing out. So a
player can't *watch* what an ability is, and an enemy's turn is over before it can be read.

This brief gives abilities a **real animation**, and uses it in two states:

- **Ghost / preview state** — when an ability is **added to the queue** (or its plan icon is hovered), the
  animation plays **translucently**, as the current ghost does, but now **moving**.
- **Live state** — when the ability **executes**, the animation plays **for real** — **including on the
  enemy's turn**, so an enemy's action **plays out at a readable pace** instead of resolving instantly.

Because there is no animation art yet, the **code track authors the animation itself as a placeholder**
(the same way it authored the placeholder skeleton frames for the body-plans work) — a functional,
readable motion that **covers the ability's whole affected area**, good enough to read and to be replaced
by real art later.

It also adds a **committed-queue readiness cue** on enemies: today the plan icons hang above a unit but
**don't read as "this enemy is armed and about to act"**. When an enemy has a committed queue, it now
signals **wound-up readiness** so the threat reads at a glance during the plan phase.

This is a **presentation / readability** change. It does **not** change what any ability does, its damage,
its cells, or the turn structure.

## User stories

- As a player, when I **queue an ability** I see its **animation play out as a translucent ghost** — I watch
  the motion, not just a frozen pose — then it fades, exactly where the current ghost preview appears.
- As a player, when an ability **actually fires**, I see that **same animation play for real** over the cells
  it affects.
- As a player, an **enemy's action on its turn plays out with a visible animation** — it takes a readable
  moment, so I can **see** what it did instead of it snapping instantly.
- As a player, the animation **covers the whole area the ability acts on** — a line ability animates along its
  line, an area ability animates across its cells — so the motion reads as *this ability*, matched to its
  shape.
- As a player, I can tell an enemy's **committed movement direction** on the board from an **arrow**, so I
  read where it will go before it goes.
- As a player, I can tell at a glance that an **enemy has locked in a queue of abilities and is about to
  act** — its overhead plan icons go **restless (jitter/pulse)** and the enemy strikes a **"ready-to-attack"
  pose** — so a wound-up threat reads before its turn comes.

## Functional requirements

### Ability animation (authored by the code track as a placeholder)

1. **Every combat ability has an animation.** Because production animation art does not exist yet, the **code
   track authors a placeholder animation** for abilities — a functional, readable motion — rather than
   deferring to a future art asset. (Treat this like the placeholder skeleton frames already authored for the
   body-plans work: good-enough-to-read now, replaceable by real art later.)
2. **The animation spans the ability's whole affected area** — it plays **across all the cells the ability
   acts on**, matched to the ability's shape (e.g. a line ability reads along its line; an area/ring ability
   reads across its footprint), not as a single in-place pose on the caster only.
3. The animation is **driven by the ability's own data** (its shape and the existing per-ability animation
   hook), so **adding or changing an ability** carries its animation without bespoke per-ability wiring beyond
   what an ability already declares.

### Ghost / preview state (queued or hovered)

4. **When an ability is added to the queue**, its **animation plays in the ghost/preview treatment** — the
   same **translucent / spectral** look the current ghost uses — now **animated** rather than a static clone.
   As today, it plays **once on submit** and **fades when the animation ends**, against the current board.
5. **Hovering the ability's plan icon replays the animated ghost** — for the player's own queued abilities and
   for an **enemy's committed intent** — so the player can re-check a plan or **read what an enemy will cast**
   as a moving preview. (This is the existing hover-replay, now animated.)
6. The preview stays **unmistakably a preview** (translucent / spectral) — it must never be confused with the
   live action even though it now moves.

### Live state (execution — player and enemy)

7. **When an ability executes, its animation plays for real** (opaque, on the live board) over the cells it
   affects — the same motion the ghost previewed, now live.
8. **An enemy's committed action plays its animation on the enemy's turn**, at a **readable pace** — the
   action is **no longer instant**; the player can watch it resolve. (This uses the existing paced enemy
   resolve; the action now **animates** during that beat instead of snapping.)
9. The live animation does **not** change the ability's outcome, damage, affected cells, timing of *when* the
   result applies, or the turn order — it is the **visible playback** of an action that resolves by the
   existing rules.

### Enemy movement-direction arrow

10. An enemy's **committed movement** is shown on the battlefield as an **arrow indicating its direction**
    (where it will move), as part of the intent telegraph during the plan phase — replacing the placeholder
    `»` glyph as the movement read. The arrow shows the **committed / intended** direction; if the move
    whiffs because the board shifted, that is consistent with the existing committed-intent model (the
    telegraph is a promise of intent, not a guarantee of outcome).

### Committed-queue readiness cue (enemy)

11. **An enemy that has a committed queue reads as "armed / about to act"** during the plan phase, via
    **two combined cues**:
    - its **overhead plan icons become restless** — a subtle **jitter / pulse** instead of sitting static —
      so the queued abilities themselves signal "loaded and imminent";
    - the enemy strikes a **"ready-to-attack" / wound-up pose** on its body, so the threat reads at the
      character level too. (Because there is no animation art yet, the **code track authors this pose as a
      placeholder**, the same way it authors the ability placeholders in requirement 1.)
12. The cue applies to **enemies only** — it is about reading the **threat**; the player's own committed
    queue is not required to carry it.
13. The cue is **uniform**: **every** enemy holding a committed queue shows the **same** readiness signal,
    regardless of where it sits in the turn order — it does **not** escalate for whoever acts next. (Turn
    order is read from the order strip in [Initiative & Turn-Order Queue](combat-initiative-and-turn-queue.md),
    not from this cue.)
14. The cue is **tied to having a committed queue**: it appears once the enemy's queue is locked for the
    round and **clears** when it has no committed actions left (empty queue / after its actions resolve). It
    is **presentation only** — it does not change the queue, the outcome, or the turn order.

## Content authoring rules (for the designer)

- **No new designer content is required to ship this** — the **code track authors the placeholder animations**
  (requirement 1). A designer replacing them with real animation clips later does so through the **ability's
  existing animation hook**; a new ability inherits animated ghost + live playback with **no per-ability ghost
  authoring** beyond declaring its animation and shape (as today).
- The **ghost/preview treatment** (translucent look, fade) and the **move-arrow** cue are **shared visual
  styles**, authored once, not per ability.

## Acceptance criteria

- **Queuing an ability plays its animation as a translucent, moving ghost** once, then fades — in the same
  place the current ghost preview appears.
- **Hovering a plan icon replays the animated ghost** — verified on a **player** ability **and** an **enemy**
  committed intent.
- **Executing an ability plays that animation for real** over its affected cells.
- An **enemy's turn action plays its animation at a readable pace** — the enemy's move is **visibly not
  instant**.
- The animation **covers the ability's whole affected area** (line along its line, area across its cells), not
  just a pose on the caster.
- An enemy's **committed move shows a direction arrow** on the board during the plan phase.
- An enemy holding a **committed queue reads as "armed / about to act"** — its **plan icons jitter/pulse**
  **and** it holds a **"ready-to-attack" pose** — during the plan phase; the cue **clears** once its queue is
  empty / its actions have resolved. The cue is on **enemies only** and is **uniform** (same on every armed
  enemy, no escalation by turn order).
- **No change** to combat outcomes, damage numbers, affected cells, or turn order.

## Out of scope / open points (do not build now)

- **Production-quality animation art** — the code track ships **placeholder** animations that read correctly;
  real clips / VFX / render-look are a later art pass (the deferred ghost-caster-animation art item).
- **Per-ability bespoke choreography** beyond "animation spans the affected area" — the placeholder reads the
  ability's shape; richly hand-tuned motion per ability is a later polish pass.
- **Queue-simulation preview** — the animated ghost still plays against the **current** board (chained
  previews that depend on an earlier queued push are the existing deferred "dry-run the queue" upgrade).
- **Full movement path visualisation** (the whole step-by-step route) — only the **direction arrow** /
  destination is required here; a full path line stays deferred (see
  [Combat Move-Destination Telegraph](combat-move-destination-telegraph.md)).
- **Exact VFX / shader / timing** of the ghost material, the live animation, and the arrow — tech-art /
  render-look; this brief sets the intent and behaviour.
- **No change** to what any ability does, to the plan/commit model, or to determinism.
