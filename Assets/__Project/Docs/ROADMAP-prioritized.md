# Prioritised Build Plan

> A **priority-ordered cross-cut** of `ROADMAP.md` (which is organised by system). Every open item is
> grouped into build **waves P0–P6** and carries a **stable ID** (`P0-1`, `P1-3`, …) so it can be
> referenced by number. This is a planning index, not a system doc — `ROADMAP.md` stays the maintained
> per-system source of truth; when an item ships there, strike it here too.

**Ordering rule.** A prerequisite always sits above its dependents (race roster → body-plans;
save/load → D20 & mirror-lore; biome-selection → biome-visual).

**Model** is a recommendation, not a lock. Default is **Opus 4.8**. **Fable 5** goes to the current
long pole where correctness on a hard, cross-system change outweighs its premium cost. The **three P2
poles** (body-plans · save/load · director-threads) **all shipped 2026-07-05**, so the Fable lane is now
**Track I — Director Depth II** (owner call 2026-07-05): its core P3-1 (spine reveal lane) + P3-3
(cross-run meta-consumers) — **both shipped 2026-07-06; the Fable lane is clear** (next Fable
assignment is an owner call). **Sonnet 5 / Haiku** carry scoped and mechanical work so the budget isn't
burned. (`art` = the model just wires the seam; the asset itself is authored by the designer.)

**Owner.** `Designer` = product-owner work (design decisions + content/art authoring), done in parallel,
gates nothing on the coding model. `Code` = coding model. `Hybrid` = code builds the system, the
designer authors the content it consumes.

**Sources.** Open items of `ROADMAP.md` + the 2026-07-03 inbox briefs (`product-requirements/`) + the
design-track gates that live in `design/roadmap.md` (race roster, apex, hub, cauldron voice, reveal
spine, escalation).

---

## Track map *(bird's-eye — the initiatives, 2026-07-05)*

> The **P0–P6 waves** below are the ID index; the **letter Tracks D–N** are the *initiatives* the work
> actually ships in (each groups adjacent P-IDs). **P0–P2 are done** (gates · core P1 landed piecemeal ·
> the three Fable poles). Recommended order of what's left: **D–G (active) → I → H (∥) → J → K → L → N → M.**

| Track | Theme | Model | Order | State |
|---|---|---|---|---|
| **D** | Bandit camp & combat legibility II (humanoids · initiative/turn-order · ability animation · **D4** camp shady offer · **D5** clear dead units on death · **D6** unit uniformity · **D7** aim-input redesign · **D8** move-anim & highlight-reset bugs) | Opus | active | D1–D3 shipped; D4–D8 spec-ready |
| ~~**E**~~ | Environment dressing — demo kits (binding/swap · biome decor · site/camp · backdrop fill) | Opus+art | **done** | **E1–E5 all shipped 2026-07-06** (`environment-dressing.md`): kit contract · biome kits · site/camp kits · backdrop scatter · footprint/edge-fit |
| **F** | The Cauldron View (spatial spine · liquid+fullness · stable brew physics · re-homed medallion · stomach backdrop) | Opus+art | active | F1 gates F2/F3; all spec-ready |
| **G** | Arena / multiplayer polish (seat camera + oriented backdrop · dev console · hex-highlight bug · ~~**G4** parts-draft screen UI~~) | Opus/Sonnet | active | G1–G3 spec-ready (G1+G3 verified together); **G4 + P4-5 shipped 2026-07-06** (draft model + screen + shared ability-preview popover) |
| **O** | The Hub (staging scene · starting-part + starting-biome choices · cauldron voice · death-return) | Opus | new | **O1 shipped 2026-07-06** (owner-revised: tasted-pool offer, bare launch, tier-1 homelands); follow-ups filed in `ROADMAP.md` |
| **I** | **Director Depth II** — spine reveal lane · cross-run meta-consumers · escalation · OR-composition | **Fable** (P3-3) + Opus | **1st after D–G** | **the Fable pair is done** — P3-1 + P3-2 + **P3-3 all shipped 2026-07-06**; remainder = P3-5 → P3-8 (Opus) |
| **H** | Quest-as-Reward economy — rolled reward · offer card · attack card · competing/multi offers · quest log · reward sinks | Opus | ∥ with I | P1-5 gates P1-6; mostly spec-ready |
| **J** | Crafting & mutation loop completion — blank loot · **un-equip/re-install gap** · trend bark · craft presentation | Opus | after H | P1-1 brief-ready; rest scoped |
| **K** | Combat depth II — smarter AI · pull/dash/hook · speed initiative · passive stat modifiers | Opus *(Fable opt. AI)* | **after D** | scoped |
| **L** | Systems depth & correctness — marker refresh · radius overrides · loot/platform/progression correctness | Opus/Sonnet | opportunistic | scoped |
| **M** | Production art / M5 look — palette · production meshes · shader spike · VFX · locomotion polish | Designer+art | parkable | Track E covers demo look |
| **N** | Debt & tooling — remove branching UI · Core.Hex · dead code · dev-tools · arena hardening (P4-2/3/4) | Haiku/Sonnet | background | P6-2 ready to execute |

*Also live: the **Designer parallel track** (below) — content/art authoring that gates nothing on code.*

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
| ~~**P1-2**~~ | ~~Biome visual styles (per-theme SO + feature pool)~~ ✅ shipped 2026-07-06 with **Track E · E2** (`environment-dressing.md`): feature-kit binding + placement model + Desert/Forest demo kits; palette-key/light residue filed in `ROADMAP.md` | Opus+art | Hybrid | consumed brief `biome-visual-styles.md` via `biome-decoration-kits-demo.md` |
| **P1-3** | Medallion socket UI + confirm-before-unseal | Opus | Code | Socketed Blanks + mutation-cards done · **now Track F/F4** (re-homed into the under-pot ribbon) |
| **P1-4** | Enemy move-destination telegraph | Sonnet | Code | intent-phase done · **D3 shipped the direction arrow** (2026-07-05); a full step-by-step **path** line stays open |
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
| ~~**P2-2**~~ | ~~Save/load — file IO (R14) + window/horizon state + run-state~~ ✅ shipped 2026-07-05 (`save-persistence.md`): platform-entry autosave + MainMenu Continue · permadeath (Defeat flushes meta then consumes `run.json`) · whole-run image (recorded windows + cursors, hero body, inventory/rack/socketing, quests, facts/threads/actors, seed + RNG) · deterministic resume · always-on `meta.json` + demo fact `world.barn_bounty_honored` · fail-safe versioned/atomic independent files; follow-ups filed in `ROADMAP.md` (W3-1 option-b · currency model · PlatformEvents→signals) | **Fable** | Code | consumed brief `save-continue-run.md`; **unblocks P3-3** (meta consumers) |
| ~~**P2-3**~~ | ~~Director depth — R8 first-class threads + cross-window continuity + run/meta fact boundary~~ ✅ shipped 2026-07-05 (`narrative-procedural.md` §2.6/§3): Thread entity + ephemeral/arc + conflict-fail (incl. arc) + expiry + concurrency cap + no-stale-re-placement ledgers + run/meta `_horizon` partition for **P2-2**; follow-ups filed in `ROADMAP.md` (OR-resolution → P3-5 · thread readout → P1-11 · meta store → P2-2) | **Fable** | Code | consumed brief `director-threads-and-continuity.md`; D7/D19/D20-consumers stay P3 |
| **P2-4** | Smarter ability-using enemy AI | Opus *(Fable opt.)* | Code | combat depth |

