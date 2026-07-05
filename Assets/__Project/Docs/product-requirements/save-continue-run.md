# Save / Continue a Run + Cross-Run Memory — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Fuller design background — do not restate it: `world/overview.md` §8 (the Hades frame — diegetic
> death, story unfolds across runs) and `vision.md` §7. Current serialization boundary and the
> deferred pieces this brief closes: `Docs/narrative-procedural.md` §2.6 / §6 (R14) and the
> `INarrativeSaveService` snapshot. This is the roadmap item **P2-2** — the last of the three P2
> long poles. It **unblocks P3-3** (cross-run meta consumers: mirror-lore, cauldron memory, spine
> cursor) and the mirror-lore payoff.
> **Scope note:** the run/meta **fact partition** already landed in **P2-3**
> (`director-threads-and-continuity.md`, D20). This brief adds the **file IO**, the **whole-run
> continue image**, and the **cross-run meta store**. The long-arc **reading consumers** of the meta
> facts stay **P3-3**.

## Goal

Make a run **survivable across a session boundary** and make the world **remember across deaths** —
the two halves of the Hades frame (`world/overview.md` §8).

Today the run lives entirely in memory: quit the game mid-run and the run is gone; die and nothing
of the run's world persisted for the next attempt. This brief adds a single, always-on **continue**
so a player can **close the game mid-run and pick up exactly where they left off**, and a separate
**cross-run memory** so the world **remembers what happened in past runs** even though the run itself
(the mortal beast you built) is lost on death.

There is **no manual save/load and no save-scumming** — death is real and meaningful (`vision.md`
§4), so the in-progress save is an autosave that a death **consumes**, and the persistent layer is a
separate, always-on world memory.

## User stories

- As a player, I can **quit the game in the middle of a run and come back later to exactly the same
  run** — the same world stretched out around me, the same creature I had built, the same quests and
  storylines in progress, standing where I left off. It resumes; I don't restart.
- As a player, when I **die**, that run is **over** — I don't get to reload an earlier moment of it to
  undo the death. I come back the Hades way (reformed at the hub), starting a fresh run.
