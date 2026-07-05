# Camp Shady Offer — `NPC·quest-bearer` as a Fill Beat — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Builds on** [World Sites & Landscape](world-sites-and-landscape.md), the shared content
> vocabulary there, [NPC Proximity Interaction](npc-proximity-interaction.md), and the
> reward/fork economy of `design/narrative/quest-as-reward.md`.
> Fuller design background: `design/world/sites-and-landscape.md` §5, `design/world/content-kinds.md`.

## Goal

The site capacity recipes (Sites brief §5) use `NPC·quest-bearer` two ways: as a **Camp** anchor is
`Combat·bandit`, but its **fill** table lists `NPC·quest-bearer` at a low weight — *"a shady offer"*.
This brief pins down the one thing that needs a design decision: **what a quest-bearer means when it
appears as a Camp's fill beat rather than as a settlement's anchor.** A Village quest-bearer is an
honest plea from tired folk; a Camp quest-bearer is an **outlaw's dirty job** — the same content
*kind*, coloured by where it lands. This is the world's **dark-currency quest source** (the Monster /
Conquest road of `world/overview.md` §9), and the one quest source open to a hero who is **nobody's
kin**.

## User stories

- As a player who has walked into a bandit **camp**, sometimes instead of an immediate fight a **boss**
  fronts the camp and offers me a **shady job** — a real quest, with a real reward, delivered as the
  camp's face rather than a wall of aggro.
- As a player whose body is a **chimera that reads as kin nowhere** (0-marker to every race), the camp
  is the **one place that still hands me work** — outlaws don't care what I look like; a scary beast is
  welcome.
- As a player, a camp offer **feels shady** — its reward leans to **power / combat** currency and the
  cauldron **purrs** at it, unlike a village's honest, door-opening favour.
- As a player, I can always **turn on the boss** — the deal is one card, the fight is the other; his
  crew stands behind him, not between us.
- As a designer, I get this for **free from the Camp's occupancy** — I do **not** author a new
  "shady" content type; I place a Camp, and its quest-bearer fill is coloured by the fact the Camp is
  outlaw-held.

## Functional requirements

### Fill vs anchor semantics
1. A quest-bearer placed as **fill** is **optional and rare** (it rides the recipe's weighted fill
   budget, not the guaranteed anchor slot) and **does not define the Site** — the Camp's reason to
   exist stays its **bandits** (`Combat·bandit` anchor). When the fill does not roll, the Camp is just
   a fight. This is the inverse of a Village/City, where the quest-bearer **is** the anchor.
2. A fill quest-bearer **takes its colour from the Site it lands in.** The **same** base·flavor
   `NPC·quest-bearer` beat reads as an honest plea in a Village and as a dirty job in a Camp — the
   difference is the **Site's occupancy**, not a different content asset.

### Shady is derived, not a new flavor
3. **No new flavor tag.** "Shady" is **derived from the Camp's outlaw occupancy**, exactly as
   quest-bearing / hostile intent is derived from facts (NPC Proximity brief). A quest-bearer beat in
   an **outlaw-occupied** Site draws its offer from the **occupant faction's** offer pool → a dirty
   job; the same beat in a race-occupied settlement draws an honest one. Occupancy is the input, the
   offer's tone and currency are the output.

