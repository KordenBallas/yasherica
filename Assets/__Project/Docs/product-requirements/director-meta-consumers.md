# Director — Cross-Run Meta Consumers (Spine Cursor · Mirror-Lore Echoes · Cauldron Memory) — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Fuller design background — do not restate it: `design/world/overview.md` §7/§11 (the mirror
> antagonist, "your choices echo in the tyrant's lore"), `design/narrative/cauldron-voice.md`
> (memory of past hosts as gated reveal beats), `design/narrative/lore-pacing.md`, and
> `design/narrative/director.md` D20 (two fact horizons). Current engine — the run/meta partition
> (P2-3) in `Docs/narrative-procedural.md` and the meta **store + file IO** shipped in
> `Docs/save-persistence.md` (**P2-2**). This is the roadmap item **P3-3**.
> **Scope guard:** this brief is the **reading** side — the consumers that turn **persisted meta
> facts** into in-fiction reveals. The run/meta **partition** (P2-3) and the meta **store + file IO**
> (P2-2) already shipped; the **reserved lane + reveal cap** that these reveals ride is the sibling
> **P3-1**. **No new delivery channel** — reveals ride P3-1's spine lane and existing in-fiction
> surfaces (dialogue / lore drops), **not** the real-time tempter-bark channel (**P1-10**, Track H).

## Goal

Make the world **remember across runs, in the fiction** — the three long-arc payoffs of the Hades
frame that a single run can't deliver. The meta horizon now **persists** (P2-2 shipped `meta.json`),
but **nothing reads it** to produce a reveal. This brief adds the three **consumers**:

1. **The spine cursor** — the set of reveals that already happened — **persists across runs** (meta),
   so the spine **advances** from run to run and a reveal is **never repeated**, even after death.
2. **Mirror-lore echoes** — a **bounded, authored** set of tyrant-lore lines **gated on your
   persisted key choices**, so the antagonist reflects **what you've done across runs** — a mirror,
   not a fixed script, and **not** bespoke per-choice authoring.
3. **Cauldron memory of past hosts** — the voice's knowing asides about the tyrant and past hosts, as
   **meta-gated reveal-beats** that deepen over runs and stay on the lore-spine schedule.

This does **not** build the store or the partition (P2-2 / P2-3), and it does **not** build the
reserved-lane mechanism (P3-1) — it is the content + reading logic that rides them.

## User stories

- As a player, the mystery I've **assembled stays assembled** between runs — a reveal I earned three
  runs ago doesn't come back as if new, and the spine **keeps advancing** from where it was, even
  across deaths.
- As a player, the tyrant feels like a **mirror of my own history** — his lore **echoes the path I've
  leaned** (conquest vs alliance) and the deeds I've done — not a script everyone hears, and not an
  obviously bespoke line per single choice.
- As a player, the cauldron drops **knowing asides about past hosts** (including the tyrant) that
  **deepen over runs** and are timed so they never dump early.
- As a designer, I mark **which key choices persist**, author a **bounded table** of echo lines each
  gated on a persisted flag, and author cauldron-memory asides as **spine reveal-beats** — all as
  data, delivered through the existing spine lane, with **no new channel**.

## Functional requirements

### The spine cursor as a meta consumer
1. **The set of already-revealed spine beats persists across runs** as a **meta fact** (rides the
   shipped P2-2 store). P3-1's reserved lane reads it, so a revealed beat is **never re-revealed** and
   the spine **advances** run over run.
2. **Death does not reset it** (the meta horizon, D20). A fresh run **continues** the assembled
   puzzle; a **losing** run's reveal still sticks (decoupled from win/lose, consistent with P3-1).

### Mirror-lore echoes (bounded, curated — never bespoke)
3. **A bounded, authored set of tyrant-lore line variants**, each **gated on a persisted meta flag**
   (a path lean, a landmark deed). This is **curated, not generated**, and **not** per-choice bespoke
   authoring (the KCD-budget anti-pattern, `world/overview.md` §11, `vision.md` §5) — a handful of
   variants keyed to meta facts, so a conquest-leaning history yields different tyrant lore than an
   alliance-leaning one.
