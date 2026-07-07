# Mutation Choice Cards — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-02
> Level: product-owner (what & feel). The code track owns the technical "how".
> Reference: `references/mutation-card-reference.png` (added 2026-07-06 — see **Visual reference**;
> locks the two-zone layout + labeled stat strip + single ability-preview popover).
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

## Visual reference

`references/mutation-card-reference.png` (composition + mood target — the "Boar Tusk / Gore Charge"
card). Read it as **normative** vs **mood-only**:

**Normative (the layout & content this brief locks in):**
- **Vertical card, two zones:** top ~**2/3** = the part visual, with the **mutation name** on a top
  banner and the **race/belonging sigil** in the top corner; bottom ~**1/3** = the **ability block**.
- **Ability block = crest + ability name over a labeled stat strip.** Each stat is a column of
  **icon → value → label**: **Turns** (cooldown · hourglass "2"), **Damage** (claw "3"), **Shape**
  (cone/line · the cone glyph), **Effect** (the applied **status glyph**, or a neutral "Physical"
  untyped mark — the chevron; there is **no elemental damage-type system**).
- **Rarity reads by frame colour + a soft outer glow** (belonging = colour, potency = glow).
- **One hover popover** (the detached card on the right): the **hero on a hex battlefield in the
  in-game orthographic view**, **mid-cast**, the **affected tiles lit to match the Shape icon**;
  ability **name + description** beneath, with an optional italic **flavour line**.

**Mood-only (art direction — may drift, not a spec):**
- The exact painterly boar-tusk render, the dark-forest backdrop, the specific palette, the precise
  glyph shapes and flavour prose. Target look = **semi-stylized, between ornate and flat-minimal**;
  muted, low-poly-friendly, no hard outlines (consistent with `art/render-look.md`).

## User stories

- As a player, when a blank unseals I see a **hand of mutation cards**; each shows the **organ I'd
  grow** and, at a glance, **what it does**, and I pick one.
- As a player, the card **front is visual** — the body part fills the **top two-thirds**, and a tidy
  **labeled stat strip** (turns · damage · shape · effect) sits in the **bottom third** — so I read it
  instantly.
- As a player, I can **flip a card** (a corner button) to see the **part it would replace** and that
  part's abilities — the before→after, only when I want it.
- As a player, I **hover the ability** to open **one popover** that shows **my hero wearing the new
  part** on a battlefield **casting that ability's animation** (its area lit up to match the shape
  icon) with the **name + description** beneath — I *see* both the silhouette I'd become and what the
  skill does, I don't just read it.
- As a player, the card's **glow and colour** tell me its **potency and belonging** at a glance, the
  same language as quest and encounter cards.
- As a player, **picking a card commits** — it installs that mutation and the other variants (and the
  socketed reagents) are gone; the choice feels weighty.

## Functional requirements

### When & where
1. The mutation-card menu appears at the **unseal** of a ripened blank on the operating table (from
   the Socketed Blanks loop). It presents the **small menu of variant mutations**; the player picks
   **exactly one**.

### Card front — two-zone layout (visual-first)
The card is vertical with two stacked zones (see the reference): **top ~2/3 = the part visual**,
**bottom ~1/3 = the ability block**.
2. **Top ~2/3 — the body part pictured** (the organ this variant grows), with the **mutation name**
   on a banner at the very top and the **race/belonging sigil** in the top corner.
3. **Bottom ~1/3 — the ability block:** the ability's **crest + name**, over a **row of labeled stat
   columns**. Each column stacks **icon → value → label**, so the strip reads itself. The reference's
   four columns are the baseline set:
   - **Turns** — cooldown in turns (ref: hourglass · "2")
   - **Damage** — damage dealt (ref: claw · "3")
   - **Shape** — the attack's area shape: cone / line / single / blast (ref: cone glyph)
   - **Effect** — the **applied status glyph** (from the `combat-status-effects.md` registry —
     burn / poison / stun / slow…), or the neutral **`glyph.untyped`** ("Physical") mark when the
     ability applies **no** status. Owner call 2026-07-07: **no elemental damage-type system** —
     "Physical" is the untyped mark, **not** an element. The card glyph is the **same** glyph the
     status shows on the unit (`combat-status-effects.md` FR8).
   Columns without a numeric value (Shape, Effect) show **icon + label** only. This is the ability
   **outcome** readout — the **hidden crafting traits are still never shown** (FR9).

