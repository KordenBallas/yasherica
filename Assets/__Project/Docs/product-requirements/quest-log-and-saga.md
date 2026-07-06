# Quest Log & Saga Readout

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Design background: `design/narrative/quest-as-reward.md` (reward stays hidden), the first-class
> threads of `director-threads-and-continuity.md` (P2-3, which deferred a player-facing saga view to
> here). Covers ROADMAP **P1-11** (quest log UI) — **and** the thread/saga readout the director
> deferred. Reads the shipped progression record; reuses the hidden-reward rule from
> `quest-offer-card.md`.

## Goal

Give the player a place to **see what they've taken on and what it became** — a **quest log** of
**active / completed / failed** quests with their objectives, and, because a run is meant to read as
**one growing saga**, a **thread/saga readout** grouping quests by the actor/thread they belong to.
It is a **read-only window** onto state that already exists (the progression record + first-class
threads); it changes nothing about how quests or threads work.

## User stories

- As a player, I can open a **quest log** and see my **active** quests with their **objectives** (what
  I still have to do and for whom).
- As a player, I can see which quests I've **completed** and which have **failed / expired**, so the
  run's history is legible.
- As a player, the log groups related quests into **threads/sagas** (the recurring-actor arcs), so I
  read the run as **one growing story**, not a flat to-do list — and I can see a thread that **failed
  because I made its premise impossible** (I joined the other side).
- As a player, the log tells me the **job** (objective / who / why) but **never the exact reward** —
  the reward stays the promised **tier + belonging** mystery, exactly as on the offer card.

## Functional requirements

### The quest log
1. A **quest log** view lists quests in three states: **Active**, **Completed**, **Failed/Expired**.
2. Each **active** quest shows its **objective(s)**, giver/actor, and a short summary — enough to know
   what to do next and why. (Longer detail on demand, terse at a glance — same discipline as the offer
   card.)
3. **Completed** and **Failed/Expired** entries stay in the log as the run's **history**; a failed
   entry reads as **failed/expired** (indicator only — the P2-3 thread fail/expire is a state, not a
   closing beat).

### Reward stays hidden
4. The log shows the reward exactly as the offer promised — **tier (glow) + belonging (colour), exact
   item hidden**. It **never** names the rolled artifact/blank (`quest-offer-card.md` rule holds
   inside the log too). For a **completed** quest the received item is of course already in the
   inventory; the log entry itself does not become a reward catalog.

### The thread / saga readout
5. The log groups quests by their **thread** (the first-class thread entity, P2-3): a thread with its
   member quests reads as **one arc/saga**, in the order they occurred.
6. A thread shows its **state** — live / completed / **failed-by-conflict** / **expired** — so a moral
   fork the player resolved (and the opposing thread it foreclosed) is legible as **saga**, not a
   dangling entry.
7. The readout is **read-only** — it surfaces thread state, it does not let the player advance, drop,
   or edit threads.

### Read-only window
8. The whole view **derives from existing state** (progression record + threads) and **mutates
   nothing**. Opening/closing it has no gameplay effect.

## Content authoring rules (for the designer)
- The log reads what a quest **already declares** — title, summary, objective(s), giver, reward
  **tier + belonging** — and what a thread already tracks. **No per-quest log authoring** and no new
  reward field.
- Keep quest **summaries/objectives terse**; the log inherits the same "terse at a glance, detail on
  demand" discipline as the encounter card.

## Acceptance criteria
- Opening the log shows **Active / Completed / Failed-Expired** sections; active quests list their
  **objectives + giver**.
- A quest that **failed by fact-conflict or expired** appears in the **Failed/Expired** section with a
  failed/expired indicator (no fabricated closing text).
- Quests belonging to the **same thread** appear **grouped as one saga** with the thread's **state**
  shown; a resolved moral fork shows both the taken arc and the **foreclosed** opposing thread.
- The log shows reward **tier + belonging** but **never the exact item** — consistent with the offer
  card; the view **mutates no state**.

## Out of scope / open points (do not build now)
- **The exact UI layout / art** (panel, icons, animation, where it lives on screen) — presentation /
  render-look; this brief sets content + behaviour.
- **Any change to quests or threads** — read-only; thread lifecycle stays P2-3.
- **Non-item reward display** (Currency / XP / Ability) — deferred with P1-12.
- **A meta / cross-run saga history** (past runs) — separate; this log is the **current run**. Cross-
  run memory is P3-3 (`director-meta-consumers.md`).
- **Filtering / sorting / search** beyond the three-state grouping + thread grouping — deferred.