4. **Persisted key choices.** A **small** set of key run-choices persist as **meta flags** — the
   **path lean** (Conquest/Alliance) and **landmark deeds** (e.g. killed the king, joined the
   Lizards) — so the echoes can read them. **Which** choices persist is **authored data** (which
   effects are meta-scoped, via the P2-3 partition), not hardcoded.
5. **Delivered in-fiction only** — as gated reveal-beats through **P3-1's spine lane** and existing
   lore/dialogue surfaces. **No codex / meta puzzle-board UI** (decision 2026-06-20); the puzzle
   assembles in the player's head.

### Cauldron memory of past hosts
6. **The voice's "past hosts / the tyrant" asides are meta-gated reveal-beats** — authored lines
   gated on **meta facts + a soft floor**, delivered via **P3-1's reserved lane** and therefore
   **capped** (≤1–2/run) and rush-proof. They **deepen across runs** as more gates open. **Not** the
   real-time reactive tempter-bark channel (that is **P1-10**, Track H — a separate delivery surface).
7. **Mystery preserved.** The voice **never states what the cauldron is** — the Tier-0 hook
   (`lore-pacing.md`) stays intact; these asides reveal the **antagonist/cycle**, not the cauldron's
   own origin.

### Robustness
8. **Deterministic & graceful.** Consumer selection stays **seeded/replayable** (D21). A **missing or
   empty** meta store degrades **gracefully** — no memory ⇒ no echoes/asides, and the run still runs
   normally (consistent with P2-2's independent-recovery fail-safe).

## Content authoring rules (for the designer)
- **Mark the key choices that must persist** as **meta-scoped** effects (the P2-3 partition) — path
  lean, landmark deeds — so the echoes have something to read.
- **Author mirror-lore echoes as a bounded table**: each line variant + the **meta fact** that gates
  it. Keep the set small and curated; do **not** write a line per raw choice, and do **not** generate.
- **Author cauldron-memory asides as spine reveal-beats** with **meta-fact preconditions** (+ a soft
  floor), so they ride P3-1's lane and inherit its cap/ordering.
- Delivery is the **existing spine lane + in-fiction surfaces** — do **not** introduce a new channel.
- All of the above is **data** — no code.

## Acceptance criteria
- A spine beat **revealed in an earlier run is never re-revealed**; the spine **advances** across runs;
  a **losing** run's reveal persists through the next launch.
- The tyrant's lore lines **vary with persisted meta flags** (a conquest history ⇒ different echoes
  than an alliance history), drawn from a **bounded authored set** — **no** bespoke per-choice line,
  **no** generated text.
- Cauldron-memory asides about past hosts appear **only** when their meta gate + floor hold, **deepen
  across runs**, and stay within the **≤1–2/run** reveal cap.
- With an **empty/absent** meta store, **no** echoes/asides fire and the run **still runs** (graceful
  degrade); a corrupt memory does not crash.
- Same seed + same **cross-run history** ⇒ **identical** echoes and reveals.

## Out of scope / open points (do not build now)
- **The reserved-lane + reveal-cap mechanism** itself — **P3-1** (the sibling brief); this brief only
  supplies the meta-reading consumers and the content that rides the lane.
- **The meta store + file IO + the run/meta partition** — **P2-2** / **P2-3** (shipped); this brief
  reads those facts, it does not build them.
- **Capturing the key choices in the first place** — the data-driven **`record_choice`** capture
  mechanism is **P3-7**; this brief **reads** persisted key-choice facts (marked meta-scoped), it does
  not add the capture verb. If P3-7 hasn't landed, the demo persists choices via whatever effect
  authoring already exists.
- **The real-time reactive tempter-bark channel** (temptation barks, socketing hints) — **P1-10**
  (Track H), a separate delivery surface; the cauldron **memory** here is scheduled reveal-beats, not
  real-time barks.
- **Escalation tier gating** (D19) — **P3-2**.
- **The full reactive line-pool per beat × path lean, and the hub-presence lines** — deferred in
  `cauldron-voice.md` (depends on the hub design).
- **A player-facing codex / puzzle-board** — cut (2026-06-20); reveals stay in-fiction.
- **The exact meta-flag roster and echo-line mapping** — production content, authored with the spine.
