# In-Run Currency & Shops — Coins You Earn and Spend Within a Run — Product Requirements

> Status: **Verified** (the five forks were discussed and **owner-confirmed 2026-07-07**; ready for
> the code track) · 2026-07-07
> Level: product-owner (what & feel). The code track owns the technical "how".
> Model: **Opus / Sonnet** (a simple within-run economy — deliberately **not** Fable work).
> **Track R — the run-economy companion** to `product-requirements/meta-progression-spine.md` (the
> meta half). The two are **orthogonal**: the meta spine unlocks a *possibility vocabulary* by deeds
> (no currency); this brief adds a *within-run wallet* you earn and spend and **lose on death**.
> Background — do not restate it: `design/vision.md` (anti-grind; Pillar 1 "no correct build";
> Pillar 3 crafting depth), `design/narrative/quest-as-reward.md` ("different currency, not more" —
> the moral fork is balanced on facts/threads/parts, **never** loot-EV), `design/world/content-kinds.md`
> (the `Loot·market` / merchant beats this rides), `design/narrative/hub-junkyard.md` ("direction +
> floor, not a vending machine"). **Reconciles:** `product-requirements/camp-shady-offer.md` (its
> "power/combat currency" is **defined here**) and **P1-12** (the deferred Currency reward sink —
> this is the Currency half; Experience/Ability stay deferred). **Save:** the run image already lists
> "currency" (`product-requirements/save-continue-run.md`) — it persists within a run's Continue and
> is **consumed on death**.

## Goal

Give a run a **simple, spendable currency** — coins you find and earn while playing and spend at
**in-world vendors**, gone when you die. This is the missing **sink layer** (P1-12) and the concrete
backing for the camp's "power currency". It stays **within a run** (permadeath), stays **off the
identity-fork economy** (quests still pay in parts/access/facts, not coins), and buys **reagents to
craft with**, never a finished strong body — so it feeds Pillar 3 without becoming the vending
machine we rejected.

## The model (product-owner level) — forks owner-confirmed 2026-07-07

- **One neutral currency, not two.** A single **coin** wallet, run-scoped. The moral fork's
  "different currency, not more" stays expressed in **facts / threads / parts** (dark = power-leaning
  goods, moral = access/doors) — **not** in a second "dark" wallet. The camp shady-offer's
  "power/combat currency" is realised as **coins + a power-leaning payout**, the *dark* flavour in the
  fiction and what's on offer, not a separate purse. *(Rejected for MVP: a distinct Conquest/dark
  currency — it reopens the parked temptation-as-economy / devil-deal thread.)*
- **You buy ingredients, not power.** The primary sink is an **in-world vendor** that sells
  **artifacts, raw primitives, and occasionally a Part-Blank** — the **inputs** to crafting, priced by
  tier. You buy *what to brew*, then still build the organ yourself (Pillar 3). This is why a shop does
  **not** break "direction + floor, not vending": coins buy reagents and rerolls, never a finished
  strong part on demand.
- **Coins come from the world, not from quests.** Sources: **coin caches in the world** (a
  `Loot·market`/money flavor over the existing loot content-kind), **small drops from ambient
  combat** (a separate trickle, distinct from the "you are what you eat" corpse-loot channel), and
  **selling** unwanted artifacts/blanks back to the vendor. **Quests are deliberately not a primary
  coin source** — keeping the reward economy on parts/access preserves "different currency, not more".
- **In-world, never the hub.** The shop is a **run-time merchant beat** (placed by the existing
  content/site system — a market flavor / merchant NPC), **not** a hub store: currency is lost on
  death, so a pre-run hub shop would have nothing to spend. The hub stays "direction + floor".
