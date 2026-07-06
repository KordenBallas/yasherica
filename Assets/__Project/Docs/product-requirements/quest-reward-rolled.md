# Rolled Quest Reward — Tier + Belonging (not a literal item)

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Design background: `design/narrative/quest-as-reward.md` §2–§3 (reward telegraph + economy),
> `design/crafting/model.md` (Socketed Blanks — where archetype/race now lives), reconciled with the
> 2026-07-02 Socketed-Blanks reframe. Covers ROADMAP **P1-5** (rolled reward) **+ P0-3·b**
> (belonging-colour consumer). Feeds `quest-offer-card.md` (the card that displays this).

## Goal

Stop a quest handing out a **named, literal item**. A quest instead declares **how good** (a reward
**tier**) and **what kind** (a **belonging**), and the reward is **rolled** on completion. This is
what makes the offer card's glow **honest** (it reflects a real declared tier, not a thumb on the
scale) and keeps the world **non-catalog** — "direction + floor, not a vending machine" (Pillar 3).
It replaces today's fixed `(artifactId, count)` reward.

**The reconcile this brief settles.** Since Socketed Blanks (2026-07-02), **archetype/race lives on
Part-Blanks; an Artifact is function-only** (substance/property/tier, no race). So a reward's
**belonging** now depends on *what kind of thing* the reward is. The owner's call: a quest declares
**which payload kind** it rolls, and belonging is read accordingly.

## User stories

- As a designer, I author a quest's reward as a **tier + a belonging + a payload kind** — never a
  specific item id. The world rolls the actual reward when the quest completes.
- As a player, the reward I get is a **surprise of the promised potency and family** — the card told
  me *how good* and *what kind*, the cauldron voice teased it, but the exact item was never named.
- As a player, an **access/passport** reward reads and pays differently from a **power/combat**
  reward — the colour on the offer card matched what I actually received.

## Functional requirements

### Declared, then rolled
1. A quest's reward is declared as **(reward tier, belonging, payload kind)**, **not** a literal item
   id/count. On completion, the world **rolls** a concrete reward matching that declaration.
2. The **tier** is authored / director-set and rides the **fiction** (the scope of the trouble),
   never an abstract counter ("kill 5 vs kill 10 for better loot" stays cut). Tier is the same scale
   as the artifact / mutation tier glow.

### Payload kind (owner: declared per quest)
3. A quest declares its reward payload as **either**:
   - a **Part-Blank** — a body-part item carrying **form + a race tag + sockets** (the mutation-loop
     currency); **or**
   - an **Artifact** — a **function-only** reagent (substance/property/tier, **no race**) for the
     cauldron.
   The kind is a per-quest authoring choice; a quest rolls exactly one kind.

### Belonging (P0-3·b — the colour source)
4. **Belonging** is the reward's family, surfaced as the offer card's **belonging colour**, and is
   read **from the payload kind**:
   - **Part-Blank reward → belonging = the blank's race** (Ibex/Lizard/Fox/… or *kindless*). Its
     colour is the authored **race belonging-colour** (`RaceDefinition._belongingColor`) — the same
     hue the race uses everywhere else.
   - **Artifact reward → belonging = a coarse function family** (e.g. power/combat vs utility/access),
     **not** a race — it has no race tag. Its colour comes from that function family, not a
     `RaceDefinition`.
5. The belonging the card shows and the belonging the roll uses are the **same declared value** — the
   glow/colour can never promise a family the roll won't deliver.

### The roll
6. The roll respects the declared **tier** (potency) and **belonging** (family/race), and otherwise
   draws from the eligible pool — it is a **bias, never a guaranteed specific piece** (consistent with
   the hub "direction + floor" principle and `blank-loot-sources.md`). Deterministic under the run
   seed.
7. **Access is still paid in facts too, not only the item.** A passport/access reward's *door-opening*
   is the facts the quest writes (`design/narrative/quest-as-reward.md` §4); the rolled race-marker
   blank is the item half. This brief owns the **item roll**; the fact writing is the quest's own
   effects (unchanged).

## Content authoring rules (for the designer)
- Author a reward as **tier + belonging + payload kind**. Do **not** name an artifact/blank id.
- For an **access/passport** feel, declare a **Part-Blank** reward of the target **race** (belonging =
  that race's colour); for a **power/combat** feel, declare either a combat-family **Part-Blank** or a
  power-family **Artifact**.
- Belonging colour is **derived** (race colour for a blank, function-family colour for an artifact) —
  never hand-set per quest.
- Keep the reward's *stake* on the **fiction** (how big the trouble is), not a tunable number.

## Acceptance criteria
- A quest authored with **(tier, belonging, payload kind)** and **no item id** yields, on completion,
  a concrete reward of that tier and family — and a **different concrete item** is possible across runs
  (non-catalog), while tier/belonging always hold.
- A **Part-Blank** reward's belonging colour equals its **race's** belonging colour; an **Artifact**
  reward's belonging colour equals its **function-family** colour — and both match what the offer card
  showed.
- Two same-tier quests declaring **different belongings** (e.g. a Fox race-blank vs a power artifact)
  produce rewards the player reads as **different currency, same potency** — no dominant pick.
- Removing all literal `(artifactId, count)` reward authoring does not break existing demo quests
  (they are re-expressed as tier + belonging + kind).

## Out of scope / open points (do not build now)
- **The offer-card visual** (glow/tint/mystery slot) — separate, `quest-offer-card.md`; this brief
  supplies the **honest values** that card displays.
- **Competing / several-offers placement** — separate, `multiple-and-competing-offers.md`.
- **Non-item reward sinks** (Currency / Experience / Ability) — **deferred** (P1-12) until a currency
  model exists; quests pay in items + facts/doors for now.
- **The exact roll tables / bias weights** and any new tier/belonging **balance numbers** — code-track
  tuning; this brief sets the shape, not the numbers.
- **Per-run dig meta-bias** toward the player's pursued direction — deferred (unchanged).
- **Cauldron-voice tease content** on the hidden reward — separate, `cauldron-voice-barks.md`.
