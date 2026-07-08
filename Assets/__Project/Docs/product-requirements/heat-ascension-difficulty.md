# Heat — Player-Chosen Difficulty as the Cauldron's Dare — Product Requirements

> Status: **Verified** (discussed with the product owner, all forks confirmed 2026-07-07; ready for
> the code track) · 2026-07-07
> Level: product-owner (what & feel). The code track owns the technical "how".
> Model: **Fable** (a rules-composition layer many systems must obey, under determinism +
> persistence). **Track Y.**
> **Hard dependency:** the meta-progression spine (`product-requirements/meta-progression-spine.md`)
> must be **built first** — Heat's reward is expressed entirely through R's unlock vocabulary and
> dig-bias; there is no reward here without it. **R core has shipped** (`meta-progression.md`, R1–R14) —
> Heat is **additive over its existing seams**, no R rework: it adds a **min-Heat field on the
> per-token `MetaGate`** and maps total Heat onto the shipped `MetaProgressionConfig` dials (the tier
> run-floors, `_biasStrength`/`_biasCeiling`, `_reserveDirectionSlot`). **Two as-built precisions the
> build must honour:** **(i)** R's pacing is **discrete tier run-floors** (`_unlockTierRunFloors`), not
> a continuous progress bar — the brief's "accelerate pacing" is realised as **lowering a gated
> token's *effective* run-floor / bumping the effective run count** with Heat (R dropped the
> continuous "progress contribution" dial in MVP); **(ii)** R's direction bias is **dig-only** (world
> loot / quest rolls consult eligibility but not bias) — Heat's "raise the dig floor" is correctly
> **dig-scoped**, matching, and adds no world-draw bias.
> Background — do not restate it: `design/vision.md` (Pillar 4 "fast chess", anti-grind; Pillar 1
> "no single correct build"; Variant A in-world, no meta winking), `design/world/overview.md` §3 (the
> cauldron the sardonic **tempter**), `product-requirements/run-escalation.md` (D19 — the run
> **auto**-climbs; **no stat-multiplier**, difficulty = tougher *creatures to read*), `hub-staging-and-launch.md`
> + `hub-as-a-normal-platform.md` (O1 — the hub where a run is launched). **Reconciles the three
> reiterability axes:** D19 (auto within-run climb) · R (the possibility space grows) · **Y (the
> player *chooses* to climb harder for accelerated R rewards)** — Y is the agency/mastery axis the
> other two lack, and the throttle that keeps R's runway meaningful deep into the hundredth run.

## Goal

Give the player an **optional, in-fiction way to make runs harder on purpose**, and pay them for it
through the meta vocabulary they already care about. Today difficulty is **not the player's to
choose** — D19 climbs automatically inside a run, and R's vocabulary opens at whatever pace the deeds
allow. Once a player has seen the base game, nothing lets them **trade more danger for faster
becoming**.

Heat is the cauldron's **dare**: before a run, you accept a **pact of harder rules**; the more you
take on, the **faster and wider the cauldron opens richer forms** (R's tokens) and the **stronger it
leans the dig toward your pursued direction** — always under R's **never-guarantee** cap. It is
**player-chosen, rules-based (never stat-inflation), reward-gated through R, and framed by the
cauldron** — and it introduces **no new reward currency**.

## The model (product-owner level) — forks confirmed 2026-07-07

- **(1) Heat is rules, not numbers.** Every Heat modifier is a **rule change or pool shift that
  demands better play**, never a stat multiplier. No enemy HP/damage inflation (that is exactly the
  grind D19 refused). Difficulty rises because the *situation* is harder to out-chess, not because
  fights are longer slogs. (Pillar 4 + anti-grind hold.)
- **(2) Heat feeds R — no reward of its own.** The payout is entirely through the meta spine:
  **(a)** some **exotic tokens are Heat-gated** — unlockable only when the run's Heat meets an
  authored threshold; **(b)** higher Heat **accelerates R's unlock pacing** and **raises the
  direction-bias floor** in the dig — but **never past R's clamped ceiling** (Heat can raise odds and
  the floor, never manufacture a guarantee). There is **no separate Heat currency, prestige score, or
  cosmetic economy**.
