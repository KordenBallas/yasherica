# Director — Spine Reserved Lane & Reveal Cap — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Fuller design background — do not restate it: `design/narrative/lore-pacing.md` (the curated
> reveal spine, reveal tiers, rush-proof) and `design/narrative/director.md` (D6 reveal clock, D7
> reserved lane); `vision.md` Pillar 2 (§2). Current engine — the `_isSpine` + preconditions
> machinery and the windowed planner/budgets: `Docs/narrative-procedural.md` §2.6. This is the
> roadmap item **P3-1**.
> **Scope guard:** this brief is *only* the placement **mechanism** — the reserved lane, the per-run
> reveal cap, and eligible-pool selection. The **cross-run persistence of what's been revealed**
> (the spine cursor as a meta fact), the **mirror-lore echoes**, and the **cauldron memory of past
> hosts** are the reading side — **P3-3** (see out-of-scope). Escalation tier gating (D19) is
> **P3-2**; the meta store + file IO is **P2-2** (shipped).

## Goal

Deliver the game's **curated reveal spine** at the right tempo: the big secrets — the antagonist is a
**mirror** of the hero, the cauldron's seduce→tyranny→abandonment **cycle** (`world/overview.md` §3,
§7) — assemble across runs **like a puzzle**, arriving **never too early**, at a **controlled rate**,
and **whether the run is won or lost** (the Hades frame — death is fuel).

Today spine beats have no reserved placement and no reveal cap: they compete in the same narrative
density (weight) budget as ordinary content, so the "≤1–2 per run, never-too-early" promise of
`lore-pacing.md` cannot be kept. This brief gives the director a **reserved lane** that places spine
beats **first** (outside the density weight budget), throttled by a **per-run cap**, drawing from a
**pool of precondition-gated beats** — not a forced linear sequence.

This does **not** author the reveal content itself (the lines/storylets), and it does **not** make the
spine persist across runs — that is P3-3.

## User stories

- As a player, across many runs the biggest truths — *he was once like me*, what the cauldron really
  does — **assemble like a puzzle**; they **do not dump on me in the first hour**, and the heaviest
  reveals only come once I've gone deep — but not on a rigid script I could predict.
- As a player, even a **losing** run still hands me a piece of the mystery, so the next run is always
  worth starting.
- As a player, I'm **never flooded with lore** — at most one or two reveals land in a run, so each one
  actually registers.
- As a designer, I author each reveal-beat with its **preconditions** and (for the heavy ones) a
  **soft floor** ("not before run N / not before you've seen Y"), mark it as a **spine** beat, and the
  director schedules it for me — I do **not** hand-place it or author a linear order.

## Functional requirements

### Reserved lane (place first, don't compete)
1. **Spine beats are placed first, by their own quota**, before the narrative density (weight) budget
   fills the rest of the window — they **never** compete for that weight budget (D9). The reservation
   mirrors the combat minimum: a window heavy with ordinary content still yields its spine beat.
2. **Per-run reveal cap.** At most an **authored** number of spine beats (a tuning value, default
   **1–2**) are revealed **per run**. Once the cap is reached, no further spine beats place this run;
   ordinary content continues to fill windows normally.

### Eligible-pool selection — a gated pool, NOT an ordered queue
3. **The spine is a pool of gated beats, not a linear sequence.** A beat becomes **eligible** when its
   **preconditions** hold **and** its **soft floor** is satisfied **and** it has not already been
   revealed. The reserved lane draws from the **eligible** set, up to the cap.
4. **No forced linear order.** The picture **assembles as a puzzle** — which beat lands depends on the
   player's path, not an authored index. When **several** beats are eligible at once, the pick is
   **seeded/deterministic** (a stable tie-break), never unseeded random (D21).
5. **"Never too early" is enforced per-beat, not by sequence.** The anti-too-early guarantee comes
   from each **heavy** reveal carrying **deep preconditions + a soft floor** (a mastery milestone
   and/or "not before run N"), so key reveals **cannot** surface at the start — while **not** being
   forced to the very end. The reveal **tiers** of `lore-pacing.md` (Tier 0…3) are expressed as
   **per-beat floors**, not a global cursor.

### Decoupled from win/lose
6. **The lane fires regardless of run outcome.** A spine beat may advance on **both won and lost**
   runs — progress on the mystery is guaranteed each run that eligibility + the cap allow (the Hades
   frame, `world/overview.md` §8). Winning is not a prerequisite for a reveal.

### Gating inputs (reuse facts — no new subsystem)
7. **Gates are ordinary facts.** A mastery milestone is a **plain precondition fact** (reached biome
   X, hit mutation stage N, completed a race questline, leaned Conquest/Alliance…). A soft floor "not
   before run N" reads a **meta-scoped run-counter fact**. There is **no new "milestone" subsystem** —
   the reserved lane reuses the existing precondition machinery plus a run-count meta fact (KISS).
8. **Never re-reveal a beat.** The lane must not place a beat already shown. **Within a run** the
   already-revealed set is tracked here; the **cross-run persistence** of that set (the spine cursor as
   a meta fact) is **P3-3** — this brief **consumes** an already-revealed set and honours it, P3-3
   makes it survive death.

### Robustness
9. **Deterministic & replayable.** Reserved-lane placement, the cap, and eligible-pool tie-breaks stay
   **seeded and replayable** (D21) — same seed + same player path ⇒ the same reveals in the same
   places.

## Content authoring rules (for the designer)
- Mark a reveal-beat as **spine** (rides the existing `_isSpine` flag).
- Give it **preconditions** (the facts that make it eligible) and, for a heavy reveal, a **soft
  floor** — a minimum run count (via the run-counter meta fact) and/or a "must have seen Y" fact — so
  it can't land too early.
- **Do not author a linear order.** There is no sequence index; each beat gates itself. The puzzle
  assembles in whatever order the player's path unlocks.
- Set the **per-run reveal cap** as a tuning value (default 1–2).
- All of the above is **data** — a new reveal-beat, a new gate, or a re-tuned cap needs **no code**.

## Acceptance criteria
- Spine beats are placed **before** the density budget and are **never** counted against it — a window
  full of ordinary content still yields its spine beat if one is eligible.
- **At most** the authored number (default 1–2) of spine beats appear **per run**.
- A beat appears **only** when its preconditions **and** soft floor hold **and** it hasn't been
  revealed; a **heavy** reveal (the mirror) **cannot** appear in an early/low-floor run.
- Both a **won** and a **lost** run can advance the spine (given eligibility + remaining cap).
- With **several** eligible beats, selection is **deterministic** (seeded); same seed + same path ⇒
  identical reveals. No beat is revealed **twice**.

## Out of scope / open points (do not build now)
- **Cross-run persistence of the revealed set** (the spine cursor as a meta fact), **mirror-lore
  echoes**, and **cauldron memory of past hosts** — the **reading** side, **P3-3**. This brief only
  operates the lane within a run and honours an already-revealed set.
- **The reveal-beat content itself** — the actual lines/storylets/asides are authored alongside the
  spine content, not by this mechanism.
- **Escalation tier gating** (D19) — **P3-2**; a separate pool filter on `run_escalation_tier`.
- **The meta store + file IO** (persisting the run-counter and revealed set) — **P2-2** (shipped);
  this brief consumes those facts, it does not build the store.
- **Achievements-as-lore-fragments** — parked (2026-06-20).
- **Exact cap value (1 vs 2) and the concrete milestone→beat mapping** — production tuning, authored
  with the spine content.
