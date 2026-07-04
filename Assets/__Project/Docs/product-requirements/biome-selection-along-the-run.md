# Biome Selection Along the Run — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-04
> Level: product-owner (what & feel). The code track owns the technical "how".
> Background — do not restate it: `design/world/overview.md` §8 (a run **escalates through biomes
> toward the Order's seat**), `design/narrative/director.md` D18 (**commit to opened doors** —
> a run opens only *some* racial worlds) and D19 (escalation shifts pool/register — **deferred**,
> see out-of-scope). Biomes are **race homelands** — see `product-requirements/race-roster-and-passport.md`.
> Builds on the existing per-biome content configs (`biome-visual-styles.md` look; the per-biome
> monster pool + loot table).

## Goal

Stop the run being stuck in one hardcoded biome. Today the streaming generator runs a **fixed Forest**
(`AreaSceneEntrypoint` hardcodes it), so every run looks, fights, and loots the same and there is only
ever one race's homeland. We make the **biome advance along the run** through an **authored, seeded,
escalating sequence**: the run starts in a gentle low-tier biome and climbs toward higher, more
dangerous country, and **which biomes a given run passes through diverges by seed** (it opens *some*
homelands, not all). Switching biome switches its **monsters, loot, look, and — for a homeland — its
people**.

This makes the world **change along a run** and **diverge across runs**. It does **not** scale
difficulty or shift tone/density by altitude (that is the separate Escalation design), and it does
**not** add the run's final apex or a player-facing choice map (see out-of-scope).

## The model (product-owner level)

- **Each biome carries an escalation tier** (authored): low = backwater entry, high = the dangerous
  approach toward the Order (per `overview.md` §8; the desert/lizard-inquisition sits high — see the
  race brief). The tier is an **ordering key** for selection here; nothing scales difficulty/tone off
  it yet (that is D19, deferred).
- **A run climbs tiers.** It begins in a **low/entry tier** and moves **upward** as the player advances.
- **Within a tier, the biome is seeded-selected from that tier's eligible pool** by authored weights —
  so **different seeds send you through different homelands**, deterministically.
- **A run visits a subset, not all biomes** (D18): the run opens the homelands it happens onto and
  leaves the rest dark — "the world reads bigger than any single run".
- **You travel in stretches, not a flicker.** The run stays in one biome for an **authored stretch**
  (a run of platforms) and then **transitions** into the next; the crossing is legible.
- **Homeland biomes bring their race.** Forest→Fox, Mountain→Ibex, Desert→Lizard (race brief); the
  passport context applies there. **Cave stays out of the rotation for now** (it has no race yet).

## User stories

- As a player, a run **travels through different homelands** — I start in a gentle backwater and climb
  toward higher, more dangerous country — and **different runs take me through different peoples**.
- As a player, when I **cross into a new biome** its **monsters, loot, and look change**, and (if it's a
  homeland) I'm now among a different race.
- As a designer, I author **which biomes sit at which escalation tier**, their **selection weights**, and
  how **long** a run lingers in one — all as **data**; adding a biome or re-tiering one is **no code**.
- As a designer, the biome journey is **deterministic**: the same seed yields the same sequence.

## Functional requirements

### Selection
1. **No hardcoded biome.** The fixed-Forest start is removed; the run's biome is **selected** from data.
2. **Escalation tier per biome** (authored data) gives a low→high ordering (backwater → approach).
3. **The run climbs tiers.** The first biome is a **low/entry tier**; the run advances **upward** through
   tiers as the player progresses.
4. **Seeded pick within a tier.** For each biome stretch, the biome is chosen **deterministically** from
   the current tier's **eligible pool** by **authored weights** — so runs **diverge** in which homelands
   appear.
5. **A run covers a subset** of the biomes (not all every run) — the commit-to-doors divergence (D18).

### Progression along the run
6. **Stretch, not flicker.** The run stays in a biome for an **authored stretch** (a length or range of
   platforms/windows), then **transitions** to the next selected biome. Deterministic.
7. **Switching biome switches its content.** On a biome change, the active **monster pool**, **loot
   table**, and **biome appearance** switch to that biome's existing configs, and — for a **homeland**
   biome — that race's people/passport context applies.
8. **Cave excluded.** The rotation uses the homeland biomes (**Forest / Mountain / Desert**) only; **Cave
   never appears** for now.

### Seams for later (light)
9. **Expose the current tier as a fact** (the run's altitude/tier), so later consumers can read it
   (D19 register/difficulty, and the lore-pacing "reached biome depth tier" milestones). **Nothing scales
   difficulty or tone off it in this brief** — it is only published.

### Robustness
10. **Determinism.** Same run seed → same biome journey (which biomes, in what order, for how long).
11. **Data-authored & extensible.** Tiers, pools, weights, and stretch lengths are **data**; adding a new
    biome (give it a tier + weight) or re-tiering one is **no code change**.

## Content authoring rules (for the designer)
- Give each biome an **escalation tier**, a **selection weight** within that tier, and a **stretch
  length** (or range) it holds before transitioning.
- The per-biome **look, monster pool, and loot table already exist** as their own configs — selection
  only decides **which biome is active** for a stretch; it does not re-author that content.
- Keep it additive: a new biome needs only a tier + weight (+ its existing look/monster/loot configs) to
  join the rotation.

## Acceptance criteria
- A run **starts in a low-tier biome** and, given enough length, **changes biome** as it advances,
  reaching higher tiers — no longer stuck on Forest.
- **Different seeds → different biome journeys** (a divergent subset of homelands); **same seed →
  identical journey**.
- **Crossing into a biome switches** its monsters, loot, and look; a **homeland** biome brings its race
  context (passport applies).
- **Cave never appears** in the rotation.
- **Adding or re-tiering a biome is data-only.**
- The **current tier is readable as a fact** (even though nothing consumes it for difficulty/tone yet).

## Out of scope / open points (do not build now)
- **Escalation richness (D19).** Tonal-register shifts, content-**density** growth, and **monster-
  difficulty scaling** by tier — monsters stay **flat-difficulty** for now. The separate **Escalation
  design** thread (`design/world/overview.md` §8; `design/narrative/director.md` D19) owns these; this
  brief only **orders** biomes by tier and **publishes** the tier fact.
- **The Order's Seat / run apex / a defined run end** — deferred (`design/roadmap.md` World & Sites).
  At the top tier the run simply keeps serving top-tier biomes; there is no throne/finale yet.
- **Player-choice junction map** ("whose doors next") — a later agency layer; selection is **seeded**
  now, not chosen.
- **Progression-driven selection** — biasing which homeland appears by the player's mutations/passport
  or past runs — a later coupling; selection here is seed + authored weights only.
- **Cave's race/role** and a **fourth race** — deferred (race brief §6).
- **Biome-transition art** — the gate/backdrop/skyline that *signals* a crossing rides world-backdrop /
  site-dressing (M5); here the crossing is legible only through the existing per-biome look.
- **Cross-run persistence** of the biome journey — rides the general save/load work.