- **Permadeath.** Coins live in the run's Continue image (already anticipated by save/load) and are
  **consumed on death** with the rest of the run — no carry-over. (Any future run→meta "donation"
  tail is **out of scope** and would have to obey the meta-spine's never-guarantee cap.)
- **A meaningful spend choice, not a grind.** Following Isaac: the currency is simple, but *what* you
  spend on is the interesting decision (this artifact vs saving for a Blank vs a reroll vs a heal).
  Amounts stay low and **hand-tunable from config** — this is not an idle-game number treadmill.

## User stories

- As a player, I **find coins** in the world and off the odd fight, and I **spend them at a merchant**
  I come across — on **things to brew with**, a reroll, or a patch-up — a small, welcome economy.
- As a player, the shop sells me **ingredients and the occasional blank**, never a finished
  power-part — I still have to **build** the strong thing myself.
- As a player, when I **die, my coins die with me** — the run is one mortal beast; nothing banks.
- As a player, the **camp's shady job** pays me in that same coin **plus** power-leaning goods — the
  "dark currency" I heard about is real, and it's just money earned the ugly way.
- As a player, my **quest choices** are still about **who I side with and what I become**, not about
  which option pays more coins.
- As a designer, I **place a merchant like any other content beat** and **price goods by tier from
  config**; coins, drop rates, and prices are **data**, no code.

## Functional requirements

### The currency
1. **A single run-scoped coin wallet.** One integer currency, held in run state, shown to the player,
   **consumed on death** with the run (persists across a Continue, not across a death).
2. **No second currency in MVP.** The dark/power "currency" is the same coin; power vs access stays a
   distinction of **goods and facts**, not wallets.

### Sources
3. **World coin caches.** Coins can be placed as a **loot flavor** (money cache) via the existing loot
   content kind — deterministic per seed, low/tunable frequency, consistent with the breathing-world
   density (they are loot, not a new content kind).
4. **Ambient-combat trickle.** A fight may drop a **small** coin amount — a **separate channel** from
   corpse-loot / "you are what you eat" (removal/drop of the part loot must not be replaced by coins).
   Tunable, may be zero for some creatures.
5. **Selling.** The vendor **buys back** artifacts/blanks/primitives the player no longer wants, at a
   configured fraction of buy price.
6. **Quests are not a primary source.** Quest rewards remain **parts / access / facts** (the rolled
   tier+belonging economy); a quest may incidentally include a little coin, but the identity-fork
   offers are **never** balanced on coin amount.

### Sinks (the vendor)
7. **An in-world merchant beat.** A vendor is placed by the existing content/site system (a
   market/merchant flavor), reachable in a run — **not** at the hub. Interacting opens a **buy/sell**
   surface.
8. **Sells crafting inputs.** Stock = **artifacts, raw primitives, and (rarely) a Part-Blank**, drawn
   from tier-appropriate pools, **priced by tier** (higher tier = pricier, rarer stock). The stock is
   deterministic per seed.
9. **Buys reagents, not finished power.** The vendor never sells a **finished, socketed, unsealed
   organ**; it sells the **inputs**, so spending still routes through crafting (Pillar 3). It respects
   the meta-spine's gating — a **meta-gated** artifact/blank is **not** in stock until unlocked.
10. **Optional secondary sinks (tunable, may be off).** A **reroll** of the vendor stock and a
    **patch-up / heal** service, each priced from config. **No** sink that hands raw build-power
    directly (no "buy a socket", no "buy a marker") in MVP.
11. **XP / Ability sinks stay deferred.** This brief is the **Currency** half of P1-12 only; there is
    no experience/level system and abilities come from parts — **Experience and Ability sinks remain
    deferred** (no new leveling pillar).

### Shady-offer reconciliation
12. **The camp offer pays coins + power-leaning goods.** `camp-shady-offer.md`'s "power/combat
    currency" is realised as a coin payout plus a power-leaning reward; the "shady" read stays in the
    fiction + the cauldron bark, not a separate purse. No change to that brief's placement/derivation.

### Robustness
13. **Deterministic.** Same seed → same caches, drops, stock, and prices.
14. **Data-authored & additive.** Coin drop rates, cache frequency, price bands, buy-back fraction,
    service prices, and stock pools are **config data**; adding a priced good or a merchant is no code.
15. **Save-safe.** Coins ride the run Continue image and are consumed on death (save/load already
    anticipates a currency field); a corrupt save degrades per the shipped save fail-safe.

## Content / authoring rules (for the designer)
- **Keep amounts small and choices sharp.** Tune coins so a purchase is a **real trade-off**, not a
  number that trivially accumulates; the fun is *what* you buy, not *how much* you farm.
- **Vendor sells inputs.** Stock reagents and the occasional blank — **never** a finished strong
  organ. Let the power still come from **building** it.
- **Don't pay identity forks in coin.** Keep quest/moral-fork rewards on **parts/access/facts**; coin
  is world-loot and shady work, so it never flattens "different currency, not more".
- **Price by tier** and let stock respect meta-gating (unlocked vocabulary only).

## Tuning surface
One **currency/economy config asset** exposes: **coin-cache frequency**, **ambient-combat drop
range**, **buy-back fraction**, **vendor price bands by tier**, **stock size / tier mix / blank
rarity**, **reroll & heal prices** (and on/off). Per-good price overrides live on the good's asset.
Existence of the dials is the requirement; exact values are **playtest** (system-doc Open/deferred).

## Acceptance criteria
- The player can **earn coins** (world caches + ambient drops + selling) and **spend them at an
  in-world vendor** on **crafting inputs**, within a single run.
- **Death consumes the coins** with the run; nothing carries to the next run.
- The vendor **never sells a finished power-part** — only inputs (Pillar 3 preserved); **meta-gated**
  goods are absent from stock until unlocked.
- **Quests still pay parts/access/facts**, not coins; the identity fork is unchanged.
- The **camp shady-offer** now pays a defined coin amount + power-leaning goods.
- **Same seed → same** caches, drops, stock, and prices; adding a good/merchant is **data only**.
- **No** experience/ability/second-currency system is introduced.

## Out of scope / open points (do not build now)
- **A second / "dark" currency and devil-deal sinks** — a distinct Conquest currency and pay-with-
  body power buys are **parked** (they reopen the corruption/temptation-meter thread, `overview.md` §3).
- **Experience / Ability reward sinks (rest of P1-12)** — deferred; no leveling pillar; abilities
  come from parts.
- **A run→meta "donation" tail** — feeding coins into the junkyard's meta dig-bias is a **later**
  option and must obey the meta-spine's **never-guarantee cap**; not built here.
- **Hub / pre-run shop** — excluded by design (currency is run-scoped, lost on death).
- **Power-buying services** — buying sockets, markers, or finished organs; excluded (Pillar 1).
- **Deep vendor systems** — haggling, restocking economies, reputation pricing, black markets —
  beyond the simple buy/sell MVP.
- **Exact tuning values** — coin amounts, prices, drop rates, stock sizes — are **playtest**, not
  fixed here.
