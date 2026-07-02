# Crafting & Mutation — Socketed Blanks — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-02
> Level: product-owner (what & feel). The code track owns the technical "how".
> Design background (fuller intent): `design/crafting/model.md`; world tie: `design/world/overview.md` §4–§5.

## Goal

Replace the current "feed the cauldron a pile of artifacts and hope the mutation lands" loop with a
tactile loop where the player **builds the body part**. A body part is a **blank with sockets**; the
player slots **crafted artifacts** into it and **unseals** it into a mutation. The feeling we want is
**a testable theory with a payoff**: *"I think this combination grows a poison club-tail — let's see
if it works."*

This also settles a long-standing content problem: an artifact should not carry a "which race" tag
(why would a meteorite be "reptile-flavoured"?). Under this model **form/species lives on the blank**
(a lizard tail reads as lizardfolk) and **function lives in the artifacts** (a mace, a stinger). A
lizard tail with a mace and a stinger socketed in becomes *a lizard tail that clubs and poisons* —
nobody tagged the meteorite.

This is a **model change**: it changes *how a mutation is obtained and how crafting steers it*, and
it **retires the existing feeding/digestion flow**. It does not change what a mutation, once
installed, does in combat.

## User stories

- As a player, I **craft the organ**: I fit crafted artifacts into a body-part blank and it ripens
  into a mutation, instead of throwing everything into a pot and hoping.
- As a player, I **form a theory** from what I socket (a heavy, venomous mix → I expect a poison
  bludgeon-tail) and feel the **anticipation of whether it pans out** when the blank unseals.
- As a player, when a blank ripens it offers me a **small menu of variant mutations** to choose from,
  not a single fixed result — and I don't know the full menu until it opens.
- As a player, I **combine artifacts** because the interesting results are only reachable by fusing
  (a raw stick and a raw needle do little; a crafted mace or stinger is a real step) — combining is
  the power move, not busywork.
- As a player, I **incubate several organs at once** and feel the tension of spending a rare reagent
  on one blank's theory versus another's.
- As a player, I read what an artifact is **from its name and look** (a fang is sharp, a stinger
  sharp and toxic) rather than from a stats panel, and the **cauldron's voice hints at the trend** as
  I socket.
- As a designer, I add new blanks, artifacts, and fusion results **as data**, with no code change.

## Functional requirements

### Two item categories
1. **Part-Blanks** (skull, tail, wings, arms, legs, …) carry **form + a species/passport marker** and
   act as a **socketed recipe**: they fix **which organ** (a tail-blank grows a tail) and expose a
   number of **sockets**. They come from the world as naturalistic loot (monster remains, finds,
   relics, meteor-fall exotica).
2. **Artifacts** carry **function only** — *what they are made of* and *what they do* — plus a
   **tier/potency**, and **no species tag**. Quality is a gradient: **raw** finds are weak and
   diffuse; **crafted** fusions are strong and targeted.

### Two stations
3. **The cauldron** fuses artifacts (raw finds → crafted artifacts). This is the "interesting
   combinations" layer (e.g. meteorite + stick → mace; bee + needle → stinger).
4. **The operating table** is a **separate** surface where the player sockets artifacts into a blank
   and unseals it. Blanks are **not** mixed into the cauldron's artifact inventory, so the loop never
   collapses back into "toss everything in the pot".

### The artifact-fusion layer (cauldron)
5. **No failure.** Every combine yields something: a rare **signature** result if the exact inputs
   match a known recipe, otherwise an **emergent** result derived from the inputs' function. The
   player reasons by *meaning*, never by memorising pairs.

### The socket → unseal layer (operating table)
6. **Socketing.** The player places artifacts into a blank's sockets and may **rearrange freely**
   while it is unsealed-in-waiting. Raw finds are allowed (weak); crafted artifacts are the strong,
   targeted option — this is the *pull to combine*.
7. **Ripen threshold.** Filling enough of / the right sockets crosses the blank's threshold and lets
   it be unsealed.
8. **Unseal → a menu → pick.** Unsealing offers a **small menu of candidate mutations** (variants
   with different abilities/enhancements); the player **picks one**.
9. **Commit on unseal.** Unsealing is the **point of no return**: the socketed artifacts are consumed
   and the unchosen variants are lost. (Rearranging before unseal is free; unsealing is the risk.)
10. **Install.** The chosen mutation becomes an installable body part through the existing mutation
    install path.

