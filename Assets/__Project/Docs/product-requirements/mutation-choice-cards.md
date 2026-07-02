# Mutation Choice Cards — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-02
> Level: product-owner (what & feel). The code track owns the technical "how".
> Depends on: `crafting-mutation-socketed-blanks.md` (this is the **unseal variant menu** of that
> loop). Shares the card grammar with `design/narrative/quest-as-reward.md` +
> `design/narrative/npc-encounter-cards.md`. Absorbs the ROADMAP "Mutation — Ability-aware mutation
> choice panels" item.

## Goal

When a ripened Part-Blank **unseals** (Socketed Blanks), the player chooses from a small menu of
**variant mutations**, presented as **cards**. This brief defines what a mutation card shows and how
the choice reads: **visual-first and charming** (a picture of the organ, not a stat wall), yet
**legible** enough to make an informed pick (Pillar 4, "no dominant build"). Depth — the
before→after comparison, ability descriptions, and a silhouette preview — is available **on demand**,
not crammed onto the card face.

**Trait/ability split (the crux, carried from the crafting model):** the *crafting traits*
(substance/property) stay **hidden** — they were the puzzle during socketing. At the choice, the card
reveals the **abilities** (the combat outcome you are choosing between). Hidden input, revealed
outcome.

## User stories

- As a player, when a blank unseals I see a **hand of mutation cards**; each shows the **organ I'd
  grow** and, at a glance, **what it does**, and I pick one.
- As a player, the card **front is visual** — the body part pictured in the centre, its abilities as
  **icons** beneath — so I read it instantly without reading stats.
- As a player, I can **flip a card** (a corner button) to see the **part it would replace** and that
  part's abilities — the before→after, only when I want it.
- As a player, I **hover an ability icon** to read its **name + description** in a tooltip.
- As a player, I **hover the pictured part** to see a **mini-model of my hero wearing the new part** —
  the grotesque silhouette I'd become.
- As a player, the card's **glow and colour** tell me its **potency and belonging** at a glance, the
  same language as quest and encounter cards.
- As a player, **picking a card commits** — it installs that mutation and the other variants (and the
  socketed reagents) are gone; the choice feels weighty.

## Functional requirements

### When & where
1. The mutation-card menu appears at the **unseal** of a ripened blank on the operating table (from
   the Socketed Blanks loop). It presents the **small menu of variant mutations**; the player picks
   **exactly one**.

### Card front — visual-first
2. **Centre: the body part pictured** (the organ this variant grows).
3. **Beneath it: ability icons** for the abilities the part grants — **active and passive**. This is
   the at-a-glance readout; the face carries **no stat blocks**.

### Before→after on demand (flip)
4. A **corner control flips the card**. The **back** shows the **part this mutation would replace**
   (the current part in that slot) and **its** abilities, so the player can weigh gained / lost /
   changed. If the slot is currently **empty** (a brand-new organ), the back conveys "nothing
   replaced" (a bare slot), not a false comparison.

### Depth on hover
5. **Hovering an ability icon** opens a context tooltip with that ability's **name + description**
   (both active and passive abilities carry this).
6. **Hovering the pictured part** (the card centre) opens a context popover with a **mini 3D model of
   the hero wearing the new part** — the silhouette preview of what you'd become.

### Shared card grammar
7. **Frame glow = potency / tier; colour = belonging / archetype** — the **same visual language** as
   quest and encounter cards (accepted grammar). Variants in one menu are of the **same organ** (the
   blank fixes type/species), so they **share the belonging colour** and are distinguished by
   **ability and potency** (a stronger variant glows brighter), not by race.

### Commit
8. **Picking a card commits** (Socketed Blanks commit-on-unseal): the chosen mutation installs, the
   socketed reagents are consumed, and the unchosen variants are discarded. The interaction must read
   as a **deliberate, weighty confirmation**, not an idle tap.

### Legibility
9. **Crafting traits are never shown** as a stats panel on the card — only abilities (the outcome),
   the part picture, the belonging colour, and the potency glow. The **cauldron's voice** may colour
   the moment (path-reactive), but is not required for the choice.

## Content authoring rules (for the designer)
- A body part authored as data already carries what a card needs: its **active/passive abilities**
  (each with name, description, icon) and its **part image/icon**; the card reads these — **no new
  authoring** beyond the part itself.
- **Belonging colour** and **potency/tier glow** derive from the part/blank data via the shared card
  grammar — not authored per card.
- The **mini-model preview** uses the existing character/part visual assets (the same meshes the
  installed part would use).

## Acceptance criteria
- At unseal, a **hand of mutation cards** appears; each **front** shows the **part picture + ability
  icons**; picking one **installs** it and dismisses the rest.
- A **corner control flips** a card to reveal the **replaced part + its abilities** (or a clear
  "nothing replaced" for an empty slot).
- **Hovering an ability icon** shows its name + description; **hovering the pictured part** shows a
  **mini-model** of the hero with the new part.
- Card **glow = potency, colour = belonging**, visually consistent with quest/encounter cards.
- **No crafting-trait stats** appear anywhere on the card.
- **Picking commits** — reagents consumed, other variants gone — and reads as a weighty confirmation.

## Out of scope / open points (do not build now)
- **The socketing / unseal loop itself** — owned by `crafting-mutation-socketed-blanks.md`; this
  brief is only the choice cards it surfaces.
- **A full live-hero preview in the world** — this brief uses a **mini-model popover** on hover; a
  full live-model mutation preview stays the separate ROADMAP backlog item.
- **Quest-card visuals** — a separate brief; they only **share the grammar** with these.
- **Exact card art, layout polish, and VFX** (flip animation, glow shader, tooltip styling) are the
  render-look / tech-art call; this brief sets the content and behaviour.
- **No change** to combat rules or to what an ability does.
