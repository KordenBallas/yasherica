# Attack Card — the Monster Verb

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Design background: `design/narrative/npc-encounter-cards.md` §4 (the attack card), the moral-fork
> channel rule in `design/narrative/quest-as-reward.md` §4, `design/world/overview.md` §9 (Conquest).
> Covers ROADMAP **P1-7**. Builds on the shipped encounter hand (`encounter-dialogue-ui.md`) and
> first-class threads (`director-threads-and-continuity.md`, P2-3). Cauldron bark content is authored
> in `cauldron-voice-barks.md` (P1-10).

## Goal

Make **attacking a talkable NPC** a **first-class, legible choice** — the purest expression of the
Monster / Conquest road — instead of an accident. The encounter hand can already show an attack
option; this brief makes it a **real verb with real consequences**: it is offered **only where it
fits the fiction**, it **closes that character's future arc**, it drops **combat corpse-loot on the
separate mutation channel**, it **writes Conquest/path facts**, and it is a **bark slot for the
cauldron voice**. It also formalises an NPC **self-initiating** the fight.

## User stories

- As a player, on some NPCs I can **pick an attack card** to turn on the quest-giver — a clear,
  deliberate dark option, not a hidden interaction.
- As a player, when I kill an NPC I **forfeit whatever else they might have offered me** (their future
  quests/arc) — the dark road has an in-fiction price, not just upside.
- As a player, slaying an NPC drops **loot from its body** (the "you are what you eat" channel),
  separate from any quest reward — and the **cauldron approves out loud**.
- As a player, the world **remembers** I went Monster here (facts that lean my run toward Conquest and
  close kin-doors).
- As a player, some NPCs **attack me first** without my choosing a card (an ambush / a boss fronting a
  camp), and that reads the same as picking the attack verb — a fight, not a conversation.

## Functional requirements

### Eligibility (present only where it fits)
1. The attack card is **not on every NPC**. It appears in the hand **only on eligible NPCs** (authored
   eligibility — e.g. an outlaw, a marked target, a boss), so attacking is a legible option where the
   fiction supports it, and absent where it would be noise.
2. An NPC may also **self-initiate** the fight (no card picked by the player) — the same consequences
   below fire as if the attack verb were chosen. (This is the derived-hostility / ambush / camp-boss
   path already in `npc-proximity-interaction.md` and `bandit-camp-humanoids.md`.)

### Consequences on attack (card picked OR self-initiated)
3. **Closes the actor's thread.** Killing the NPC **ends its thread as foreclosed** (the first-class
   thread from P2-3) — the player forfeits that character's future recurring-actor arc. This
   foreclosure is the in-fiction price of the extra corpse loot.
4. **Corpse-loot on the separate combat/mutation channel.** The slain NPC drops loot via the existing
   combat "you are what you eat" loop — **orthogonal** to the quest-reward economy. **The fork is
   never balanced on this loot** (raising an offer's tier to "equalize" is forbidden — it reintroduces
   loot-EV optimisation). No-kill branches pay in a currency killing can't give (access/doors, a
   recurring ally, a thread that only opens by sparing).
5. **Writes Conquest / path facts.** The attack writes the path-lean facts that feed passport gating
   and the Monster-vs-Alliance systems (§9) — the run leans toward Conquest and kin-doors close.
6. **Cauldron bark slot.** Presenting and choosing the attack verb is a **bark slot** for the cauldron
   voice (path-reactive, "why stay small — devour him"). The bark *content* is authored separately
   (`cauldron-voice-barks.md`); this brief owns the **slot/trigger**, not the lines.

### Reads as one verb
7. Whether reached by **picking the card** or by **NPC self-initiation**, the outcome is the **same
   fight and the same consequences** (3–6) — one Monster verb with two entry points, never two
   divergent code paths in feel.

## Content authoring rules (for the designer)
- Mark an NPC/storylet as **attackable** where the fiction supports the Monster verb (outlaws, marked
  targets, bosses); leave it **off** for ordinary townsfolk — a peaceful villager should not offer
  "kill me" as a card.
- **Do not** author a compensating tier bump on any competing offer to "make up for" corpse loot — the
  fork balances on **facts/threads/access**, never loot-EV.
- The corpse-loot itself is authored on the **combat/mutation** side (the enemy's part drops), not as a
  quest reward — unchanged by this brief.

## Acceptance criteria
- An **eligible** NPC shows an attack card in its hand; an **ineligible** ordinary NPC does not.
- Picking the attack card (or the NPC self-initiating) **starts the fight**; on the NPC's death its
  **thread is foreclosed** (no further offers from that actor) and **corpse-loot drops** on the combat
  channel.
- The attack **writes the Conquest/path facts** (observably: a subsequent passport/gating read leans
  as expected).
- A **cauldron bark fires** on the attack beat (placeholder line acceptable until content lands).
- A no-kill resolution of the same actor does **not** drop the corpse loot and **keeps** the actor's
  thread open — demonstrating the "different currency, closing-the-door is the price" rule.

## Out of scope / open points (do not build now)
- **The encounter-hand UI / card face** — shipped (`encounter-dialogue-ui.md`); this brief is the
  verb's **consequences**, not the card visual.
- **Cauldron bark line content** — separate (`cauldron-voice-barks.md`).
- **The competing/moral-fork placement** (which no-kill offer stands opposite this attack, time-
  separated on a shared-actor thread) — separate (`multiple-and-competing-offers.md`).
- **Combat itself** and corpse-loot roll internals — unchanged.
- **Talkable-with-optional-fight** (an NPC that keeps a quest *and* offers an optional attack that is
  neither derived-hostile nor self-initiating) — remains the deferred case noted in
  `npc-proximity-interaction.md`; not introduced here.
