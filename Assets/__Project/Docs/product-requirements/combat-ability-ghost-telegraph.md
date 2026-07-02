# Combat — Ability Ghost Telegraph — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-02
> Level: product-owner (what & feel). The code track owns the technical "how".
> Related: `vision.md` Pillar 4 (fast chess, read-the-queue — Shogun Showdown reference). Unifies the
> ROADMAP "Ability-queue display" + "Ability telegraphing on the battlefield" items and the backlog
> "Enemy-intent telegraph".

## Goal

Today, when you aim an ability the battlefield highlights **which cells** it hits — the "where". It
never shows **what it does** — the motion and the outcome (I dash here, this enemy is pushed there,
these take damage). We add a **ghost preview**: a translucent playback of the ability's **full
outcome**, so the player can plan the "fast chess" and read the board's intentions.

One mechanism does three jobs at once:
- **Understand your own ability** — a ghost plays when you queue it.
- **See the whole plan** — each unit shows its **queued abilities as icons above it**.
- **Read the enemy** — hovering any ability icon **replays its ghost**, including the enemy's, so you
  know what the opponent is about to cast.

This is a **readability layer on top of combat**; it does not change combat rules, damage, or turn
order.

## User stories

- As a player, when I **queue an ability** I see a **ghost play out its full outcome once** — my hero
  performs it, affected enemies show where they'd be pushed/pulled and who'd be hit — then the ghost
  fades. Now I understand what I just committed.
- As a player, I see **icons above each unit** for the abilities it has queued — the **plan** on the
  board, not just a HUD list.
- As a player, I can **hover an ability icon** above any unit to **replay that ability's ghost** — so
  I can re-check my own plan **and read what the enemy will cast**.
- As a player, the ghost is clearly a **preview** (translucent/ghostly), never confused with the real
  action.

## Functional requirements

### Aiming (unchanged base)
1. While aiming an ability, the existing **affected-cell highlight** still shows **where** it lands
   (the "where" layer stays). The ghost is the new **"what it does"** layer on top. Directional aim
   now follows the unit's **facing** (`combat-hero-facing.md`): **turning the hero re-points the
   whole queued volley and all its ghosts** together (global facing), so the highlight/ghosts swing
   as one.

### Ghost on submit
2. **At the moment an ability is submitted to the queue**, a **ghost of its full outcome plays once**,
   then **disappears when the animation ends**. (It does not persist or follow the aim.)
3. **Full outcome** = the **caster's action** (its movement/dash/cast, using the ability's own
   animation) **plus** ghost outcomes for **affected units**: where each would be **displaced**
   (pushed / pulled / to which cell) and **who would take damage**.
4. The preview is computed against the **current board state** (it does **not** simulate the effect of
   already-queued abilities). *(Accepted tradeoff — see open points.)*

### Queued-ability icons above units
5. **Each unit — player and enemy — displays its queued/planned abilities as icons above it**, in
   order. This is the readable "plan" on the battlefield.

### Hover to replay
6. **Hovering an ability icon replays that ability's ghost** (the same full-outcome playback, against
   the current board). This works on **enemy** icons too — the player reads **enemy intent** by
   replaying the enemy's planned abilities.

### Ghost styling
7. The ghost reads unmistakably as a **preview** — translucent / spectral — distinct from real units
   and real actions, so it never misleads about the live board.

## Content authoring rules (for the designer)
- Abilities **already** define what the ghost needs: their **animation**, **shape** (line/ring,
  length/radius), and **effect type**. The ghost is generated from that ability data — **no per-ability
  ghost authoring** beyond what an ability already declares.
- Any **displacement semantics** an ability has (push / pull / dash distance) must be expressible in
  the ability data so the ghost can show the resulting positions (extend the ability data if a needed
  effect can't be described yet — record it, don't hard-code).
- The **ghost visual treatment** (spectral material, fade) is a shared style, authored once, not per
  ability.

## Acceptance criteria
- Queuing an ability plays a **ghost of its full outcome once** (caster action + affected-unit
  displacement + who's hit) and the ghost **fades when its animation ends**.
- Each unit (player **and** enemy) shows **icons of its queued abilities** above it, in order.
- **Hovering an ability icon replays** that ability's ghost — verified on both a player and an
  **enemy** unit (enemy intent is readable).
- The ghost is visually a **clear preview** (translucent), not confused with live units.
- The **affected-cell highlight** during aiming still works (the "where" layer is preserved).
- **No change** to combat outcomes, damage numbers, or turn order.

## Out of scope / open points (do not build now)
- **Simulating the queue.** The preview is against the **current** board, so a chain (ability B that
  depends on where ability A pushed a unit) may not preview perfectly. A "dry-run the queue then
  preview" upgrade is a **later** enhancement, not this brief.
- **Enemy planned-ability visibility depends on the combat turn structure** surfacing enemy queued
  abilities *before* they resolve. That turn-flow change is owned by
  `combat-turn-intent-phase.md` (Plan → Act → Resolve, locked enemy intent) — **a prerequisite of
  this brief's enemy-intent half**. This brief owns the **surfacing + ghost replay**; that one owns
  the structure/commitment.
- **Exact ghost VFX / shader** (spectral material, motion trail, fade timing) is tech-art /
  render-look; this brief sets the intent and behaviour.
- **Category iconography** (grouping abilities as advance/jump/area/hook/ring) is an **optional
  supplement** to the ghost, not the primary telegraph.
- **Smarter enemy AI** (choosing/aiming abilities well) is a separate ROADMAP item.
- No change to what any ability does mechanically.