- **(3) A small modular pact, not a linear ladder.** Heat is a **menu of independent
  rules-modifiers**, each with a small number of **ranks**; the run's **total Heat = the sum** of what
  you take. A Heat-gated reward requires a **minimum total**. Modular (over a StS-style single ladder)
  so runs **diverge by Heat configuration too** and the player picks **which** pain suits the build
  they are chasing (Pillar 1). Kept **small** (a handful of modifiers × 1–3 ranks) so it stays
  authorable and balanced.
- **(4) The cauldron's dare, chosen at the hub, free each run, with a remembered best.** Heat is set
  **at the hub** (O1) before launch, alongside the starting-part dig and the biome portal, **framed by
  the cauldron's voice** (in-world, Variant A — never a bare difficulty menu that winks at the
  player). Choice is **free every run** — dial up **or down**, no wall. The game **persists a
  high-water mark** (the hottest total Heat cleared) as the mastery signal; Heat-gates may key off
  either the *current* pact or the *remembered best* (authored per gate). This is **not** the parked
  corruption/thrall-meter — nothing accumulates on its own; Heat is a per-run chosen toggle.

## User stories

- As an experienced player, once the base game holds no surprise, I can **accept a harder pact** and
  feel the run get genuinely tougher — **new rules to solve**, not enemies with more hit points.
- As a player, when I play hot, the **cauldron opens richer forms faster** and **leans the dig toward
  what I'm chasing** — my danger buys **becoming**, the reward I already want, not a side-currency.
- As a player, I **choose which pain** — flip initiative, give up a body slot, face a nastier creature
  floor — so a hot run is **still my run**, shaped by the pact I picked.
- As a player, the cauldron **dares me at the hub** in its own sardonic voice; accepting is a
  **bargain in the fiction**, not a settings slider.
- As a player, I can **dial Heat down** any run with no penalty, and the game still **remembers my
  hottest clear**.
- As a designer, I **author a modifier** (its rule, its ranks, its Heat value) and **Heat-gate a
  token** (min total Heat) as **data** — no code, no per-modifier branching.

## Functional requirements

### The pact (selection)
1. **A modifier menu with ranks.** Heat offers a set of **independent modifiers**, each with **1–N
   ranks**; enabling a rank contributes an authored **Heat value**. The run's **total Heat** is the
   sum of enabled ranks.
2. **Set at the hub, per run.** The pact is chosen at the hub before launch (O1 flow), framed by the
   **cauldron voice**. Once the run launches, the pact is **locked for that run** (run-scoped state).
3. **Free choice, up or down.** Any run may take any affordable pact (including **zero** — a bare
   run); there is **no ratchet** requiring a prior clear to access a level.
4. **Persisted high-water mark.** The **highest total Heat cleared** persists across runs (meta) as
   the mastery record; the current pact persists within the run's Continue image.

### The reward gate (through R)
5. **Heat-gated tokens.** A meta token (R vocabulary) may carry an authored **minimum-Heat gate**; it
   becomes unlockable only when the run's Heat (current or the remembered best, per the gate) meets
   the threshold. Below it, the token stays out of reach — a reason to play hot.
