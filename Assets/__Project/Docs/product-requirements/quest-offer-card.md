# Quest-Offer Card — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-02
> Level: product-owner (what & feel). The code track owns the technical "how".
> Design background: `design/narrative/quest-as-reward.md` (offer-as-prize + reward economy),
> `design/narrative/npc-encounter-cards.md` (the card hand). Extends the shipped
> `encounter-dialogue-ui.md` (which already puts the job title + summary on the card). Shares the card
> grammar with `mutation-choice-cards.md`. **Unblocked** now that the artifact **tier** exists.

## Goal

Make a quest **offer** land as a **rare prize**, not a wall of obligation. The encounter hand and the
job text (title + summary) already ship; this brief adds the **reward-card treatment** now that
artifact tier exists: an **ornate framed card** distinct from plain chatter, that **telegraphs how
good and what kind** the reward is — **tier by glow, belonging by colour** — while keeping the **exact
item hidden**.

**Design symmetry (state it):** the **mutation card reveals** the outcome (you choose what you get);
the **quest card conceals** the reward (a rolled surprise). Same card grammar, opposite information
stance — this is "direction + floor, not a vending machine".

This is the offer card's **look/feel**; it does not change what a quest is or how rewards are rolled.

## User stories

- As a player, an ordinary NPC line is a **plain bubble**, but a **quest offer** is an **ornate framed
  card** — the moment reads as a small "rare card" reveal, a prize.
- As a player, the card tells me **how good** (frame **glow = tier**) and **what kind** (a **belonging
  colour** = the reward's archetype/race — power/combat vs access/passport), but **never the exact
  item** — the mystery is the point, and the **cauldron voice teases** it.
- As a player, I can **inspect** the card to read the **job** in more detail (objective / who / why);
  the **reward stays hidden**.
- As a player, when an NPC offers **several ways to solve their trouble**, I see **several offer
  cards** side by side, **same tier** (same glow), differing by **belonging colour** — the choice
  reads as *which currency / whose side*, not "bigger loot".

## Functional requirements

### Two registers
1. **Plain bubble = chatter**; **ornate framed card = a quest offer**. The offer card is terse
   (title + short summary + reward telegraph) and **never grows into a text wall**.

### Card face
2. **The job** — title + short summary, with author-marked keyword highlighting (already shipped in
   `encounter-dialogue-ui.md`).
3. **A mystery reward slot** — a reward area on the card that **glows by tier**, is **tinted by the
   belonging colour** (reward archetype/race), and shows the item itself as a **hidden silhouette /
   "?"**. It reads as "a prize of this potency and family, identity unknown". (Symmetric to the
   mutation card's part slot, but concealed.)

### Reward telegraph (holds the decided rules)
4. **Tier = glow intensity**, **belonging = colour**, **exact item = hidden** — the same grammar as
   artifacts and the mutation card. The **cauldron voice** is the hint channel; the UI never spells
   out the item.

### Inspect
5. **Hovering / inspecting** a card reveals the **quest detail** (objective / giver / why); the
   **reward stays hidden**. The card stays terse at a glance; detail is on demand.

### In the hand
6. The quest-offer card is **one card-type in the encounter hand** (alongside attack / leave,
   `npc-encounter-cards.md`). When a single NPC offers **several** resolutions, the offers appear
   **together in the hand**, **same tier** (same glow), distinguished by **belonging colour** —
   "different currency, not more".

### Honest glow (dependency)
7. The glow must reflect the quest's **declared tier** and the belonging its **declared
   archetype-bias** (`quest-as-reward.md` §3: reward is **rolled** from tier + archetype-bias, not a
   literal item). The card's honesty depends on that rolled-reward model — *(the roll itself is a
   separate loot/quest change; this brief owns the card that displays it.)*

## Content authoring rules (for the designer)
- A quest already declares what the card needs: **title**, **short summary**, **objective**, a
  **reward tier**, and a **reward archetype-bias** (belonging). The card reads these — **no per-card
  art authoring**.
- **Glow intensity** derives from the declared tier and **belonging colour** from the declared
  archetype-bias, via the shared card grammar — not authored per card.
- Keep the summary **terse**; put longer text behind **inspect**, never on the face.

## Acceptance criteria
- A quest offer shows as an **ornate framed card** (distinct from the plain chatter bubble), with the
  **job** (title + summary) and a **mystery reward slot**.
- The reward slot **glows by tier**, is **tinted by belonging colour**, and shows the **item hidden**
  (silhouette / "?") — the exact artifact is never named on the card.
- **Inspecting** the card reveals the **quest detail** while the **reward stays hidden**.
- Several offers on one NPC appear **together, same glow (tier)**, differing by **belonging colour**.
- The card visuals **reuse the artifact / mutation-card grammar** (glow = tier, colour = belonging).

## Out of scope / open points (do not build now)
- **Rolling the reward** by tier + archetype-bias (replacing the literal `(artifactId, count)`) is a
  separate **loot/quests** change (`quest-as-reward.md` §3); this brief displays the result, the roll
  is elsewhere.
- **Several-offers-per-NPC authoring** and the **time-separated competing/moral-fork** logic
  (shared-actor thread, mutual-exclusion facts) are separate **director/quests** items; this brief is
  the card visual only.
- **Cauldron-voice tempter content** on a dark offer — separate (`cauldron-voice.md`).
- **Exact card art / VFX** (frame ornament, glow shader, reveal animation, inspect popover styling) —
  tech-art / render-look; this brief sets content + behaviour.
- **Moral-fork balance** stays on facts/threads/access, never loot-EV (`quest-as-reward.md` §4) — a
  design rule, not this card.
- **No change** to what a quest is or does.