### The offer is passport-free (outlaws take any beast)
4. The Camp's shady offer is **not passport-gated.** The Sites brief makes Settlements passport-gated
   *per their occupant faction*; the **outlaw faction's acceptance rule is "any monster"** — so the
   gate is authored **wide open**, not special-cased in the schema. Consequence (the point): a
   **kin-nowhere chimera** who is locked out of every race's settlement can **still** get a quest at a
   Camp. This is not a loophole — it is the Monster road having its own door (`world/overview.md` §9:
   the monster needs no one's welcome).

### The boss fronts the camp; the fight is behind the deal
5. When a Camp presents a shady offer, the **offer-giver is the bandit boss standing as the camp's
   face** — a **talkable quest-bearer** (`?` marker, engaged by the **F** prompt, per the Proximity
   brief), **not** an auto-aggro wall. The player can **reach the boss without fighting through the
   camp first.**
6. The camp's **muscle reads as the boss's crew behind the deal**, i.e. a fight the player **chooses**
   — by picking the **attack card** (the Monster verb, `npc-encounter-cards.md` §4) or by the fiction
   of refusal/betrayal — **not** independent aggro beats placed between the player and the boss. (A
   Camp that did **not** roll a shady offer stays a straight `Combat·bandit` fight as today; the boss
   face appears **only** when the offer rolls.)
7. Because the boss **has** a quest, he is a quest-bearer, not "hostile" — this stays inside the
   Proximity brief's derived-intent model and does **not** need the deferred
   "talkable-with-optional-fight" case (he is talkable *because* he has an offer).

### Dark currency and the moral fork
8. A shady offer pays in **power / combat** archetype currency and is a **cauldron-voice tempter
   slot** — the same dark-side economy as `quest-as-reward.md` §4 (glow still shows tier; the exact
   item stays hidden). It never pays in a **bigger** reward — the "different currency, not more" rule
   holds; the Camp is simply the **spatial home of the dark currency**, as a Village is the home of
   access/passport currency.
9. A Camp shady offer may be **either** a **self-contained** dirty job **or** the **dark half of a
   cross-actor moral fork** (the robber's counter-offer to a village victim's plea, `quest-as-reward.md`
   §4) — the director decides via threads. This brief only fixes that the Camp is the **natural place
   the dark side of such a fork lives**; it adds no new fork mechanism.

### Determinism & authoring
10. Everything above is **data-authored and deterministic**: the Camp's occupancy (outlaw) drives the
    shady colouring and the open acceptance; **same seed → same Camp, same offer roll.** Adding this
    changes **no** existing site or flavor asset (requirement 3) and needs **no** new content kind.

## Content authoring rules (for the designer)
- Do **not** author a "shady offer" type. Author a **Camp** with **outlaw occupancy**; its
  `NPC·quest-bearer` fill is coloured shady automatically (requirement 3).
- Set the outlaw faction's **acceptance rule to open** ("any beast") so the Camp offer is
  passport-free (requirement 4). Race-occupied settlements keep their tiered passport gate unchanged.
- Author the Camp boss's offer to pay in **power/combat** archetype currency and to carry the
  cauldron-voice **tempter** bark slot (requirement 8) — reuse the existing dark-offer hooks, don't
  invent a new reward path.
- The Camp's `Combat·bandit` anchor stays the crew **behind** the offer; do **not** place independent
  aggro beats in front of the boss when a shady offer is present (requirement 6).

## Acceptance criteria
- Traversing runs, **most Camps are a straight fight**; a Camp presenting a **talkable boss with a
  shady offer** is **rare** (the low fill weight), and when it does, the player can **walk up to the
  boss without fighting first** and sees the **F** prompt + `?`.
- A **kin-nowhere chimera** (0 markers to every race) is **turned away at race settlements** but
  **still offered the Camp job** — demonstrating the offer is passport-free.
- The shady offer's reward **glows by tier**, is **tinted power/combat** belonging, keeps the exact
  item **hidden**, and the **cauldron voice** tempts it.
- **Picking the attack card** (or refusing) triggers the crew fight; the boss's crew never auto-aggros
  **between** the player and the boss while the offer is on the table.
- **Same seed → same Camp and same offer roll.** No existing site/flavor asset changed; **no new
  content kind** introduced.

## Out of scope / open points (do not build now)
- **A generic "shady" flavor / occupancy-colouring engine for other factions** — this brief only
  wires the **outlaw → shady** colouring the Camp needs. A broader "occupant faction colours its
  quest-bearer" table can grow later by authoring, not now.
- **New fork mechanics** — the cross-actor moral fork (thread, mutual-exclusion facts, cross-dialogue
  continuity) is owned by `quest-as-reward.md` + the Director threads brief (P2-3); unchanged here.
- **Reward-roll-by-tier, the offer-card UI, the attack card, and the cauldron-voice bark** are their
  own already-filed needs-code entries — this brief **consumes** them, it does not respecify them.
- **The engine "how"** — how the planner reserves the Camp cluster, expresses the boss-fronts-crew
  layout, and weights the fill roll — the code track's call (balance numbers are playtest-tuned).
- **Passport of non-outlaw settlements** — unchanged (tiered by part count, Race Roster brief).
