# The Hub — Staging, Start-of-Run Choices & Death Return — Product Requirements

> Status: **CONSUMED by O1** (shipped 2026-07-06, `hub-staging.md`) — with **owner revisions made
> at build planning (2026-07-06)** that supersede parts of this brief: the starting offer is
> **drawn from the tasted-forms pool** (up to 3 cards, deterministic; NOT the three fixed authored
> organs of FR4/FR6), **bare launch is always allowed** (the cold start with nothing tasted
> launches with no part — 0 markers), and all three homelands are authored at **tier 1** as the
> entry pool. The as-implemented behavior lives in `hub-staging.md`.
> Original status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-06
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track O · O1** (stands up the Hub). Builds on: `save-continue-run.md` (P2-2, shipped — death
> already consumes the run) and `biome-selection-along-the-run.md` (P0-2, shipped — this brief
> **refines** its entry point). Design intent: `/design/narrative/hub-junkyard.md`,
> `/design/world/overview.md` §8/§11, `/design/narrative/races.md`. Related: `vision.md` §7 (core
> fantasy), Pillars 1 & 2.

## Goal

Stand up the **Hub** — the Junkyard — as a real scene for the first time: the **pre-run staging
ground** the "Journey" button leads to (today it drops straight into a run) and the **place death
returns you to** (the Hades reform point). On the Hub the player makes the run's **two opening
choices** and launches:

1. **Pick a starting body part** — one of **three ready organs**, each carrying an **active ability**
   and a **race marker**, one per starting race/homeland (Ibex / Lizard / Fox).
2. **Pick the starting biome** — which of the three starting homelands to fly into.

Then a launch/descent sends the player into the run with that part installed, in that biome. When
the run ends in death, the player **returns to the Hub** to choose again — the loop of the Hades
frame made front-facing.

This realizes the canonical **"direction + floor, not a build vending machine"** dig
(`hub-junkyard.md`): the two picks give the run a **starting shape + a starting direction**; the
actual build is still **earned in the world**.

## The fiction it must hold (reconciliations — read before scoping)

- **The kindless mongrel stays kindless.** The hero is a formless junkyard nobody (0 innate markers,
  `races.md`). Each run he **scavenges a found organ** to launch with — the choice *is* the
  expression of formlessness (he can become anyone, so each run he bolts on a different scrap). The
  starting part is **plain, viable, and temporary** (lost on death), not a finished build.
