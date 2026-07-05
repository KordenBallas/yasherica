# Prioritised Build Plan

> A **priority-ordered cross-cut** of `ROADMAP.md` (which is organised by system). Every open item is
> grouped into build **waves P0–P6** and carries a **stable ID** (`P0-1`, `P1-3`, …) so it can be
> referenced by number. This is a planning index, not a system doc — `ROADMAP.md` stays the maintained
> per-system source of truth; when an item ships there, strike it here too.

**Ordering rule.** A prerequisite always sits above its dependents (race roster → body-plans;
save/load → D20 & mirror-lore; biome-selection → biome-visual).

**Model** is a recommendation, not a lock. Default is **Opus 4.8**. **Fable 5** is reserved for the three
P2 long poles where correctness on a hard, cross-system change outweighs its premium cost.
**Sonnet 5 / Haiku** carry scoped and mechanical work so the budget isn't burned. (`art` = the model
just wires the seam; the asset itself is authored by the designer.)

**Owner.** `Designer` = product-owner work (design decisions + content/art authoring), done in parallel,
gates nothing on the coding model. `Code` = coding model. `Hybrid` = code builds the system, the
designer authors the content it consumes.

**Sources.** Open items of `ROADMAP.md` + the 2026-07-03 inbox briefs (`product-requirements/`) + the
design-track gates that live in `design/roadmap.md` (race roster, apex, hub, cauldron voice, reveal
spine, escalation).

---

## Legend

| Model | Meaning |
|---|---|
| **Fable 5** | Hardest · cross-system · high-ambiguity — spend the premium here only |
| **Opus 4.8** | Default — capable systems work |
| **Sonnet 5** | Scoped / presentation |
| **Haiku** | Trivial / mechanical |

| Owner | Meaning |
|---|---|
| **Designer** | You — design + content/art; run in parallel, blocks nothing |
| **Code** | Coding model |
| **Hybrid** | Code builds the system; you author the content |

---

## Your parallel track (Designer) — start now, no code needed

Design decisions and content/art authoring. Several **unblock** the coding model. IDs point into the waves below.

- **P0-1** Race roster — ✅ **DESIGNED** (`design/narrative/races.md` + brief `product-requirements/race-roster-and-passport.md`): 3 races (Ibex/Lizard/Fox) + part-count passport + kindless hero. Code consumption = **P0-3**.
- **P1-10** Cauldron-voice lines (tempter barks, socketing hints, hub)
- **P1-13** Ambient monster content (per-flavor enemies + loot tables)
- **P4-5** Arena character layer — ✅ **DESIGNED** (`design/arena-mode.md` §"The character layer" + brief `product-requirements/arena-part-draft-and-catalog.md`): in-match **parts draft** off a shared board (floor + union of unlocked catalogs), passive Journey **part-catalog** (not a hero deck), fenced budget guard. Deck rejected.
- **P5-1** Palette swatches (master + per-biome + per-archetype)
- **P5-2 / P5-3 / P5-4** World backdrop + site dressing kits + decoration assets
- **P5-5** Stomach-interior backdrop art
- **P5-6** Production body-part meshes (to the authoring contract)
- **P5-10** Marker/name art + animation direction + VFX language
- **P6-10** Biome↔archetype affinity + emergent-fusion authoring density
- *(from `design/roadmap.md`)* Reveal-spine beats · NPC/story/quest content · Order's Seat + Hub design · Escalation design

---

## P0 — Gates
*Unblock the most downstream work.*

