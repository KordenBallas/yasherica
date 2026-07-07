# Combat Status Effects — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-07
> Level: product-owner (what & feel). The code track owns the technical "how".
> Design background: `design/roadmap.md` §Combat (status-effect thread), `design/decisions.md`
> (2026-07-07 pillar-track survey). Covers ROADMAP **Track S** (`ROADMAP-prioritized.md`) — this brief
> is **S1 (status core) + S2 (legibility)**. **S3** (reconcile with the P3-13 passive stat-modifiers)
> is a requirement here (FR10). **S4** (status interactions / combos) is **out of scope** (parked).
> Ties the effect-type icon slot in `mutation-choice-cards.md` (the card already shows an effect icon
> with nothing behind it). Must fit the Arena hidden-simultaneous-resolve (`arena-mode.md`).

## Goal

Turn combat from **instant damage** into a game with a **persistent condition layer**. Today an
ability's effect happens once, the moment it resolves; a mutation card even shows an **effect-type
icon** (Physical, and by implication burn / poison / stun) with **no system behind it**. This brief
adds that system: a **status** is a **named condition on a unit that persists across turns**, **ticks
or gates** each round, and **expires** — **data-authored**, **legible on the board**, and
**deterministic / Arena-lockstep-safe**. This is the depth glue for the "fast chess" pillar (Pillar 4)
— board control, damage that unfolds over time, and timed buffs/debuffs — without adding a rules wall.

**Design stance (state it):** statuses are **additive** — they do not change what existing instant
damage does; they layer a *time* dimension on top. Kept **flat for v1**: no status→status combos, no
rich dispel trees. One clean, legible, data-driven layer.

## User stories

- As a player, when I hit an enemy with a **burn** ability it **keeps taking damage** over the next
  turns, not just once — I set up a kill, not only land one.
- As a player, I can **stun / root / slow** an enemy to take its turn or pin it — I **control the
  board**, not just trade hits.
- As a player, a **buff / debuff** (weakened, hardened…) shifts a unit's stats for a few turns — the
  same language as the passive modifiers I already get from parts.
- As a player, I **see on each unit** which statuses are on it and **how many turns are left** — I read
  the board at a glance.
- As a player, a mutation / ability card's **effect icon tells me what status it will apply** *before*
  I commit — the icon on the card is the same mark I'll see on the unit.
- As a player, statuses resolve the **same way every time** (and in multiplayer, **identically for
  everyone**) — no surprises, no desync.

## Functional requirements

### What a status is
1. A **status** is a **named condition applied to a unit** that **persists across turns**. It carries a
   **duration in turns** and a **kind**; each round it **ticks or gates** per its kind, and it is
   **removed when its duration runs out** (or is explicitly cleared). Multiple different statuses may be
   on one unit at once.

