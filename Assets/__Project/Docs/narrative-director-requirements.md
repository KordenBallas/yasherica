# Narrative Director — Requirements & Task Brief (design handoff)

> **Forward-looking design requirements, NOT an as-implemented system doc.** Handed off from the
> design track for the code track to translate into code requirements and implement. The
> implementing system doc is `narrative-procedural.md` — when this is built, fold the relevant
> as-implemented behavior into that doc (CLAUDE.md §8) and mark items here as done. Requirements
> are written in game-design terms (D1–D21); the active task is the **Priority-1 scope** at the
> bottom. Status: handed off 2026-06-22.

---

## 0. The director's one job

The director **selects which storylets enter the world, where, and at what tempo** — nothing
else. It **never** authors a link between two stories; coupling is emergent, through facts and
shared actors only (no hand-authored "story A → story B"). Everything below is discipline on *how
it selects*, not new authoring power. Engine baseline: the story-first, budgeted, windowed planner
— `narrative-procedural.md` §2.6. These requirements extend it.

## 1. The world is platforms; the director streams a small window

- **D1 — Platform islands.** The world is delivered as fast-paced platform islands the hero hops
  between quickly. Platforms are the generation grammar, not a unit of perception. A platform
  carries at most one narrative object (a story's NPC, a combat antagonist, loot) or is **empty**.
- **D2 — Streaming behind the active window.** The player perceives the active platform plus 1–2
  ahead. The director generates upcoming platforms within this window while the player is busy, so
  generation is hidden. The window is **small by design** — this is what keeps consequence
  immediate (D5), not a thing to grow.
- **D3 — Empties are deliberate breath, not leftover padding.** The 2–4 empty platforms between
  story beats serve three jobs at once: hide generation, let a beat land before the next arrives,
  and give traversal rhythm. The minimum gap between story beats is a **tuned pacing parameter**.
  Empties are the natural home for loot / secrets / traversal challenges so the breath is not dead
  space.

## 2. Two clocks, never one

- **D4 — Separate the consequence clock from the reveal clock.** Two independent throttles; mixing
  them into one budget is forbidden.
- **D5 — Consequence clock (immediate, causal).** A player or world action writes a fact; its
  consequence appears within the **next planned window**, **spatially adjacent**, after a short
  breath (D3). The lag is at most the window depth (a freshly written fact can land on platform
  +2/+3, since +1 is already locked) — and that lag *is* the breath. This delivers immediate,
  readable cause→consequence without authored links.
- **D6 — Reveal clock (slow, curated).** The lore reveal spine runs on a long horizon across many
  runs, gated on mastery milestones + soft floors, rush-proof, decoupled from win/lose. (Detailed
  pacing rules belong to the reveal-spine handoff; this director only enforces the throttle, D7.)

## 3. The spine is a reserved lane

- **D7 — Reserve, don't compete.** Spine reveal-beats are **placed first by their own quota**
  (mirroring the combat minimum), capped at ≤1–2 per run, gated on mastery milestone + soft floor;
  the rest of the window fills around them. They **never** compete for the narrative density budget
  by weight — otherwise the ≤1–2/run, never-too-early promise breaks. Decoupled from win/lose.

## 4. Two budgets, two jobs — weight is tempo, not importance

- **D8 — Combat budget.** A minimum and maximum of combat-bearing stories per window, as a
  **separate budget dimension** — the pacing of fast tactical fights.
- **D9 — Narrative density budget (weight).** Stories fill the window while summed weight stays
  within budget. **`Weight` is pacing cost — how much of a window a story consumes — not difficulty,
  span, or importance.** Importance lives in eligibility and the spine (D7), density lives here.
  Never conflate them.

## 5. The actor is the unit of continuity

- **D10 — Story = one platform; clusters are emergent.** A story occupies a single platform. A
  felt "scene" (e.g. the NPC who lost grain + the raider who took it) is **two storylets coupled by
  a shared actor + facts on one thread**, not a multi-platform authored set-piece. The raider is an
  **actor cast into the barn story's antagonist slot** (by tag, not authored), and his motive is a
  **separate story on that same actor**. **Stories never reference stories — the only join is a
  shared actor + facts** (Variant A).
- **D11 — Recurring actors carry the arc.** Stories are transient; an **actor instance persists**,
  accumulates facts, and the director casts the **same** actor into multiple stories to build a
  character arc. *Example:* the raider antagonist of the barn story is later the subject of his own
  motive story — a desperate father in one run, a shakedown bureaucrat in the next. One scaffold ×
  N motives = N distinct-feeling encounters (divergence and saga for free). When an arc owns a
  story, that actor is a **hard pin** — it overrides the soft tag-preference (P1) the planner uses
  to match archetypes to stories, so the arc cannot be broken by casting a different archetype.
- **D12 — Glue stays adjacent.** When storylets are coupled on a thread, the director places them
  **spatially adjacent**; the breath (D3) comes **after** the cluster, never inside it, so the
  cause→consequence reads.

## 6. Threads make a saga, not noise

- **D13 — Two thread classes.** *Ephemeral* threads (the default) are under closure pressure — the
  director drives them to a payoff or retires them when they go stale. *Arc / spine* threads are
  the exception: they may live a whole run (or most of it) and are exempt from closure pressure.
  - **Player-facing trigger — the attack card.** Picking the encounter's **combat/attack card** (or an
    NPC self-initiating combat) is the in-fiction way the player forces a thread closed: it forecloses
    that actor's future arc (his recurring-actor motive stories, D11) in exchange for corpse-loot on the
    separate combat channel. See `design/narrative/npc-encounter-cards.md` §4, `quest-subsystem.md` §6,
    and `narrative-procedural.md` §6 "Encounter card model".