- As a player, across my deaths the **world remembers** — the things that carry between runs (who I
  am becoming, what I've done) persist, so the world of a later run isn't a blank slate.
- As a player, if I quit **in the middle of a fight or a conversation**, coming back drops me at the
  **clean start of that spot** — the encounter simply begins again — rather than mid-swing in a
  half-finished action.
- As a player, a **crash or a corrupted save never destroys my long-term progress** — at worst I lose
  the one in-progress run, never the world's accumulated memory.

## Functional requirements

### A. The continue model (autosave, one implicit save, permadeath)

1. **One implicit in-progress save, no slots.** The run auto-persists; there is **no manual save
   action and no multiple save slots**. Relaunching the game with an in-progress run present resumes
   it (a **Continue** entry point); no in-progress run present starts fresh.
2. **Death consumes the in-progress save.** When the run ends by death, the in-progress run save is
   **cleared** (the next launch starts a fresh run) — there is **no way to reload an earlier point of
   a dead run**. Save-scumming a death is impossible by construction.
3. **Autosave is invisible and automatic.** The player never triggers it and never waits on a save
   menu; it happens on its own at the savepoint boundaries below (C).

### B. What "continue" restores — the whole-run image

4. **Continue restores the entire run as one image**, so the resumed run is indistinguishable from
   the one the player left. At a product level it must bring back **everything the player would notice
   was lost**:
   - **The world around me** — the stretch already generated behind me **and the same beats coming up
     ahead** (the same upcoming quests, monsters, sites, biome stretch), so re-entering does **not**
     re-roll a different world.
   - **Where I am** — the hero resumes at the **platform** they were on (per C, at that platform's
     clean start).
   - **The creature I built** — my installed body parts, my current **body-plan / frame**, and my
     in-progress crafting (the blank rack and any part being socketed).
   - **My stuff** — my artifacts, blanks, currency, and my **quest log** (each quest at its exact
     stage).
   - **The story state** — all in-run **facts**, all live **threads** and their stages, the NPCs I've
     met with their **accumulated per-actor state** (so a recurring character still remembers me), and
     the run's **procedural seed + generator position** so every future roll and every future world
     beat plays out **identically** to the un-interrupted run.
5. **One coherent restore, not per-system fragments the player must reconcile.** Continue is felt as
   a single "resume", even though several systems each contribute their slice — the player never sees
   a partially-restored run (body back but world regenerated, or world back but quests reset).

### C. Savepoint boundaries (platform-clean, no mid-action state)

6. **Savepoints are platform boundaries.** The run persists at **clean platform transitions**, not in
   the middle of an action. A mid-encounter moment (an ongoing fight, an open conversation) is **not**
   a savepoint.
7. **Quitting mid-encounter resumes at the platform's clean start.** If the player quits during a
   fight or a dialogue, Continue drops them at the **start of the platform they were resolving**, and
   that encounter **begins again** — the transient state of the half-finished fight/conversation is
   **not** preserved. (This matches the already-shipped rule that a suspended dialogue is a
   non-savepoint, W3-1.)
8. **No lost progress before the current platform.** Everything the player completed on **earlier**
   platforms (facts written, quests advanced, parts installed, loot taken) is fully preserved — only
   the **current** in-progress encounter re-begins.

### D. Cross-run memory (the meta layer)

9. **A separate, always-on world memory that survives death.** Distinct from the in-progress run save
   (which a death consumes, A2), the **cross-run memory persists across every death and every fresh
   run**. It carries the facts marked to persist (the run/meta fact partition already drawn in P2-3):
   the world's long-term memory of who the player is becoming and what they've done.
10. **It is the store + the persistence, not the consumers.** This brief builds the cross-run memory
    **store and its file persistence**, and persists the **meta-scoped facts** into it as its **first
    working client**. The **curated content that reads that memory** — the mirror-antagonist echoes,
    the cauldron's recollections, the reveal-spine cursor — stays **P3-3** and is **out of scope
    here**.
11. **Prove it end-to-end with one demo persistent fact.** Ship **at least one demo meta fact** whose
    persistence is **observable in a following run** (a fact set in run 1 is present and readable in
    the store of run 2) — a minimal proof that "the world remembered", not the full curated
    mirror-lore content (that is P3-3). This makes the meta layer a demonstrable feature, not just
    untested plumbing.

### E. Robustness (determinism + fail-safe)

12. **Deterministic resume.** After Continue, all subsequent procedural generation (world beats, loot
    rolls, casting, thread lifecycle) plays out **identically** to the un-interrupted run — the resume
    is seed- and state-faithful, not an approximation. Same run + same player actions after resume ⇒
    the same outcomes.
13. **A corrupt or missing save never crashes and never destroys the meta layer.** The in-progress run
    save and the cross-run memory are **independently recoverable**: a corrupt/unreadable **in-progress
    run save** is discarded (the player starts a fresh run) **without touching the world memory**; a
    corrupt/unreadable **cross-run memory** degrades to **empty world-memory** (a blank-slate world,
    as if a first-ever run) rather than crashing. At worst the player loses the one in-progress run,
    **never** the accumulated cross-run progress.
14. **Version-tolerant, or fail safe.** A save written by an older build either loads, or is treated as
    the corrupt case above (discard-and-continue) — a stale save must **never** hard-crash the game.

## Content authoring rules (for the designer)

- **Marking a fact as persistent is already data** (P2-3): set a `FactKeyDefinition`'s horizon to
  **Meta** and it is carried by the cross-run memory; leave it **Run** (the default) and it resets on
  death. No code, no new authoring surface — this brief just makes the Meta side actually persist to
  disk.
- **The demo persistent fact (FR11)** is authored the same way: one Meta-horizon fact, set in-fiction
  during a run, observably present in the next run.
- **Nothing else in this brief is authored content** — the continue image and the file persistence are
  engine behavior over the state the existing systems already own.

## Acceptance criteria

- Quitting the game mid-run and relaunching offers **Continue**, and Continue restores the **same
  world ahead, hero body, inventory, quest log, facts, threads, met-actors, and seed/generator
  position** — the resumed run is indistinguishable from the un-interrupted one (FR4/FR5/FR12).
- **Death clears the in-progress run save**: after a death the next launch **cannot** reload the dead
  run at any earlier point (FR2) — save-scumming is impossible.
- Quitting **mid-fight or mid-dialogue** resumes at the **clean start of that platform**, the
  encounter beginning again; all **earlier** platform progress is intact (FR7/FR8).
- A fact marked **Meta** and set in run 1 is **present and readable** in run 2's store; a fact marked
  **Run** is **not** (FR9/FR11) — demonstrated by the one shipped demo meta fact.
- Corrupting/deleting the **in-progress run save** → the game starts a **fresh run** with the
  **cross-run memory intact**; corrupting/deleting the **cross-run memory** → the game runs with
  **empty world-memory**; **neither crashes** (FR13).
- A save from an older build **never hard-crashes** the game (FR14).

## Out of scope / open points (do not build now)

- **The cross-run meta *consumers*** — mirror-antagonist lore echoes, cauldron memory barks, the
  reveal-spine cursor reading meta facts — are **P3-3** (D20 read side). This brief only builds the
  **store + persistence** and proves it with one demo fact.
- **Manual save slots / save-anywhere / load-an-earlier-point** — deliberately excluded (A1/A2);
  permadeath and a single autosaved continue are the model.
- **Mid-encounter resume fidelity** — serializing an in-progress fight or an open conversation to
  resume mid-action is excluded (C); savepoints are platform-clean and the encounter re-begins.
- **The hub / meta-progression *systems*** that would spend cross-run memory (hub cast reactions,
  unlocks, the dig meta-bias) — those are their own design/roadmap items; this brief only guarantees
  the memory **persists and is readable**, not what consumes it.
- **The Arena part-catalog** (P4-5) is also meta-persistent and will ride the same cross-run memory
  later; its content and Arena wiring are **not** built here.
- **A save/continue UI beyond a single Continue entry point** — no save menu, slot browser, or
  in-run save indicator is specified; the autosave is invisible (A3).
- **Cloud / cross-device sync** — local persistence only.
```