### Starter kinds — all data-authored
2. Three **kinds** ship as the starter vocabulary, **all expressed as data** (adding a new status is an
   asset, **never code**):
   - **Damage-over-time (DoT)** — deals damage each turn while active (burn, poison, bleed…).
   - **Control** — restricts the unit's action for its duration: **stun** (loses its turn), **root**
     (can't move, may still act), **slow** (reduced movement). The exact control set is authored.
   - **Stat-modifier** — raises / lowers a unit's stats (damage / defence / heal…) for its duration
     (weakened, hardened…). This is the **same dimension as the P3-13 passive modifiers** — see FR10.

### Duration & ticking (deterministic)
3. Duration is counted in **turns**. A status resolves at **one fixed point: the end of the afflicted
   unit's turn** — DoT deals its damage there, control is evaluated for that unit's turn, and durations
   **count down** there. This resolve point is **deterministic and identical in PvE and Arena**
   (lockstep-safe): **no status may resolve at a nondeterministic or frame-dependent time**.
4. When a status's duration reaches **zero it expires and is removed**, and its expiry is **visible**
   (the on-unit icon clears).

### Stacking (per-status rule, refresh by default)
5. Re-applying a status already on a unit **refreshes its duration** by **default** (no unbounded
   pile-up). A status may instead be authored to **stack to a cap** (up to N stacks, each adding
   magnitude/duration per its rule) where that is the intended design, or to **ignore** re-application.
   The stack rule is **per-status data**.

### Application
6. An ability applies a status by **referencing which status, its magnitude, and its duration**
   (authored on the ability / part). Application **rides the ability's existing targeting / area** — a
   cone ability applies its status to **every unit in the cone**. **No new targeting concept** is
   introduced.

### Legibility on the board (Pillar 4) — S2
7. **Every active status shows on the unit** on the board: **an icon per status + its remaining turns**.
   Several statuses read as a small **row / stack** on the unit; the board stays legible ("fast chess").
8. The **card / ability effect-icon is the same glyph as the status's on-unit icon** — the effect-type
   icon slot on a mutation / ability card (`mutation-choice-cards.md`) shows the **status that ability
   applies**, so the player connects "this ability → this on-board condition" **before** committing.
9. The **ability-preview popover** (mutation card / Arena draft) may name the status it applies in its
   description — **reuse the shared preview grammar** (`mutation-choice-cards.md` / `arena-draft-ui.md`);
   no new popover surface.

### Reconcile with passive modifiers — S3
10. Status **stat-modifiers** and the **P3-13 passive stat-modifiers** share **one modifier model** — "a
    condition that changes a unit's stats" is authored **once** and used both as a **timed status** and
    as a part's **always-on passive** (a passive is the duration-less case). **Two parallel systems are
    forbidden.**

### Removal (thin)
11. A status ends by **expiry** (default) or by **explicit removal** where a design calls for it (a
    cleanse ability/part, or an **immunity** flag that refuses application). Cleanse/immunity is a
    **thin data flag**, authored only where needed — **not** a required part of v1.

## Content authoring rules (for the designer)
- A **status** is one data asset: its **kind** (DoT / control / stat-mod), its **on-unit icon**, its
  **default stack rule**, and — for stat-mod — **which stat** it moves and by how much. **No code per
  status.**
- An **ability / part that applies a status** declares only **which status, magnitude, duration
  (turns)** — that is the whole hookup.
- The **effect-icon on the ability / card** and the **status's on-unit icon** are the **same asset** —
  author the glyph once.
- Keep **magnitudes and durations** to the balance pass (they feed the balance-ledger) — never hardcode.
- **Combos are out for v1** — author each status as **independent**; do not design a status that only
  works by reacting to another.

## Acceptance criteria
- A **DoT** applied to a unit **deals its damage each turn at the end of that unit's turn** and **stops
  when its duration expires**.
- A **control** status (stun / root / slow) **restricts the unit's action** for its duration, then
  **clears**.
- A **stat-modifier** status **changes the unit's stats for its duration and reverts on expiry**; the
  **same modifier authored as a part passive is always-on** — demonstrating **one model** (FR10).
- **Re-applying** a status **refreshes its duration** by default; a **stack-to-cap** status accumulates
  up to its cap.
- **Every active status is visible on the unit** (icon + remaining turns), and the **card effect-icon
  matches the status glyph** (FR8).
- Statuses resolve at a **deterministic point, identically in PvE and Arena** — no lockstep divergence.
- **Adding a new status is data-only** (a new asset + an ability referencing it) — **no code change**.

## Out of scope / open points (do not build now)
- **Status interactions / combos** (wet→fire, spreading, triggered reactions) — **parked (S4)**; keep
  statuses flat for v1.
- **Rich cleanse / dispel trees** — only the **thin removal / immunity flag** of FR11; a full dispel
  system is later.
- **New targeting / area concepts** — statuses ride **existing** ability targeting; no new aim step.
- **Exact starter magnitudes / durations and the full control set** — **balance / authoring**, tuned in
  playtest via the balance-ledger, not fixed here.
- **VFX / animation for status application & tick** — tech-art / render-look (ties the VFX-language
  item); this brief sets **content + behaviour**, not the visual effect.
- **AI reasoning about statuses** (an enemy choosing to cleanse, or avoiding a DoT tile) — **smarter-AI**
  (P2-4 / Track K); v1 AI applies statuses but need not strategise around them.
- **No change** to what existing abilities' **instant damage** does — statuses are **additive**.

## Appendix — starter values (demo, tunable · not spec)

*A concrete **starter set to author for the demo** so the system ships with content, not an empty
vocabulary. These are **tunable demo defaults**, not fixed requirements — the balance pass (feeding the
balance-ledger) owns final numbers. Calibrated to the current combat scale: **hero 100 HP · enemies
50–80 HP · ability damage 20–100 · cooldown ~1**, so a fight is ~2–4 rounds and durations stay short.
The set deliberately **exercises all three stack rules** (refresh · stack-to-cap · ignore) and all
three kinds.*

| Status | Kind | Magnitude | Duration | Stack rule | Glyph id | Note |
|---|---|---|---|---|---|---|
| **Burn** | DoT | 8 dmg / turn | 2 | refresh | `glyph.burn` (flame) | front-loaded (~16 total) |
| **Poison** | DoT | 4 dmg / turn **per stack** | 3 | **stack-to-cap 3** (duration refreshes) | `glyph.poison` (droplet) | the stack example (up to 12/turn) |
| **Bleed** | DoT | 6 dmg / turn | 2 | refresh | `glyph.bleed` (slash) | steady chip |
| **Stun** | Control | skips its next turn | 1 | **ignore re-apply** | `glyph.stun` (stars) | strong → short; the ignore example (no chain-lock) |
| **Root** | Control | cannot move (may still act) | 2 | refresh | `glyph.root` (shackle) | pin in place |
| **Slow** | Control | −1 move cell | 2 | refresh | `glyph.slow` (snail) | kite / zone |
| **Weakened** | Stat-mod | outgoing damage −5 | 2 | refresh | `glyph.weakened` (arrow-down) | soften a hitter |
| **Empowered** | Stat-mod | outgoing damage +5 | 2 | refresh | `glyph.empowered` (arrow-up) | buff channel |

**Gated on P3-13** (need the def / heal stat dimension — not in the demo build): **Hardened**
(`glyph.hardened`, shield — incoming damage −5, 2 turns) and **Regen** (`glyph.regen`, heart-plus —
+5 HP / turn, 3 turns). Author them once P3-13's stat targets land; they reuse the **same modifier
model** (FR10).

### Effect glyph registry (canonical — single source of truth)

The **glyph id** column above is the **canonical status-glyph registry**. It is authored once (one icon
asset per status, per FR8) and is the **same glyph** shown in **both** places: **on the unit** on the
board (S2 · FR7) and **on the ability / mutation card's effect slot** (FR8, `mutation-choice-cards.md`).
A status and its glyph are 1:1 and **shape-distinct** (not colour-only) — this is also the
**colour-blind redundancy** the Track-U accessibility pass asks for, applied to statuses. Glyph ids are
**stable**: adding a status adds a row here; renaming a glyph is a breaking change to both surfaces.

**Neutral mark (`glyph.untyped`).** One extra glyph — plain, the reference card's "Physical" chevron —
is the **no-status** mark, shown in the card's effect slot when an ability applies **no** status (pure
untyped damage). Owner call 2026-07-07: **there is no elemental damage-type system** — "Physical" is
this neutral mark, **not** an element. A real damage-type / resistance layer is a **deferred design
fork** (would be its own track near K/S), explicitly **not built now**.

**Demo wiring:** hang **Burn** on a fire-flavoured demo ability, **Poison** on a lizard/serpent part,
**Stun**/**Slow** on a control ability, **Weakened**/**Empowered** on a debuff/buff ability — so the
board shows each kind in a play-through. Exact ability→status assignment is content, tuned with the
rest of the demo abilities.
