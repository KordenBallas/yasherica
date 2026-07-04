# Director — First-Class Threads, Cross-Window Continuity & Fact Horizon — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-04
> Level: product-owner (what & feel). The code track owns the technical "how".
> Fuller design background — do not restate it: `design/narrative/director.md` (R8, D10–D14, D20,
> D21) and `vision.md` Pillar 2 (§2) + Storytelling Stance (§6). Current engine state and the
> continuity limitation: `Docs/narrative-procedural.md` §2.6 and the NPC-interaction thread-ordering
> note. This is the roadmap item **P2-3**.
> **Scope guard:** this brief is *only* R8 threads + cross-window continuity + the run/meta fact-scope
> boundary. The **spine reserved lane / reveal cap** (D7 = P3-1), **escalation tier gating** (D19 =
> P3-2), and the **cross-run meta-store + save-file IO and its long-arc consumers** (P2-2 / P3-3)
> are separate items — see out-of-scope.

## Goal

Make the run read as **one growing saga**, not scattered noise, and make its threads place **in causal
order** across the streamed world. Today a "thread" is a bare string label with only a within-window
grouping preference: the planner runs **one window ahead**, so a freshly written fact can arrive too
late and the planner may **re-place a story that was already offered or already closed**, or place a
beat **before** the beat that causes it. And when the player's choices make a thread's premise
**impossible** (you joined the Lizard clergy, so "join the Fox order" can never resolve), nothing
retires it.

This brief turns a thread into a **first-class thing the director manages** — with a lifecycle, a
closure/expiry rule, and a concurrency ceiling — and makes coupled beats land in the right order. It
also draws the **run-vs-meta fact boundary** now, so the state the next item (save/load, P2-2)
persists is already cleanly partitioned.

This does **not** add authored links between stories (coupling stays emergent through facts + shared
actors — Pillar 2), and it does **not** grow the streaming window.

## User stories

- As a player, the stories I engage with **feel connected into a few ongoing storylines**, and beats
  arrive **in the order that makes sense** — the raider shows up *after* the barn is robbed, never
  before; a quest I was already offered is **never re-offered** as if fresh.
- As a player, when my choices make a storyline **impossible** — I joined the Lizards, so I can never
  join the Foxes; I killed the Ibex king I was meant to befriend — that storyline **ends as failed**,
  because I closed the door, not at random.
- As a player, a minor errand I never picked up **quietly lapses** (marked failed/expired) instead of
  hanging forever — while a **long, important storyline persists** across the whole run and isn't
  dropped just because I was slow to get to it.
- As a player, the world keeps only a **handful of live storylines at once**, so the run stays legible
  rather than a scatter of half-open threads.
- As a designer, I declare a thread's **kind** (ordinary/ephemeral vs long/arc), the **facts whose
  contradiction ends it**, and (for ephemeral) how long it may linger — all as **data, no code**. I
  also mark whether a fact is **run-scoped** (resets on death) or **meta-scoped** (persists).

## Functional requirements

### Threads are first-class
1. A **thread is a managed entity** with an **id** and a **state** (live / resolved / failed-or-expired),
   not just a label. The director tracks it across windows; a story beat belongs to a thread, and the
   director advances a thread's **stage** as its beats resolve.
