# Meta-Progression Spine — Unlock the Vocabulary, Never the Power — Product Requirements

> Status: **Verified · shovel-ready** (discussed with the product owner; the two open design forks
> — the direction metric and the MVP deed scope — were **closed 2026-07-07**, see FR4/FR8 and the
> "Design forks closed" note) · 2026-07-07
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track R** (Meta-progression & Economy) — this is the **meta half only**. The in-run
> currency (coins, shops, the P1-12 reward sinks, the camp dark-currency) is a **separate
> companion brief** and is out of scope here (see below).
> Background — do not restate it: `design/narrative/hub-junkyard.md` ("direction + floor, not a
> vending machine"; the deferred dig **meta-bias**), `design/world/overview.md` §8 (the Hades
> frame — run-permanence coexists with world-permanence), `design/vision.md` §1–§2 (Pillar 1 "no
> single correct build", Pillar 3 "alchemy you can't predict", anti-grind). **Builds on shipped
> work:** `product-requirements/save-continue-run.md` (P2-2 — the cross-run `meta.json` store this
> brief reads/writes), `product-requirements/hub-staging-and-launch.md` + `hub-as-a-normal-platform.md`
> (O1 — the start-of-run **dig** already draws up to 3 cards from the **tasted-forms pool**; this
> brief grows and biases that pool), `product-requirements/arena-part-draft-and-catalog.md` (P4-5 —
> the **passive, currency-free** part-catalog unlock precedent this generalises). **Companion for
> legibility:** the Tasted-Forms bestiary (Track T) is the **readout** of the vocabulary this brief
> grows — depended on for making the runway visible, not built here.

## Goal

Answer the reiterability question — **what makes run 2 mechanically different from run 1** — without
betraying a single prior decision (no vending machine, no grind, no "correct build", permadeath
stays real). Today nothing carries mechanical weight between runs except the narrative meta
(mirror-lore, spine cursor — already shipped) and the passive Arena catalog; a hundredth run draws
from the same possibility space as the first.

We make the world **remember what the cauldron has tasted** and let that memory **widen the space of
what is possible in future runs** — new body-part forms, new artifacts, new ability axes, higher
tiers, rare recipes, exotic pools **become able to appear**. The player's cross-run progress is a
**growing vocabulary**, never a growing power bar.

This is the **Isaac model, not the Hades model**: meta-progression **unlocks content into the
possibility pool** (and biases the odds toward your chosen direction), and **never** hands durable
combat power, permanent stat buffs, or a guaranteed piece. Run 100 still opens new things; run 1 is
already deep.

## Design forks closed (2026-07-07 — made this brief shovel-ready)

Two forks were left open at first draft; both are now resolved so the code track needs no further
design input:

- **How "pursued direction" is measured (the bias input).** Direction is a **sliding-window tally
  over the parts the hero has installed across the last few runs** (a small window, ~3–5 runs, so
  the direction can **shift** as the player changes what they build — it is not a lifetime average),
  aggregated on **two axes that already exist as data**: the part's **race marker / belonging** and
  the socketed artifact's **function family** (substance/property). It is **not** measured on
  damage/effect types (those do not exist yet — S5 is parked). The window length and the two axes'
  relative weight are config dials.
- **The MVP deed surface.** MVP unlocks are earned through **exactly two deed channels**:
  **(1) tasting a form** (the persisted tasted-forms catalog — the primary channel) and
  **(2) reveal-spine milestones** (already persisted facts / the meta run-counter). Deeds that lean
  on **unbuilt content — beating a biome apex, an ending (Alliance/Conquest) resolution — are
  deferred**, not authored now. Adding a deed channel later is **data only** (FR15).

## The model (product-owner level)

- **Meta buys possibility, not power.** No permanent stat/ability upgrade, no "keep your build on
  death", no start-stronger. What grows is **what can appear**, and **how likely** your pursued
  direction is to appear — never a guarantee, never raw strength. (Pillar 1, the vending-machine
  rejection, and permadeath all stay intact.)

- **The unlock atom is a *grammar token*, not a finished item.** Our content space is
  **combinatorial** (`blanks × artifacts × axes` → an enormous outcome space), so unlocking a whole
  finished organ would be meaningless. Instead the meta unlocks a **token** — a **multiplicand** of
  that space:
  - a new **body-part form** (blank), especially rare **frame-changers**,
  - a new **artifact** (substance/property),
  - a new **ability axis** — a trait, a **damage/effect type**, a **higher tier ceiling**,
  - a **signature recipe**,
  - an **exotic content pool** (e.g. a biome's rarer part table).

  Unlocking one token **multiplies** the reachable outcome space (one new axis × all blanks × all
  artifacts). The player feels "my vocabulary grew", not "my item list grew".

- **The base grammar is fully open from run 1.** All three starting races' base parts, the core
  artifact/trait set, base tiers, and the whole emergent crafting grammar are available **from the
  first run**. The **combinatorial depth lives inside every run** (crafting/discovery — Pillar 3);
  the meta **widens the edges** (exotica/axes) over many runs. **We never lock the base
  multiplicands** — early runs are deep, not a starved demo.

- **Unlocks are earned by *deeds*, not bought with a number.** A token enters your vocabulary by
  **doing a thing** — the passive/deed model (the Arena catalog precedent), not by hoarding a
  currency. Deeds are **in-fiction milestones**: **tasting a new form** (the tasted-forms catalog,
  already persisted, is the primary channel — "you are what you eat" *is* the unlock), **beating a
  biome's apex creature**, **reaching a run-tier band**, **a first Alliance/Conquest resolution**,
  etc. Many of these are the **same milestones the reveal spine already gates on** — one deed can
  both advance lore and open a token.

- **Direction is biased, never guaranteed** (the deferred "junkyard meta-bias", realised). Across
  runs, pursuing a category (spark/aquatic/a given race) **raises the odds and the floor** that the
  dig and the world surface that category — **capped below certainty**. The instant it could
  guarantee a specific piece it becomes the vending machine we rejected, so the bias carries a
  **hard ceiling (< 100%)** the system clamps.

- **The pool dilutes as it grows (a free guardrail).** A wider unlocked vocabulary means any **one**
  token is **rarer** to be offered (the draw spreads over more tokens). So "unlock everything" does
  **not** converge on a guaranteed strong build — expansion has a built-in dilution cost. Direction
  (the bias) is how you fight dilution, and it too is capped.

- **The runway is stratified and generous (starting stance).** Aim for **content still opening at
  run 100** while **run 10 already feels alive**. Two different levers deliver this, and conflating
  them is the trap:
  - **run 10 is alive** ← the **rich open base** (combinatorial depth is in-run) + **frequent small
    token unlocks** early;
  - **run 100 still opens** ← a **long runway of edge/axis tokens**, spaced out by a **front-loaded
    pacing curve** (something new nearly every early run, rarer later).

  The runway is **stratified**: **many small tokens early** (breadth), **rare run-shaping unlocks
  late** (new starting parts / frame families / biomes at the hub — our equivalent of Isaac's "new
  character"). **How generous** the runway is (pool size, curve shape) is **tuning, not
  architecture** — see the Tuning surface; the owner's starting stance is **generous**.

- **It rides the shipped meta store.** Persistence uses the P2-2 `meta.json` cross-run store; a
  corrupt/empty store degrades to "base vocabulary only", never crashes (consistent with P2-2's
  fail-safe).

## User stories

- As a player, when I **do something notable** (taste a new form, fell a biome's apex, reach the
  courts), the world **remembers it**, and a **later run can surface something I couldn't before** —
  a new form, a nastier axis, a rare recipe.
- As a player, my hundredth run **still opens new things**, and my tenth run is **already deep** — I
  never feel I have to grind runs open before the game is fun.
- As a player, chasing a direction across runs makes that direction **show up more often** — but the
  dig **never just hands me the exact piece**; I still have to build it in the world.
- As a player, I **never get permanently stronger** by playing more — a fresh run is a fresh mortal
  beast; what changed is **what's possible**, not how buffed I start.
- As a designer, I **author a token** (a part/artifact/axis/recipe/pool) and mark **whether it's
  base or meta-gated, how it's earned, and how likely it is once unlocked** — no code.
- As a designer, I **tune the whole unlock curve** — pacing, which deeds count for how much, how
  strong the direction-bias is and its ceiling, the dig shape — **from one config**, and I can slide
  it from generous toward minimal **without a rebuild**.

## Functional requirements

### The vocabulary (what persists)
1. **A persisted meta vocabulary.** The cross-run store (P2-2 `meta.json`) holds the set of
   **unlocked tokens** (which forms/artifacts/axes/recipes/pools are available beyond the base set),
   surviving death and quit.
2. **Base vs meta-gated is per-token data.** Every gateable token carries a **gating mark** — **base**
   (available from run 1) or **meta-gated** (absent from every pool until its deed is met). Marking a
   token base/gated is **data only**; the min-vs-generous stance is expressed by **how many tokens are
   marked gated**, not by code.
3. **A meta-gated token is absent everywhere until unlocked.** A gated token does not appear in the
   hub dig, world finds, quest-reward rolls, or any other draw until its unlock is earned. Unlocking
   adds it to the eligible set for **all** consumers that already consult eligibility.

### Earning (deeds)
4. **Deed-driven unlock — two MVP channels.** A token unlocks when its authored **deed** is
   satisfied — an in-fiction milestone expressed over **existing facts / the meta run-counter** (no
   new milestone subsystem). MVP supports **exactly two channels**: **(1) tasting a form** (the
   persisted tasted-forms catalog — the primary channel; tasting a form unlocks that form's token)
   and **(2) reveal-spine milestones** (existing persisted facts). Deed channels that depend on
   **unbuilt content** (biome-apex kills, ending resolutions) are **deferred**; adding a channel
   later is **data only** (FR15).
5. **Deeds may be shared with the reveal spine.** A single milestone may both advance the spine and
   unlock a token; the two systems read the same facts and do not conflict.
6. **No currency purchase in this brief.** Unlocks are **earned by deeds, not bought** with an
   in-run or meta currency. (The in-run coin economy is the companion brief; if a currency-fed
   "donation" unlock tail is added later it must still obey the never-guarantee cap.)

### The dig consumer + direction bias
7. **The hub dig is the proven consumer.** The start-of-run dig (O1) already draws from the
   tasted-forms pool; this brief makes that pool the **unlocked vocabulary** (base + earned tokens)
   and adds the direction-bias below. The dig's **floor holds** — it always offers a viable body
   (bare launch remains allowed).
8. **Direction bias.** The **pursued direction** is a **sliding-window tally over the parts the hero
   has installed across the last few runs** (a small window so direction can **shift** with the
   player's choices — not a lifetime average), aggregated on **two existing data axes**: the part's
   **race marker / belonging** and the socketed artifact's **function family**. (**Not** damage/effect
   type — S5 parked.) The matching direction **raises the weight** of matching tokens in the dig (and
   any biased world draw), **capped** so it can raise odds and the floor but **never reach certainty**.
   The **window length** and the **two axes' relative weight** are config dials.
9. **Hard never-guarantee ceiling.** The bias weight is **clamped below a configured ceiling < 100%**.
   No configuration — however extreme — can make the dig deterministically yield a specific token.
10. **Dilution is real.** Once unlocked, a token shares the draw with the rest of the vocabulary; a
    larger unlocked set makes any single token **less likely** absent a bias toward it.

### Pacing & stratification
11. **Front-loaded pacing.** The rate at which meta-gated tokens become reachable follows an authored
    **pacing curve** — early unlocks come **frequently**, later ones **space out** — so the runway
    stays populated deep (run 100) without starving the start (run 10).
12. **Stratified runway.** Tokens carry a weight/tier such that **small options** unlock **early and
    broadly** and **run-shaping options** (new starting parts, frame families, biomes) unlock **late
    and rarely**. This is per-token data, not a separate system.

### Robustness
13. **Deterministic.** Given the same meta store + the same run seed, the eligible pool, the bias,
    and the dig offering are **identical**. No unseeded randomness in unlock or selection.
14. **Graceful degrade.** An empty or corrupt meta store degrades to **base vocabulary only** (a
    fully playable run) and never crashes; a new/first-ever player plays the open base with an empty
    unlocked set.
15. **Data-authored & additive.** Adding a token, a deed, a gating mark, or a pool is **no code
    change**; a token with **no gating mark defaults to base** (existing content keeps working).
16. **A demonstrator gated set exists.** The MVP ships a **small set of existing content marked
    meta-gated** (e.g. a placeholder frame-changer + a couple of recipes), each behind one of the two
    deed channels, so the engine is exercised against a **non-empty** gating surface and the
    run-N→run-N+1 proof (acceptance) is real. This is a **marking of existing assets**, not new
    content authoring; the deep well of exotic tokens stays the standing designer commitment (below).

## Content authoring rules (for the designer)
- **Mark the base set generously open.** Keep all base multiplicands (the three races' base parts,
  core artifacts/traits, base tiers, the grammar) **base** — never gate the fundamentals. Gate only
  **edges/axes**: frame-changers, new damage/effect types, higher tier ceilings, exotic recipes and
  pools.
- **Author the deed in fiction.** A token's unlock should read as *earned by doing* — tasting the
  form, felling the apex, reaching the register — not as an abstract counter.
- **Stratify.** Give many small tokens **cheap, early** deeds (breadth for run 10) and reserve
  **rare, run-shaping** tokens for **late** deeds (events for run 50/100).
- **Feed the deep well over time.** A generous 100-run runway is a **standing content commitment** —
  the designer keeps authoring exotic tokens; the code must **not cap the runway** or assume a small
  pool. (This authoring is out of scope for the code track — see below.)
- **Never author a guarantee.** No deed or bias may be authored to force a specific piece; belonging
  and direction are always odds-and-floor.

## Tuning surface (the control the owner asked for)
The unlock/progression balance must be **designer-controllable without code** (CLAUDE.md §7).
Existence of these dials is a requirement; **exact values are playtest tuning** (they live in the
system doc's Open/deferred, not here). Two homes:

- **Per-token fields on the content assets** (a token carries its own): **gating mark** (base /
  meta-gated), **deed reference** (what earns it), **unlock weight/tier** (early-broad vs
  late-run-shaping), **dig/world draw weight** once unlocked. New token → brings its own dials, no
  central edit.
- **One master meta-progression tuning asset** (a single SO the designer balances in one place):
  the **pacing curve** shape (front-loading/escalation), the **deed→progress contribution weights**
  (what each kind of deed is worth), the **direction-bias strength and its hard ceiling (< 100%)**,
  the **dig shape** (N offered, the floor rule), and the **global dilution slope**. Sliding this
  asset moves the game from **generous** toward **minimal** gating **without a rebuild** — the "see
  both in action" the owner wanted.

## Acceptance criteria
- A **deed in run N** (e.g. tasting a new form, felling a biome apex) makes a **previously
  unavailable token appear as possible in run N+1** — the demo-observable proof of the loop.
- **Run 1 plays deep** with an empty unlocked set (the open base grammar is fully craftable); no
  content the first run needs is meta-gated.
- **No run ever starts mechanically stronger** than another from meta-progression — no persistent
  stat/ability/build carries across death; only the **possibility pool and its odds** change.
- **Pursuing a direction** raises how often it is offered, but **no setting** makes the dig yield a
  **specific** token with certainty (the ceiling clamps).
- **A larger unlocked vocabulary dilutes** any single token's odds (expansion is not a shortcut to a
  guaranteed build).
- **Same meta store + same seed → identical** eligible pool, bias, and dig.
- **Empty/corrupt meta store → a playable base-only run**, no crash.
- **Adding a token / deed / pool is data-only**; an unmarked token defaults to base.
- The **whole curve is tunable from the config** (pacing, deed weights, bias + ceiling, dig shape,
  dilution) with **no code change**, and can be slid generous↔minimal.

## Out of scope / open points (do not build now)
- **The in-run currency economy** — coins, shops/vendors, the **P1-12** reward sinks
  (Currency/XP/Ability), the camp **dark-currency** — is the **separate companion brief** (Track R's
  run-economy half). This brief is meta-only; it introduces **no spendable currency**.
- **Devil-deal / temptation-as-economy** — paying body/HP/a marker for monstrous power — is a
  **parked separate thread**: it reopens the deliberately-parked corruption/thrall-meter
  (`overview.md` §3), so it is **not** part of R MVP.
- **The Tasted-Forms bestiary UI (Track T)** — the **readout** that makes the runway visible — is a
  **companion**, not built here; this brief only **grows and reads** the catalog it surfaces.
- **World-loot / quest-reward rebalancing** — the vocabulary is exposed to every eligibility-aware
  draw as data, but **retuning the world loot/quest economy** around gated tokens is downstream
  tuning, not this brief; the **hub dig** is the MVP's wired consumer + bias home.
- **Downstream consumers** — **Track Y (Heat)** difficulty/reward modifiers and the **Order's Seat**
  run apex read this spine later; not built here.
- **Hub meta-progression breadth** — recurring cast, a hub you visibly upgrade, digs beyond N=3,
  multiple dig kinds — stay deferred (Track O follow-ups); this brief adds only the **vocabulary +
  bias + config** behind the existing dig.
- **The content itself** — authoring the deep well of exotic tokens is **standing designer work**,
  not a code deliverable; the code builds the **seams and dials**, not the tokens.
- **Exact tuning values** — N, the bias ceiling, the pacing curve, deed weights, dilution slope —
  are **playtest** (system-doc Open/deferred), not fixed here.
