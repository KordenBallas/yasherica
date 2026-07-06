# Humanoid Bandit Camp — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Part of the "Bandit Camp & Combat Legibility II" initiative (Track D, brief 1 of 3).**
> **Builds on** [NPC Proximity Interaction](npc-proximity-interaction.md) (the derived-intent + radius
> + marker model), [World Sites & Landscape](world-sites-and-landscape.md) (the Camp Site + its
> `Combat·bandit` anchor), and [Camp Shady Offer](camp-shady-offer.md) (the boss-fronts-crew fiction).
> Fuller background: `world-sites.md`, `narrative-procedural.md` §2.6 (the density allocator + monster pools).

## Goal

Today the enemies the player fights are **placeholder capsules**, and a bandit "camp" is just a knot of
those capsules with no face and no read. We turn a bandit camp into a **place that reads**: a **boss**
(the camp's leader) fronting a **crew** of accomplices, all rendered as **ordinary humanoids** using the
**same model as the hero and the classic NPCs** — no more capsules — and told apart **by colour** for the
demo. Crucially, walking onto a camp platform **does not start a fight**: the crew only fights when the
player **engages the boss** — by crossing the boss's interaction area (a fight, or a talk if he has a job)
or by turning on him in dialogue. The camp is a thing you approach and read, not a tripwire you land on.

This brief owns the **who's-in-the-camp, what-they-look-like, and when-the-fight-starts** half. The
**combat feel** once it starts (initiative, turn-order UI, facing input, ability animation, move arrows)
is the other two briefs of this initiative.

## User stories

- As a player, the enemies I fight are **humanoids like my hero and the townsfolk**, not floating capsules.
- As a player, I can tell **at a glance** who is who: peaceful villagers read **green**, a **bandit boss**
  reads **deep maroon**, and his **crew** reads a **lighter shade** of that same bandit colour.
- As a player, when I **jump onto a platform that holds a bandit camp** (or other aggressive monsters), the
  **fight does not start just because I landed** — I can look around and choose my approach.
- As a player, the **boss has a reach around him** — a larger circle than an ordinary NPC — and I start the
  encounter by **stepping into it**: if he has a job for me it opens a **talk**; if he doesn't, the **camp
  fight begins**. His **crew joins that one fight**; they are not separate tripwires standing between us.
- As a player, once a fight starts the bandits **keep looking like the humanoids they were** — the maroon
  boss stays the maroon boss on the battlefield.
- As a designer, I decide **how many** bandits a camp holds and whether the boss **carries a quest**,
  through the same director/data path that already sizes the world — not by hand-wiring a scene.

## Functional requirements

### Humanoid models (retire the capsule)

1. Enemies are rendered with the **existing humanoid character model** — the **same one** the hero and the
   classic NPCs use — **reusing** that model, not a new asset. The placeholder capsule look is **retired**
   for these enemies.
2. The **same model** is used **in the world and in combat**: a bandit that reads as a maroon humanoid when
   the player approaches **still reads as that same maroon humanoid** on the battlefield (no capsule swap at
   combat entry).

### Demo colour coding (conditional, demo-only)

3. Characters are tinted by role, purely to make the demo readable — these colours are **placeholder /
   conditional**, not final art:
   - **Peaceful / classic NPCs → green.**
   - **Bandit boss (leader) → deep maroon (bordeaux).**
   - **Bandit crew (accomplices) → a lighter shade of the boss's maroon** — clearly the same faction, clearly
     subordinate.
4. The colouring is a **simple tint over the shared model**, applied by role, so a new coloured role is a
   data/authoring choice, not new art. It is explicitly a **demo affordance** and is expected to be replaced
   by real per-faction art later (out of scope here).

### Camp composition (director-sized; boss may or may not carry a quest)

5. A bandit camp is a **boss + a crew of accomplices**. The **number of accomplices is configurable and
   decided by the director / world data** (the same content-sizing path that already populates the world) —
   **not** a fixed, hand-placed count.
6. The **boss may carry a quest thread or not**:
   - **Boss with a quest** → he is a **talkable quest-bearer** (a job to offer; see the engagement rules).
   - **Boss without a quest** → he is the **fight's trigger** (engaging him starts the camp battle).
   In both cases the **accomplices carry no quest** — they are combatants only, the muscle behind the boss.

### Boss engagement (bigger reach; talk-or-fight; platform-scoped)

7. The boss has an **interaction/engagement area** around him — a circle, the same kind an ordinary NPC has,
   but with a **noticeably larger radius** than a normal NPC's.
8. **Crossing into the boss's area engages him**:
   - if the boss **has a quest**, it **opens the dialogue** (from which the player may accept the job, walk
     away, or turn on him — the fight can still start from inside the talk, per the existing dialogue path);
   - if the boss **has no quest**, the **camp fight starts immediately** (no talk).
9. The boss's area is **active only on the platform the boss stands on**. Approaching from a neighbouring
   platform, or being near in screen space but not on his platform, does **not** engage him — engagement is a
   local, on-platform act.

### The camp does not aggro on landing; the crew fights behind the boss

10. **Landing on or standing on a camp platform starts nothing** — no fight, no talk. The **only** starts are
    the boss-engagement rules above (crossing his area, or the dialogue→fight path). This holds for a bandit
    camp and for any **other aggressive-monster** group placed the same way.
11. The **crew is the boss's fight, not independent tripwires**: the accomplices do **not** each carry their
    own fight-starting reach placed **between** the player and the boss. When the boss is engaged into a
    fight, **his crew joins that one fight**. (A lone aggressive monster that is *not* part of a boss-led camp
    keeps the existing approach-aggro behaviour from the Proximity brief — this brief changes the **camp**
    case, where a boss owns the trigger.)

### Determinism & authoring

12. Everything above is **data-authored and deterministic**: **same seed → same camp** (same crew size, same
    boss, same quest-or-not roll, same layout). Adding or resizing a camp, or changing a role colour, is an
    **authoring/config** act, not new code.

## Content authoring rules (for the designer)

- Enemies **reuse the shared humanoid model**; author the enemy's **combat representation to use that same
  model** so the look is identical in the world and in combat (requirement 2). Do not leave enemies on the
  capsule.
- Set role colours as **demo tints**: green for peaceful NPCs, deep maroon for the bandit boss, a lighter
  maroon for the crew (requirement 3). Treat these as placeholders.
- Size the camp crew through the **director/world-content path** (the same one that sizes ambient content) —
  do not hand-place a fixed number (requirement 5).
- Give the boss a quest **or not** by authoring his encounter; the **crew never carries a quest**
  (requirement 6). A boss **with** a quest reads as a talkable `?` quest-bearer; a boss **without** one is the
  camp fight's trigger.
- Give the **boss** the larger engagement radius; do **not** put fight-starting radii on the crew
  (requirements 7, 11).

## Acceptance criteria (demo)

- Every enemy in a bandit camp renders as the **shared humanoid model** — **no capsules** — in the world
  **and** in combat.
- A camp reads at a glance: **green** villagers, a **deep-maroon boss**, **lighter-maroon crew**.
- **Jumping onto a camp platform starts no fight.** The player can move around the platform freely until they
  cross the boss's area.
- The boss's area is **visibly larger** than an ordinary NPC's; **crossing it** either **opens his dialogue**
  (boss has a job) or **starts the camp fight** (boss has none). The **crew joins that fight**; no crew member
  starts a separate fight in front of the boss.
- Approaching the boss's platform **from an adjacent platform does not engage him** — the area only bites on
  his own platform.
- The camp's crew **count and the boss's quest-or-not are director/data driven** and **stable per seed**.

## Out of scope / open points (do not build now)

- **Final per-faction art / real bandit models & palettes** — the colours here are **demo tints** over the
  shared model; the production look is a later art pass.
- **The shady-offer content itself** (dark-currency reward, cauldron tempter, moral fork) — owned by
  [Camp Shady Offer](camp-shady-offer.md); this brief only puts the **boss + crew + engagement** on the
  ground so that offer has a face. When the boss carries a quest, that quest's *content* is that brief's job.
- **What the fight feels like once it starts** — initiative, the turn-order queue, facing input, ability
  animation, and the move arrow are the **other two briefs** of this initiative
  ([Combat Initiative & Turn-Order Queue](combat-initiative-and-turn-queue.md),
  [Ability Animation & Enemy Action Read](combat-ability-animation.md)).
- **Per-NPC radius tuning surface** beyond "boss bigger than normal" — a general per-archetype radius
  override is the existing deferred ROADMAP item; this brief needs only the boss's larger reach.
- **Non-camp aggressive monsters' trigger model** is unchanged from the Proximity brief — this brief only
  fixes the **boss-led camp** case (the boss owns the trigger; the crew doesn't).
- **The engine "how"** — how the director reserves the camp cluster and lays out boss-fronts-crew — the code
  track's call.