### Before→after on demand (flip)
4. A **corner control flips the card**. The **back** shows the **part this mutation would replace**
   (the current part in that slot) and **its** abilities, so the player can weigh gained / lost /
   changed. If the slot is currently **empty** (a brand-new organ), the back conveys "nothing
   replaced" (a bare slot), not a false comparison.

### Depth on hover — one popover
5. **Hovering the ability block** opens a **single ability-preview popover** that shows **both** the
   part and what it does (see the reference's detached right-hand card): a **3D model of the hero
   wearing the new part**, standing on a **battlefield mock in the in-game orthographic combat view**,
   **playing that ability's cast animation** with the **affected tiles highlighted to match the Shape
   icon** (cone / line / single / blast). Beneath the model sit the ability's **name + description**
   (plus an optional flavour line). A **passive** ability (no cast) shows the hero wearing the part in
   an **idle pose** with its name + description. Reuse the **D3 code-authored placeholder animations**
   (`combat-round-and-telegraph.md` R15–R18). This popover is a **shared grammar** with the Arena
   draft screen (`arena-draft-ui.md`) — one preview mechanism, two surfaces.

   *(Design change 2026-07-06: the earlier two separate hovers — an ability-icon popover **and** a
   part-picture mini-model — are **merged into this one popover**, which shows the worn part and the
   cast together, per the reference.)*

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
9. **Crafting traits (substance/property) are never shown** on the card — the socketing puzzle stays
   hidden. The bottom strip shows only **ability-outcome** stats (turns / damage / shape / effect), the
   part picture, the belonging colour, and the potency glow. The **cauldron's voice** may colour the
   moment (path-reactive), but is not required for the choice.

## Content authoring rules (for the designer)
- A body part authored as data already carries what a card needs: its **active/passive abilities**
  (each with name, description, icon, and the stat-strip values — turns / damage / shape / effect) and
  its **part image/icon**; the card reads these — **no new authoring** beyond the part itself.
- **Belonging colour** and **potency/tier glow** derive from the part/blank data via the shared card
  grammar — not authored per card.
- The **hover popover** uses the existing character/part visual assets (the same meshes the installed
  part would use) — the hero wears the new part and plays the ability's D3 placeholder animation; no
  new art authored per card.

## Acceptance criteria
- At unseal, a **hand of mutation cards** appears; each **front** shows the **part picture (top 2/3)**
  + a **labeled ability stat strip (bottom 1/3)**; picking one **installs** it and dismisses the rest.
- The bottom strip shows **labeled columns** (icon → value → label): at least **Turns, Damage, Shape,
  Effect**, matching the reference — where **Effect** is the applied **status glyph** (or the neutral
  "Physical"/untyped mark if the ability applies no status).
- A **corner control flips** a card to reveal the **replaced part + its abilities** (or a clear
  "nothing replaced" for an empty slot).
- **Hovering the ability** opens **one** ability-preview popover — the **hero wearing the new part**
  on a battlefield **casting the ability** (area lit to match the Shape icon; idle pose for a passive)
  with **name + description** beneath.
- Card **glow = potency, colour = belonging**, visually consistent with quest/encounter cards.
- **No crafting-trait stats** appear anywhere on the card.
- **Picking commits** — reagents consumed, other variants gone — and reads as a weighty confirmation.

## Out of scope / open points (do not build now)
- **The socketing / unseal loop itself** — owned by `crafting-mutation-socketed-blanks.md`; this
  brief is only the choice cards it surfaces.
- **A full always-on live-hero preview in the actual run world** — the ability-preview popover shows
  the hero casting on a **battlefield mock**, not the live world scene; an always-on in-world live
  model stays the separate ROADMAP backlog item (**P5-12**). The popover **reuses the D3 placeholder
  ability animations**; production VFX/clips stay P5-12.
- **Quest-card visuals** — a separate brief; they only **share the grammar** with these.
- **Exact card art, layout polish, and VFX** (flip animation, glow shader, tooltip styling) are the
  render-look / tech-art call; this brief sets the content and behaviour.
- **No change** to combat rules or to what an ability does.
