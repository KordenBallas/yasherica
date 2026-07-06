# Arena — Developer Console (HP + Turn Log) — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (observable behavior of a **developer tool**). The code track owns the "how".
> Track: **G — Arena / Multiplayer Polish** (item **G2**).
> Related: `arena-mode-mvp.md` §7–12 (the hidden-simultaneous-commit → simultaneous-resolve round and
> its determinism guarantee — this console is the instrument that makes that model legible);
> ROADMAP dev-tools / logging items (**P6-6 / P6-7**) — this extends that surface for Arena.

## Goal

A **developer-only** overlay in the **Arena** mode showing two things:

1. **Live player HP** for every combatant, and
2. a **turn log** of each round — the **reveal of each player's committed queue** and the
   **step-by-step order in which committed actions resolve**, with **whiff / conflict** annotations.

It is the instrument for eyeballing the **hidden-simultaneous-commit → simultaneous-resolve** model
and confirming that resolution is **deterministic** (`arena-mode-mvp.md` §9–12). It is **dev-only**:
toggled by a key, **hidden/absent in a release build**, and **not player-facing content**.

## User stories (developer)

- As a developer running an Arena match, I **toggle a console** and see **each player's current HP**
  at a glance.
- As a developer, I read a **log of the round** — whose queues were committed and the **exact order
  actions resolved in** — so I can confirm the simultaneous resolve is **deterministic**.
- As a developer, I can see **when a committed action whiffed** or **how a conflict was resolved**, so
  I can debug the conflict-resolution rule.

## Functional requirements

### Console
1. A **dev-only overlay** in the **Arena** mode, **toggled by a key**, **off by default**, and
   **absent/inert in a release build**. **Not** player-facing.
2. Two sections: an **HP readout** and a **turn / resolution log**.

### HP section
3. Lists **every combatant** with their **current HP only** (owner: current HP only — **no** per-round
   delta or damage history). Updates as HP changes.

### Turn / resolution log section  *(owner: both granularities)*
4. **Committed-queue reveal.** Each round, logs the **reveal of each player's committed queue** — who
   queued what, in order.
5. **Step-by-step resolution order.** Logs **each committed action as it resolves**, in the
   **deterministic resolution order**, annotated when an action **WHIFFS** (target moved/gone) or a
   **CONFLICT** is resolved (two units into one hex, mutual blows, an action landing where a unit just
   left).
6. **Running history.** The log is a **running sequence across rounds** (most-recent visible, readable
   back) so a match's whole turn history can be reviewed.

## Acceptance criteria
- Toggling the key **shows/hides the console** in Arena; it is **absent in a release build**; **Journey
  is unaffected**.
- The HP section shows **every player's current HP** and updates on damage/heal.
- Each round the log shows **both** the per-player **committed-queue reveal** **and** the ordered
  **resolution steps**, with **whiffs and conflicts marked**.
- Reading the log, a developer can confirm that **the same seed + the same committed queues produced
  the same resolution order** (`arena-mode-mvp.md` §10).

## Out of scope / deferred
- **Player-facing** combat log / spectator UI — dev-only for now.
- **HP deltas / damage-number history**, DPS, or other stat read-outs.
- **Mutating dev controls** (spawn / kill / set-HP), a **configurable keybind**, and further sections —
  those ride the general dev-tools item (**P6-6**).
- **Persisting** the log to a file.