- **D14 — Concurrency cap.** A small ceiling on simultaneously-live threads (ephemeral + arc
  together) keeps a run readable as one growing saga. The director prefers advancing an open thread
  over opening a new one.

## 7. The world reacts to what you became

- **D15 — The body speaks only in facts.** The director never reads mutations directly. The
  mutation system projects the body into "passport" facts (e.g. "reads as frog-folk"); the director
  reads facts.
- **D16 — Passport gating is faction-scoped.** Wearing a race's marker parts flips that race's
  reading of the hero, which **opens that race's stories/quests** and closes them to an unmarked
  outsider. This requires eligibility to gate on **actor- and faction-scoped facts**, not only
  world/global facts (the current limit; `narrative-procedural.md` §2.6 step 1).
- **D17 — Passing is a lie that can flip.** Acceptance holds only while the hero is believed kin;
  deeper mutation or exposure can flip a faction's facts from trust to horror, changing which
  stories become eligible.

## 8. Divergence, escalation, memory

- **D18 — Commit to opened doors (scarcity is a feature).** A run opens only some racial worlds.
  The director does **not** try to show everything — it deepens the doors the player actually
  opened and leaves the rest dark. Depth in the opened subset over breadth.
- **D19 — Escalation shifts the pool and the register.** The director reads an altitude/tier fact
  and shifts the eligible story pool and tonal register as the run climbs (backwater → courts →
  gods). Density may grow with tier.
- **D20 — Two fact horizons.** *Run-scoped* facts reset on death; *meta-scoped* facts persist
  across runs. Long arcs — the spine cursor, mirror-lore choice flags, the cauldron voice's memory
  — ride the **meta** horizon only. The director reads both.
- **D21 — Deterministic and replayable.** All selection stays seeded and save-replayable; no
  unseeded randomness in the director.

---

## Maps to the engine (current gaps)

- **Have:** story-first windowed planner, two budgets, thread label + same-window coherence
  preference, soft actor preference, seeded replay, streaming entry — `narrative-procedural.md`
  §2.6. **Plus (Priority-1, done 2026-06-22):** actor/faction-scoped eligibility resolved as a
  casting query (D16) and recurring-actor casting via `ILiveActorRegistry` — the planner recasts a
  pinned live actor instead of minting per story (D11).
- **Gaps to close:** ~~actor/faction-scoped eligibility (D16)~~ ✅; ~~recurring-actor casting (D11)~~ ✅;
  spine reserved lane + per-run reveal cap (D7); first-class threads with closure pressure +
  concurrency cap (D13–D14); escalation tier gating (D19); meta-scoped fact horizon (D20).

---

## Priority-1 scope (the active task) — ✅ done 2026-06-22

> **Implemented.** Folded into `narrative-procedural.md` §2.6 (eligibility classification + casting
> query + recurring-actor recast) and §2.2 (`ILiveActorRegistry`). Proven by the `RunWindowPlannerTests`
> raider-arc test (`RecurringActor_RecastIntoMotiveStory_ByActorScopedFact`) plus the two negative
> cases. The crux below was resolved as a **casting query** with **continuation semantics**: an
> actor-scoped story is eligible only when a previously-minted live actor satisfies it, and that actor
> is hard-pinned and recast. See CHANGELOG.

D11, D16, and the mirror-antagonist all converge on a single missing capability. **Implement only
this in this change:**

1. **Actor/faction-scoped eligibility** (D16) — `RunWindowPlanner` must evaluate storylet
   preconditions over **actor- and faction-scoped facts**, not only world/global (today it uses an
   actor-less, world-only context; `narrative-procedural.md` §2.6 step 1).
2. **Recurring-actor casting** (D11) — the planner must **reuse the same `NpcInstance` across
   multiple placed stories** instead of minting a fresh actor per placed story (§2.6: "each placed
   story gets an actor minted once"). The factory already supports a persistent instance carrying
   `actor.<id>.*` facts (R12, §2.5); the gap is the **planner** reusing it and gating on its facts.

This unblocks the passport loop, character arcs (the raider-motive example, D11), and the mirror
antagonist — all three ride this one capability.

**The two items must work together — acceptance scenario (the raider arc).** This is the
end-to-end test of the change, the actor-scoped + recurring analogue of the existing R7 cross-encounter
proof (`EncounterDirectorTests`):

> Window N: the barn story is placed; its antagonist slot is cast with a raider actor (minted);
> resolving it writes `actor.<raiderId>.looted_barn = true`. → Window N+1: a "raider motive" story
> becomes eligible **by that actor-scoped fact** (item 1) and the **same** raider instance is recast
> into it (item 2). Wrong-actor or world-only eligibility fails the test.

**Hard sub-problem to solve (call it out in the §0 plan).** Evaluating an actor-scoped precondition
**during planning, before casting** requires resolving *which* actor the fact is about. So actor-scoped
eligibility likely cannot stay a global predicate — it becomes a **casting query** ("an actor carrying
fact X exists and can be (re)cast into this slot") rather than a fact lookup against a fixed subject.
Decide and document this resolution path; it is the crux of the change.

**Out of scope for this change** (keep/add as ROADMAP items): D7 spine reserved lane + per-run
reveal cap, D13/D14 first-class threads, D19 escalation tier, D20 meta-scoped fact horizon.

**Process (CLAUDE.md §0/§8).** Before coding, produce the §0 plan: restate the goal, the
layer/responsibility of each new or changed type, the dependency flow, and the doc/CHANGELOG/ROADMAP
impact. Keep it data-driven and unit-tested. Fold the implemented behavior into
`narrative-procedural.md` and append a CHANGELOG entry in the same change. Flag any tension with
these requirements rather than silently diverging.
