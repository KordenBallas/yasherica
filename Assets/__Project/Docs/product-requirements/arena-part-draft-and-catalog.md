# Arena Mode — Part Draft & the Tasted-Forms Catalog — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Builds on: `arena-mode-mvp.md` (this replaces its deferred **default-hero** step with a
> character layer). Related: `vision.md` Pillar 4. Forward-looking background:
> `/design/arena-mode.md` (conflict-1 resolution).

## Goal

Give Arena a **hero-assembly layer** in place of the MVP's single default hero, **without**
importing the campaign's progression, economy, or the "one mortal beast" fiction it would break.

Instead of picking a pre-built character or a frozen run-snapshot, each Arena match opens with a
**parts draft**: players take turns picking **individual body parts** off a **shared board** and
**assemble a monster** for the fight. The board is stocked from a **common floor** of baseline
parts plus the parts the players have **encountered while playing Journey** — so your own
discoveries show up on the table, but everyone drafts the **same** board that match, and no one is
ever left without a viable body.

This stays a **secondary combat mode** on Pillar 4. It deliberately does **not** add a currency,
an unlock grind, ranking, or any power that carries back into Journey. The one intentional,
**bounded** coupling: Arena may **read** which parts you have met in Journey; that catalog grows as
a **passive side effect of normal play**, never as a farm target.

## Why this shape (the resolved conflict — do not relitigate)

- **A "deck of run-snapshots" is rejected.** Freezing a completed run's final body as a reusable
  roster entry breaks the Hades frame (a run is *one mortal beast* that death reforms at the
  Junkyard — there is no stable of champions) **and** risks PvP snapshots dictating PvE build
  choices ("grind the run for the arena"). See `/design/arena-mode.md`, `decisions.md` 2026-07-03.
- **A catalog of *parts* is on-theme, not a fiction break.** The framing is **the cauldron
  remembers every form it has tasted** — a memory of forms, not a collection of heroes. This fits
  "the spirit of becoming" directly.
- **The Journey→Arena coupling is deliberately relaxed, but fenced.** Arena *reads* the part
  catalog; it must not grow its own economy. Guardrail: **passive unlock only** (no currency, no
  achievement gate), **parts are tactical sidegrades** (a bigger catalog = more *variety*, not more
  *power*), and a **common floor** guarantees a viable body regardless of catalog size.

## User stories

- As a player, when I start an Arena match, I get a **draft**: I pick body parts one at a time and
  **build the monster** I'll fight with, instead of everyone being the same default hero.
- As a player, I **recognise parts I found in Journey** appearing on the draft board — my own
  discoveries feed the table.
- As a player, drafting is **turn-based and competitive**: if I take a part, **no one else can have
  it this match**, so I weigh grabbing what I want against denying an opponent.
- As a new player with almost nothing unlocked, I can **still draft a complete, viable monster**
  from the baseline parts on the board — I'm never left with an empty or crippled body.
- As a player, my **catalog of parts grows just by playing Journey** — I never grind a separate
  currency or "unlock track" to get arena parts.
- As a player, once everyone has drafted and assembled, the fight runs exactly like the Arena MVP
  (hidden simultaneous commit → simultaneous resolve, last hero standing wins).

## Functional requirements

### The tasted-forms catalog (persistent, meta-scoped)

1. **Passive unlock in Journey.** When the player **encounters** a body part in a run — at minimum
   **installing** it on the hero; a part met in the world may also qualify — that part is added to
   a **permanent catalog** of parts. No currency, action, or achievement is required; it is a
   side effect of play.
2. **The catalog persists across runs** (meta-scoped — it survives death/reform, consistent with
   world-permanence in the Hades frame). Losing a run never removes a catalogued part.
3. **The catalog is read-only for Arena.** It only widens which parts *can* appear on a draft
   board. It confers **no power, stat, or advantage** inside a match and **never** feeds back into
   Journey (it does not change what a run offers, drops, or costs).