### Where the puzzle lives (the anti-recipe-table)
11. **Direction is readable; sufficiency and the exact menu are hidden.** The socketed function makes
    the *trend* legible (a theory can be formed and tested), but whether the threshold is crossed and
    exactly which variants appear are only revealed at **unseal**. It must **not** degrade into a
    transparent 1:1 lookup ("mace + poison = poison-mace-tail, always") — that is the recipe table we
    reject.
12. **Sockets interact.** Multiple artifacts can produce an **emergent third property** (not a plain
    sum), so results are reasoned by meaning rather than memorised.
13. **The blank fixes type, not ability.** The blank honestly determines the organ and slot count;
    the *ability* is shaped by the reagents. (The same reagents are **not** silently re-routed to a
    different organ depending on the blank.)

### The scarce Blank Rack (multi-track tension)
14. Blanks are held in a **limited rack** — a small cap on how many organs can be incubated at once.
    That cap **is** the parallel-work tension: several blanks ripening, a scarce reagent, and the
    decision of which theory to feed.

### Legibility & the cauldron voice
15. Artifact traits are **hidden from any stats panel** and read from the fiction; an artifact's name
    and look **must telegraph** its traits. Only the coarse axes are surfaced visually — **archetype
    by colour, tier by glow intensity**.
16. As the player sockets, the **cauldron's voice** telegraphs the *trend* ("turning heavy and
    venomous, are we…") but never spells out the exact menu.

### Passport consistency
17. Socketing function **does not** retag species. The **blank** carries the racial marker; once
    installed, the *part on the body* is what races read (`design/world/overview.md` §5). "A frog with
    lasers" emerges from a frog blank + beam artifacts, not from tagging an artifact "frog".

### Migration (retire the old loop)
18. The existing **feed-the-cauldron / digestion / stage-up** mutation flow is **replaced** by this
    model: mutations now come from socketing and unsealing a blank, not from feeding artifacts to a
    per-stage tally. The old feeding interaction is removed from the player experience. (The technical
    de-scoping of the shipped feeding/tally/digestion/scoring pieces is tracked on the ROADMAP.)

## Content authoring rules (for the designer)
- **Add a Part-Blank** as data: its organ/slot, its species/passport marker, its socket count and
  ripen threshold.
- **Add an artifact** as data: its function traits (substance + property) and tier — with a name and
  look that telegraph those traits (binding).
- **Add a fusion result** as data: a rare **signature** recipe (exact inputs → a named result) or
  rely on the **emergent** rule for everything else — never author an exhaustive pair table.
- **Tune** socket counts / thresholds / variant-menu size and the blank-rack cap as data to balance
  the puzzle and the multi-track pressure.

## Acceptance criteria
- The player can **craft artifacts** in the cauldron (raw finds → a stronger crafted artifact) and,
  on a **separate** surface, **socket** artifacts into a blank and **unseal** it into a mutation.
- Unsealing offers a **menu of variant mutations**; picking one **consumes** the socketed artifacts
  and discards the rest (commit-on-unseal); the chosen part installs on the body.
- The **same socketed set does not always yield one fixed result** — variants and slot-interaction
  are observable; it does not read as a memorisable lookup table.
- **Raw-only** socketing produces weak results; **crafted** artifacts visibly steer harder — the
  player has a reason to combine.
- **Several blanks** can be in progress at once, bounded by the blank-rack cap.
- No stats panel exposes traits; **archetype colour + tier glow** are the only surfaced axes, and the
  **cauldron voice** hints at the trend while socketing.
- The old **feeding/digestion** interaction is gone; mutations are obtained only through blanks.
- **New blanks, artifacts, and fusion results are addable as data**, no code change.

## Out of scope / open points (do not build now)
- **Cauldron-will stochastic surprise** — volatile/high-tier inputs adding a readable, non-griefing
  twist — stays deferred (as with the corruption meter). The base is deterministic-discoverable.
- **Mutation-card and quest-card visual design** (the unseal variant cards, tier-glow/belonging
  treatment) are their **own** briefs; this brief defines the loop and the data, not the card art.
- **Whether deep grotesque function strains a blank's passport marker** (`world/overview.md` §5's
  "exposure flips trust") — open, later.
- **Optional inspect UI** revealing traits for completionists — not in the main flow.
- **Biome ↔ archetype/blank bias** (a biome nudging which blanks/artifacts drop) — a separate loot
  item, not part of this brief.
- Implementation specifics (data shapes, structure, which shipped pieces are removed vs. repurposed)
  are the code track's call — see the ROADMAP migration items.