## P3 — Systems depth & correctness
*Opus by default, Sonnet where scoped. Reconcile the stale narrative-gen doc (P6-1) before P3-8.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| ~~**P3-1**~~ | ~~Director — D7 reserved spine lane + reveal cap~~ ✅ shipped 2026-07-06 (`narrative-procedural.md` R15/§2.6): reserved pre-slot lane for `_isSpine` beats, per-run cap (`_maxSpineRevealsPerRun`, default 2) + ≤1/window, gated pool (per-beat floors over new meta `world.run_count`), revealed=placed in `StoryRunLedger`, quest-gate + thread-ceiling bypass; 1341/1341 green; placed-vs-seen → P3-3 | **Fable** | Code | consumed brief `director-spine-reveal-lane.md`; **unblocks P3-3** (the lane the cursor rides) |
| ~~**P3-2**~~ | ~~Director — D19 escalation tier gating~~ ✅ shipped 2026-07-06 (`narrative-procedural.md` §2.6): run-tier band on `StoryTemplate` + `EnemyDefinition`; planner gates story eligibility + ambient/site monster draw by `run_escalation_tier`; pool-shift only (no stat multiplier, no density change); unbanded = every tier | Opus | Hybrid | ← escalation design (you) |
| ~~**P3-3**~~ | ~~Director — D20 meta-scoped fact horizon (long-arc **consumers** reading meta)~~ ✅ shipped 2026-07-06 (`narrative-procedural.md` R15/§2.6/§4): spine cursor = `world.<storyId>.spine_seen` (**seen, not placed** — PO call; `SpineSeenRecorder` writes, the lane's channel-split reads) + mirror-lore echo pair over persisted deeds + deepening cauldron-memory beat; sibling-exclusion/"seen Y" floors = authored literal-subject preconditions (data-only); 1355/1355 green | **Fable** | Code | consumed brief `director-meta-consumers.md`; known limitation: echo pairs at cap ≥ 2 (run spine at cap 1) |
| **P3-4** | Reactive-rule cascade layer (R11) | Opus | Code | optional central cascade |
| **P3-5** | OR / boolean precondition composition | Opus | Code | AND-only today; **scope-locked**: OR+AND only, no NOT/nesting (2026-07-05) |
| **P3-6** | Progression — AND/OR conditions + StoryNodeRequirement gating | Opus | Code | consume Required* fields |
| **P3-7** | Progression — generic `record_choice` Ink fn | Sonnet | Hybrid | data-driven key-choice capture |
| **P3-8** | Narrative-gen **residue** (P4 soft cooldowns + opt. P1 compat-scoring; P2/P3 superseded) | Opus | Code | **after P6-1** — re-scoped 2026-07-05, fold residue into streaming director |
| **P3-9** | Live fact-driven NPC marker refresh | Opus | Code | per-frame re-eval |
| **P3-10** | Per-NPC / per-archetype interaction radius overrides | Sonnet | Code | boss carve-out shipped with **D1** (`_bossEngagementRadius`); the general authored surface stays open |
| **P3-11** | Combat — more displacement kinds (pull / dash / hook) | Opus | Code | push-only today |
| **P3-12** | Combat — ring push + queue-simulation preview + initiative order | Opus | Code | Track-C follow-ups |
| **P3-13** | Ability — passive modifiers beyond outgoing damage (HP/def/heal) | Opus | Code | add stat-target dimension |
| **P3-14** | Loot — capacity feedback (R11) + progression gating (R13) + despawn on leave | Sonnet | Code | rebind capacity policy |
| **P3-15** | Platform — unit grounding per-model override + ContentSpawner on concave islands | Sonnet | Code | place on CenterCell |
| **P3-16** | Platform — camera/entry pass at arena scale | Opus | Code | play-mode tune |
| **P3-17** | Sites — `NPC·quest-bearer` as a fill beat (Camp's shady offer) — **re-homed to Track D as D4** (finishes the camp) | Opus | Hybrid | **spec-ready** — verified brief `product-requirements/camp-shady-offer.md` (2026-07-05). See **D4**. |

## P4 — Arena (the networked half)
*Self-contained, Pillar-4-only. Offline arena ships; this is the netcode.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| ~~**P4-1**~~ | ~~Networked FFA session (Phase 3, 2–4 players, host/join on NGO)~~ ✅ shipped (Arena MVP, `arena-mode.md` §2.3): host/join by `ip[:port]` on NGO, seed + seat roster broadcast, lockstep on NGO named messages; hardening (P4-2 anti-cheat · P4-3 resolution alt · P4-4 rename) stays open in **Track N** | Fable/Opus | Code | transport seam network-shaped |
| **P4-2** | Host-side commit validation (anti-cheat) | Opus | Code | MVP trusts peers |
| **P4-3** | Seeded-shuffle resolution alt + per-step damage batching | Opus | Code | drop-in behind IArenaResolutionOrder |
| **P4-4** | Rename `EnemyIntent → CommittedIntent` + arena camera pass | Sonnet | Code | mechanical; needs compiler |
| ~~**P4-5**~~ | ~~Parts draft + tasted-forms catalog~~ ✅ shipped 2026-07-06 with **G4** (`arena-mode.md` §2.9): host-composed shared board (floor + catalog union), snake draft with denial, `world.<partId>.arena_tasted` meta catalog, lockstep picks + host auto-pick; follow-ups filed in `ROADMAP.md` (loadout hash checkpoint · real thumbnails · tasted-trigger widening) | Opus | Code | consumed brief `arena-part-draft-and-catalog.md` |

## P5 — Visual, art & production
*The M5 look pass. Heavily your track; code only builds the seams that consume your assets.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P5-1** | Palette swatches (master + per-biome + per-archetype) | art | **Designer** | turns the bible into a tool |
| ~~**P5-2**~~ | ~~Natural landscape read: routed path + elevation tiers + world backdrop~~ ✅ shipped 2026-07-04 (`world-landscape.md`); your half that remains = silhouette/landmark meshes via **P5-4** | Opus+art | Hybrid | gen=code done; silhouettes=you; brief `world-backdrop-and-elevation.md` |
| **P5-3** | Site dressing kits + biome×site material matrix | Opus+art | Hybrid | **demo half shipped 2026-07-06** (Track E · E3 — `SiteStamp` seam consumed); remainder = production kits + material matrix + Ruin/Lair |
| **P5-4** | Decoration asset-gen pipeline + biome features/props | tooling | Hybrid | feeds P1-2 + P5-3 |
| **P5-5** | Stomach-interior inventory backdrop | Sonnet+art | Hybrid | no scene swap; art=you · **now Track F/F5** |
| **P5-6** | Production body-part assets | art | **Designer** | to the authoring contract |
| **P5-7** | Part / animation integration workflow | Opus | Hybrid | pairs with authoring contract |
| **P5-8** | Muted→crisp render + figure-ground shader spike (tech-art) | Opus | Hybrid | readability without outlines |
| ~~**P5-9**~~ | ~~Ghost-caster ability animation (tech-art)~~ ✅ shipped 2026-07-05 as a **code placeholder** in **D3** (animated ghost = translucent cell-sweep; live = opaque). Remaining art seam: production clips/VFX + consuming `_animationTrigger` per ability (Track M / render-look) | Sonnet | Hybrid | resolved by D3; real clips=you |
| **P5-10** | Marker/name art + animation direction + VFX language | art | **Designer** | art-direction pass |
| **P5-11** | Character locomotion polish (clips, accel, yaw, wall-velocity) | Sonnet | Hybrid | clips=you, logic=code |
| **P5-12** | Mutation card VFX + animated mini-model + full live-hero preview | Sonnet | Hybrid | real tier-glow shader |
| **P5-13** | Signature-vs-emergent craft presentation | Sonnet | Code | distinct effect for authored results |

## P6 — Debt, tooling & demo hardening
*Opportunistic, cheap models. P6-1 precedes P3-8.*

| ID | Task | Model | Owner | Note |
|---|---|---|---|---|
| ~~**P6-1**~~ | ~~Reconcile/retire `narrative-generation.md` · refresh `loot-subsystem.md`~~ ✅ shipped 2026-07-06: `narrative-generation.md` retired to a tombstone (§4 preserved as the P3-8 residue); `loot-subsystem.md` refreshed to the streaming/density path; dead loot API flagged for P6-4 | Sonnet | Code | **before P3-8** |
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

---

## Track D — Bandit Camp & Combat Legibility II *(initiative, verified 2026-07-05)*

*One initiative, three verified PO briefs — turns the demo's placeholder **capsule** enemies into a living
**humanoid bandit camp** and finishes the combat-legibility layer Track C deferred. Meant to land in one of the
next build passes and be referenced as a single initiative. The three briefs are largely independent (the
world half vs the two combat halves) and can parallelise; the two combat briefs share a combat pass and are
best built together. Subsumes/advances several already-listed items (noted per row). **D6–D8** are
**play-test follow-ups on the shipped D2/D3** (2026-07-05): a unit-uniformity bug cluster (D6), an
owner-decided aim-input redesign that supersedes D2's hold-to-aim (D7), and two presentation bug fixes
(D8) — all combat-legibility, best folded into the next combat pass.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| ~~**D1**~~ | ~~Humanoid bandit camp~~ ✅ shipped 2026-07-05 (`world-sites.md` boss-led camp · `npc-proximity-interaction.md` R10–R12 · `character-system.md` demo tint): humanoid enemies (capsule retired by data) · role tints · boss + seeded crew 2–4 · larger platform-scoped boss circle (talk-or-fight on cross) · no aggro on landing · crew joins the one fight; platform scoping applied to **all** NPCs (PO decision); follow-ups in `ROADMAP.md` (boss re-engagement · boss visual continuity · general radius override) | Opus | Hybrid | consumed brief `bandit-camp-humanoids.md`; **subsumed P3-10** for the boss's reach; boss quest *content* stays a placeholder for **P3-17** |
| ~~**D2**~~ | ~~Combat — initiative (initiator acts first) + horizontal top-right turn-order strip + hold-Enter-turn-to-cursor / release-Enter-to-execute facing input~~ ✅ shipped 2026-07-05 (`combat-round-and-telegraph.md` R4/R4a/R4b · `ability-subsystem.md` R8): `CombatInitiator` captured at the 3 engagement sites (player Attack vs. `start-combat:` tag vs. ambush) → `CombatController.StartRound` reorders the opening round enemy-first when enemy-led (`RoundLeadPolicy`) · code-built top-right turn-order strip (PvE only; Arena keeps `IArenaResolutionOrder`) · Enter hold-to-aim/release-to-execute + right-click cancel; deterministic, no ability change | Opus | Code | consumed brief `combat-initiative-and-turn-queue.md`; **advanced P3-12** (initiative-order half; the *speed*-based half + multi-round policy stay deferred to Track K) |
| ~~**D3**~~ | ~~Combat — code-authored placeholder ability animation + enemy move-direction arrow + committed-queue readiness cue~~ ✅ shipped 2026-07-05 (`combat-round-and-telegraph.md` R15–R18): shape-driven **cell-sweep** placeholder (`AbilityCellFlash`/`AbilityAreaSweep`) played translucent as the animated ghost + opaque on live execution via a shared executor **`AbilityFiredCue`** sink (player queue + enemy paced resolve — enemy action now visibly not instant) · board **move arrow** replaces the `»` glyph (`EnemyIntentTelegraphView`) · enemy **readiness cue** = restless plan icons + placeholder wind-up pose (transform lean/scale/bob; enemies only, uniform, cleared on resolve); presentation only, PvE-wired | Opus | Code | consumed brief `combat-ability-animation.md`; **resolved P5-9** (ghost-caster animation — code placeholder; production clips/`_animationTrigger` stay the art seam) + **advanced P1-4** (move-intent → arrow) |
| **D4** | Sites — the Camp's **shady offer** as an `NPC·quest-bearer` fill beat (the boss's real quest content, D1 shipped it as a placeholder) — **re-homed here from P3-17** since it finishes the camp | Opus | Hybrid | **spec-ready** — verified brief `product-requirements/camp-shady-offer.md` (2026-07-05): shady = derived from outlaw occupancy (no new flavor) · passport-free (any beast) · boss fronts camp, crew fight *behind* the deal · power/combat currency + cauldron tempter; deterministic, no asset/kind change. **Is P3-17.** |
| **D5** | Combat — **clear a dead unit from the board the moment it dies**, not at battle end. Today a killed unit stays in its cell until the whole fight resolves — it still reads as **occupying the cell** (movement/targeting treat the corpse as a blocker) and clutters the multi-enemy read. Remove it on death: **free the cell** + drop the model, so the "fast chess" stays legible (Pillar 4). **Placeholder = instant removal now; a death animation comes later** (rides D3's code-authored-placeholder approach). The **corpse-loot / "you are what you eat" drop must still fire** on death (separate combat/mutation channel — removal must not skip it) | Sonnet | Code | **task description is enough** (no brief); combat-legibility follow-up; advances the Track-C read-the-board layer |
| **D6** | Combat — **unit uniformity: one code path for all units** (a play-test bug cluster + one legibility requirement, all from D3). **Symptoms:** the D3 **move-direction arrow does not render for the bandit crew and some single enemies**; the **crew spawns stacked in a single cell** (violates one-unit-per-cell); the **hero can spawn onto an already-occupied cell** at battle start. **Requirement:** **every** unit — hero, single enemy, seeded crew — resolves **spawn placement, facing, and intent-telegraph through one uniform path**: no two units ever share a cell, and **every unit orients toward its next move/action** (uniform facing, not just some). **Likely root (owner's read):** divergent per-origin combat code (hero vs boss vs crew vs ambient enemy) — **diagnose and unify**; the arrow/stacking/facing defects are symptoms of the same split. | Opus | Code | **task description is enough** (no brief); combat-legibility (Pillar 4); hardens D2/D3 (`combat-round-and-telegraph.md` R4/R15–R18) + the one-unit-per-cell invariant; pairs with **D8** |
| **D7** | Combat — **aim input redesign: short-press fires, long-press aims** (owner change to D2's input). **New scheme:** a **short Enter tap** discharges the queued volley immediately **along the hero's current facing** (no aim step); **holding Enter** enters **aim / orientation mode** (turn the hero toward the cursor, re-pointing the whole queue + ghosts), and **releasing Enter** discharges along the aimed facing. **Right-click still cancels.** The press-duration threshold is a tunable. **Supersedes** the shipped D2 hold-to-aim/release-to-execute (`combat-round-and-telegraph.md` R4b) — a deliberate reversal so fast play needs no re-aim. | Opus | Code | **task description is enough** (no brief); interaction change over shipped D2; deterministic, no ability change |
| **D8** | Combat — **presentation bug fixes** (two independent read-the-board defects from D3 play-test). **(a) Smeared enemy move animation** — an enemy's move reads as a **teleport**: the figure **vanishes in the source cell and pops into the neighbour**, recoiling slightly, instead of gliding; replace with a **smooth cell-to-cell move** (rides D3's paced enemy resolve, `combat-round-and-telegraph.md` R15–R18). *(If the crew moves via a divergent path, may share **D6**'s root.)* **(b) Planning-outline not reset** — the plan-phase **cell outline sometimes fails to reset to the default (black)** after planning ends, leaving cells **stuck yellow for the rest of the fight**; the highlight must always clear back to default when the plan phase closes. | Sonnet | Code | **task description is enough** (no brief); combat-legibility follow-ups; (b) is a highlight-state reset defect (cf. Arena **G3**, a different scene) |

---

## Track E — Environment Dressing (Demo Kits) *(initiative, verified 2026-07-05)*

*One initiative, four verified PO briefs — carve out world **decoration** + the **background behind the
platforms** (the Windblown read), wired now with two Unity Asset Store packs as **demo/testing** content
that swaps to production by repointing one field. ~90% is populating already-designed seams (biome feature
pool · site dressing theme · world backdrop); the one new design decision is the **swap contract** (E1),
on which the three population briefs depend. Advances/consumes the P5 look-pass items (P1-2 · P5-2 remainder
· P5-3 · P5-4) and satisfies the **demo half** of P5-4's asset-gen (store packs instead of generated meshes).
E1 gates E2–E4; the three population briefs then parallelise. **Status 2026-07-06: the whole track
shipped** (`environment-dressing.md` + CHANGELOG) — E1+E2+E3 in one pass, then E4 (backdrop scatter,
the additive third kit kind) and E5 (footprint/edge-fit polish) as follow-ups.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| ~~**E1**~~ | ~~Dressing-kit **binding & swap**~~ ✅ shipped 2026-07-06 (`environment-dressing.md` R1–R6): data-only kit SOs (abstract base + biome-feature + site-dressing kinds) · **whole-kit binding** (`BiomeAppearanceDefinition._featureKit` / `SiteStamp.DressingThemeId` → kit) · demo quarantine `Resources/World/Dressing/Demo/` + `DemoPackQuarantineTests` living guard · bind-time tone treatment (URP-Lit rebuild, muted biome-key lerp — also fixes pack Standard materials under URP) · fail-safe base layer (no kit → `Empty` plan) | Opus | Hybrid | consumed brief `dressing-kit-binding-and-swap.md`; gated E2–E4 |
| ~~**E2**~~ | ~~Biome **decoration kits (demo)**~~ ✅ shipped 2026-07-06 **with the P1-2 placement model**: `PlatformHexSurface.BlockedCells` live (grid/anchors exclude), clustered homogeneous decoratives (multi-per-cell), sparse whole-cell blockers under battlefield-min/lane/connectivity guards, rim/rear bias; Desert + Forest demo kits + ground materials authored; Mountain/Cave stay base-layer (follow-up in `ROADMAP.md`) | Opus+art | Hybrid | consumed brief `biome-decoration-kits-demo.md`; **shipped P1-2's core** (palette/light residue filed) |
| ~~**E3**~~ | ~~Site & camp **dressing kits (demo)**~~ ✅ shipped 2026-07-06: Settlement kit (buildings on a **block-shared skyline band**, gate on the anchor's approach edge, street ground; Village + City re-keyed to one `settlement-kit`) + Camp kit (hanger-campfire focal + facing prop ring, packed dirt); per-biome recolour via the tone treatment; follow-ups (Ruin/Lair kits · material matrix · gradient/alignment · prop animation) in `ROADMAP.md` | Opus+art | Hybrid | consumed brief `site-camp-dressing-kits-demo.md`; **shipped P5-3's demo half**; stayed orthogonal to D1 |
| ~~**E4**~~ | ~~World **backdrop fill (demo)**~~ ✅ shipped 2026-07-06 (`environment-dressing.md` R20–R22): the **third kit kind** (`BackdropKitDefinition`, bound whole-kit from `BiomeAppearanceDefinition._backdropKit` — the additive subclass E1 foretold) · pure `BackdropScatterPlanner` (fixed X slots, per-slot stream, streaming-safe, low density) · world-fixed scatter placed along the camera's yawed depth axis + ortho drop, hazed (`ToneMaterialCache.GetHazed`), colliderless; Desert + Forest demo backdrop kits; Mountain/Cave = ridge rig only | Opus+art | Hybrid | consumed brief `world-backdrop-fill-demo.md`; finishes the **P5-2 remainder**; parallax/sky-anim/escalation stay deferred (`world-backdrop.md` §5) |
| ~~**E5**~~ | ~~Decoration **footprint & edge-fit** (polish)~~ ✅ shipped 2026-07-06 (`environment-dressing.md` R17–R19, R16a): per-entry rough footprint (kind defaults + override) · deterministic inward nudge (`PlatformEdgeFit`) so grounded props sit fully on the platform · rim-overhang now **opt-in** per entry (`_mayOverhang`, demo: desert Tree_01) · **extended to site kits** (camp rings/gates nudged, house/fire cells edge-margined) after playtest; density/clustering/determinism unchanged | Opus | Code | consumed brief `decoration-footprint-and-edge-fit.md`; out-of-scope residue (mesh-derived bounds · prop-vs-prop spacing · pivot-not-at-base) noted in the doc §6 |

---

## Track F — The Cauldron View *(initiative, verified 2026-07-05)*

*One initiative carving out the **pot/inventory look-and-feel** into a single pass, as the owner asked.
It **absorbs** the already-verified **P1-3** (medallion socket UI) and **P5-5** (stomach-interior backdrop)
and adds the new asks (liquid you can feel · fullness by fill-level · stable non-reshuffling brew · a
medallion ribbon under the pot · continuous result flow). **F1 is foundational** (the three-zone spine +
the "bubble = in the brew" rule) and gates F2/F3; F1 also re-homes the P1-3 medallion into the under-pot
ribbon. Everything here is **presentation over shipped inventory/crafting/socketing — no mechanic or
outcome change**. Owner-locked forks: fullness = **liquid level only** (the model is a **fixed size, never
resized in code**); in-brew behaviour = **stable places + event-physics** (untouched bubbles never move;
drop-in splashes and settles); artifacts read as in-liquid via **bubbles that pierce the surface**. Look
mudboard: **Potion Craft** + **Cult of the Lamb**. **Target-composition reference (all three briefs at
once):** `product-requirements/references/cauldron-view-reference.png`, annotated normative-vs-mood in the
**F1** brief. See `design/decisions.md` (2026-07-05).*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **F1** | Cauldron view **spatial spine + zone rule** — a vertical **crafting-top · brew-centre · medallion-ribbon-bottom** layout, **one rule (bubble = suspended in the liquid, bare everywhere else)**, and **re-homing** the socketing rack from left-of-cauldron to the **under-pot ribbon**; ribbon/crafting anchor to the frame, not the pot | Opus | Code | **spec-ready** — brief `product-requirements/cauldron-view-spatial-spine.md`; **foundational — gates F2/F3**; **absorbs the placement half of P1-3** (`medallion-socket-ui.md` FR7) |
| **F2** | Cauldron **liquid you can feel + fullness** — a stylised low-poly translucent **surface + crisp waterline**, **bubbles that pierce it** (half-submerged), and **fullness driven by the liquid level** (few=shallow, many=brimming, **snapshotted on open**); **cauldron model fixed-size, never resized** | Opus+art | Hybrid | **spec-ready** — brief `cauldron-liquid-and-fullness.md`; tech-art surface/shader; ← F1; stays flat-low-poly/no-outline (`render-look.md` §1) |
| **F3** | Cauldron **stable brew layout + event physics** — **stable per-artifact spots** (adding/removing **never reshuffles** the untouched ones), **event physics** (drop-in splash + neighbour nudge that settles back), and **continuous result flow** (a hovering result **auto-commits into the brew** on the next combine, retiring R19's "rejected until collected"); non-overlap invariant holds | Opus | Code | **spec-ready** — brief `cauldron-brew-layout-and-physics.md`; layout stays deterministic/testable, reaction is view-juice; ← F1 |
| **F4** | Medallion socket UI (**re-homed** into the under-pot ribbon) | Opus | Code | ✅ **spec-ready** (P1-3) — brief `medallion-socket-ui.md`; **absorbed into Track F**, placement now per **F1**; internals (rim/gems/progress/confirm) unchanged |
| **F5** | Stomach-interior **inventory backdrop** | Sonnet+art | Hybrid | **decided** (P5-5) — `art/render-look.md` §5; charming-not-gross, muted, screen-space overlay (no scene swap); art = designer |
| **F6** | **Upgraded, prettier cauldron mesh** — a more attractive model for the pot (silhouette/charm), fixed-size (F2 assumes whatever mesh is current) | art *(code wires the seam)* | **Designer** | new art item; the one net-new authored asset in the track; feeds F2 (the liquid sits in it) |

---

## Track G — Arena / Multiplayer Polish *(initiative, verified 2026-07-05)*

*A separate track collecting **Arena presentation, diagnostics, and defects** — the "make the networked
fight feel right and debuggable" pass, distinct from the **P4** netcode half. Stays inside the Arena
**budget guard** (`design/arena-mode.md`): **presentation + dev-tooling + bugfix only** — no new
progression, economy, or pillar. Owner forks locked 2026-07-05: seat camera = **static** (a hex board
needs a stable frame to read the queue — Pillar 4); backdrop = **rotates with the seat** on one local
view rig (not a 360° surround); dev console = **dev-only**, **both** log granularities, **current-HP
only**. Advances the deferred **P4-4** "arena camera pass" and **P3-16**. G1 and G3 share the rotated-view
seam and are best verified together.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **G1** | Arena **per-client seat camera + oriented backdrop** — each client views the shared board **from behind its own hero** (near-edge seat), **static** for the match; the world backdrop **turns with the seat** on one local view rig (arena/platform/units unmoved); **client-side only**, determinism preserved; **Arena only** | Opus | Code | **spec-ready** — brief `product-requirements/arena-seat-camera-and-backdrop.md`; advances **P4-4** (arena camera pass) + **P3-16**; board interaction (hover/ghost/telegraph) must stay correct under the rotated view — **verify with G3** |
| **G2** | Arena **dev console** — dev-only overlay: **current player HP** + a **turn log** (per-round committed-queue reveal **and** step-by-step resolution order with **whiff/conflict** marks); instruments the hidden-simultaneous-resolve **determinism** | Sonnet | Code | **spec-ready** — brief `product-requirements/arena-dev-console.md`; extends dev-tools **P6-6** / logging **P6-7** |
| **G3** | **BUG — hexes not highlighted in Arena** — the tile hover/targeting highlight that works in the PvE combat scene does **not** fire in the Arena scene (likely a scene/installer wiring gap, or the networked spawn/camera breaks the screen→hex raycast) | Sonnet | Code | **defect**; task description is enough (no brief); fix **under the G1 rotated view** — verify the highlight lands on the correct hex from the local seat |
| ~~**G4**~~ | ~~Arena **parts-draft screen (UI & presentation)**~~ ✅ shipped 2026-07-06 together with **P4-5** (`arena-mode.md` §2.9 + new `ability-preview-popover.md`): slot-grouped 3D board + live-assembling monster on one stage, whose-pick/snake/timer reads, opponents = name + parts text, remote-pick flights, part-info popover with legality-gated Draft + visible rejection, "your monster" beat; the **shared ability-preview popover** shipped and the mutation cards migrated onto it (old text tooltip deleted); 1407/1407 green | Opus | Code | consumed brief `product-requirements/arena-draft-ui.md`; production VFX/clips = P5-12 |

---

## Track O — The Hub *(initiative, verified 2026-07-06)*

*Stands up the **Junkyard Hub** as a real scene for the first time — the pre-run **staging ground**
the "Journey" button leads to (today it drops straight into a run) and the **death-return** reform
point (the Hades frame made front-facing). Two **independent** start-of-run choices realize the
canonical **"direction + floor, not a vending machine"** dig (`hub-junkyard.md`): a **starting part**
(1-of-3 ready organs, each an active ability + a race marker → the hero launches a **1-marker
tolerated freak**) and a **starting biome** (which homeland to enter; part-race need not match →
match=at-home vs cross=marked-outsider divergence). **Refines** shipped biome-selection (P0-2) for the
**entry point only** — the climb after entry stays the seeded pool; the no-choice-map stance holds for
the route. MVP = **staging + return only**; hub meta-progression, recurring cast, artifact digs / N>3,
and a full route-choice map are deferred. See `design/decisions.md` (2026-07-06).*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| ~~**O1**~~ | ~~The **Hub scene + start-of-run choices + death return**~~ ✅ shipped 2026-07-06, reworked to a walkable world the same day, then **corrected to "just another biome"** (brief `hub-as-a-normal-platform.md`; `hub-staging.md`): Journey → Hub = **one normal world platform** — the Area's Cinemachine camera + locomotion verbatim, the island through `PlatformView` + the Track-E dressing chain as the first-class **`LevelTheme.Hub`** biome (authored appearance/kit, excluded from the rotation by data), the hero free-walks it; **owner-revised offer** — up to 3 cards drawn deterministically from the **tasted-forms pool** (not 3 fixed organs; bare launch always allowed), dealt by **talking (F) to the junk-keeper NPC** on the shared mutation card panel (re-openable — the pick is revisable until launch); the **entry homeland** = one of three **labelled portals** (walk up + F, prompt = biome name; window-0 override, seeded climb unchanged; all three homelands re-authored to tier 1 + kept at their climb tier — mapper now dedupes (theme, tier)); data-authored cauldron voice (`HubVoiceLinesConfig`); the portal = the new-run **commit point** (run-setup.json carrier, RunSaveSnapshot v2 `StartingBiome`); **death returns to the Hub** (arrival marker + scene load after the P2-2 consume); all Hub suites green; follow-ups in `ROADMAP.md` (reader relocation · launch-line linger · death vignette · match/cross hint · Forest-at-tier-2 tuning · movement lock under the cards · junkyard ground material · P1-10 absorbs the voice SO) | Opus | Hybrid | consumed brief `hub-staging-and-launch.md` (2026-07-06 owner revisions); **refined P0-2** (entry biome now a choice) + fronted P2-2's death-consume; **realized the MVP dig** (`hub-junkyard.md`) |

---

## The remainder, regrouped — Tracks H–N *(2026-07-05)*

*The three P2 Fable poles + Tracks D–G took the "important now" work. Everything **else** still open in the
P0–P6 waves is folded here into **seven adjacency-grouped tracks**, same granularity as D–G, so the backlog
reads as initiatives instead of scattered IDs. Rows reference the stable P-IDs (the P-waves stay the index);
a track's **internal order** is its own dependency chain. **Recommended global order after D–G:**
**I → H (parallel) → J → K → L → N → M.** The Fable lane is **Track I** (owner call 2026-07-05).*

---

## Track H — Quest-as-Reward Economy *(narrative felt-value; Opus)*

*The "a quest **offer** feels like a prize" cluster (`design/narrative/quest-as-reward.md`, vision §6) plus the
reward **plumbing** it needs. **All briefs written 2026-07-05** (Track-H brief pass); the card grammar shipped.
**P1-5 is the foundation** (an honest tier-glow needs a rolled reward, not a literal id); it gates the card-visual
P1-6. The rest parallelise. **P1-12 deferred** (no currency model yet). No Fable.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P1-5** | Quest reward = **rolled by tier + belonging** (not a literal id); payload = **blank OR artifact, declared per quest** | Opus | Code | **spec-ready** — brief `quest-reward-rolled.md` (with **P0-3·b**); **foundational — gates P1-6's honest glow**; reconciles Socketed-Blanks (archetype off artifacts) |
| **P0-3·b** | **Belonging-colour consumer** — belonging = race (blank, via `RaceDefinition._belongingColor`) or function-family (artifact) | Opus | Code | **spec-ready** — folded into `quest-reward-rolled.md`; feeds **P1-6** tint |
| **P1-6** | Quest-offer card visual treatment (ornate frame · mystery reward slot · glow=tier · tint=belonging · hidden item) | Sonnet+art | Hybrid | **spec-ready** — brief `quest-offer-card.md` + drafted plan; ← P1-5 + belonging-colour |
| **P1-7** | Attack card — the **Monster verb** (eligible NPCs; closes thread, corpse-loot channel, Conquest facts) | Opus | Code | **spec-ready** — brief `attack-card-monster-verb.md`; + **P1-10** cauldron tempter bark fires here |
| **P1-10** | Cauldron-voice tempter + socketing-trend barks | Sonnet | Hybrid | **spec-ready** — brief `cauldron-voice-barks.md`; you write the barks; pairs with P1-7 **and** Track J's socketing trend |
| **P1-8** | Competing / mutually-exclusive **same-tier** offers, separated in time on a shared-actor thread | Opus | Code | **spec-ready** — brief `multiple-and-competing-offers.md` (with **P1-9**); mutual-exclusion = P2-3 thread-fail |
| **P1-9** | **Several** quest offers per NPC/storylet (multi-slot authoring shape) | Opus | Code | **spec-ready** — folded into `multiple-and-competing-offers.md`; distinct from P1-8's time-separated fork |
| **P1-11** | **Quest log UI** (active/completed/failed + objectives) | Sonnet | Code | **spec-ready** — brief `quest-log-and-saga.md`; also the **thread/saga readout** the P2-3 director deferred here |
| **P1-12** | Non-item reward **sinks** — Currency / Experience / Ability receiving systems | Opus | Code | **DEFERRED** (owner 2026-07-05) — no currency model yet; quests pay items + facts/doors for now; revisit when currency lands (`save-continue-run` W3-1 follow-up) |

---

## Track I — Director Depth II *(THE Fable pole; cross-system narrative correctness)*

*Now that P2-3 threads + P2-2 save shipped, finish the director's **meaning** layer — the reveal spine
(mirror/antagonist), cross-run memory, and escalation. Highest-ambiguity, correctness-critical, story-bearing
work; P2-2 just unblocked the meta store P3-3 reads. **Fable on P3-1 + P3-3** (the spine + cross-run memory —
the game's central reveal); **Opus** on the escalation/composition rows. P6-1 (cheap Sonnet) precedes P3-8.*

**Launch order (owner call 2026-07-05).** Do **not** hand the whole track to Fable — only **P3-1 + P3-3**
are Fable work; the rest stay on their cheaper models so the premium isn't burned on scoped/mechanical rows.
And run the **Fable pair sequentially, P3-1 → P3-3** (not both at once): P3-3 **reads P3-1's spine lane**
(cauldron-memory rides the lane as reveal-beats; the spine cursor advances the in-run mechanism P3-1 builds),
so — even though the two were scoped to **test independently** — P3-3 should build on a **verified** P3-1;
Fable is here for correctness on this ambiguous reveal, not for speed. Recommended sequence: **(1)** `P6-1`
(Sonnet) + `P3-2` (Opus) in parallel first (block nothing) → **(2)** `P3-1` (Fable), build + verify the lane
→ **(3)** `P3-3` (Fable) on the verified lane → **(4)** `P3-5` (Opus), then `P3-8` (Opus, after P6-1).
Parallelism is **across models** (the Sonnet/Opus rows alongside Fable), never **within** the Fable pair.
**Status 2026-07-06:** steps (1)–(3) are done — P6-1, P3-2, P3-1, and **P3-3 all shipped**; the Fable
pair is complete. The track's remainder is **P3-5 (Opus)**, then **P3-8 (Opus, post-P6-1 scope)**.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| ~~**P6-1**~~ | ~~Reconcile/retire `narrative-generation.md` · refresh `loot-subsystem.md`~~ ✅ shipped 2026-07-06 (tombstone + §4 residue preserved for P3-8; loot doc refreshed; dead API → P6-4) | Sonnet | Code | **precedes P3-8** |
| ~~**P3-2**~~ | ~~Director — **D19 escalation** tier gating (pool/register shift by `run_escalation_tier`)~~ ✅ shipped 2026-07-06 (`narrative-procedural.md` §2.6; consumed brief `run-escalation.md`): `RunTierBand` on `StoryTemplate` + `EnemyDefinition`, planner + ambient/site allocators gate by `run_escalation_tier`, pool-shift only (no stat multiplier, no density change), unbanded = every tier; **1325/1325 EditMode green** | Opus | Hybrid | first consumer of the biome-selection tier fact |
| ~~**P3-1**~~ | ~~Director — **D7 reserved spine lane + reveal cap**~~ ✅ shipped 2026-07-06 (see P3 wave row / CHANGELOG); the verified lane P3-3 builds on | **Fable** | Code | consumed brief `director-spine-reveal-lane.md`; **P3-3 is now unblocked** |
| ~~**P3-3**~~ | ~~Director — **D20 meta-scoped consumers** (mirror-lore echoes · cauldron memory of past hosts · spine cursor reading the meta horizon)~~ ✅ shipped 2026-07-06 — see the P3 wave row; **seen**-based cursor (PO call), echoes + cauldron memory as data-only spine beats over meta facts; 1355/1355 green | **Fable** | Code | consumed brief `director-meta-consumers.md` |
| **P3-5** | **OR / boolean** precondition composition (AND-only today) | Opus | Code | **scope-locked (2026-07-05):** add **OR to today's AND** (groups/OR-list), **no NOT, no deep nesting** (KISS); enabler for OR-composed thread resolution + reward conditions. **ROADMAP-scope, no brief** |
| **P3-8** | Narrative-gen residue folded into the streaming director | Opus | Code | **re-scoped (2026-07-05):** the legacy P1–P4 are mostly **already delivered/superseded** by the streaming director — **P2** (quotas) = two budgets D8/D9, **P3** (progression-driven) = D19 escalation + passport facts. **Live residue = P4 soft cooldowns** (recently-seen scores lower, graceful small-pool degrade) **+ optional P1** compat-scoring as an actor-casting tuning. Exact scope set by **P6-1**'s reconcile audit. **after P6-1** |

---

## Track J — Crafting & Mutation Loop Completion *(Opus; part brief-ready)*

*Give the shipped **Socketed Blanks** loop a **world source** + feedback, and close the one real hole in the
shipped body-plan runtime (you can shed a governor but not re-install it — going back to base leaves a legless
biped). Content-authoring rows are yours.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P1-1** | Part-Blank **loot sources** (enemy-remains + landmark finds) | Opus | Hybrid | **spec-ready** — brief `blank-loot-sources.md`; you author drop pools |
| **P2-1·f** | Body-plan follow-up — **re-install / un-equip flow** + explicit `RemovePart` API (+ un-equip raises `PartsChanged` so passport tiers move) | Opus | Code | **real gap**, not polish — no way back to base frame today |
| **J-craft** | Cauldron-voice **socketing-trend** bark delivery (the seam `ISocketingTrendSource` exists) | Sonnet | Hybrid | shares the bark channel with **P1-10** |
| **P5-13** | Signature-vs-emergent craft **presentation** (distinct effect for authored results) | Sonnet | Code | `isSignature` reported, puff identical today |
| **P6-10** | Emergent-fusion **authoring density** + biome↔archetype affinity | author | **Designer** | more artifacts across the trait space so emergent output stays legible |
| **J-reconcile** | Reconcile mutation **species (blank) vs. race tag (equipped part)** — does an unsealed part inherit a race? | Opus | Code | two acceptance axes; decide the inheritance rule |

---

## Track K — Combat Depth II *(mechanics beyond legibility; Opus, Fable-opt. on AI)*

*The combat **mechanics** deepening, distinct from Track D's **legibility**. Best sequenced **after Track D** so
smarter AI + new displacements read clearly under the animation/arrow/turn-order layer D lands.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P2-4** | **Smarter ability-using enemy AI** (target/area/direction selection beyond flat scoring) | Opus *(Fable opt.)* | Code | the "fast chess" quality (Pillar 4); reads best after D3's enemy-action animation |
| **P3-11** | More **displacement kinds** — pull / dash / hook (push-only today) | Opus | Code | own data semantics + executor/preview |
| **P3-12** | Ring push + queue-**simulation** preview + **speed-based initiative** | Opus | Code | the *speed* half Track D's D2 defers — **D2 shipped initiator-first** (2026-07-05), so this is the speed-based + multi-round-policy remainder |
| **P3-13** | **Passive modifiers beyond outgoing damage** (HP / def / heal — stat-target dimension) | Opus | Code | also the **Ability reward sink** (Track H P1-12) |

---

## Track L — Systems Depth & Correctness *(Opus/Sonnet; opportunistic)*

*Scoped world/platform/loot/progression correctness that doesn't rise to a felt initiative — pick up as its
neighbours land. No hard internal chain.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P3-9** | Live fact-driven NPC **marker refresh** (per-frame re-eval, not consume-on-start) | Opus | Code | |
| **P3-10** | Per-NPC / per-archetype **interaction-radius overrides** (boss carve-out shipped with D1) | Sonnet | Code | general authored surface stays open |
| **P3-14** | Loot — capacity feedback (R11) + progression gating (R13) + despawn on leave | Sonnet | Code | rebind capacity policy |
| **P3-15** | Platform — unit grounding per-model override + `ContentSpawner` on concave islands | Sonnet | Code | place on `CenterCell` |
| **P3-16** | Platform — camera/entry pass at arena scale | Opus | Code | play-mode tune (overlaps Track G) |
| **P3-6** | Progression — AND/OR conditions + `StoryNodeRequirement` gating | Opus | Code | consume Required* fields |
| **P3-7** | Progression — generic `record_choice` Ink fn | Sonnet | Hybrid | data-driven key-choice capture |
| **P1-14** | Sites — occupancy/passport/tier/biome schema fields | Opus | Code | deferred until consumers exist |

---

## Track M — Production Art / M5 Look *(Designer-heavy; parkable)*

*The M5 production look pass. **Track E already covers the demo look**, so this waits for production intent —
low priority now. Mostly your track; code only wires the consuming seam.*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P5-1** | Palette swatches (master + per-biome + per-archetype) | art | **Designer** | turns the bible into a tool |
| **P5-6** | Production body-part meshes (to the authoring contract, incl. per-race signature markers) | art | **Designer** | feeds per-frame host fit (P2-1·d) |
| **P5-7** | Part / animation integration workflow | Opus | Hybrid | pairs with the authoring contract |
| **P5-8** | Muted→crisp render + figure-ground shader spike (tech-art) | Opus | Hybrid | readability without outlines |
| **P5-10** | Marker/name art + animation direction + VFX language | art | **Designer** | art-direction pass |
| **P5-11** | Character locomotion polish (clips, accel, yaw, wall-velocity) | Sonnet | Hybrid | clips=you, logic=code |
| **P5-12** | Mutation card VFX + animated mini-model + full live-hero preview | Sonnet | Hybrid | real tier-glow shader |

---

## Track N — Debt & Tooling *(cheap models; background)*

*Opportunistic cleanup + the Arena hardening follow-ups. Several need the compiler (deferred sweeps).*

| ID | Task | Model | Owner | Note / dep |
|---|---|---|---|---|
| **P6-2** | Remove branching-choice dialogue UI (card hand landed) | Sonnet | Code | **ready to execute** — retire OnChoices + migrate `.ink` |
| **P6-3** | Extract neutral `Core.Hex` namespace (~43 files) | Sonnet | Code | mechanical; needs compiler |
| **P6-4** | Remove dead platform-loot API · duplicate `CombatInputModeManager` | Haiku | Code | delete no-caller code |
| **P6-5** | Small cleanups (WorldArtifactView dup · TelegraphStyle→SO · AbilityTooltip · Hero prefab · part-data coupling) | Sonnet | Code | extract on 2nd consumer |
| **P6-6** | Dev tools — more sections · mutating controls · configurable key | Sonnet | Code | rack/quest/actor read-outs |
| **P6-7** | Logging — in-game level control · installer bootstrap seam | Sonnet | Code | optional dev panel |
| **P6-8** | PartSwap / SocketMounter play-mode tests | Sonnet | Code | manual-verified today |
| **P6-9** | Demo content sharpeners (barn loss, recast, fact-web, no-forced-combat NPC, PerLocation, D12) | Sonnet | Hybrid | content + small engine bits |
| **P6-11** | Runtime skinned-mesh combining (perf) · cauldron-will stochastic surprise | Opus | Code | both deferred — only if needed |
| **P4-2** | Arena — host-side commit validation (anti-cheat) | Opus | Code | MVP trusts peers |
| **P4-3** | Arena — seeded-shuffle resolution alt + per-step damage batching | Opus | Code | drop-in behind `IArenaResolutionOrder` |
| **P4-4** | Arena — rename `EnemyIntent → CommittedIntent` + arena camera pass | Sonnet | Code | mechanical; needs compiler; overlaps Track G |