| ID | Task | Model | Owner | Unblocks |
|---|---|---|---|---|
| **P0-1** | Race roster & signature markers — ✅ **done (design)**; brief `race-roster-and-passport.md` | Opus (design) | **Designer** | narrative content · per-race body-plans · marker parts · silhouette sheets |
| ~~**P0-2**~~ | ~~Biome selection along the run (unhardcode Forest)~~ ✅ shipped 2026-07-04 (`biome-journey.md`); follow-ups filed in `ROADMAP.md` (real Mtn/Desert rosters · per-stretch route character · D19 consumers) | Opus | Code | biome-visual payoff · per-biome monster/loot · race homelands (brief `race-roster-and-passport.md`) |
| ~~**P0-3**~~ | ~~Races as data + part race-tags + passport acceptance-tier fact~~ ✅ shipped 2026-07-04 (`races-passport.md`); follow-ups filed in `ROADMAP.md` (belonging-colour consumer · species-vs-race reconcile · un-equip event) | Opus | Code | passport-gated narrative (Pillar 2) · per-race body-plans (P2-1); consumed brief `race-roster-and-passport.md` |

## P1 — Core loop & moment-to-moment
*Ready now, high felt-value, mostly cheap, parallelisable. None need Fable.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P1-1** | Part-Blank loot sources (enemy-remains + landmark finds) | Opus | Hybrid | Socketed Blanks done; you author drop pools |
| **P1-2** | Biome visual styles (per-theme SO + feature pool) | Opus+art | Hybrid | system=code, look=you; softly ← P0-2 |
| **P1-3** | Medallion socket UI + confirm-before-unseal | Opus | Code | Socketed Blanks + mutation-cards done |
| **P1-4** | Enemy move-destination telegraph | Sonnet | Code | intent-phase done |
| **P1-5** | Quest reward = rolled by tier + archetype-bias | Opus | Code | artifact tier done |
| **P1-6** | Quest-offer card visual treatment | Sonnet+art | Hybrid | plan drafted; reuse card grammar |
| **P1-7** | Attack card — the Monster verb | Opus | Code | encounter card-hand done |
| **P1-8** | Competing / mutually-exclusive same-tier offers | Opus | Code | cross-dialogue continuity done |
| **P1-9** | Several quest offers per NPC | Opus | Code | card-hand done |
| **P1-10** | Cauldron-voice tempter + socketing-trend barks | Sonnet | Hybrid | you write the barks |
| **P1-11** | Quest log UI | Sonnet | Code | progression record done |
| **P1-12** | Non-item reward receiving systems (Currency/Experience/Ability) | Opus | Code | cross-cutting gap |
| **P1-13** | Ambient monster content (per-flavor enemies + loot tables) | author | **Designer** | consuming systems exist |
| **P1-14** | Sites — occupancy/passport/tier/biome schema fields | Opus | Code | deferred until consumers exist |

## P2 — The long poles (where Fable 5 goes)
*Hardest, cross-system, high-ambiguity. Spend the premium model here and nowhere else.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| ~~**P2-1**~~ | ~~Independent body-plans + skeleton-swap runtime~~ ✅ shipped 2026-07-04 (`character-system.md` R19–R24): machinery + serpent/spider placeholder frames + confirm-and-shed + part stash; follow-ups filed in `ROADMAP.md` (re-install flow · dormant UX · per-frame host fit) | **Fable** | Code | superseded character-system R10/R3; consumed brief `body-plan-skeleton-swap.md`; production frame meshes = P5-6 |
| **P2-2** | Save/load — file IO (R14) + window/horizon state + run-state | **Fable** | Code | **spec-ready** — verified brief `product-requirements/save-continue-run.md` (2026-07-05): autosave + Continue, **permadeath** (death consumes the save, no slots/scum) · **whole-run** image (owner chose run-as-one-image, not narrative-only — folds in hero body/inventory/position) · **platform-clean** savepoints (mid-encounter re-begins) · separate always-on **cross-run meta store** + one demo persistent fact · fail-safe independent files. Unblocks **P3-3** + mirror-lore; the meta **consumers** stay P3-3 |
| ~~**P2-3**~~ | ~~Director depth — R8 first-class threads + cross-window continuity + run/meta fact boundary~~ ✅ shipped 2026-07-05 (`narrative-procedural.md` §2.6/§3): Thread entity + ephemeral/arc + conflict-fail (incl. arc) + expiry + concurrency cap + no-stale-re-placement ledgers + run/meta `_horizon` partition for **P2-2**; follow-ups filed in `ROADMAP.md` (OR-resolution → P3-5 · thread readout → P1-11 · meta store → P2-2) | **Fable** | Code | consumed brief `director-threads-and-continuity.md`; D7/D19/D20-consumers stay P3 |
| **P2-4** | Smarter ability-using enemy AI | Opus *(Fable opt.)* | Code | combat depth |