6. **Heat accelerates R, within R's cap.** Higher total Heat **speeds R's unlock pacing** and
   **raises the direction-bias floor** in the dig. It **never** lifts the bias past R's **clamped
   ceiling (< 100%)** — Heat improves odds and the floor, **never** guarantees a piece. *(As-built
   mapping: "speeds pacing" = **lowering the effective tier run-floor / adding to the effective run
   count** for gated tokens — R's pacing is discrete `_unlockTierRunFloors`, not a continuous bar;
   "raises the dig floor" rides `_biasStrength` / `_reserveDirectionSlot`, and stays **dig-scoped** as
   R's bias is dig-only.)*
7. **No reward outside R.** Heat introduces **no** separate currency, score, prestige tier, or
   cosmetic. Its entire payout is the accelerated/opened R vocabulary and the raised dig floor.

### The modifiers (rules, not stats)
8. **Every modifier is a rule/pool change, never a stat multiplier.** No modifier inflates enemy HP or
   damage. Difficulty comes from **rules** (turn order, resources, timing, body economy, passport
   strictness) and **pool shifts** (a tougher creature floor, riding the D19 pool seam — *which*
   creatures, not scaled ones).
9. **Modifiers compose deterministically.** Multiple active modifiers apply together predictably; the
   combined effect is well-defined and reproducible.
10. **Modifiers interact correctly with the systems they touch** — combat turn-flow, the director's
    pool selection (D19), the cauldron/unseal menu, loot, and the passport facts — without breaking
    those systems' own invariants (determinism, one-unit-per-cell, arena untouched, etc.).

### Robustness
11. **Deterministic.** Same seed + same pact → the same run (modifier effects, pool shifts, and the
    resulting R pacing are reproducible). No unseeded randomness introduced by Heat.
12. **Save-correct.** The **current pact is run-scoped** (rides the Continue image, consumed on
    death); the **high-water mark is meta** (rides the shipped `meta.json`, survives death). A corrupt
    store degrades gracefully (no pact / no record), never crashes.
13. **Data-authored & additive.** Adding a modifier, changing its ranks/Heat value, or Heat-gating a
    token is **data only**; content with no Heat data behaves as today (Heat 0 = the current game).
14. **Arena untouched.** Heat is a **Journey/campaign** system; the Arena budget guard holds (no Heat
    in the networked mode).

## Content / authoring rules (for the designer)
- **Author modifiers as rules, not numbers.** Prefer *"enemies act first" · "one fewer body slot" ·
  "the creature-pool floor is raised" · "the cauldron offers fewer unseal variants" · "less healing
  between fights" · "a tighter aim window" · "races read you stricter"* over any HP/damage scalar.
- **Keep the menu small and legible.** A handful of modifiers × a few ranks — each one a pain the
  player can *feel and name*, so choosing a pact is a real decision, not a spreadsheet.
- **Gate the deep/exotic end of R behind Heat.** Reserve **minimum-Heat gates** for the rarer,
  run-shaping tokens (the late runway) — so hot play is the path to the most exotic vocabulary, while
  the base and early tokens stay open at Heat 0 (never gate the base grammar — meta-spine rule).
- **Write the cauldron's dare in voice.** The pact is offered in the tempter's register (monstrosity
  as good taste); accepting reads as a bargain, declining as prudishness.
- **Balance in playtest.** Heat values, thresholds, and the pacing acceleration are tuning.

## Tuning surface
One **Heat config asset** exposes: the **modifier set** (each with its ranks and per-rank Heat
value), the **pacing-acceleration curve** (how total Heat speeds R's unlock pacing), the **direction-
floor lift per Heat** (bounded by R's ceiling), and **default/soft-cap Heat**. Per-token **minimum-
Heat gates** live on the token asset (alongside its other R gating data). Existence of the dials is
the requirement; exact values are **playtest** (system-doc Open/deferred).

## Acceptance criteria
- The player can **accept a pact of rules-modifiers at the hub**, launch, and feel a **genuinely
  harder run** whose difficulty comes from **rules/pool changes, not inflated stats**.
- Playing at higher total Heat **opens Heat-gated R tokens** and **accelerates R's unlock pacing +
  raises the dig floor**, **never** past R's clamped ceiling; **no separate Heat reward** exists.
- Heat is **chosen freely each run** (up or down, zero allowed); the **hottest clear persists** across
  runs; the **current pact is consumed on death**.
- **Same seed + same pact → the same run**; adding/re-valuing a modifier or Heat-gating a token is
  **data only**; Heat 0 reproduces today's game.
- Heat is **absent from the Arena**; existing systems keep their invariants under any pact.

## Out of scope / open points (do not build now)
- **The concrete modifier list & all values** — the shipped set, Heat costs, thresholds, and the
  acceleration curve are **content + playtest tuning**, authored on the config; this brief fixes the
  *shape*, not the numbers.
- **A second/Heat reward currency, prestige titles, cosmetics, leaderboards** — deliberately **cut**;
  the payout is R only.
- **A linear Ascension ladder / ratcheting unlock walls** — rejected for the modular free-choice pact.
- **Stat-multiplier difficulty** — deliberately **cut** (rules/pool only), consistent with D19.
- **Gating the run apex / ending behind Heat** — the Order's Seat / endings (Track Z) are unbuilt;
  Heat gates **R tokens**, not a finale, for now.
- **Heat-driven deeds beyond R** — Heat modulates the R vocabulary; it does not add new deed channels
  (the meta-spine's two channels stand).
- **Per-modifier UI/art polish** and the cauldron's exact dare lines — presentation/content follow-ups.
- **Any corruption/thrall meter** — Heat is a chosen per-run toggle, not an accumulating stat; the
  parked meter stays parked.
