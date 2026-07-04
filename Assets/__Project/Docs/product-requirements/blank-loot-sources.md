# Part-Blank Loot Sources — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-03
> Level: product-owner (what & feel). The code track owns the technical "how".

## Goal

Give **Part-Blanks** (the socketed body-part items — skull, tail, wings, …) a real **world source**.
Today a blank only enters the rack via the `MutationConfig.StartingBlanks` dev seed, so the core
transformation loop (**find a blank → socket artifacts → unseal → install a mutation**) can't actually
run over a real journey — the player never *earns* new organs in the world.

Blanks now come from the world as **naturalistic, varied loot**, on **two channels**:

1. **Enemy remains (primary).** You are what you eat — kill a creature, it can drop the blank for one of
   its body parts (the dragon's skull drops from the dragon). This is the bulk source and rides the
   existing combat/mutation loot channel, **separate from the quest-reward economy**.
2. **Finds / relics / chests in Landmark sites (rarer, stronger).** Ruins and lairs are the spatial home
   of the strongest, most exotic blanks — earned by exploring a dangerous place, not by grinding fights.

This is a **loot-sourcing** change only. It does **not** change what a blank *is*, how socketing/unsealing
works, or the quest-reward economy.

## User stories

- As a player, when I defeat a creature I can loot **the blank of a body part it had** — beating a
  scaled brute is how I get a scaled tail to build on. The fantasy "eat what you kill, become it" is
  legible.
- As a player, I don't get a blank from **every** kill — a blank drop feels like a **notable** result of
  a fight, not a guaranteed reward.
- As a player, the **rare, powerful** blanks (a dragon skull, an exotic wing) are things I find by
  braving a **ruin or lair**, not something that trickles out of ordinary combat.
- As a player, the blanks I run into **lean toward** where I am and what I'm fighting (a marsh's
  creatures tend to yield marsh-kind blanks) without ever being **guaranteed** a specific piece.
- As a designer, I control **which creatures and which sites drop which blanks, and how often**, from
  data, with no code change, and the same run seed produces the same drops.

## Functional requirements

### Channel A — enemy remains (primary)
1. A defeated creature can drop a **Part-Blank** corresponding to one of its **own body parts**, at the
   fight's location, to the victor — on the **existing combat/mutation loot channel** (the same channel
   as corpse-loot artifacts), **not** the quest-reward channel.
2. Blank drops are **occasional, not guaranteed** — most kills yield no blank; a blank is the exception
   that makes a particular fight memorable.
3. Which blank(s) a creature can drop, and the drop chance, are **authored per creature** (with a
   per-biome fallback pool), so a creature's anatomy and its droppable blanks stay consistent.

### Channel B — landmark finds / relics / chests (rarer, stronger)
4. **Landmark sites** (ruin, lair — the wild, monster-held/unoccupied sites) are the home of **rare,
   higher-tier blanks**, obtained as **finds / relics / chest contents**, independent of any fight.
5. Landmark blanks skew **stronger and more exotic** than the common enemy-remains drops — the payoff
   for entering a dangerous place, consistent with "**direction + floor, not vending**": the site
   raises the odds/tier of a strong blank, it does not hand out a specific guaranteed piece.

### Bias & determinism (both channels)
6. Blank drops may carry a **light bias** toward the local biome/kind (a marsh tends toward marsh-kind
   blanks) — a **tendency, never a guarantee** of any specific blank. The strength of this bias is
   **tunable** and may be dialled to none; deeper per-run "your pursued direction" bias is **deferred**.
7. Blank sourcing is **reproducible**: the same run seed produces the same drops and finds (the existing
   determinism guarantee holds).

## Content authoring rules (for the designer)

- A creature declares its **droppable blank(s)** and their drop chance as data; unspecified creatures
  fall back to a **per-biome blank pool**. Kept distinct from the creature's artifact corpse-loot.
- A **Landmark site type** (ruin, lair, …) declares a **blank find pool** (tier-weighted) it can surface
  as a find/relic/chest, distinct from the ambient-loot and enemy-drop tables.
- Blank pools reference blank **kinds/tiers**, not a hand-picked single asset per location, so the world
  stays non-catalog and procedural.
- Drop rates, site find rates, and the biome-bias strength are **data dials** with no code change.

## Acceptance criteria

- Over a representative run, defeated creatures **occasionally** drop a blank matching one of their body
  parts, on the combat/mutation channel — and **most** kills drop none.
- **Ruin/lair** landmarks yield **rare, higher-tier** blanks as finds/relics/chests, without a fight
  being required, and skewing stronger than enemy-remains drops.
- Blank drops show a **legible lean** toward the local biome/kind but are **never** a guaranteed specific
  piece.
- **Editing the drop/find/bias data** visibly changes what blanks appear and how often, with no code
  change.
- **Same seed → same blanks.**

## Out of scope / open points (do not build now)

- **The socketing / unseal / mutation flow itself** — unchanged (`crafting-mutation-socketed-blanks.md`,
  `mutation-choice-cards.md`).
- **Quest rewards** — a quest reward stays a **rolled artifact** by tier + archetype-bias
  (`quest-offer-card.md`); a quest does **not** become a blank source. The two channels stay separate.
- **Per-run "dig / pursued-direction" meta-bias** toward the player's chosen path across runs — accepted
  in principle but **deferred** (must raise odds/floor, never guarantee).
- **Biome ↔ blank archetype** deep bias tables beyond the light local tendency above — later loot pass.
- Implementation specifics (drop tables, roll structure) are the code track's call, not this brief.
