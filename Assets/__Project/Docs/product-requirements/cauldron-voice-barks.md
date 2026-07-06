# Cauldron-Voice Barks — Tempter & Socketing-Trend Delivery

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Design background: `design/narrative/cauldron-voice.md` (character, register, sample lines),
> `design/narrative/quest-as-reward.md` §4 (tempter on the dark offer), `design/world/overview.md`
> §3/§7. Covers ROADMAP **P1-10** (cauldron-voice tempter + socketing-trend barks). **Hybrid** — the
> code track builds the delivery seam; the **designer authors the line content**. Shares the bark
> channel with Track J's `J-craft` (the `ISocketingTrendSource` seam already exists). Distinct from
> the **reveal-beat** cauldron lines (those ride P3-1's spine lane, `director-meta-consumers.md`).

## Goal

Give the cauldron its **reactive, in-the-moment voice** — the audible form of the temptation, with no
corruption meter behind it. The voice **barks short lines at authored moments** (a strong/monstrous
mutation offered, the dark offer or attack verb presented, a socketing trend forming) and its **tone
shifts with the player's path lean** (bolder as you indulge the monster, sour as you lean to
friendship). This is the real-time **bark channel** — deliberately **separate** from the curated,
rush-proof **reveal beats** about past hosts (those are gated on the spine lane, not fired live).

## User stories

- As a player, when a **strong/monstrous mutation** is on offer, the cauldron **purrs approval** —
  tempting me toward the bigger monster.
- As a player, when a **dark offer or the attack verb** is presented, the cauldron **leans in** — the
  pull to Conquest is delivered as its voice, not as better loot.
- As a player, as I **socket a consistent trend** (leaning one substance/family), the cauldron
  **remarks on the pattern** — a needling read of what I'm becoming.
- As a player, the voice's **temperature tracks my run** — proprietary and bold when I indulge the
  monster, clipped and sour when I lean to passing/allies — so it feels like a companion reacting to
  me, not a random quip generator.

## Functional requirements

### Bark slots (the triggers)
1. The voice fires a short bark at authored **slots**, at least:
   - **Temptation** — a strong / monstrous mutation is offered (the unseal variant menu surfaces a
     high-tier / monstrous option).
   - **Dark offer / attack** — the power/combat (Monster-lean) offer of a fork is presented, or the
     **attack card** is presented/chosen (`attack-card-monster-verb.md`, `multiple-and-competing-
     offers.md`).
   - **Restraint** — a modest / marker (passport) part is taken (the sour counterpoint).
   - **Socketing trend** — a consistent socketing pattern is detected via the existing
     `ISocketingTrendSource` seam (shared with Track J `J-craft`).
2. A bark is a **short, terse line** in the plain chatter register (not the ornate card) — it never
   grows into a wall of text and never blocks input.

### Path-reactive tone
3. Each slot resolves its line against the player's current **path lean** (monster/indulgent vs
   friendship/restrained): the **indulgent** register is bolder/proprietary, the **restrained**
   register sour/clipped. The lean is read from existing path facts — **no new meter**.
4. When multiple lines fit a slot, selection is **deterministic** under the run seed (no jarring
   repeats back-to-back where the pool allows avoidance).

### Content is data-authored
5. Bark lines are **authored content** (a data-authored pool keyed by slot × path-lean), not
   hard-coded — the designer adds/edits lines without code. New slot = a new authored key where the
   engine already fires that event.

## Content authoring rules (for the designer)
- Author lines in the **cauldron register** (`design/narrative/cauldron-voice.md`): dry, decadent,
  gourmand framing; backhanded pet names; **Variant A** (no fourth-wall, never states what the
  cauldron *is*).
- Provide, per bark slot, **both** an **indulgent** and a **restrained** variant so the tone can track
  the lean.
- Keep each line **short and pithy** — a bark, not a monologue.
- **Do not** author "past hosts" / tyrant / origin reveals here — those are **spine reveal beats**
  (capped, gated) delivered via `director-meta-consumers.md`, not the live bark channel.

## Acceptance criteria
- Each defined **slot** fires a cauldron bark at the right moment (temptation, dark offer / attack,
  restraint, socketing trend), pulling from the authored pool.
- The **same slot** produces a **bolder** line under an indulgent lean and a **sourer** line under a
  restrained lean — demonstrating path reactivity off existing facts, no new meter.
- Barks are **short chatter-register** lines that don't block play; selection is deterministic under
  the seed.
- A designer can **add a new line** (or a new slot's lines) as **data only**, with no code change,
  where the engine already raises that event.

## Out of scope / open points (do not build now)
- **Reveal-beat cauldron lines** (past hosts / mirror / cycle) — separate, ride P3-1's spine lane
  (`director-meta-consumers.md`); capped and gated, **not** this live channel.
- **Voice acting / audio** — text barks only for now (audio direction later, `design/audio/`).
- **Hub / den presence lines** — deferred (depends on the hub design, `world/overview.md` §11).
- **The full reactive line pool per beat × lean** — content authoring continues past this brief; the
  brief establishes the slots + the data shape.
- **The socketing-trend detection itself** (`ISocketingTrendSource`) — the seam exists (Track J); this
  brief consumes it for a bark, it does not build it.