4. **A common floor of baseline parts is always available**, independent of any catalog — enough,
   per body slot, for every player to complete a legal, viable monster even with an empty catalog.

### The draft board (shared, per match)

5. **One shared board per match.** When a match's draft begins, the game assembles a **single
   board of parts visible to all players**, drawn from: the **common floor** (req 4) **plus** a
   sample from the **union of the participating players' catalogs**. Everyone drafts the **same**
   board.
6. **The board guarantees viability.** It always contains **enough parts of each body slot** that
   every player can assemble a complete, legal monster — competition is over the *desirable* parts,
   never over basic viability.
7. **Deterministic composition.** Given the same match seed and the same set of participant
   catalogs, the board is composed **identically** every time (preserves the MVP's reproducibility
   guarantee).

### The draft (snake order, with denial)

8. **Turn-based snake draft.** Players pick **one part at a time** in a **snake order** (…P1, P2,
   P3, P3, P2, P1…) so pick order stays fair across rounds.
9. **Denial is real.** A part taken by one player is **removed from the board** — no other player
   can draft it this match. Choosing between taking a part and denying it to an opponent is
   intended play.
10. **Draft to a complete body.** The draft continues until every player has filled the **standard
    Arena body slots** (a fixed slot loadout for the match). Parts are drafted **into** their
    matching slot.
11. **Assemble, then fight.** When drafting ends, each player's picks **form their monster** for the
    match; the fight then proceeds under the **unchanged Arena MVP rules** (hidden simultaneous
    commit → simultaneous resolve; last hero standing wins).

### Balance stance (PO-level; numbers are playtest)

12. **Parts are tactical sidegrades, not a power ladder.** Arena parts are tuned so that a choice
    is about **kit and matchup**, not "strictly better" — a larger catalog broadens options, it
    does **not** raise a power ceiling. (Consistent with Pillar 1 "no single correct build" and the
    hub "direction + floor, not a vending machine" principle.)
13. **Fairness comes from the shared board, not from equal catalogs.** Because everyone drafts the
    same board, differing catalog sizes do not create a per-player power gap; the floor covers the
    thinnest catalog.
14. **This is a friends-fun mode.** Consistent with the MVP (no matchmaking/ranking), strict
    competitive parity is a **soft goal** served by reqs 6/12/13, not a hard tournament guarantee.

## Acceptance criteria

- Starting an Arena match opens a **parts draft** (not a fixed default hero, not a character
  select of pre-built heroes).
- The draft board shows a **common floor** of parts **plus** parts the participants have met in
  Journey; **the same board is presented to every player** in a match.
- A player with an **empty catalog** can still draft a **complete, viable monster** from the floor.
- Drafting is **turn-based**; a **part taken by one player is unavailable to the others** for the
  rest of that match.
- **Same seed + same participant catalogs → identical board and, given identical picks, identical
  bodies.**
- **Playing Journey adds parts to the catalog with no separate currency, unlock action, or grind**;
  the catalog **persists across runs** and **never alters Journey itself**.
- After the draft, the match runs under the **existing Arena MVP round rules** and win condition,
  unchanged.

## Out of scope / open points (do not build now)

- **Frame-changing parts in Arena** (skeleton-swap plans — serpent, spider, etc.): the first draft
  is on the **base body-plan** only. Radical frames in Arena are a later pass.
- **In-match draft variants** beyond snake (auction, blind simultaneous pick, ban phase) and
  **team/round formats** — last-hero-standing FFA holds (MVP).
- **Exact board size, sample weighting, slot loadout, and per-part PvP tuning numbers** — combat/
  content-balance detail owned by the code track and playtest.
- **Any cosmetic-only unlocks, prestige, or ranking on top of the catalog** — the catalog stays a
  pure read of Journey encounters.
- **The Hub scene, matchmaking, lobby, reconnect, spectator polish** — unchanged from the MVP's
  deferred list.
- **Whether Journey "encounter" also counts merely *seeing* a part vs. only installing it** — start
  from **install-at-minimum**; widening the trigger is a tuning decision, not a structural one.
