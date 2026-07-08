# Medallion — Heat Slot-Reduction Reflected in View — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-08
> Level: product-owner (what & feel). The code track owns the technical "how".
> Related: `medallion-socket-ui.md` (the medallion gem/rim model),
> `heat-ascension-difficulty.md` FR8/FR10 (the slot-reduction modifier).

## Goal

When a Heat modifier removes one or more socket slots from a Part-Blank, the medallion View
must reflect that reduction. Currently the gem positions around the rim do not change when
Heat cuts a slot — the player sees "phantom" sockets that appear available but are not, and
the rim-progress math is wrong (it still targets the original count).

The fix: **slots taken by the pact are shown as sealed** — visually distinct from both an
empty (available) slot and a filled one, so the player reads "the pact locked this gem" at a
glance. The rim only tracks the **Heat-adjusted slot count** (available slots), not the
original.

## User stories

- As a player running a Heat pact that cuts my medallion's slots, I see **only the available
  gem positions** as real slots; sealed ones are shown as **visibly locked/burned**, not as
  normal empty gems.
- As a player, the **rim-progress only tracks available slots** — it closes when the reduced
  count is filled, not the original.
- As a player, I understand at a glance that the pact is responsible (the sealed look echoes
  the dare's register, not a broken socket).

## Functional requirements

1. **Gem count = Heat-adjusted slot count.** The medallion rim shows `original_slots` gem
   positions in total; slots removed by Heat are rendered as **sealed** (a distinct visual
   state: locked/burned/darkened), not as normal empty gems.
2. **Sealed gems are non-interactive.** A sealed slot does not accept artifact drops; it does
   not contribute to fill state.
3. **Rim progress tracks available slots only.** The rim fills and closes based on
   `available_slots = original_slots − Heat_removed`, not the original count.
4. **Edge case — artifact already socketed into a now-removed slot.** If a pact is accepted
   (or applied mid-session) while an artifact occupies a slot that Heat removes, **the artifact
   is ejected back to inventory** and the slot shows as sealed. No artifacts are silently
   discarded.
5. **No sealed slots at Heat 0.** A run with no slot-reducing pact shows all gems available as
   today; this change is additive and invisible at baseline.

## Acceptance criteria

- With a Heat pact that removes N slots, the medallion renders `original − N` available gems
  and N sealed gems; sealed gems do not accept drops and do not advance rim progress.
- The rim closes (offering the unseal beat) when `available_slots` are filled.
- If an artifact occupied a now-sealed slot when the pact was applied, the artifact is in
  inventory and the slot shows sealed; nothing is lost silently.
- At Heat 0 the medallion renders identically to today.

## Out of scope / open points (do not build now)

- **Exact sealed art** (burned ring, dark stone, cracked gem stub, pact glyph) — visual
  treatment; code track authors a placeholder, production pass later.
- **Animation of the sealing** (slots animate shut when the pact is applied in the hub view)
  — deferred to art/polish.
- **Rim colour/tint shift under Heat** — not required by this brief; a future atmosphere pass.
- **Multiple stacking slot-reduction modifiers** — the mechanic is the same; tuning the cap so
  the blank always retains at least 1 slot is playtest / Heat config authoring, not this brief.