2. **Two thread kinds.** *Ephemeral* threads are the default and are under **closure pressure**;
   *arc* threads (a long storyline — a race's induction arc, the mirror-antagonist spine) may live most
   of a run and are **exempt from expiry** (but not from conflict — FR5). The kind is **authored data**.

### Retiring a thread — two distinct triggers
3. **Prefer advancing over opening.** When choosing what to place, the director **advances a live
   thread** in preference to opening a new one, so threads reach a payoff instead of accumulating.
4. **Expiry (timeout) — ephemeral only, silent.** An ephemeral thread that **does not advance within its
   authored lifespan** (measured in the director's own units — windows/platforms of no progress) is
   **retired as failed/expired**: no closing story beat, just a **state change + an indicator fact** the
   quest-log / thread readout can show later. Arc threads never expire this way.
5. **Conflict — either kind, causal.** A thread whose **premise facts are contradicted** by a written
   fact is **retired as failed** — because the player made it **impossible** (joined the rival order;
   killed the person to be befriended). This applies to **arc threads too**. Like expiry, it produces a
   **state change + a fail indicator, no closing beat**. This is the same mechanism as a **mutually-
   exclusive offer** (choosing offer A writes the fact that fails offer B's thread).
6. **No closure-beat content in scope.** Retirement (expiry or conflict) is a **state transition + an
   indicator only** — the director does not place a special "the raider gives up" storylet. (A future
   optional closure-beat pass may add one; not here.)

### Concurrency
7. A **small ceiling on simultaneously-live threads** (ephemeral + arc together), **authored as a tuning
   value**. At the ceiling the director **does not open a new thread** — it advances or lets existing
   ones resolve/expire to free room (never force-drops an engaged thread to make space; it waits).

### Cross-window continuity (the fragile bit)
8. **Coupled beats land in causal order.** When two beats are coupled on a thread (cause then
   consequence), the consequence beat is **only placed once its prerequisite fact is live**, and it is
   placed **after** the cause — never before, never in the same breath ahead of it. A beat whose
   prerequisite is not yet true is **held**, not placed early.
9. **No stale re-placement.** A story that was **already offered, already resolved, or already failed**
   is **never re-placed** as if new — the look-ahead plans against the **current** thread/quest state,
   not stale facts. (If holding a beat leaves a window thinner, that is acceptable — **correctness over
   density**; the gap reads as deliberate breath, per D3.)

### Run vs meta fact boundary (draw it now)
10. **Every fact carries a scope: run-scoped or meta-scoped.** Run-scoped facts **reset on death**;
    meta-scoped facts **persist across runs** (the long arcs — spine cursor, mirror-lore flags, cauldron
    memory — are meta). The scope is **authored data** on the fact/effect.
11. **The director keeps the two horizons cleanly separated** so that state handed to save/load is
    already partitioned. **Persisting** meta facts across runs (the cross-run store + save-file IO) and
    the **long-arc consumers** that read them are **not** built here — this requirement only draws and
    enforces the boundary so P2-2 persists the right partition.

### Robustness
12. **Deterministic & replayable.** Thread selection, retirement, ordering, and the scope split stay
    **seeded and replayable** — no unseeded randomness (D21). The same seed + same player choices yields
    the same thread lifecycle every time.

## Content authoring rules (for the designer)
- Give a thread a **kind** — *ephemeral* (default, can expire) or *arc* (long, expiry-exempt).
- Declare the thread's **premise facts** — the facts whose contradiction **fails** it (this is how a
  moral-fork / mutual-exclusion closes the road not taken).
- For an ephemeral thread, set its **lifespan** (how long it may linger without advancing before it
  expires). Arc threads omit this.
- Tag each **fact/effect** with its **scope**: *run* (resets on death) or *meta* (persists).
- All of the above is **data** — a new thread, a new premise/exclusion, or a re-scoped fact needs **no
  code**.

## Acceptance criteria
- A coupled pair (cause → consequence) **always** appears with the consequence **after** the cause and
  **only** once the cause's fact is live; the consequence is **never** placed ahead of the cause.
- A story already **offered / resolved / failed** is **never re-placed** as fresh across a window
  boundary.
- Making a thread's premise **impossible** (a contradicting fact) **fails** that thread — **including an
  arc thread** (kill the king → the "befriend the king" arc ends failed) — with a **fail indicator and
  no closing beat**.
- An **ephemeral** thread left un-advanced past its lifespan **expires** (failed/expired indicator, no
  beat); an **arc** thread in the same situation **does not** expire.
- With the concurrency ceiling reached, the director **opens no new thread** and instead advances/lets
  existing ones close.
- Facts marked **run-scoped reset on death**; facts marked **meta-scoped survive** — and the state is
  **partitioned** so save/load can persist each horizon separately.
- Same seed + same choices ⇒ **identical** thread lifecycle and ordering (deterministic replay).

## Out of scope / open points (do not build now)
- **Spine reserved lane + per-run reveal cap** (D7) — a separate director item (**P3-1**); this brief
  manages threads generally, not the curated reveal spine's quota.
- **Escalation tier gating** (D19) — a separate item (**P3-2**); the tier fact is already published, its
  consumers are the escalation thread.
- **Cross-run meta persistence + save-file IO** and the **long-arc consumers** that read meta facts
  (mirror-lore echoes, cauldron memory, spine cursor) — this brief only **draws the run/meta boundary**;
  the **store + file IO** is **P2-2** (save/load) and the reading consumers ride **P3-3** (D20).
- **A player-facing thread readout / saga view** — presentation, sits beside the quest-log UI
  (**P1-11**); this brief's "player can read the thread" is satisfied by state + indicators the UI later
  surfaces, not a new screen here.
- **Closure-beat content** (an authored "the thread winds down" storylet on retirement) — deliberately
  excluded (FR6); retirement is a state change + indicator. A future optional pass may add closure
  beats.
- **Growing the streaming window** — the small window is a feature (D2/D5); continuity is fixed by
  ordering/holding within it, not by looking further ahead.