- **A starting part carries exactly one marker (owner call 2026-07-06).** Because each organ is a
  race-tagged part, launching with it makes the hero a **1-marker "tolerated freak"** of that race
  (the passport gradient's middle rung, `overview.md` §5 / `races.md`) — **not kin**. It is a lean,
  not membership. This is a **direction lever**, honestly costed (it spends a slot on social lean
  instead of raw combat).
- **Direction + floor, never a vending machine** (`hub-junkyard.md`). The three parts are
  **side-grades** — different opening abilities + different racial leans, none strictly best (Pillar
  1, "no single correct build"). Strong/late parts still come from the **world**, never the Hub.
- **This is the MVP content of the "dig".** The canonical dig ("pick 1-of-N for floor + direction")
  is realized here as the **three starting parts**. Raw-artifact digs, N > 3, and the deferred
  cross-run **dig meta-bias** stay out (see out-of-scope).

## The two choices are independent levers (design decision 2026-07-06)

The **part** and the **biome** are chosen **independently** — the part's race need **not** match the
biome you fly into. This is deliberate divergence:

- **Match them** (Fox part → fly to the Forest): you arrive as a **tolerated Fox freak at home** —
  some doors ajar from the first platform.
- **Cross them** (Fox part → fly to the Desert): you arrive **marked as a Fox among Lizards** — an
  outsider here, carrying a lean that pays off elsewhere.

Two 3-way picks combine into a spread of distinct opening fictions, all viable — the honest form of
"direction, not vending". (The code track may present a soft hint of the match/cross state; it must
not **forbid** a cross.)

## Refinement of shipped biome-selection (state explicitly — do not silently override)

`biome-selection-along-the-run.md` (P0-2) drives the run's biome sequence from a **seeded,
escalating-tier pool** and deliberately **rejected a player-choice map** for the run. This brief
refines that for the **entry point only**: the player **chooses the run's starting (tier-0)
homeland** among the three starting biomes at the Hub; from there the **seeded escalation pool
governs the climb unchanged**. The no-choice-map stance holds for the *route*; only the *entrance*
becomes a choice. (Cave stays out of the starting set — no race yet, consistent with P0-2.)

## User stories

- As a player, "Journey" takes me to my **Hub** — the junkyard I call home — where I set up the run,
  not straight into a fight.
- As a player, I **pick one of three starting organs**, each with a **visible active ability** and a
  **race lean**, so I begin every run with a working move and a direction I chose.
- As a player, I **choose which homeland to fly into**, and I can pair or deliberately mismatch it
  with my part's race for a different opening situation.
- As a player, the **cauldron's voice comments** on what I pick — a smug remark from my gut — so the
  staging has the game's tone even before the run.
- As a player, I **launch/descend** into the run with my choices applied.
- As a player, when I **die**, I come back **here** — the junkyard reforms me — and set up a fresh
  run; I never hit a game-over screen.

## Functional requirements

### The Hub scene & flow

1. **Journey leads to the Hub.** The main menu's **Journey** enters the **Hub scene**; the run is
   launched **from** the Hub (replacing today's straight-to-run). Arena is unaffected.
2. **The Hub is the Junkyard** (`hub-junkyard.md`) — presented as the hero's home/origin at the
   bottom of the world; art/dressing to the render-look bible (muted, charming-not-gross).
3. **Launch/descent action.** A clear action starts the run, applying the two choices; the player
   cannot launch until both a **part** and a **biome** are chosen (both have sensible defaults so a
   player can launch fast).

### Choice 1 — the starting part (1 of 3)

4. **Three starting organs offered**, one per starting race (Ibex / Lizard / Fox). Each is a
   **ready installed body part** (not a raw artifact, not a socket-blank to craft) with:
   - a **visible active ability** it grants (readable before choosing), and
   - a **race marker** → installing it makes the hero a **1-marker tolerated freak** of that race.
5. **Exactly one is chosen** and **installed on the hero** for the run; it is a **side-grade**
   (different opening ability + lean, none dominant). It is **lost on death** (run-scoped).
6. **These three parts are authored content** (three `PartDefinition`-style assets the designer
   owns) — adding/retuning them is data authoring, not code.

### Choice 2 — the starting biome

7. **Choose the entry homeland** among the three starting biomes; the run **begins** in it.
8. **Independent of the part** (req: the part's race need not match the chosen biome — see levers
   above); a cross is allowed and meaningful, never blocked.
9. **The climb after entry is unchanged** — the seeded escalation pool (P0-2) takes over from the
   chosen entry biome onward.

### Cauldron voice on the Hub

10. **The voice reacts to the picks** — short, in-world, smug-gourmand lines at the part choice and
    the launch (the hub-presence of `cauldron-voice.md`; **path-reactive** tone rides existing
    facts). Lines are **data-authored** (designer writes them). Audio is deferred.

### Death → return

11. **Death returns the player to the Hub** (Hades reform, `overview.md` §8). A finished/lost run
    ends by landing back on the Hub for a fresh setup — no game-over screen.
12. **Consistent with shipped save/load** (`save-continue-run.md`): death already **consumes** the
    run image (permadeath); this brief adds the **front-end** of that — the return to the Hub — while
    **cross-run meta memory persists** (unchanged). The starting part/biome picks are **run-scoped**
    (a new run re-chooses).

## Acceptance criteria

- **Journey → the Hub scene**; the run launches **from** the Hub, not directly.
- The Hub offers **three starting parts** (each with a **visible active ability** + a **race
  marker**) and a **starting-biome choice** among the three homelands.
- Launching **installs the chosen part** (hero becomes a **1-marker freak** of its race) and **starts
  the run in the chosen biome**; the two choices are **independent** (a mismatch is allowed).
- After entry, the **biome climb proceeds via the shipped seeded pool** (P0-2), unchanged.
- The **cauldron voice** comments on the picks (data-authored lines).
- **Death returns the player to the Hub**; the run image is consumed (permadeath) while **meta
  memory persists**; picks are re-made for the next run.

## Out of scope / open points (do not build now)

- **Hub meta-progression** — investments/upgrades that persist between runs, unlock trees, currency
  spend (`overview.md` §11). MVP Hub is **staging + return only**.
- **Recurring hub cast** beyond the cauldron's voice (allies who react to your last run) — deferred
  (`overview.md` §11).
- **The dig beyond the three parts** — raw-artifact digs, **N > 3**, mixed part/artifact offers, and
  the deferred **cross-run dig meta-bias** toward a pursued direction (`hub-junkyard.md`).
- **Starting biomes beyond the three homelands** (Cave/4th race) — gated on the roster (P0-1 defers
  the 4th race).
- **A full run-choice map** — the no-choice-map stance of P0-2 holds for the **route**; only the
  **entry** is a choice here.
- **Hub art production** (final junkyard meshes/dressing) and **voice audio** — art/authoring passes.
- **Whether a starting part can be swapped/removed on the Hub before launch** — treat as a single
  commit-on-launch pick for the MVP; re-pick freedom is a tuning decision, not structural.
