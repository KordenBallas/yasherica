# World Content Density — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-06-29
> Level: product-owner (what & feel). The code track owns the technical "how".

## Goal

Stop the Run Director from flooding the world with quests. Today the world is effectively
**binary** — a platform is either an NPC/quest or empty, and the empties are leftover gaps rather
than deliberate space — so the director fills run after run with back-to-back quests and the player
never meets the other content a run should be made of. The result reads as an overloaded quest
board, not a world to explore.

We replace that with a **rare, breathing world** the hero crosses quickly. Most of the journey is
open traversal; it is dotted with **simple loot artifacts**, **ambient aggressive monsters** to
fight, and the **occasional NPC quest** that lands as a *prize* (reinforcing the accepted
"quest offer = rare card"). The whole mix is tunable by the designer from data.

This changes **what fills the world and how densely** — not what a dialogue, a quest, or a battle
does once it starts.

## User stories

- As a player, I cross long, quiet stretches of world and a quest appearing feels like an **event**,
  not the default — the world is not a wall of NPCs.
- As a player, I run into **aggressive monsters in the landscape** that I can fight on the spot, with
  no quest or conversation attached.
- As a player, I find **simple artifacts** scattered around as I travel — ordinary finds, distinct
  from the prize at the end of a quest.
- As a player, the empty space between beats gives the run a **traversal rhythm** and lets each find
  or fight land, instead of feeling like dead filler.
- As a designer, I control the **fullness of the world** — how rare quests are, how many monsters,
  how much loot, how much empty space — from a single config, with no code change, and the same run
  seed always produces the same world.

## Functional requirements

### The world's content vocabulary
The world is built from **four** kinds of content, not two:
1. **Traversal / empty space** — the majority of platforms. Deliberate breath and movement rhythm,
   the home of the fast platform-hopping feel. **Not** leftover padding.
2. **Simple loot** — low-value, naturalistic finds scattered in the world (the "you are what you
   eat" feeder). Strong artifacts are *earned* elsewhere, not scattered ("direction + floor, not
   vending").
3. **Ambient aggressive monsters** — standalone fights in the landscape (the "fast chess" loop and a
   source of corpse-loot), with **no** quest or dialogue attached.
4. **NPC quests** — rare. A quest appearing is the exception, not the default.

### Density & rarity
5. **Quests are rare.** A run must have long stretches with no quest at all; quests must not cluster
   back-to-back. Starting dial (tunable): on the order of **~1 quest per ~10 platforms**.
6. **Monsters are ambient and separate from quests.** Aggressive monsters appear as their own
   content, independent of any quest, and are the **main** source of combat. A quest *may* still
   occasionally carry a fight, but that is the exception.
7. **Simple loot is present and stays "simple".** Loot platforms carry **low-tier** artifacts — a
   regular but unremarkable find, distinct from quest rewards.
8. **Empty space is real, budgeted breath** — a deliberate share of the world sized to give traversal
   rhythm and let a beat land, never just "whatever platforms were left over".
9. **Ambient monster difficulty is flat for now** — monsters are drawn from a per-biome pool with no
   progression/escalation scaling yet (escalation is deferred).

### Designer control (data-driven)
10. The full mix — quest rarity, monster count, loot amount, empty-space share, and how far apart
    quests must sit — is governed by **ScriptableObject configuration**. Changing the config visibly
    changes the felt fullness of the world, with **no code change**.
11. Content selection stays **reproducible**: the same run seed produces the same world layout and
    content mix (the existing determinism guarantee holds).

## Content authoring rules (for the designer)
- One config asset governs world fullness: per-stretch budgets for **quests** (rare), **ambient
  monsters**, **simple loot**, and **empty/traversal space**, plus the minimum spacing between
  quests.
- Ambient monsters are drawn from a **per-biome pool** the designer maintains as data (flat
  difficulty — no progression scaling).
- Simple loot draws from the existing biome loot tables at the **low/simple** end, kept distinct from
  quest-reward loot.

## Acceptance criteria
- Walking a representative stretch (~20+ platforms), the observed mix matches the configured rarity:
  quests are clearly rare, with ambient monsters, simple loot, and genuine empty/traversal space
  between beats — **never** a wall of back-to-back quests.
- **Ambient monsters** appear and are fightable with **no** quest or dialogue attached.
- **Loot platforms** yield simple/low-tier artifacts, distinct from quest rewards.
- **Editing the SO density config** visibly changes the world's fullness (e.g. dialling monsters up
  or quests down) with no code change.
- **Same seed → same world.**

## Out of scope / open points (do not build now)
- **Content drives size & landscape** — content declaring a *setting scale* so a camp gets a camp
  footprint and a city spans several platforms reading as one place (vision §4), and varied platform
  sizes/textures, is a **separate, larger design pass** (footprint vocabulary, biomes, races). This
  brief is density + the content vocabulary only.
- **Monster difficulty escalation** with run progression/altitude (can ride director D19 later).
- **No "kill N monsters" quest grind** — ambient combat is not a counted quest objective (cut design).
- **No change** to the crafting/mutation loot economy or the quest-reward economy — corpse-loot stays
  on its own channel.
- Implementation specifics (algorithms, structure) are the code track's call, not this brief.