## P3 — Systems depth & correctness
*Opus by default, Sonnet where scoped. Reconcile the stale narrative-gen doc (P6-1) before P3-8.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P3-1** | Director — D7 reserved spine lane + reveal cap | Opus | Code | ≤1–2 reveals/run |
| **P3-2** | Director — D19 escalation tier gating | Opus | Hybrid | ← escalation design (you) |
| **P3-3** | Director — D20 meta-scoped fact horizon (long-arc **consumers** reading meta) | Opus | Code | **after P2-2**; the run/meta **partition** now lands earlier in **P2-3**, so this is the cross-run reading side only (mirror-lore echoes, cauldron memory, spine cursor) |
| **P3-4** | Reactive-rule cascade layer (R11) | Opus | Code | optional central cascade |
| **P3-5** | OR / boolean precondition composition | Opus | Code | AND-only today |
| **P3-6** | Progression — AND/OR conditions + StoryNodeRequirement gating | Opus | Code | consume Required* fields |
| **P3-7** | Progression — generic `record_choice` Ink fn | Sonnet | Hybrid | data-driven key-choice capture |
| **P3-8** | Narrative-gen P1–P4 (compat scoring, quotas, progression-driven, cooldowns) | Opus | Code | **after P6-1** — fold into streaming director |
| **P3-9** | Live fact-driven NPC marker refresh | Opus | Code | per-frame re-eval |
| **P3-10** | Per-NPC / per-archetype interaction radius overrides | Sonnet | Code | global-only today |
| **P3-11** | Combat — more displacement kinds (pull / dash / hook) | Opus | Code | push-only today |
| **P3-12** | Combat — ring push + queue-simulation preview + initiative order | Opus | Code | Track-C follow-ups |
| **P3-13** | Ability — passive modifiers beyond outgoing damage (HP/def/heal) | Opus | Code | add stat-target dimension |
| **P3-14** | Loot — capacity feedback (R11) + progression gating (R13) + despawn on leave | Sonnet | Code | rebind capacity policy |
| **P3-15** | Platform — unit grounding per-model override + ContentSpawner on concave islands | Sonnet | Code | place on CenterCell |
| **P3-16** | Platform — camera/entry pass at arena scale | Opus | Code | play-mode tune |
| **P3-17** | Sites — `NPC·quest-bearer` as a fill beat (Camp's shady offer) | Opus | Hybrid | **spec-ready** — verified brief `product-requirements/camp-shady-offer.md` (2026-07-05): shady = derived from outlaw occupancy (no new flavor) · passport-free (any beast) · boss fronts camp, crew fight *behind* the deal · power/combat currency + cauldron tempter · deterministic, no asset/kind change |

## P4 — Arena (the networked half)
*Self-contained, Pillar-4-only. Offline arena ships; this is the netcode.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P4-1** | Networked FFA session (Phase 3, 2–4 players, host/join on NGO) | Fable/Opus | Code | transport seam network-shaped |
| **P4-2** | Host-side commit validation (anti-cheat) | Opus | Code | MVP trusts peers |
| **P4-3** | Seeded-shuffle resolution alt + per-step damage batching | Opus | Code | drop-in behind IArenaResolutionOrder |
| **P4-4** | Rename `EnemyIntent → CommittedIntent` + arena camera pass | Sonnet | Code | mechanical; needs compiler |
| **P4-5** | ✅ **DESIGNED** — parts draft + tasted-forms catalog (model decided; deck rejected) | Opus (design) | **Designer** | brief `arena-part-draft-and-catalog.md`; code = draft phase + meta catalog + shared-board seeding |

## P5 — Visual, art & production
*The M5 look pass. Heavily your track; code only builds the seams that consume your assets.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P5-1** | Palette swatches (master + per-biome + per-archetype) | art | **Designer** | turns the bible into a tool |
| ~~**P5-2**~~ | ~~Natural landscape read: routed path + elevation tiers + world backdrop~~ ✅ shipped 2026-07-04 (`world-landscape.md`); your half that remains = silhouette/landmark meshes via **P5-4** | Opus+art | Hybrid | gen=code done; silhouettes=you; brief `world-backdrop-and-elevation.md` |
| **P5-3** | Site dressing kits + biome×site material matrix | Opus+art | Hybrid | SiteStamp seam exists |
| **P5-4** | Decoration asset-gen pipeline + biome features/props | tooling | Hybrid | feeds P1-2 + P5-3 |
| **P5-5** | Stomach-interior inventory backdrop | Sonnet+art | Hybrid | no scene swap; art=you |
| **P5-6** | Production body-part assets | art | **Designer** | to the authoring contract |
| **P5-7** | Part / animation integration workflow | Opus | Hybrid | pairs with authoring contract |
| **P5-8** | Muted→crisp render + figure-ground shader spike (tech-art) | Opus | Hybrid | readability without outlines |
| **P5-9** | Ghost-caster ability animation (tech-art) | Sonnet | Hybrid | `_animationTrigger` unused; clips=you |
| **P5-10** | Marker/name art + animation direction + VFX language | art | **Designer** | art-direction pass |
| **P5-11** | Character locomotion polish (clips, accel, yaw, wall-velocity) | Sonnet | Hybrid | clips=you, logic=code |
| **P5-12** | Mutation card VFX + animated mini-model + full live-hero preview | Sonnet | Hybrid | real tier-glow shader |
| **P5-13** | Signature-vs-emergent craft presentation | Sonnet | Code | distinct effect for authored results |

## P6 — Debt, tooling & demo hardening
*Opportunistic, cheap models. P6-1 precedes P3-8.*

| ID | Task | Model | Owner | Note |
|---|---|---|---|---|
| **P6-1** | Reconcile/retire `narrative-generation.md` · refresh `loot-subsystem.md` | Sonnet | Code | **before P3-8** |
| **P6-2** | Remove branching-choice dialogue UI (card hand landed) | Sonnet | Code | retire OnChoices + migrate .ink |
| **P6-3** | Extract neutral `Core.Hex` namespace (~43 files) | Sonnet | Code | mechanical; needs compiler |
| **P6-4** | Remove dead platform-loot API · duplicate CombatInputModeManager | Haiku | Code | delete no-caller code |
| **P6-5** | Small cleanups (WorldArtifactView dup · TelegraphStyle→SO · AbilityTooltip · Hero prefab · part-data coupling) | Sonnet | Code | extract on 2nd consumer |
| **P6-6** | Dev tools — more sections · mutating controls · configurable key | Sonnet | Code | rack/quest/actor read-outs |
| **P6-7** | Logging — in-game level control · installer bootstrap seam | Sonnet | Code | optional dev panel |
| **P6-8** | PartSwap / SocketMounter play-mode tests | Sonnet | Code | manual-verified today |
| **P6-9** | Demo content sharpeners (barn loss, recast, fact-web, no-forced-combat NPC, PerLocation, D12) | Sonnet | Hybrid | content + small engine bits |
| **P6-10** | Biome↔archetype affinity + emergent-fusion authoring density | author | **Designer** | author more artifacts / bias |
| **P6-11** | Runtime skinned-mesh combining (perf) · cauldron-will stochastic surprise | Opus | Code | both deferred — only if needed |
