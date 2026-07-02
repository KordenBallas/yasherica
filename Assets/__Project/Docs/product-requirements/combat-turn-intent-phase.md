# Combat Turn Structure — Enemy Intent Phase — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-02
> Level: product-owner (what & feel). The code track owns the technical "how".
> Related: `vision.md` Pillar 4 (fast chess — "read the queue, plan, act", Shogun Showdown). This is
> the **structure** prerequisite that `combat-ability-ghost-telegraph.md` (the **presentation**)
> depends on.

## Goal

Today enemies **decide and act instantly on their own turn** — round-robin, no forewarning: on its
turn the AI picks a unit's best action and immediately executes it. So the player can never *read*
what an enemy is about to do, and the ghost-telegraph's "read the enemy" half has nothing to show.

We change the round to a Shogun-style **Plan → Act → Resolve**: enemies **commit and reveal** their
plan, the player plans and acts **seeing** that plan, and each enemy's committed action **fires as
shown**. Reading the board and baiting/dodging telegraphed blows becomes the tactical core (Pillar 4).

This changes **when enemies decide and when their actions resolve**; it does **not** change what any
ability does.

## User stories

- As a player, at the **start of each round I see what every enemy will do** — its committed plan,
  shown above it (and inspectable).
- As a player, I **plan my turn against that** — reposition, dodge the telegraphed blow, or set up a
  trade — knowing the enemy will do exactly what it showed.
- As a player, the enemy's shown action **fires as committed even if I moved out of the way**, so
  dodging and baiting are real tactics.
- As a player, **my actions resolve first**, then the enemies' committed actions land — so I can act
  on what I read.

## Functional requirements

### The round: Plan → Act → Resolve
1. **Plan.** At round start, **every enemy chooses its plan for the round and reveals it** — a
   **committed, locked** intent. All enemies reveal together, up front (not one-at-a-time on separate
   sub-turns).
2. **Act.** The player, **seeing all enemy intents**, builds and executes their own actions; the
   player's actions resolve **during this phase**, against the live board.
3. **Resolve.** The enemies' committed actions **fire as shown**.

### Commitment
4. **Enemy intent is locked.** A revealed enemy action executes as committed — same
   **committed facing** / cells / target-intent (`combat-hero-facing.md`: directional actions fire
   along the unit's facing) — **regardless of how the board changed** during the player's phase. If
   the player dodged and it now hits nothing, **it whiffs** — that is the intended payoff, not a bug.
5. **The player is not locked.** The player acts freely in the Act phase and sees results as they go;
   only **enemy** intent is pre-committed. (The player still queues/executes their own actions as
   today.)

### Visibility & order
6. **Full committed plan is visible.** An enemy reveals its **whole planned action(s) for the round**,
   in order — enough to plan against, not a vague single hint. *(How it's drawn — icons above the
   unit + ghost replay — is the ghost-telegraph brief.)*
7. **Resolution order: player, then enemies.** In a round the **player's committed actions resolve
   before** the enemies' committed actions. *(Initiative/speed-based ordering — some fast enemies
   acting before the player — is a deferred enhancement.)*

### Robustness & determinism
8. **Invalid committed action.** If a committed enemy action cannot execute at resolve time (its
   target is gone, or the enemy was displaced), it **attempts as committed and fizzles** if
   impossible — it does **not** silently re-target to something better. (Exact edge rules are a
   combat-design detail.)
9. **Determinism holds.** Enemy planning is deterministic per seed (same enemies + board → same
   committed plan), preserving the existing reproducibility guarantee.

## Design / AI note (for the code track)
- Enemies must **decide at round start** (up front), not on a separate sub-turn, and the decision is
  **locked and revealed**. The existing action **scoring** stays; only the **timing** (decide up
  front) and **commitment** (lock + reveal) change.
- This reshapes the round-robin, per-player turn flow into a **phase model** (enemy-plan → player-act
  → enemy-resolve). Technical touch-points are the code track's call (turn management + AI decide
  timing).

## Acceptance criteria
- At **round start, every enemy shows a committed, inspectable plan**.
- The player **plans and acts seeing** those plans; **player actions resolve, then enemy committed
  actions fire**.
- An enemy attack **fires at its committed target/cells even if the player moved** — dodging works.
- A committed action whose target is gone at resolve **fizzles**, not re-targets.
- **Same seed → same enemy plans**.

## Out of scope / open points (do not build now)
- **Presentation of intent** — icons above units, ghost replay of the enemy plan — is owned by
  `combat-ability-ghost-telegraph.md`. This brief owns the **structure/timing/commitment**; that one
  owns the **visuals**.
- **Initiative / speed-based resolution order** (fast enemies acting before the player) — deferred;
  player-then-enemies for now.
- **Smarter enemy AI** (better plan choices, aiming) — a separate ROADMAP item; unchanged here.
- **Exact fizzle / edge-case rules** for a committed action gone invalid — combat-design/code detail.
- **No change** to what any individual ability does.
