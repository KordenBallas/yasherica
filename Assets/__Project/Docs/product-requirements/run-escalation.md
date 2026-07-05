# Run Escalation — Tier Shifts the Pool and the Register — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-04
> Level: product-owner (what & feel). The code track owns the technical "how".
> Background — do not restate it: `design/narrative/director.md` D19 (the resolution this brief
> builds), `design/world/overview.md` §8 (a run **escalates through biomes toward the Order's
> seat**), `design/vision.md` §4 ("the backwater is the entry, not the ceiling" — **satire scales
> with the stakes**, epic-but-vile). **Prerequisite:** `product-requirements/biome-selection-along-the-run.md`
> already **publishes** the `run_escalation_tier` fact — this brief is its first **consumer**.
> Related axes it deliberately does NOT re-own: **Site-tier** local scale
> (`product-requirements/world-sites-and-landscape.md`) and content **density**
> (`product-requirements/world-content-density.md`).

## Goal

Make a run **feel like it climbs**. Today the run's altitude tier is published but **nothing
consumes it** — every tier fights the same monsters and hears the same register of stories, so the
approach to the Order feels no different from the starving backwater. We make the director **shift
the eligible content pool as the run climbs** so that, higher up, **the monsters are tougher
creatures** and **the stories carry a grander, more rotten register** — "epic, but vile".

This is **one mechanism with three effects**: a **run-tier band on content**. The director filters
what is eligible by the current `run_escalation_tier`. It does **not** inflate monster stats, does
**not** grow content density, and does **not** add the run's apex/finale (see out-of-scope).

## The model (product-owner level)

- **Everything placeable carries a run-tier band** — an authored range of altitudes at which a
  piece of content is appropriate. The director only draws content whose band contains the current
  tier. `min`-tier is the primary gate (content **opens upward** as you climb); `max`-tier is
  **optional and used sparingly** (to age out register-breaking low content near the top).
- **(1) Register — stories shift by altitude.** Story content is banded into **three named
  registers** layered over the numeric tier:
  - **Backwater** (low) — starving peasants, petty local troubles; the entry register.
  - **Courts** (mid) — corrupt nobility and clergy, venal officials; the middle stakes.
  - **Divine / Apex** (high) — the approach to the Order, crowned rot; grand-but-vile.

  As the run climbs, **low-register stories leave the eligible pool and high-register stories
  enter** — the world's problems grow in scale *and* in rot (`vision.md` §4). The register is a
  **coarsening** of the numeric tier into these named bands; authors tag a story with a register
  (or a tier range), not a bespoke per-tier list.
- **(2) Difficulty — monsters get tougher, not bigger-numbered.** Monsters carry the **same
  tier-band**. A higher tier draws a **tougher monster pool** — **different creatures** (bigger
  bodies, more parts, nastier kits, built from the same body-part system, so their anatomy still
  reads as their kit — Pillar 4). There is **no per-tier stat multiplier**: a monster's stats stay
  flat; what changes is **which creatures appear**. Difficulty is *new anatomy to read and out-
  chess*, never a longer slog against inflated HP (the "grindy combat" anti-goal).
- **(3) Density does NOT scale with tier.** The rare, breathing world (the density brief) holds at
  **every** altitude — climbing raises the **stakes and register**, not the raw amount of content.
  The intuition that "a court is busier than the backwater" is real but is a **local** effect owned
  by **Site-tier** (a city's capacity recipe places more beats than a camp's), **independent** of
  run altitude. This brief does not touch the density config.
- **Two register axes, cleanly split.** *Run-tier* (how high the whole run has climbed — this
  brief) and *Site-tier* (how large a given settlement is — the sites pass, 2026-07-01) are
  **orthogonal**. A city in the backwater still reads as starving-but-organised; a city at the
  approach reads as the rotten court. This brief owns only the **run-tier** consumer and leaves
  Site-tier to the sites work.

## User stories

- As a player, as my run **climbs**, the **monsters I meet change into tougher, stranger
  creatures** — I have to read new anatomy, not just chew through more hit points.
- As a player, the **kind of trouble the world is in escalates** — from a village that is starving,
  up to corrupt courts and crowned rot — and the **satire gets grander and more vile** the higher I
  go, never flattening into earnest high-epic.
- As a player, the world **does not get more crowded** as I climb — it stays a rare, breathing
  world; what rises is the **stakes**, not the clutter.
- As a designer, I **tag content with a run-tier band** (or a named register for stories) and the
  right content simply **appears at the right altitude** — no code, no per-tier branching.
- As a designer, the escalation is **deterministic**: the same run seed climbs and shifts the pool
  the same way.

## Functional requirements

### The tier consumer
1. **Read the published tier.** The director reads the `run_escalation_tier` fact (published by the
   biome-selection work) and uses it as an **eligibility input** to content selection.
