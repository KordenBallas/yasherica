# Race Roster & Passport Model — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-04
> Level: product-owner (what & feel). The code track owns the technical "how".
> Fuller design background — do not restate it: `design/narrative/races.md` (the roster, per-race
> facets, marker parts, mundane-absurd problems) and `design/world/overview.md` §5 (the passport
> model, refined here). Related: `design/needs-code.md` (2026-07-04 passport = part-count tier).
> This is the **content gate** referenced by the roadmap item **P0-1**; downstream work
> (per-race body-plans/marker parts, per-race NPC/quest/enemy content, biome selection along the
> run) builds on it.

## Goal

Give the game its **starting races as data** and turn "the world reads what you are" into a built,
data-driven rule. Today there are no races and "passing as a people" is at most a single hand-set
flag. We add: (1) a small **roster of starting races**, each tied to a biome and a belonging colour;
(2) a **race tag on every body part**; and (3) a **passport rule** — a race accepts you by **how many
of its parts you wear** — exposed so the narrative can gate on it.

This changes **who the world's peoples are** and **how they read the player's body**. It does **not**
author the marker part meshes, the per-race questlines/NPCs/enemies, or which biome appears when —
those are separate, downstream (see out-of-scope).

## The three starting races

Three races, one per existing biome, framed as **three estates of the ruling Order** (fuller lore in
`design/narrative/races.md`). Each carries a **belonging colour** (reused by the quest/mutation card
grammar — archetype/belonging = hue) and a set of **signature marker parts** named by silhouette.

| Race | Biome (`LevelTheme`) | Estate / register | Signature marker parts (any counts) | Combat flavor |
|---|---|---|---|---|
| **Ibex-folk** | Mountain | Nobility — birth-rank = horns; honor-feuds | horns (head), hooves (legs), highland coat (skin) | horn-charge / ram (displacement) |
| **Lizard-folk** | Desert | Clergy / sun-inquisition that hunts "becoming"; carries the run's rising danger | scales (skin), frill-crest (head), long tail (tail), claws (arms) | tail-sweep (area push) |
| **Fox-folk** | Forest | Underworld / merchants who fake becoming (the hero's foil); comic heartland | brush-tail (tail), pointed ears + muzzle (head), russet fur (skin) | feint-dash + bite |

- The **hero is kindless** — a Junkyard mongrel with **no race** (all his starting parts are tagged
  *kindless*), so he begins a stranger to all three peoples and earns passage one part at a time.
- The **Cave** biome intentionally has **no starting race** yet.

## User stories

- As a player, wearing a people's body parts makes them **treat me differently**: none → I'm a shut-out
  stranger; one → I'm let in but eyed as "one of us, but a freak"; two or more → I'm accepted as kin.
- As a player, I feel a **real trade-off**: covering my body in one race's markers to be respected
  costs me the slots I'd spend on my strongest parts, and mixing many races' parts makes me a
  **tolerated outsider everywhere, kin nowhere**.
- As a designer, the world's **peoples are data**: a race carries its biome, belonging colour, and
  which parts are its markers; the **narrative can gate a line/quest on "reads as race X at tier N"**.
- As a designer, **adding or editing a race** (and tagging parts to it) is **data authoring, no code**.

## Functional requirements

### Races as data
1. **A starting roster of three races** exists as data — **Ibex**, **Lizard**, **Fox** — each holding at
   least: its **home biome** (`LevelTheme`), a **belonging colour** (for the card grammar), and a
   **display name**. Authored per race, mirroring the existing one-asset-per-theme content pattern.
2. **Every body part carries a race tag** — one of the roster races, or **kindless**. The hero's
   starting parts are **kindless**. (A part's race is independent of what ability it grants.)
3. **Extensible.** Adding a **new race** (a new race asset) or **tagging a part** to a race is data
   authoring — **no code change**. The roster is not hard-coded to three.

### The passport — acceptance by part count
4. **Acceptance tier per race = the count of that race's tagged parts currently equipped**, clamped to
   three tiers: **0 = outsider**, **1 = tolerated (“one of us, but a freak”)**, **2+ = kin**. A marker
   is **any** part tagged to the race (not one special slot).
5. **No hard conflict between races.** Parts do **not** forbid each other — the player may wear a fox
   tail **and** lizard scales at once (a tier-1 tolerated freak to both). The trade-off is the **finite
   body-slot budget**, not parts excluding one another.
6. **Expose the tier as a fact** the narrative director / dialogue can gate on — a **per-race
   count/tier**, not a single boolean (e.g. a line requires "reads as Ibex ≥ 2"). This replaces the
   old single passport flag.
7. **The tier updates as the body changes** — installing/removing a race-tagged part moves the tier
   (and the fact) accordingly, before the next encounter reads it.

### Robustness
8. **Determinism.** Race data and the derived acceptance tier are deterministic — the same equipped
   body yields the same per-race tiers/facts every time.

## Content authoring rules (for the designer)
- Author **one asset per race** holding: home biome, belonging colour, display name (and room for the
  marker-part list / later fields). Keep it additive so new fields don't break existing races.
- **Tag each body part** with its race (or *kindless*). Any tagged part counts toward that race's tier.
- Keep the roster and tags **data only** — a new race or a re-tagged part needs no code.

## Acceptance criteria
- The three races exist as data (biome + belonging colour + name); the hero's starting parts read as
  **kindless** (tier 0 with every race).
- Wearing **0 / 1 / 2** parts of a race yields **outsider / tolerated / kin**, and a **narrative
  precondition can gate on the per-race tier** (a demo line/quest that only opens at tier ≥ 1 or ≥ 2).
- Wearing parts of **two** races makes the player **tier-1 to both** at once (no mutual exclusion).
- **Adding a new race** and **re-tagging a part** are **data-only**; **same body → same tiers**.

## Out of scope / open points (do not build now)
- **The marker part meshes / per-race body-plans / silhouette sheets** — authored on the code+art side
  (see `product-requirements/part-authoring.md` and the independent body-plans work); this brief only
  needs parts to **carry a race tag**.
- **Per-race questlines, NPC casts, and signature enemies** — the next content step, hangs off this
  brief (`design/narrative/races.md` §6).
- **Exposure / betrayal** (deeper mutation flipping trust to horror) — named in the design; its
  trigger rules are not built here.
- **New combat displacement kinds** (ram/tail-sweep/dash beyond today's push) — the combat flavor
  column is direction only; the abilities ride the existing part-granted ability system and any new
  displacement kind is a separate combat item.
- **Which biome/race appears when along a run** (biome selection, race homelands ordering) — a
  generation/progression concern (the separate biome-selection item).
- **A fourth (Cave/underclass) race** — deferred; the roster is built to accept it later.
- **Cross-run persistence** of any race/passport state — rides the general save/load work, not here.