2. **Tier band on content.** Every placeable content item (**story** and **monster**) can carry an
   authored **run-tier band** (a `min` and optional `max`). The director includes an item only when
   the current tier falls within its band.
3. **Opens upward.** `min`-tier is the primary gate — content becomes eligible when the run reaches
   its tier and (by default) stays eligible above it. `max`-tier is **optional**, used only to
   retire content that would break register high up.

### Register (stories)
4. **Three named registers** — **Backwater / Courts / Divine-Apex** — map to bands of the numeric
   tier (authored). A story declares its register (or an equivalent tier range).
5. **The story pool shifts with altitude.** As the tier climbs, **low-register stories age out of
   the eligible pool** and **high-register stories enter**, so the register of trouble rises with
   the run. This is a **soft pool shift**, not a hard single-tier lock.
6. **Register scales the satire, played straight** (`vision.md` §4, Variant A): higher registers are
   **grander and more rotten**, never earnest high-epic. (Content is authored, not generated — this
   is an **authoring rule**; the engine only gates by band.)

### Difficulty (monsters)
7. **Tougher pool, not scaled stats.** A higher tier makes the eligible **monster pool** tougher by
   **drawing tougher creatures** (per their tier band), **not** by applying any stat multiplier.
   Monster stats remain **as authored per creature**.
8. **Anatomy still reads as kit.** Tougher monsters are tougher because they are **bigger / more-
   parted / nastier-kitted creatures** built from the same part system (Pillar 4 legibility holds).

### Non-scaling (guardrails)
9. **Density is not a function of tier.** The content-density budgets (quest rarity, monster/loot/
   empty mix) are **not** modulated by `run_escalation_tier`. The breathing world holds at every
   altitude.
10. **Local populated-court feel rides Site-tier, not run-tier.** Any "denser because it's a court"
    read is produced by a **Site's** capacity recipe (the sites work), independently of run altitude.

### Robustness
11. **Deterministic.** Same run seed → same tier climb → same pool shift. No unseeded randomness in
    escalation selection.
12. **Data-authored & extensible.** Registers, tier bands, and the tier→register mapping are
    **data**; adding content at a new register, re-banding a monster, or adding a tier is **no code
    change**. Content with **no band authored** has a sensible default (eligible at all tiers) so
    existing content keeps working.

## Content authoring rules (for the designer)
- **Stories:** tag each with a **register** (Backwater / Courts / Divine-Apex) or an equivalent
  **tier band**. Author the higher registers as **grander *and* more rotten** (satire scales with
  the stakes; played straight, no meta). Use an optional `max`-tier only for content that would feel
  wrong high up (e.g. a purely backwater chore at the gates of the Order).
- **Monsters:** give each creature a **tier band** for where it belongs in the climb. Build
  higher-tier creatures as **genuinely tougher bodies** (more/bigger parts, nastier kits) rather
  than relying on numbers — their anatomy must still telegraph their kit.
- **Do not** try to express escalation as a density change — leave the density config alone; use
  Site-tier for local populated-ness.
- Keep it additive: content with no band defaults to "eligible everywhere"; adding a tier or a
  register band is data only.

## Acceptance criteria
- As a run **climbs tiers**, the **set of eligible monsters shifts toward tougher creatures**, and
  the **set of eligible stories shifts toward higher registers** (backwater trouble gives way to
  courtly/divine rot).
- Monster **difficulty rises by which creatures appear**, with **no stat-multiplier** applied by
  tier (a given creature's stats are identical wherever it appears).
- **Content density is unchanged by tier** — the world breathes the same at low and high altitude.
- The **same run seed** produces the **same** climb and pool shift.
- **Adding/re-banding content is data-only**; content without a band is eligible at all tiers.
- Nothing in this brief introduces a run apex, a finale, or a player-facing tier UI.

## Out of scope / open points (do not build now)
- **The Order's Seat / run apex / a defined run end** — deferred (`design/roadmap.md` World &
  Sites). At the top tier the run keeps serving top-tier content; there is no throne/finale yet.
- **Site-tier register/density** — the local settlement-scale effects are owned by the sites work
  (`world-sites-and-landscape.md`), not this brief. This brief only splits the two axes and wires
  the **run-tier** one.
- **Per-tier stat scaling of monsters** — deliberately **cut**; difficulty is pool-shift only. A
  secondary stat knob was considered and rejected (keeps combat on Pillar 4, off the grind).
- **Density-by-tier** — deliberately **cut** (owner's call); the breathing world holds at all
  altitudes.
- **Tonal/register content itself** — this brief provides the **gating seam**; authoring the actual
  courtly/divine stories and the tougher creatures is content work that rides it.
- **Player-facing altitude/tier UI** — escalation is felt in the fiction (register + creatures), not
  shown as a meter or map.
- **Cross-run persistence** of tier — rides the general save/load work (the tier is a run-scoped
  fact per the D20 boundary).
