# Roadmap

Planned work and open gaps, grouped by system, plus a cross-cutting backlog. Checkbox items.

> **Priority view:** for a priority-ordered cross-cut with stable IDs (`P0-1`, `P1-3`, …), a
> recommended model per task, and the designer-vs-code split, see
> [`ROADMAP-prioritized.md`](ROADMAP-prioritized.md). This doc stays the per-system source of truth.

Maintenance rules (CLAUDE.md §8.3):
- New requirement from the user → add an unchecked item under its system.
- AI proposes an enhancement / notices a limitation → add it here (don't just mention it in chat).
- Item implemented → check it off and move the substance into `CHANGELOG.md`.
- Items here mirror the "Known limitations / open points" sections of the system docs; keep them in sync.

Suggested tags: `[content]` data/authoring, `[arch]` structural, `[debt]` cleanup, `[perf]`,
`[rule]` fixes a CLAUDE.md violation.

---

## Milestones

Suggested sequencing. Only M1 is firm; the rest is a recommended order, not a commitment.
- **M1 — Mutation core loop.** Artifact archetype attributes → per-stage tally → feeding/digestion
  UI → stage-up mutation choice → body-part swap → part-granted abilities (active + passive).
  *(The feeding/digestion half is now superseded by Socketed Blanks — Track A / `## Crafting & Mutation`.)*
- **M2 — Progression + Quests + progression-driven narrative.**
- **M3 — Platform/hex rework + content-aware platform shape.**
- **M4 — Combat readability (ability telegraphing) + smarter enemy AI.**
- **M5 — Production character art/animation workflow + world/landscape visuals.**

### Verified-brief build order (`product-requirements/`)

The verified PO briefs group into **three independent tracks** that can run in parallel; **within** a
track the order is a hard prerequisite chain. All briefs are PO-level and ready to build — what gates
a step is a prerequisite brief, not a design gap. See `product-requirements/README.md` for each.

**Track A — Crafting / Mutation**
1. **Socketed Blanks core loop — DONE** (`crafting-mutation-socketed-blanks.md`): artifact traits +
   tier + two-tier fusion, the Part-Blank item + operating table + unseal menu + scarce blank rack,
   and the feeding removal all shipped (see `## Crafting & Mutation` + `mutation-subsystem.md`).
   Remaining polish (Known limitations): blank **loot drops**, **tier-glow** on rack bubbles,
   **cauldron-voice** trend consumer, residual archetype-weight cleanup.
2. **Mutation choice cards — DONE** (`mutation-choice-cards.md`): the unseal menu is the real card
   hand (part image + ability icons + flip before→after + ability tooltip + mini-model popover +
   two-step commit); the card's tier-glow landed as a placeholder brightness treatment (see
   `## Mutation Subsystem` + CHANGELOG). Remaining polish: glow/flip VFX (tech-art), rack-bubble
   tier-glow.
3. *(unblocked by the artifact tier)* quest-as-reward **reward-by-tier** + the **quest-offer card**
   (`quest-offer-card.md`) — reuses the card grammar from step 2 (see `## Quests`).

**Track B — World / Platforms** (hard sequential chain)
1. **World content density — DONE** (`world-content-density.md`): the density-first allocator (four
   content kinds, one SO density config, per-biome monster pool, streaming loot) shipped — see
   `## Data-Driven Procedural Narrative` + `narrative-procedural.md` §2.6. Remaining polish: biome
   selection along the run, loot-chance dead-API cleanup (below).
2. **Platform hex surface & shape — DONE** (`platform-hex-surface-and-shape.md`): hex-composed
   surface = combat grid (one source of truth), organic non-walkable rim, per-content-kind size
   profiles + battlefield minimum, deterministic — see `platform-generation.md` +
   `## Platform & Area Generation`. Remaining polish: muted→crisp shader treatment, arena-scale
   camera pass, `ContentSpawner` on concave islands (below).
3. In parallel after step 2: **Sites & landscape — DONE** (`world-sites-and-landscape.md`): the
   what-fills-where half shipped (site vocabulary + capacity recipes + block reservation + flavored
   content — see `world-sites.md` + `## Platform & Area Generation`); the visual "reads as one
   place" half shipped its demo pass with Track E (2026-07-06, `environment-dressing.md`). **Biome
   visual styles** (`biome-visual-styles.md`) shipped its feature-pool/ground core the same day
   (Track E · E2); the palette-key/light residue stays open below.

**Track C — Combat — DONE (2026-07-03)**: all three briefs (`combat-hero-facing.md`,
`combat-turn-intent-phase.md`, `combat-ability-ghost-telegraph.md`) shipped as one combat pass —
facing-relative aiming + Plan→Act→Resolve with locked/seeded enemy intents + overhead plan icons +
the honest full-outcome ghost with push displacement. See `combat-round-and-telegraph.md`,
`## Combat Experience`, and the CHANGELOG. Remaining polish (ghost animation, move-intent
presentation, telegraph style SO, pull/dash kinds) is filed under `## Combat Experience`.

Tracks A / B / C are independent and can proceed in parallel; Track A already has work in flight.

**Track D — Bandit Camp & Combat Legibility II** (verified 2026-07-05; one initiative, three briefs).
Turns the demo's placeholder **capsule** enemies into a **living humanoid bandit camp** and finishes the
combat-legibility layer Track C deferred (ability animation, animated ghost, initiator-first order, move
arrow). The three briefs are largely independent (the world half vs the two combat halves) and can run in
parallel; within combat, the facing-input/order brief and the animation brief share the same combat pass and
are best built together.
1. ~~**Humanoid Bandit Camp** (`bandit-camp-humanoids.md`)~~ ✅ **shipped 2026-07-05 (D1)**: shared humanoid
   model for enemies (capsule retired), demo colour-coding (green NPC / maroon boss / lighter crew, kept in
   combat), boss + director-sized crew, the boss's larger platform-scoped engagement circle
   (dialogue-or-fight), no aggro on landing, crew fights behind the boss. The boss's quest *content* is still
   a placeholder pending `camp-shady-offer.md` (P3-17). Follow-ups (boss re-engagement, boss visual
   continuity, general radius override) filed under `## NPC Interaction …`. See CHANGELOG.
2. ~~**Combat — Initiative, Turn-Order Queue & Facing Input** (`combat-initiative-and-turn-queue.md`)~~
   ✅ **shipped 2026-07-05 (D2)**: initiator-acts-first (player Attack vs. dialogue-turned/ambush enemy) via
   `CombatInitiator` + the opening-round reorder in `CombatController.StartRound`, a code-built horizontal
   top-right turn-order strip (PvE only), and Enter hold-to-aim/release-to-execute + right-click cancel.
   Deterministic, no ability change. The *speed*-based + multi-round-policy halves stay deferred (Track K
   P3-12). See `combat-round-and-telegraph.md` R4/R4a/R4b + `ability-subsystem.md` R8 + CHANGELOG.
3. ~~**Combat — Ability Animation, Animated Ghost & Enemy Action Read** (`combat-ability-animation.md`)~~
   ✅ **shipped 2026-07-05 (D3)**: a shape-driven **cell-sweep** placeholder animation
   (`AbilityCellFlash`/`AbilityAreaSweep`) played translucent as the animated ghost and opaque on live
   execution via a shared executor `AbilityFiredCue` sink (player queue + enemy paced resolve — the enemy
   action now animates within its beat, visibly not instant); a board **move-direction arrow**
   (`EnemyIntentTelegraphView`) replacing the `»` glyph; and an enemy **readiness cue** (restless plan icons
   + placeholder wind-up pose — enemies only, uniform, cleared on resolve). Presentation only, PvE-wired.
   Resolved the ghost-caster-animation follow-up (code placeholder; production clips + `_animationTrigger`
   stay the art seam) + advanced move-intent presentation. See `combat-round-and-telegraph.md` R15–R18 +
   CHANGELOG.

---

## Input / Cross-Device

The shared input spine (verified brief `cross-device-input-foundation.md`, shipped 2026-07-07 —
see `input-foundation.md` + CHANGELOG). The named-action vocabulary, the merged three-source
bindings, active-source tracking, device-aware prompt cues, the touch overlay, and the dev-overlay
Input section are in. What remains is the interaction DESIGN the foundation deliberately did not
fake, plus deferred conveniences:

- [x] `[arch]` **Cross-device input foundation** — named actions, per-source coverage rule (tested),
  live active-source prompts, touch overlay, dev overlay section. Moved to CHANGELOG 2026-07-07.
- [ ] `[design-gate]` **Gamepad interaction brief: cauldron brew (drag-and-drop with a stick).**
  Focus/navigation model for the pointer-only brew layout — PO design decision before code.
- [ ] `[design-gate]` **Gamepad interaction brief: inventory hover-inspect.** Non-pointer path for
  hover-driven inspection.
- [ ] `[design-gate]` **Gamepad interaction brief: mutation choice cards.** Focus model + initial
  `EventSystem` selection for card hands (also unlocks d-pad Navigate on encounter cards — bound but
  inert until a focus pass).
- [ ] `[design-gate]` **Touch combat brief.** Aim/Fire/MoveMode/ChangeDirection/AbilitySlot1–6 on
  touch are allowlisted deferred gaps in `InputBindingCatalog` — how you aim and volley with fingers
  is its own interaction design.
- [ ] `[content]` **Player-facing rebinding / remap UI** — deferred by the brief; bindings are fixed
  in `GameActions.inputactions`.
- [ ] `[content]` **Controller-specific glyph art** — prompt cues are text ("F", "RT", "D-Up");
  per-platform button art is a later pass.
- [ ] `[arch]` **Touch overlay hot-plug** — `TouchControlsView` checks for a touchscreen once at
  startup; a touchscreen connected mid-session waits for the next scene load.

---

## Cross-cutting

- [ ] `[arch]` **Non-item reward types have no receiving system.** `Currency`, `Experience`,
  `Ability` rewards are rolled by the narrative system and skipped by the loot granter (logged only).
  Planned owners: `Experience` → Character Progression; `Ability` → Ability Subsystem (part-granted);
  `Currency` still open. Wire each receiving system as its owning section lands. *(narrative + loot)*

---

## Architecture & Extensibility Audit (Track W)

A one-off, **read-only** whole-project review — Fable reads the codebase + data systems, measures them
against `Assets/__Project/CLAUDE.md`, and produces a prioritised findings backlog (it changes no
code/assets/docs; findings become work only after owner approval). Split into scoped passes, run one
per session, from the code root. Brief:
`product-requirements/architecture-and-extensibility-audit.md`; priority view = Track W in
`ROADMAP-prioritized.md`; reports land in `Docs/audits/`.

- [x] `[arch]` **W1 — Data-driven / SO extensibility.** ✅ done 2026-07-07 (`audits/W1.md`): data path
  exemplary; the one **P0** gap = biome identity is a `LevelTheme` enum (→ Audit Refactor A2).
- [x] `[arch]` **W2 — Architecture, coupling & duplication.** ✅ done 2026-07-07 (`audits/W2.md`): DI
  near-clean, MVP holds; debt = the world-platform seam + the **PvE/Arena combat-controller verbatim
  copy** (→ A1) + the `PlatformEvents` breadth correction above.
- [x] `[debt]` **W3 — Dead, unreachable & orphaned code.** ✅ done 2026-07-07 (`audits/W3.md`): ~2 200
  lines adjudicated dead, with a safe delete order (→ Audit Refactor A4; executes P6-4).
- [ ] `[perf]` **W4 — Performance hypotheses.** **Pending** — worklist collected in `W6.md` §6 (append
  as a W6 addendum when run). *(§12)*
- [ ] `[debt]` **W5 — Docs ↔ implementation drift.** **Pending** — worklist collected in `W6.md` §6
  (Audit Refactor A8 folds in here). *(§8)*
- [x] `[arch]` **W6 — Synthesis & prioritised backlog.** ✅ done 2026-07-07 (`audits/W6.md`): 8 refactor
  themes → filed as **Audit Refactors A1–A8** below; **A1 inserted into the Fable lane before Track X**;
  verdict "healthier than a violations-list suggests". *(run over the 3 completed passes; W4/W5 addendum pending)*

### Audit Refactors (A1–A8) — the W6 output

Renamed from the report's `R1–R8` to avoid colliding with **Track R** / **Track X**. Only **A1** gates a
Fable-lane track; the rest are opportunistic and fold into N/L/P6. Priority view + models + sources = the
**Audit Refactors** section in `ROADMAP-prioritized.md`.

- [ ] `[arch]` **A1 — Combat core reconvergence** — shared round/state engine under the `CombatController`
  / `ArenaCombatController` verbatim-copy pair. **Fix-first — before Track X & Track K.** Opus (Fable opt.).
- [ ] `[arch]` **A2 — Biome-as-data** — `BiomeDefinition` SO replaces the `LevelTheme` enum (the one P0
  extensibility gap); before the Mtn/Desert content wave. Opus.
- [ ] `[arch]` **A3 — One platform seam** — `PlatformRegistry`→DI, kill `GameObject.Find` in
  `ContentSpawner`, `ArenaPlatformBuilder`→`PlatformView`; unblocks the `PlatformEvents`→signals row. Opus.
- [ ] `[debt]` **A4 — Dead-code purge** — execute W3's delete order (executes P6-4); wave-2-D after A2. Sonnet/Haiku.
- [ ] `[arch]` **A5 — Hero movement single path** — delete the dormant `AICharacterMovementController`
  (**step 1 immediately**, Haiku), then extract movement logic to pure C# (Opus).
- [ ] `[arch]` **A6 — Layering enforcement** — incremental `asmdef` split (`noEngineReferences` Core);
  folds P6-3 (`Core.Hex`). Sonnet/Opus.
- [ ] `[debt]` **A7 — Small-cleanups tail** — DialogueRunner port, `EventSystem` bootstrap, stray
  `Scripts/Combat/*.md`, `LoadAll`-override guard, missing `RunPacingConfig` asset; bundle with P6-5. Sonnet/Haiku.
- [ ] `[debt]` **A8 — Doc patches** — `AIProfileDefinition` doc + ability-kind/bark-slot statements +
  ink-lockstep note; fold into W5. Haiku.

---

## Arena / Multiplayer — Online Robustness (Track X)

Owner call 2026-07-07: **Arena is a shippable feature for the broad public.** The Arena MVP is done
(deterministic lockstep core, host-authoritative draft — `arena-mode.md`); this is the **production
layer it deliberately skipped** (`arena-mode.md` §6). The netcode-correctness core is **Fable**, the
SDK/plumbing/UX half **Opus/Sonnet**. Priority view + full split = Track X in `ROADMAP-prioritized.md`.

- [ ] `[arch]` **X1 — Reconnect + desync recovery + host migration.** A dropped/diverged peer rejoins
  and re-syncs; host-drop no longer ends the match. Today a hash-mismatch is only detected, never
  recovered. **Fable — highest correctness risk.** *(arena-mode.md §6)*
- [ ] `[arch]` **X2 — Anti-cheat: host-side commit validation** (was P4-2). Re-validate relayed commits
  against canonical state; folds in P4-3 (seeded-shuffle resolution alt + per-step damage batching). **Fable.**
- [ ] `[arch]` **X3 — Online services:** relay / NAT-punchthrough / lobby / matchmaking / friend-invites
  (replace direct `ip:port`). Opus — biggest volume, integration not ambiguity.
- [ ] `[arch]` **X4 — In-match human turn-timer + AFK handling.** The round has no human timeout today. Opus.
- [ ] `[debt]` **X5 — Build version handshake + hardened disconnect UX.** Reject mismatched builds at
  approval (silent desync today). Sonnet.
- [ ] `[debt]` **X6 — Arena presentation folds here:** G1 seat camera · G2 dev console · G3 hex-highlight
  bug · P3-16 arena-scale camera · P4-4 `EnemyIntent→CommittedIntent` rename · real part thumbnails ·
  initial-facing polish. Opus/Sonnet. *(mirrors arena-mode.md §6)*

---

## Net-New / Missing Systems (survey follow-up, 2026-07-07)

Large genre-characteristic systems the plan hadn't scoped, beyond the Q–V pillar survey. Priority view +
the Fable lane live in `ROADMAP-prioritized.md`.

- [ ] `[arch]` **Difficulty / Ascension («Heat») modifiers (Track Y).** Player-chosen challenge layers
  gating rewards; the reiterability engine, distinct from within-run escalation (D19) and meta-unlocks.
  **Fable; needs the economy/meta spine (R) first.**
- [ ] `[arch]` **Boss / set-piece & run apex (Track Z).** Multi-phase bosses + the run climax (Order's
  Seat) as real combat-content tech; today only the bandit-camp boss + a `design/roadmap.md` thread.
- [ ] `[arch]` **Persistent relationship / recurring cast.** Affinity with named NPCs across runs (Hades
  gift/heart); the Hub recurring cast was deferred. Narrative-meta.
- [ ] `[content]` **In-run shops / vendors.** A spend loop for the currency R introduces (the
  `camp-shady-offer` dark-currency has no sink today). Folds under R's economy.
- [ ] `[perf]` **Runtime telemetry / analytics for balancing.** What builds win / where players die /
  which content appears — the runtime data the static `/balance-ledger` can't give. Cross-cutting infra.
- [ ] `[content]` **Post-run summary + meta-goals / prophecies.** A death-recap screen + long replay
  goals; ties collection (T) + meta (R).

---

## Races & Passport

The starting races and the "world reads what you are" rule. **Design gate delivered** (2026-07-04):
the roster is defined in `design/narrative/races.md` and handed to the code track as the verified
brief **`product-requirements/race-roster-and-passport.md`**. Three races as three estates of the
Order — **Ibex** (Mountain), **Lizard** (Desert), **Fox** (Forest); the hero is **kindless**.

- [x] `[content]` **Race roster & passport model — DESIGNED.** *(Design done — `design/narrative/races.md`,
  `design/world/overview.md` §5; verified PO brief `product-requirements/race-roster-and-passport.md`.
  Was the parked "race roster"; unblocks the code items below and the downstream content.)*
- [x] `[arch]` **Races as data + part race-tags.** *(Done 2026-07-04 — `RaceDefinition` roster
  (`Resources/World/Races/Race_{Ibex,Lizard,Fox}`, id + home biome + belonging colour + display name)
  mapped to a pure `IRaceRoster`; `PartDefinition._raceId` string tag (empty = kindless, inspector
  drop-down over the authored races), hero's `_A` starting set kindless, `_B` demo parts tagged.
  New race / re-tag = data only. See CHANGELOG; `races-passport.md`.)*
- [x] `[arch]` **Passport = per-race acceptance tier by part count.** *(Done 2026-07-04 —
  `RaceAcceptanceCalculator` clamps the equipped tagged-part count to {0,1,2+};
  `RacePassportProjector` (single writer) publishes `faction.<raceId>.reads_as_tier` (Int,
  PerFaction, one key for all races) on assembly + every part swap; the demo `frog_marsh` thread
  gates on `reads_as_tier(fox) ≥ 1` with a literal subject token, replacing the card-set
  `reads_as_frogfolk` bool. See CHANGELOG; `races-passport.md`.)*
- [x] `[arch]` **Belonging colour consumer.** *(SHIPPED — P0-3·b (Track H), `quest-subsystem.md` §2.5.
  `BelongingTintCatalog` merges `RaceDefinition._belongingColor` (Part-Blank rewards) and the new
  `RewardFamilyDefinition._belongingColor` (artifact rewards) into one id→colour lookup consumed by the
  quest-offer card's mystery slot + the quest log. ⚠ gameplay-untested. See CHANGELOG.)*
  *(races + narrative view)*
- [ ] `[arch]` **Reconcile mutation species vs. race tag.** `PartBlankDefinition.SpeciesArchetypeId`
  (what a *blank* reads as) and `PartDefinition.RaceId` (what an equipped part reads as) are separate
  axes; decide whether an unsealed part inherits a race from its blank's archetype. *(mutation + races)*
- [ ] `[arch]` **Un-equip path raises `PartsChanged`.** The assembly controller only swaps parts today,
  so passport tiers move on swap; a future remove/un-equip surface must fire the same event (and the
  projector already resets un-worn races to 0). *(character)*
- [ ] `[content]` **Per-race content — questlines, NPC casts, signature enemies.** The next content
  step; hangs off the roster (`design/narrative/races.md` §6). *(narrative)*
- [ ] `[arch]` **Exposure / betrayal.** Deeper mutation / being caught flips a race's trust to
  horror (a `heresy_exposed`-style fact); named in design, trigger rules unbuilt. *(narrative)*
- [ ] `[content]` **Fourth (Cave/underclass) race.** Deferred; the roster is built to accept it, and
  it may tie the hero's Junkyard origin. *(races)*

---

## Logging

- [ ] `[debt]` **Duplicate `CombatInputModeManager`.** Two classes share the name: a no-op stub
  `Combat.Player.CombatInputModeManager` (the live one — created in `CharacterCombatCoordinator`) and a
  fuller mode-logic+logging copy `Combat.Input.CombatInputModeManager` that is never constructed
  (dead). Delete the dead one (or consolidate onto it and update the coordinator). Surfaced during the
  logging sweep. *(combat)*

From `logging.md` §6:
- [x] `[rule][debt]` **Migrate raw `Debug.*` to the categorized logger — DONE for runtime gameplay code.**
  Every `Debug.Log/LogWarning/LogError` in runtime systems now routes through `IGameLogger` with a
  `LogCategory` (Combat, Narrative, Dialogue, Platform, Character, Camera, LevelGeneration, Area,
  Inventory, Loot, Mutation, CharacterSystem). Container-bound classes use constructor injection;
  manually-`new`'d leaves (per-unit presenters, AI strategies, platform content, hex-cell states) take
  the logger threaded from their container-connected owner (optional/null-safe so stray `new` sites
  keep compiling); scene/prefab-instantiated MonoBehaviours use null-safe `[Inject] _logger?.`. Done in
  compiler-verified batches.
  - **Deliberately NOT converted (still on `Debug.*`):**
    - `Core/Logging/UnityGameLogger.cs` — the infrastructure adapter; `Debug.*` is its job.
    - `Core/DI/LoggingInstaller.cs` — bootstrap warning when the config asset is missing (logged
      before the logger exists; chicken-and-egg).
    - Zenject feature installers (`AreaInstaller`, `MutationInstaller`, `LootInstaller`,
      `InventoryInstaller`, `CharacterSystemInstaller`, `CharacterLocomotionInstaller`) — one-shot
      auto-load diagnostics at scene init; routing them would need an install-time
      `Container.Resolve<IGameLogger>()` (service-locator), which CLAUDE.md §4 discourages.
    - `Scripts/Editor/**` — editor tooling, runs only on menu actions, not play-mode flood.
- [ ] `[debt]` **(Optional) installer bootstrap logs.** If the install-time auto-load diagnostics ever
  matter for filtering, give installers a small injected logger seam rather than `Container.Resolve`.
- [ ] `[arch]` **In-game logging control.** Optional dev-tools panel / hotkey to flip per-system log
  levels live in play mode, instead of editing `LoggingConfig` in the Inspector.

## Narrative Generation

Planned design (from `narrative-generation.md` §4 — the **P3-8 "residue" audit**, 2026-07-06). Most of
P1–P4 is already delivered or superseded by the streaming director; only P4 (+ optional P1) is live
residue. Status per item below; the tombstone (`narrative-generation.md` § Residue) carries the same:
- [ ] `[arch]` **P1 — Story↔NPC compatibility scoring** — *partially delivered / optional residue.* The
  "hard requirements prune, preferences only weight" rule already ships as a soft archetype-tag
  preference (`RunWindowPlanner.MatchArchetype`); the fuller multi-trait/faction-fit model is optional
  P3-8 casting tuning.
- [x] `[arch]` **P2 — Rule-based outcome quotas** — *superseded* by the two content-density budgets
  (quest rarity/spacing + empty/loot/combat mix, D8/D9). No further work.
- [x] `[arch]` **P3 — Progression-driven selection** — *superseded/delivered:* progression enters
  selection through facts (D19 escalation tier + passport/race preconditions), not a `GameContext` input.
- [ ] `[content]` **P4 — Soft cooldowns** — *the live P3-8 residue.* Fold cooldown/repeatability into a
  soft score (recently seen → lower) so a small pool degrades gracefully instead of hard-gating; today
  the director uses hard continuity gates (`StoryRunLedger`). Tracked as **Track I P3-8**.
- [ ] `[arch]` **M2 — Progression- & experience-driven composition.** Extend P3 so generation reads
  the run progression record and character experience/mutation state to compose level content —
  combining stories, quests, NPCs, and rewards by rules — producing a unique but coherent experience
  per run. (Supersedes the standalone P3 once Character Progression + Quests exist.)

Known limitations (§5) — *the legacy `LevelNarrativeGenerator` these described was **deleted** in the
Phase-3 cutover, so they are moot; retained struck-through for history:*
- ~~`[arch]` Skipped story (no candidate NPC) is not backfilled, so a level may fall below `MinStories`.~~ *(moot — legacy generator deleted)*
- ~~`[arch]` `GameContext` not consulted by `LevelNarrativeGenerator`; `ScenarioGenerator` hardcodes difficulty to 10.~~ *(moot — deleted)*
- ~~`[content]` `RewardSlot.Condition` is stored but never evaluated.~~ *(moot — the `RewardResolver` / `RewardSlot` reward channel was deleted; quests now grant a fixed `QuestRewardCore`. Progression AND/OR condition composition lives on as **P3-6**.)*
- ~~`[rule]` Generator logs via `Debug.Log/LogWarning` directly.~~ *(moot — deleted)*
- [x] `[debt]` **Reconcile/retire `narrative-generation.md`.** ✅ done 2026-07-06 (P6-1): retired to a
  tombstone pointing at `narrative-procedural.md` / `narrative-director-requirements.md`, with §4
  preserved as the **P3-8 residue** (statuses above). `loot-subsystem.md` refreshed alongside. See CHANGELOG.

---

## Data-Driven Procedural Narrative

Deferred design (from `narrative-procedural.md` §6):
- [x] `[arch]` **Director Priority-1 — actor/faction eligibility (D16) + recurring-actor casting (D11).**
  *(Done — `RunWindowPlanner` classifies each story: world-only stories keep the actor-less path + fresh
  mint; actor/faction-scoped stories resolve as a casting query over `ILiveActorRegistry` and hard-pin
  the satisfying live actor for recast (continuation semantics). New pure-C# `ILiveActorRegistry`/
  `LiveActorRegistry` bound `AsSingle`. Proven by the `RunWindowPlannerTests` raider-arc + negative
  tests. See CHANGELOG; `narrative-procedural.md` §2.2/§2.6.)*
- [x] `[arch]` **D7 — Spine reserved lane + per-run reveal cap — DONE (P3-1, 2026-07-06).**
  *(Done — verified brief `product-requirements/director-spine-reveal-lane.md`: `_isSpine` stories
  place through a reserved pre-slot-loop lane (never the quest/ambient channels), ≤1 per window,
  throttled by `RunPacingConfig._maxSpineRevealsPerRun` (default 2); a **gated pool, not a queue** —
  "never too early" is per-beat preconditions + a soft floor over the new meta fact
  `world.run_count` (`RunCounterService`, fresh boots only); revealed = placed in `StoryRunLedger`
  (zero new run-state, cap survives continue); bypasses the quest gate + thread ceiling
  (owner-approved); seeded/deterministic, outcome-blind. Placeholder demo beats
  `DemoSpine_CauldronHint`/`DemoSpine_MirrorGlimpse`. 1341/1341 green. See CHANGELOG;
  `narrative-procedural.md` R15/§2.6/§4. Follow-up resolved by P3-3 (2026-07-06): the cross-run
  cursor counts **seen**; within-run stays placed.)*
- [x] `[arch]` **D19 — Escalation tier gating.** ✅ shipped 2026-07-06 (P3-2, `narrative-procedural.md`
  §2.6): a **run-tier band** (`RunTierBand` / `RunTierBandAuthoring`, `min` + optional `max`, `max ≤ 0`
  = open) on `StoryTemplate` and `EnemyDefinition`; the planner gates story eligibility and the
  ambient/site monster-pool draw by `run_escalation_tier`. Pool-shift only — **no** stat multiplier,
  **no** density change (owner's call); unbanded content stays eligible at every tier. See CHANGELOG.
- [x] `[arch]` **D20 — Meta-scoped fact horizon — DONE (P2-3 + P2-2 + P3-3, 2026-07-06).**
  Run-scoped facts reset on death; meta-scoped facts persist; long arcs ride the meta horizon and
  the director reads both. *(P2-3 — **boundary/partition SHIPPED 2026-07-05**:
  `FactKeyDefinition._horizon` (Run/Meta) + the save snapshot's `Facts`/`MetaFacts` split. P2-2 —
  **cross-run store + file IO SHIPPED 2026-07-05**: `meta.json` persists the Meta partition across
  deaths, proven by the demo fact `world.barn_bounty_honored`. P3-3 — **the reading consumers
  SHIPPED 2026-07-06** (verified brief `product-requirements/director-meta-consumers.md`): the
  **spine cursor** persists as `world.<storyId>.spine_seen` (new `FactScope.PerStory`; written by
  `SpineSeenRecorder` at dialogue end, read by the planner's spine channel-split) — **PO decision:
  cross-run never-again = seen, not placed** (a placed-but-never-visited beat returns next run;
  the within-run cap stays run-ledger-based); **mirror-lore echoes** = cross-excluded spine pair
  `DemoSpine_TyrantEcho_Conquest`/`_Alliance` over the new meta deed `world.raider_pact_sworn` +
  the shipped `barn_bounty_honored`; **cauldron memory** = `DemoSpine_CauldronMemory` with a
  "must have seen the hint" cursor floor. Sibling-exclusion and "seen Y" floors are authored
  preconditions with literal story-id subjects — data-only. 1355/1355 green. See CHANGELOG;
  `narrative-procedural.md` R15/§2.6/§4/§6; `save-persistence.md` §4. Known limitation filed:
  echo sibling exclusion binds on seen, so cap ≥ 2 can place both variants in one run — run the
  production spine at cap 1 or author distinct floors on a pair.)*
- [x] `[arch]` **R8 — First-class threads + cross-window continuity — DONE (P2-3, 2026-07-05).**
  *(Done — verified brief `product-requirements/director-threads-and-continuity.md` FR1–FR12:
  `ThreadDefinition` SO (ephemeral/arc kinds, premise facts, resolution conditions, lifespan) +
  `ThreadCatalog`/`ThreadLedger`/`ThreadMaintenanceService` (retirement = fact-conflict fail (incl.
  arc) or ephemeral expiry — indicator fact `world.<threadId>.thread_retired`, no closure storylet) +
  `StoryRunLedger` (never re-place an offered/resolved/failed story) + concurrency cap
  (`_maxLiveThreads`, advance-over-open, wait not force-drop) + `StoryResolutionRelay` (a `leave`
  outcome does not advance the thread) + the D20 run/meta boundary; deterministic + save-captured.
  1234/1234 green. See CHANGELOG; `narrative-procedural.md` §2.6/§3/§4. Follow-ups: OR-composed
  resolution conditions ride P3-5; thread readout / saga view rides the quest-log UI (P1-11);
  cross-run meta store rides P2-2.)*
- [ ] `[debt]` **Legacy `EncounterDirector.BeginEncounter`/`RunDirector` path bypasses the thread
  ledgers.** The per-encounter selector neither opens threads nor respects the concurrency cap (its
  resolutions upsert into the `StoryRunLedger` via the relay). Fine while the streaming planner is
  the only production path; align or retire the legacy path when it gains a caller.
  (`narrative-procedural.md` §6.)
- [~] `[arch]` **Story-first streaming director (cutover).** Replace actor-first per-encounter selection
  with a budgeted, windowed planner that generates platforms window-by-window as the player advances;
  entering a window locks it, the next is planned from live facts (R7). Phases:
  - [x] **Planner core (Phase 0).** `RunWindowPlanner` selects a budgeted story set per window
    (narrative weight budget + separate combat-count budget), matches actors to stories, deterministic.
    `StoryTemplate.Weight` + `RunPacingConfig` SO added. Tested; not yet wired. *(See CHANGELOG;
    `narrative-procedural.md` §2.6.)*
  - [x] **Incremental generation (Phase 1).** `AreaGenerator.Generate()` split into `Initialize()` +
    `AppendPlatforms(nodes)` with a persistent layout cursor; one-shot path preserved, behavior-identical.
    *(See CHANGELOG. The `PlannedPlatform`→`GraphNode` mapping moves to the Phase 2 coordinator.)*
  - [x] **Streaming coordinator + cutover (Phase 2).** `RunStreamingCoordinator` plans/generates platforms
    window-by-window on `PlatformEvents.OnPlatformEntered`; `AreaSceneEntrypoint` drops the legacy narrative
    generator; `DialogueActiveState` runs the new engine via `EncounterDirector.BeginPlanned`; combat-end
    feeds `DialogueRunner.ReportCombatResult`. Legacy `NarrativeInstaller` left dormant. *(See CHANGELOG;
    `narrative-procedural.md` §2.6.)*
  - [x] **Delete legacy (Phase 3).** Removed `NarrativeInstaller` (file + Area SceneContext),
    `Narrative/Generation/*`, `CompositeDialoguePresenter`/`IDialoguePresenter`, `InkExternalFunctionBinder`,
    the legacy `StoryDefinition`/`NpcDefinition`/`RewardDefinition`/`LevelNarrativeConfig` SOs + assets +
    legacy Ink, and the obsolete tests; decoupled `AreaGenerator`/`ScenarioGenerator`/`CutsceneActiveState`;
    moved `IDialogueView` ownership to `NarrativeSliceInstaller`; deleted the now-unused
    `ScenarioGenerator`/`IScenarioGenerator`/`PlatformGraphGenerator`/`IPlatformGraphGenerator`/`GameContext`
    one-shot pipeline + their bindings. *(See CHANGELOG.)* `DialogueContent` kept (unused, not
    narrative-coupled); `StoryPlatformData`/`ScenarioData` data classes kept (`GraphNode.StoryData`).
- [x] `[content]` **World content density — a rare, breathing world** (brief
    `product-requirements/world-content-density.md`). *(Done — `WorldContentAllocator` allocates each
    window slot one of the four content kinds (Empty/Loot·scattered/Combat·wild-beast/
    NPC·quest-bearer): quests are a seeded 1-in-N roll behind a hard min-spacing carried across
    windows; ambient slots split by weights from the one `WorldContentDensityConfig` SO; ambient
    monsters draw from per-biome `BiomeMonsterPoolDefinition` pools at flat difficulty; loot platforms
    roll the biome platform table on the streaming path. Deterministic. See CHANGELOG;
    `narrative-procedural.md` §2.6. Subsumed the "Streaming-path loot" item; the biome half remains
    below.)*
- [x] `[content]` **Biome selection along the run — DONE (P0-2, 2026-07-04).** *(Done — the biome
    journey (`biome-journey.md`; brief `biome-selection-along-the-run.md`): authored, seeded,
    tier-climbing biome stretches (planning windows) via the new pure `LevelGeneration.Journey` core
    (`BiomeJourney`/`BiomeStretchDirector`) + `BiomeProgressionConfig` SO; crossing a stretch
    switches monster pool + loot table (live `ICurrentThemeProvider`; `AreaGenerator` reads it live
    now) + landmark dressing + backdrop; Cave excluded by data; journeys diverge by seed on their own
    random stream; `run_escalation_tier` world fact published (the D19 seam). Race
    homelands/passport context ride the existing per-theme configs as they land (P0-3). See
    CHANGELOG. Follow-ups below; D19 escalation stays a separate thread.)*
- [ ] `[content]` **Real Mountain/Desert monster rosters.** `MonsterPool_Mountain` /
    `MonsterPool_Desert` are placeholders reusing the two Forest demo enemies so ambient combat
    survives outside Forest; author distinct per-biome enemies on the ambient-monster content pass
    (P1-13). *(combat + world)*
- [ ] `[content]` **Per-stretch route/landscape character + biome-transition legibility.** The route
    model keeps the entry biome's `BiomeLandscapeSettings` for the whole run (a mid-run swap would
    discontinuously jump `Sample(x)` and desync the landmark scan); the backdrop swap is a hard cut.
    A piecewise blended multi-biome route + gate/skyline transition art rides the M5
    world-backdrop/site-dressing pass. *(world + tech-art)*
- [x] `[arch]` **D19 consumers of `run_escalation_tier`.** ✅ shipped 2026-07-06 (P3-2): the published
    tier is now consumed by the story eligibility gate and the ambient/site monster-pool draw — the
    eligible pool shifts register and toward tougher creatures as the run climbs; **density is not**
    modulated by tier and **no** per-tier stat multiplier is applied (owner's call). *(The journey's
    PRNG needed no save-state after all: P2-2 replays the seeded, idempotent `ApplyForWindow(0..k)` on
    restore.)* *(narrative)*
- [ ] `[debt]` **Remove the dead platform-loot chance API.** Loot-platform *presence* is owned by the
    density allocator now; `LootRollService.ShouldPlaceLootOnPlatform` and
    `BiomeLootDefinition._platformLootChance` have no callers — delete them (and their
    `BiomeLootData` field) on the next loot pass. *(loot)*
- [x] `[arch]` **Director pacing — thread balancing — DONE (folded into R8/P2-3, 2026-07-05).**
  *(Shipped with R8 above: cross-window advance-over-open preference, live-thread concurrency cap,
  causal-order placement + no stale re-placement via the run-scoped ledgers.)*
- [ ] `[content]` **Demo fact-web sharpeners.** The deeper two-thread demo (`narrative-procedural.md` §4)
  defers, to sharpen director fact-analysis coverage later: (a) ~~numeric-threshold facts~~ and
  (b) ~~the real mutation→fact passport projection~~ — **both done 2026-07-04** by the races/passport
  work: the `frog_marsh` thread now gates on the body-derived int tier `faction.fox.reads_as_tier`
  with `Gte`/`Lt` (D15; see `races-passport.md`); (c) a **single NPC offering several resolution cards
  at once** (`npc-encounter-cards.md` §3), which needs multiple quest slots per story (one today) —
  still open.
- [x] `[content]` **Quest-carried item rewards.** *(Done — item rewards live on `QuestDefinition._rewards`
  and are granted on completion by `QuestRewardGranter` (no longer a no-op) via the `PlatformCompletedState`
  hook. Item rewards only; currency/experience/ability kinds still lack a receiving system. See CHANGELOG;
  `quest-subsystem.md`.)*
- [x] `[arch]` **Window/horizon save-state — DONE (P2-2, 2026-07-05).** *(All five captured/restored:
  the realized-window record + streaming cursors (`RunStreamingCoordinator.CaptureWorld/BeginRestored`),
  the live-actor set (replayed `Register` by id), the live-quest set (lifecycle replay), the
  quest-spacing counter, and the site-allocator state. See CHANGELOG; `save-persistence.md`.)*
- [ ] `[arch]` **R11 — Reactive-rule cascade layer.** Optional central layer that derives cross-category
  cascades from fact reads/writes; cascades are explicit authored effects until then.
- [ ] `[arch]` **OR/boolean precondition composition.** Preconditions are AND-only; add OR/grouping.
- [x] `[arch]` **R14 — Save/load file IO — DONE (P2-2, 2026-07-05).** *(The versioned/atomic file layer
  (`run.json` + `meta.json` under `persistentDataPath/Saves`), the whole-run aggregate
  (`RunStateService`), platform-entry autosave, death-consumes-save, the cross-run meta store, and the
  MainMenu Continue entry point. See CHANGELOG; `save-persistence.md`.)*
- [ ] `[arch]` **W3-1 option-b: mid-dialogue saves.** Persist a suspended dialogue's Ink state +
  pending-external descriptor (the still-unpopulated `Sessions` DTO field) so a quit mid-conversation
  resumes mid-conversation instead of at the platform's clean start. *(narrative)*
- [x] `[arch]` **Dialogue view adapter.** *(Done — `DialogueRunnerViewPresenter` (MVP) drives the
  existing `IDialogueView` from the runner's events and is wired in `NarrativeSliceInstaller`; the runner
  gained continue-gated pumping (`AwaitingContinue` + `Continue()`) so multi-line knots are read one line
  at a time. See CHANGELOG; `narrative-procedural.md`. A whole-dialogue skip/abort path remains open.)*
- [~] `[arch]` **Whole-dialogue skip/abort.** `DialogueRunner.Leave()` now ends the whole conversation
  (outcome `"leave"`, the card-hand Leave card). Still open: skip-to-end of a running multi-line knot, and
  abort while suspended on combat (`Leave()` refuses `AwaitingExternal`).
- [x] `[arch]` **Encounter card model — cut branching dialogue (presentation cutover).** *(Done, MVP
  scaffold — the encounter is a **situation bubble + composed hand of typed cards**
  (`EncounterCardHandPresenter` + `IEncounterCardHandView`) over the unchanged runner; the tag/effect
  machinery is kept as the card-outcome channel, only presentation + choice selection changed. `DialogueRunner`
  gained `TriggerCombat`/`Leave`/`CombatAvailable`. See CHANGELOG; `narrative-procedural.md` §2.7. The
  reward tier-glow/belonging colour, the Monster verb, and several-offers-per-NPC **all shipped with
  Track H 2026-07-07** (⚠ gameplay-untested; `## Quests`); only the deletion of the old UI remains as the
  separate `[debt]` item below.)*
- [ ] `[debt]` **Remove the branching-choice dialogue UI.** The card hand has landed (`narrative-procedural.md`
  §2.7) and the old UI is dormant/unbound — this removal is now ready to execute: retire the
  branching-choice presentation: the runner's `OnChoices`/`SelectChoice` path + `StoryChoice`, the
  reading parts of `IDialogueView` (`SetDialogueText`/`ShowChoices`/`PlayTypewriterEffect`/continue-gating)
  and their `DialogueRunnerViewPresenter` handlers, and the `*`-choice authoring in the field `.ink`
  files (migrate `BarnVictim`/`BarnRaid`/`GratefulFarmer` to the card model). The fact/quest/tag engine
  stays. *(debt — narrative)*
- [ ] `[content]` **PerLocation-scope content.** Data shape supports per-location world facts (A1);
  author content that uses it (e.g. `world.<locationId>.burned` cascades).
- [ ] `[content]` **Optional ambient/bark channel.** The legacy dual-Ink bark channel was dropped; if
  ambient lines are wanted, add a separate non-narrative system rather than overloading the session.
- [x] `[debt]` **Legacy cutover.** *(Done — `DialogueActiveState`/`NpcContent` run the casting/streaming
  engine; the old `NpcDefinition`/`StoryDefinition`/`CompositeDialoguePresenter`/`NpcAssignment` path,
  `NarrativeInstaller`, and assets are deleted. See CHANGELOG; "Story-first streaming director" above.)*
- [x] `[content]` **Barn demo — exercise the quest loop end-to-end.** *(Done — `DemoQst_BarnBounty`
  (`qst_barn_bounty`, tag `bounty`, objective `obj_return_grain`, reward `1× rock`) fills the `bounty`
  Quest slot. Now split **across two platforms** to prove cross-dialogue continuity: `story_barn_victim`
  (window 1) carries the `bounty` slot and **offers** the quest on the "bring your grain back" branch;
  `story_grateful_farmer` (window 2) restores the live instance and **advances/completes** it (its
  `GratefulFarmer.ink` no longer offers), so the reward is granted on the window-2 platform. Reachable
  after accepting the victim's plea + winning the raid. See CHANGELOG; `quest-subsystem.md` (R8, §4);
  `narrative-procedural.md` §4.)*
- [ ] `[content]` **Barn demo — combat loss branch.** `BarnRaid.ink`'s fight `else` (raider wins) is a
  flavor line treated as run-end; wire a real loss consequence once a run-failure path exists.
- [ ] `[arch]` **D12 — Window-1 adjacency ordering.** The barn demo only needs the victim + raider to
  co-appear in window 1; ordered/adjacent placement within a window is not guaranteed yet.
- [ ] `[content]` **Barn demo — recast the victim into reaction A.** Optional second recurring-actor proof:
  make `story_grateful_farmer` recast the *same* villager from `story_barn_victim` via an actor-scoped fact
  (currently A/C mint a fresh `arch_villager`).

---

## Character Progression

New work (no system doc yet — author `character-progression.md` when implemented):
- [x] `[arch]` **M2 — Run progression record.** *(Done — pure-C# `CharacterProgression/Core`:
  `IRunProgressionRecord` (read) / `IRunProgressionRecorder` (write) / `RunProgressionRecord`
  tracking quests (active/completed/failed), NPCs encountered, and key choices, plus
  `RunConditionEvaluator`. Recorded from the dialogue flow (NPC encounter + Ink `start_quest`);
  consumed by `RewardResolver` to evaluate `RewardSlot.Condition`. See CHANGELOG;
  `character-progression.md`. Quest **completion/failure** recording is now wired — the quest
  lifecycle tags drive `IRunProgressionRecorder.CompleteQuest`/`FailQuest` (see `quest-subsystem.md`).
  `StoryNodeRequirement` gating remains open — see follow-ups below.)*
- [ ] `[arch]` **M2 — Condition AND/OR composition.** `RunConditionEvaluator` supports a single
  predicate only; add boolean composition so a reward/node can require multiple run-state facts.
- [ ] `[arch]` **M2 — Wire `StoryNodeRequirement` gating.** `RequiredActiveQuests` /
  `RequiredEncounteredNpcs` are still unused; consume them in story selection (reusing
  `RunConditionEvaluator`) once the Quests slice adds the required-progression authoring surface.
- [ ] `[content]` **M2 — Generic Ink choice recording.** Add a `record_choice` Ink external function
  feeding `IRunProgressionRecorder.RecordChoice` so key choices are captured data-drivenly.
- [ ] `[arch]` **M2 — Experience as a first-class reward sink.** Give the `Experience` reward type a
  receiving system here, resolving part of the Cross-cutting reward gap.

---

## Quests

See `quest-subsystem.md` for the implemented quest fragment, lifecycle, and the quest-as-reward economy.

> **Track H — the quest-as-reward economy — SHIPPED (code) 2026-07-07** (`quest-subsystem.md`,
> `encounter-dialogue-ui.md`, `cauldron-barks.md`; briefs in `product-requirements/`). Every item below
> is code-complete and edit-mode green (1605/1605).
>
> ⚠ **AWAITING OWNER GAMEPLAY TEST.** None of Track H has been play-tested — the *behaviour* is
> implemented and unit-covered, but the *on-screen feel* (offer-card mystery slot + glow/tint, the
> quest-log panel, the cauldron barks, the attack-verb + fork flow) is **unconfirmed in play mode**.
> Do a gameplay pass on the barn/frog demo before treating the presentation as done.
- [x] `[arch]` **M2 — Separate quests from stories.** *(Done — `QuestDefinition` SO (objectives, tags,
  on-complete/on-fail fact effects, rewards) distinct from the story; matched into a story Quest slot by
  tag; `QuestData`/`QuestInstance`/`QuestMapper`. The placeholder `QuestContent` is no longer used by the
  engine. See CHANGELOG; `quest-subsystem.md`.)*
- [x] `[content]` **M2 — Quest rewards live on the quest.** *(Done — item rewards authored on
  `QuestDefinition._rewards`; granted on completion via `QuestRewardGranter` through the
  `PlatformCompletedState` hook. Item rewards only — currency/experience/ability reward kinds still have
  no receiving system (Cross-cutting). See CHANGELOG.)*
- [x] `[arch]` **M2 — Explicit quest-completion signal from Ink.** *(Done — `complete-quest:` /
  `fail-quest:` / `advance-objective:` Ink tags drive `QuestInstance.Complete`/`Fail`/`AdvanceObjective`
  in `DialogueRunner`; completion is explicit, not on walking away. See CHANGELOG; `quest-subsystem.md`.
  The Loot §4 "explicit Ink quest-completed signal" walk-away limitation is resolved for the new engine.)*
- [x] `[arch]` **M2 — Cross-dialogue quest continuity.** *(Done — run-scoped `ILiveQuestRegistry` /
  `LiveQuestRegistry` (bound `AsSingle` beside `ILiveActorRegistry`) holds live `QuestInstance`s by id;
  `DialogueRunner.Begin` restores the registered instance when the casting carries an already-offered quest
  (matched by id via its Quest slot) instead of resetting, and `QuestRewardGranter` now scans the registry.
  A quest can be offered on platform A and completed on a later platform B. Demo: `story_barn_victim`
  (window 1) offers `qst_barn_bounty`, `story_grateful_farmer` (window 2) completes it. Tested by
  `LiveQuestRegistryTests` + a cross-dialogue `DialogueRunnerTests` case. See CHANGELOG; `quest-subsystem.md`
  R8.)*
- [x] `[content]` **M2 — Quest log UI.** *(SHIPPED code, ⚠ gameplay-untested — P1-11, `quest-subsystem.md`
  R12/§2.7. A read-only **J**-toggled panel projecting the live registry + thread ledger: quests grouped
  into **sagas** by thread with the lifecycle verdict, each showing state + objectives + giver + reward
  **tier + belonging only**. Pure `QuestLogModel(+Builder)` + `QuestLogView`/`QuestLogPresenter`; tested
  by `QuestLogModelBuilderTests`. **Panel art/layout is a flat rich-text placeholder — needs the gameplay
  pass + a render-look pass.** See CHANGELOG.)*

Quest-as-reward (Track H — SHIPPED code 2026-07-07, ⚠ awaiting owner gameplay test):
- [x] `[arch]` **Reward = declared `tier + belonging + payload kind`, rolled — not a literal id.**
  *(SHIPPED — P1-5 + P0-3·b, `quest-subsystem.md` R7/§2.4/§2.5. `QuestRewardCore` is a declaration;
  `QuestRewardRoller` (Loot.Core) rolls a concrete artifact/blank deterministically under the run seed —
  belonging a hard filter, tier a nearest-tier bias; `QuestRewardGranter` routes by kind. New
  `RewardFamilyDefinition` SO + `BelongingTintCatalog`; new `ArtifactDefinition._rewardFamilyId` /
  `PartBlankDefinition._raceId` fields. `QuestRewardRollerTests` + rewritten `QuestRewardGranterTests`.
  **Roll tables are uniform-weight nearest-tier — no rarity curve yet.** See CHANGELOG.)* *(quests + loot)*
- [x] `[arch]` **Competing / mutually-exclusive same-tier offers, separated in time.** *(SHIPPED — P1-8,
  `quest-subsystem.md` R10. Two offers on **opposed threads**; committing to one writes the fact that
  fails the other on conflict via the shipped maintenance mechanism. Demo `barn_raid` vs `raider_pact`.
  `CompetingOffersForkTests`. ⚠ gameplay-untested. See CHANGELOG.)* *(quests + narrative)*
- [x] `[content]` **Encounter card-hand UI (replaces the dialogue choice UI).** *(Done, MVP — the NPC
  encounter is a situation bubble + a centred **hand of typed cards** composed by `EncounterCardHandPresenter`
  over the `DialogueRunner` offer/choice events; card types **quest-offer** / **attack** (Ink `# card: attack`
  or system-added when combat-capable) / **leave** (always). Supersedes the line-reading + Ink choice-list UI
  (now dormant). The card visual is a placeholder per-type tint — the **frame glow = tier, color = belonging,
  exact item hidden** treatment is still gated on the Crafting tier model. See CHANGELOG; `narrative-procedural.md`
  §2.7. Prefabs `Prefabs/UI/Encounter/EncounterCardHandView` + `EncounterCardView` are authored and wired.)* *(quests + narrative)*
- [x] `[content]` **Encounter Dialogue UI — Hades-style box (presentation/feel upgrade).** *(Done, MVP —
  bottom-centre dialogue box with portrait + name, word-by-word line reveal (tap to complete), cards
  centred above the box, the **quest card labelled with the job** (title + summary), and author-marked
  `[[ ]]` keyword highlighting in lines and cards. Pure-C# `KeywordHighlightFormatter`; `DialogueRunner`
  gains `OfferedQuest`/`EncounterArchetypeId`/`EncounterDisplayName` read-only seams; view on TMP via
  `maxVisibleCharacters`; portrait resolved through `INpcArchetypeCatalog`. Engine unchanged. See
  `encounter-dialogue-ui.md`; `narrative-procedural.md` §2.7. **Deferred:** per-tier card glow (gated on
  Crafting), several quest cards per NPC, mid-conversation portrait/emotion changes, automatic name
  detection.)* *(quests + narrative)*
- [x] `[arch]` **Several quest offers per NPC/storylet (new authoring shape).** *(SHIPPED — P1-9,
  `quest-subsystem.md` R9. A story carries several Quest slots; the card hand shows them together, each
  resolved by `offer-quest: <tag>` (choice + branch), same tier / different belonging; picking one mints
  only that quest. Demo `story_frog_elder_open`. `MultipleOffersTests`. ⚠ gameplay-untested.)* *(quests + narrative)*
- [x] `[content]` **Quest-offer card visual treatment.** *(SHIPPED — P1-6, brief `quest-offer-card.md`,
  `encounter-dialogue-ui.md` R10/R11. The quest card gains a **mystery reward slot**: item hidden as `?`,
  glow by declared tier, chip tinted by belonging colour via `IBelongingTintCatalog`; **hover-inspect**
  reveals the job detail, reward stays hidden. `EncounterCardView.prefab` extended. ⚠ gameplay-untested —
  the mystery slot is a placeholder shader treatment; the ornate frame / real glow shader / reveal anim
  stay a render-look pass.)* *(quests + narrative)*
- [x] `[arch]` **Attack card — the Monster verb.** *(SHIPPED — P1-7, brief `attack-card-monster-verb.md`,
  `quest-subsystem.md` R11/§2.6. On a dialogue-routed kill (attack card or self-initiation, one path via
  `DialogueRunner.OnCombatResolved`): forecloses the actor's thread (`Foreclosed` + indicator), writes
  `actor.<id>.slain` (planner never recasts a slain actor) + `world.path_conquest`; corpse-loot stays on
  the separate combat channel. `MonsterVerbConsequences`; `MonsterVerbConsequencesTests`. ⚠ gameplay-
  untested.)* *(quests + narrative + loot)*
- [x] `[content]` **Cauldron-voice tempter hook on the dark offer.** *(SHIPPED — P1-10, brief
  `cauldron-voice-barks.md`, new `cauldron-barks.md`. The live bark channel — slots (temptation · dark
  offer/attack · restraint · socketing trend) × path lean (`path_conquest` vs `path_restraint`, no
  meter), deterministic, data-authored `CauldronBarkLinesConfig`. Fired from the mutation/encounter
  presenters + `ISocketingTrendSource`. `CauldronBarkServiceTests`. ⚠ gameplay-untested; RU placeholder
  lines; trend trigger has no throttle yet.)*

---

## Loot Subsystem

Known limitations (from `loot-subsystem.md` §4):
- [x] `[content]` Explicit Ink "quest completed" signal — *(Done for the new engine — quests complete on
  an explicit `complete-quest:` Ink tag, not on walking away; rewards are granted for a `Completed` quest
  only. See `quest-subsystem.md`; CHANGELOG.)*
- [x] `[arch]` Filler platforms (beyond scenario requirements) never carry loot. *(Resolved by
  world-content-density — loot platforms are a first-class content kind the density allocator places
  on the streaming path; see CHANGELOG, `narrative-procedural.md` §2.6.)*
- [ ] `[arch]` Non-item `RewardType`s have no receiving system (see Cross-cutting).
- [ ] `[arch]` Inventory capacity feedback (R11) is unreachable while inventory is unlimited;
  `UnlimitedCapacityPolicy` is the rebinding point.
- [ ] `[content]` Progression gating (R13: `minPlayerLevel`, `requiredAchievements`, `requiredPastQuests`)
  is surfaced in the roll context but no `ILootEntryFilter` consumes it yet.
- [ ] `[debt]` `WorldArtifactView` duplicates `BubbleView`'s depth-stack construction; extract a shared
  builder once a third consumer appears.
- [ ] `[arch]` World pickups are not despawned when leaving a platform; they persist until collected or scene unload.
- [x] `[debt]` **Refresh `loot-subsystem.md` to the streaming cutover.** ✅ done 2026-07-06 (P6-1):
  rewrote the requirements + §2 to the live state — loot-platform *presence* via the density allocator,
  fixed `QuestRewardCore` grant (no roll), legacy reward channel deleted; flagged the dead
  `ShouldPlaceLootOnPlatform` / `RollQuestRewards` / `_platformLootChance` for the **P6-4** cleanup.
  Seed / `WeightedPicker` / biome-table Core and pickup runtime confirmed current. See CHANGELOG.

---

## Platform & Area Generation

System doc: `platform-generation.md`.

**The three M3 platform-hex items shipped** (verified brief
`product-requirements/platform-hex-surface-and-shape.md`; see the doc + CHANGELOG):
- [x] `[arch]` **M3 — Hex-composed platform top surface.** *(Done — the surface is composed of whole
  hex cells (`PlatformHexSurface`, grown by `PlatformSurfaceGenerator`); the combat grid is derived
  1:1 from it (`SurfaceHexGrid` — the scan grids `FlatHexGrid`/`PointyHexGrid`/`HexGridFactory` are
  deleted); muted-in-traversal is a geometry MVP (per-cell shallow domes → valley seams, `CellInset`
  dial), crisp-in-combat stays the cell-outline visuals. See CHANGELOG; `platform-generation.md`.)*
- [x] `[arch]` **M3 — Natural edges over a complete hex interior.** *(Done — hole-filled whole-cell
  interior; jittered non-walkable rim ring (width/jitter/drop tunables) beyond the walkable outline;
  wall colliders sit on the outline so the rim is physically unreachable. Whole-cell biome-feature
  alignment went **live** with Track E (2026-07-06): `BlockedCells` is populated by the dressing
  planners and excluded by grid/anchors — see `environment-dressing.md`. See CHANGELOG;
  `platform-generation.md`.)*
- [x] `[arch]` **M3 — Content-aware platform size & shape.** *(Done — per-content-kind
  `ShapeProfile`s + battlefield minimum (12 cells) on the one `PlatformShapeConfig` SO; kind resolved
  from the graph node (`Type == Combat` covers ambient + story-with-required-combat); per-platform
  seeded streams (`LootSeed.Derive(runSeed, "platform-shape:{id}")`) make same-seed→same-platforms
  hold, replacing the last `UnityEngine.Random` uses on the platform path. See CHANGELOG;
  `platform-generation.md`.)*

Follow-ups from the platform-hex rework (doc §6):
- [x] `[arch]` **Regression — hero stops at an invisible wall inside the visible island.** *(Done —
  PO decision after play-test (2026-07-03): keep the drooping rim look; fix the physics instead.
  `OutlineStitcher` (inside `PlatformSurfaceGenerator`) sews the between-cell V-notches out of the
  walkable outline and paves them with `NotchFills` floor patches — walls / registry / AI boundary
  follow the smooth sewn edge, the floor continues under the hero across the notches, and the rim
  droops from the stitched edge. `PlatformAnchor` lands jumps on the nearest walkable cell center
  and teleports/spawns on the center cell — always inside the pen, replacing the mesh-edge and
  boundary-line landing probes that measured the rim beyond the walls. The rim stays non-walkable
  and never a combat cell; fills are walkable but never combat cells. See CHANGELOG;
  `platform-generation.md`.)* *(platform)*
- [x] `[arch]` **Regression — units sink waist-deep into the battlefield.** *(Done — new
  `UnitGrounding` grounds units explicitly: surface top from `HexToWorld` + a feet/pivot offset
  derived from the unit's own authored collider (CharacterController, else CapsuleCollider),
  applied at the five unit placement/animation sites; `SurfaceHexGrid` keeps returning the true
  surface top, now test-asserted. No double-add restored, no per-prefab magic numbers. See
  CHANGELOG; `platform-generation.md`.)* *(combat + platform)*
- [ ] `[arch]` **Unit grounding — per-model override for feet ≠ collider bottom.** `UnitGrounding`
  assumes a model's visual feet coincide with its collider bottom; a future rig where they differ
  needs an authored per-model offset override (doc §6 lists the limitation). *(combat + platform)*
- [ ] `[content]` **Muted→crisp render treatment (tech-art).** Replace the geometry-MVP dome seams
  with the real shader/VFX emphasis on combat entry (ties the render-look bible + M4 telegraph VFX
  language). *(platform + tech-art)*
- [ ] `[arch]` **Camera/entry pass at arena scale.** Combat platforms are now ~14–18u across
  (intended); verify camera framing, `CombatEntryAnimator`, and hop feel in play mode and tune.
  *(platform + camera)*
- [ ] `[arch]` **`ContentSpawner` placement on concave islands.** Content still spawns relative to
  the platform center, which can sit off-cell on a concave union; place on `Surface.CenterCell` (the
  character spawn already does). *(platform)*
- [ ] `[debt]` **Extract a neutral `Core.Hex` namespace.** `HexCoordinates`/`HexOrientation`/
  `HexMetrics`/`PlatformHexSurface` live in `Combat.Battlefield` and are consumed by
  LevelGeneration/Platform; the rename sweep (~43 files) was deferred (no compiler in env). Also lift
  `IRandomSource`/`DeterministicRandom` out of `Narrative.Director.Core` for the same reason.
  *(platform + combat)*
- [x] `[content]` **Sites & landscape — content-driven footprint (setting scale).** *(Done — the
  what-fills-where half of the verified brief `product-requirements/world-sites-and-landscape.md`
  shipped in four phases; see the new system doc **`world-sites.md`** + CHANGELOG. Site domain
  (`World.Sites.Core`: the shared base·flavor vocabulary, capacity recipes anchor + weighted fill +
  connective, `SiteStamp`); `SiteFamilyDefinition`/`SiteDefinition` SOs (family default +
  explicit-toggle overrides; authored Camp/Village/City/Ruin/Lair over Settlement/Landmark
  families); `SiteAwareSlotAllocator` over the untouched density allocator (quest-pulled
  settlements via `TryReserveSettlement` + `WildQuestWeight` roll / `site:<id>` hard-request tag;
  rare spaced ambient roll for landmarks; run-scoped pending queue spans window boundaries; empty
  catalog = bit-exact passthrough); planner fills site Npc slots from townsfolk-tagged chatter
  stories (excluded from quest picks); flavored monster-pool draws + loot flavor `BiasTags`;
  `GraphNode.Site`/`ContentFlavor` seam for M5 dressing. Deterministic — the acceptance histogram
  proves Wild majority, contiguous blocks, city-busier-than-village, no-townsfolk landmarks. The
  **visual** "reads as one place" half stays the M5 Site-dressing item under World & Environment.)*
- [ ] `[content]` **Sites — occupancy/passport, tier/altitude, biome-compatibility schema fields.**
  Deferred by PO decision (2026-07-03): their consuming systems (passport gating, escalation tiers,
  biome selection along the run) don't exist yet; the additive-defaults schema makes adding them
  later migration-free. *(world/sites)*
- [ ] `[arch]` **Sites — `NPC·quest-bearer` as a fill beat (Camp's "shady offer").** An NPC fill
  flavor is planner-matched by story tag; quest semantics on a fill slot are undefined, so Camp is
  authored without it (design §5.3 lists it). Needs its own design pass. *(world/sites + quests)*
- [ ] `[content]` **Sites — per-flavor loot tables if bias proves too soft.** `market/stash/chest/
  relic` multiply weights of tagged entries in the one biome platform table; a dedicated table per
  flavor (a chest that never drops commons) is the escalation path. *(world/sites + loot)*
- [ ] `[content]` **Sites — real per-flavor enemies.** `guard`/`den-monster` demo tags ride the two
  placeholder pool enemies (`TestEnemyDefinition`, `DemoEnemy_BanditBrute`); author distinct
  enemies per flavor on the next combat-content pass. *(world/sites + combat)*
- [x] `[content]` **M3 / P1-2 — Biome-driven platform appearance & features — SHIPPED (Track E ·
  E2, 2026-07-06).** *(Brief `product-requirements/biome-visual-styles.md`; see
  `environment-dressing.md` + CHANGELOG.)* Per-biome **ground material + feature pool** bound as a
  whole **biome-feature kit** on `BiomeAppearanceDefinition` (`_featureKit` + tone + density dials);
  features place on **whole hex cells**, decorative (clustered, several per cell) or blocking
  (`BlockedCells` live — one per cell, sparse, battlefield-minimum/lane/connectivity guards);
  deterministic per node; overrides the global `AreaGeneratorConfig.platformMaterial` when bound.
  Desert + Forest demo kits authored; Mountain/Cave stay base-layer.
- [ ] `[content]` **Biome appearance residue — palette key + light.** The brief's per-biome
  **palette-key modulation of the master palette** and **light** fields are not implemented (the
  bind-time `_toneTint` tone treatment stands in); lands with the render-look pass (P5-8).
  *(world + tech-art)*
- [ ] `[content]` **Mountain & Cave biome-feature kits.** The two demo packs don't cover them —
  both biomes render the base layer until a kit (demo or production) is authored. Data-only.
  *(world · designer)*

---

## NPC Interaction & Encounter Triggering

Implemented — system doc `npc-proximity-interaction.md` (brief
`product-requirements/npc-proximity-interaction.md`):
- [x] **Proximity interaction trigger (replaces land-on-platform).** F prompt in an interaction radius,
  nearest-NPC targeting; landing no longer auto-starts encounters (`PlatformStateFactory` +
  `NpcProximityPresenter` + `NpcEncounterStarter`).
- [x] **Derived NPC intent (quest-bearer / hostile / plain), pre-dialogue.** `RunStreamingCoordinator`
  casts at window generation and carries the `Casting` + `NpcIntent` on `NpcContent`; the encounter reuses
  that casting. (`NpcIntentResolver`)
- [x] **Aggro-on-approach for hostile NPCs.** Crossing the aggro radius spawns `EnemyContent` → combat,
  no dialogue.
- [x] **Always-visible intent markers** + **name label**, billboarded (`NpcOverheadView`).
- [x] **Global interaction/aggro radius config** SO (`NpcInteractionConfig`) + `NpcInteractionInstaller`.
- [x] **Toggleable dev debug overlay** for the radii (`NpcRadiusDebugView`, F2, editor/dev-build only).

Open / follow-ups:
- [x] `[content]` **Track D · Humanoid bandit camp — SHIPPED (D1, 2026-07-05)** (verified PO brief
  `product-requirements/bandit-camp-humanoids.md`; see CHANGELOG). Humanoid enemies (shared model, capsule
  retired by data), demo role tints, boss-led camp (boss + seeded crew 2–4), larger platform-scoped boss
  circle (talk-or-fight on cross), no aggro on landing, crew joins the one fight; deterministic +
  save/restore-aware. Platform scoping applies to **all** NPCs (PO decision). Playtest extension
  (2026-07-05, PO): landing never starts combat for **any** content — lone ambient monsters carry their
  own hostile handle + aggro radius (R13), the legacy `ContentSpawner` capsule path is retired, every
  enemy defaults to the shared humanoid + enemy-red tint, and camp frequency is demo-tuned (~1 per 16
  platforms). Follow-ups filed below.
- [ ] `[arch]` **Boss re-engagement after a peaceful talk.** A camp whose boss was talked to peacefully
  stands forever: his handle is consumed and the crew can no longer be fought (PO-accepted for the demo).
- [ ] `[arch]` **Boss visual continuity at fight start.** `StartAggro` destroys the boss's NPC body and
  combat respawns an identical humanoid from `DemoEnemy_BanditBoss` (same assembly + tint — visually
  seamless); reusing the exact GameObject instance is an `EnemyContent` refactor.
- [ ] `[arch]` **Live fact-driven marker refresh.** The `?` currently clears when the encounter is started
  (consumed), not by per-frame re-evaluation of quest availability against the fact store.
- [ ] `[arch]` **Per-NPC / per-archetype radius overrides.** Still global values; the camp boss's larger
  reach shipped with D1 as the single role carve-out (`_bossEngagementRadius`) — the general authored
  override surface stays open.
- [x] **No-quest-but-talkable-with-optional-fight now expressible.** Hostility requires a **required**
  (non-optional) combat slot; an optional combat slot is a dialogue branch, so the NPC stays talkable
  (e.g. `DemoStory_BarnRaid`'s fight-or-bribe raider). Resolved in `NpcIntentResolver`.
- [x] `[content]` **No forced-combat NPC in the demo — FIXED (D1, 2026-07-05).** The hostile camp boss
  (`DemoStory_CampBossHostile`, required combat slot) is the demo subject. Boss stories are **ambient
  colour** (site-catalog boss flavors), so they never enter the quest channel and do not perturb the
  seeded threads the way an always-eligible quest-channel combat story would.
- [x] `[arch]` **Streaming planner does not enforce thread ordering / cross-window continuity (R8) —
  FIXED (P2-3, 2026-07-05).** *(The run-scoped `StoryRunLedger` means a story already placed or
  resolved is never re-placed regardless of fact staleness, a consequence beat stays held until its
  prerequisite fact is live (landing a window later is the accepted thinner window), and retired
  threads' beats are excluded — the look-ahead now plans against the current story/thread state, not
  stale facts. Proven by `RunWindowPlannerThreadTests.PlacedStory_NotRePlacedNextWindow_...` and
  `ConsequenceBeat_HeldUntilCauseFactLive_NeverBeforeCause`. See CHANGELOG.)*
- [ ] `[content]` **Marker/name art polish + animation.** Currently plain 3D-TMP glyphs built in code.

---

## World & Environment

New work (no system doc yet):
- [x] `[content]` **M5 / P5-2 — Natural landscape read (routed path · elevation tiers · backdrop).**
  ✅ Shipped 2026-07-04 — see `world-landscape.md` + CHANGELOG. Routed bounded weave with
  landmark-justified feature arcs, quantized elevation tiers, hero-anchored hazed biome horizon;
  per-biome character on the new `BiomeAppearanceDefinition` (`Resources/World/Biomes`);
  `_heightDeviation` drunk walk + `PerlinNoiseMap` removed. Parked escalations stay open below.
- [x] `[content]` **M5 — Landscape around platforms.** ✅ Absorbed into P5-2's backdrop axis and
  shipped with it (`world-landscape.md` §2.3; placeholder silhouettes until P5-4 meshes).
- [ ] `[content]` **P5-2 follow-ups (landscape read).** (a) Real landmark/**ridge** meshes from the
  decoration asset pipeline (P5-4) into the `BiomeAppearanceDefinition._silhouetteKit` (the
  hero-anchored ridge strips are still procedural — the **E4 backdrop scatter** below is a separate,
  world-fixed 3D layer in front of them); (b) **parallax / sky animation** polish (backdrop is rigidly hero-anchored);
  (c) **path-following camera yaw** — parked escalation, revisit only if playtest reads the weave as
  "platforms sliding sideways"; (d) **route↔real-backdrop coupling** (thread the path through the
  actual horizon silhouette — causality is faked today); (e) **site-aware tier flattening** — the
  route is site-blind, a site block spanning a tier step / arc apex may fight "reads as one place"
  (now *felt*: Track E's shared skyline band tolerates the weave at demo scale but flattening would
  sharpen it); ~~(f) P1-2 extends `BiomeAppearanceDefinition` with feature-pool/palette/ground
  fields~~ ✅ done (Track E · E2, 2026-07-06 — `environment-dressing.md`; palette/light residue
  filed under Platform & Area Generation).
- [x] `[content]` **M5 / P5-3 — Site dressing ("reads as one place") — demo half SHIPPED (Track E ·
  E3, 2026-07-06).** *(Brief `product-requirements/site-camp-dressing-kits-demo.md`; see
  `environment-dressing.md` + CHANGELOG.)* `SiteStamp.DressingThemeId` is consumed: Settlement kit
  (buildings on a block-shared skyline band + gate/threshold on the anchor + street ground —
  Village/City share `settlement-kit`) and Camp kit (campfire focal + facing prop ring +
  packed-dirt ground); biome-keyed recolour via the bind-time tone treatment; gaps stay clean hops
  (platform-local placement by construction).
- [ ] `[content]` **Site dressing follow-ups (production half).** (a) **Ruin / Lair kits** — the
  demo packs don't target them, those sites stay undressed (data-only to add); (b) the
  **biome×site material matrix** + production structures (the P5-3 art pass proper); (c)
  **density-gradient ordering** ("core in the middle") and cross-island skyline *alignment* beyond
  the shared band; (d) **prop animation** (banner sway, fire light/smoke — the demo campfire is
  unlit by owner decision). *(world + art)*
- [ ] `[arch]` **Dressing — variant-prefab reference verification.** The demo kits reference some
  pack prefabs that are nested-prefab variants; their fileIDs were hand-computed
  (source ^ instance). If any kit slot shows `None` in the inspector, re-drag the prefab — the
  spawner warns and skips broken entries at runtime. One-time editor check. *(content)*
- [x] `[content]` **Track E · E4 — World backdrop fill (demo) — SHIPPED (2026-07-06).**
  *(Brief `product-requirements/world-backdrop-fill-demo.md`; see `environment-dressing.md`
  §1.5/R20–R22 + CHANGELOG.)* Real 3D low-poly distant scatter (cliffs/mesas + rocks + cactus
  cluster behind the desert, hills + mountain + tree-clumps behind the forest), heavily hazed
  (`ToneMaterialCache.GetHazed`), deterministic (per-slot streams, streaming-safe), cheap (low
  density, colliderless), non-walkable, world-fixed (real parallax) in a depth band in front of the
  ridge rig. Realized as the contract's **third kit kind** (`BackdropKitDefinition` bound from
  `BiomeAppearanceDefinition._backdropKit`). Mountain/Cave bind none. Follow-ups: real backdrop
  meshes (P5-4), parallax/sky-anim, horizon escalation, route↔silhouette coupling — all deferred
  (`world-backdrop.md` §5).
- [ ] `[content]` **Dressing — light biome pass under site dressing.** Site platforms currently
  skip biome features entirely (one voice per place, KISS); a sparse decorative-only biome layer
  under the site kit could soften the transition. *(world)*
- [ ] `[content]` **M5 — Render-look & palette bible.** Foundational visual direction
  (`design/art/render-look.md`): **flat low-poly, no outline** (Windblown-side; supersedes the vision
  §5 Gunfire outlined-cel candidate); **warm muted base + reserved saturated gameplay accents**
  (archetype = hue, tier = glow); **balanced charming-grotesque** silhouettes with anatomy legibility.
  Because there is no outline, figure-ground + readability rely on silhouette + value + reserved
  colour. Follow-ups: concrete palette **swatches**, a **flat-shading / figure-ground shader spike**
  (tech-art), animation direction, per-archetype concept sheets (gated on the parked race roster),
  and the tier-glow / ability-telegraph **VFX language** (ties M4 combat readability).

---

## The Hub (Junkyard)

**Verified PO build brief: `product-requirements/hub-staging-and-launch.md` (2026-07-06).** Design
intent in `design/narrative/hub-junkyard.md`, `design/world/overview.md` §8/§11. Stands up the
Junkyard Hub as a real scene — the pre-run staging ground **Journey** leads to (today it drops
straight into a run) and the **death-return** reform point (Hades frame). MVP = **staging + return
only**.

- [x] `[arch]` ~~**O1 — Hub scene + start-of-run choices + death return.**~~ ✅ shipped 2026-07-06
  (`hub-staging.md`; CHANGELOG "The Hub — staging scene, start-of-run choices & death return").
  **Owner revisions over the brief (2026-07-06):** the offer is **drawn from the tasted-forms
  pool** (up to 3 cards, deterministic variety-greedy `StartingPartSelector`, shared mutation card
  panel) instead of 3 fixed authored organs; **bare launch is always allowed** (cold start = empty
  pool = bare); all three homelands re-authored to **tier 1** (entry pool) while keeping the
  tier-2 climb pool (mapper now dedupes on the (theme, tier) pair). Launch = the new-run **commit
  point** (the Journey delete moved from the menu to the Hub launch); carriers = one-shot
  `run-setup.json` / `hub-arrival.json`; `RunSaveSnapshot` v2 carries `StartingBiome`.
- [ ] `[polish]` **O1 follow-ups.** Relocate `ArenaTastedCatalogReader` to a shared tasted-catalog
  home (Hub + Arena both consume it); a short **launch-line linger/fade** (the launch voice line
  is currently unseen — the scene loads immediately); a **death-return vignette/fade** (the cut to
  the Hub is hard); the brief's optional **match/cross soft hint** on the portals;
  **Forest-at-tier-2** as a pure-data climb-pool tuning option (after a Desert/Mountain start,
  stretch 1 is forced to the other tier-2 theme); **movement lock while the card panel is open**
  (`IMovementInputLock` hookup — today the hero can walk with the cards up). *(scoped)*
- [x] `[arch]` ~~**O1 corrective — the Hub is just another biome** (brief
  `hub-as-a-normal-platform.md`)~~ ✅ shipped 2026-07-06: the parallel Hub camera/locomotion/
  platform implementation removed — the Area's Cinemachine rig + `CharacterLocomotionInstaller`
  in the scene, the island built through `PlatformView` (new standalone `Initialize` overload) +
  the dressing chain as the first-class **`LevelTheme.Hub`** biome (authored
  `BiomeAppearance_Hub` + hub feature kit, excluded from the rotation by data), placement in the
  shared camera's screen basis with unblocked-cell snapping.
- [ ] `[art]` **Author the Hub biome's real junkyard look.** `Demo_Ground_Hub.mat` is a flat
  muted tint and `Demo_BiomeFeatureKit_Hub` reuses the Desert demo rocks — swap in a real
  junkyard ground texture + scrap-pile decor prefabs (data-only, the kit contract); the portals
  are flat tinted discs and the keeper is the placeholder humanoid — the Hub half of the Track M
  art pass. *(designer)*
- [ ] `[arch]` **P1-10 absorbs the Hub voice.** When the full bark channel lands, migrate
  `HubVoiceLinesConfig`'s moment/race pools onto it as the hub-presence slots (the SO was shaped
  for this). *(with P1-10)*
- [ ] `[content]` **Deferred (out of the O1 brief).** Hub **meta-progression** (persistent
  investments/upgrades/currency), the **recurring hub cast** beyond the cauldron's voice, the dig
  **beyond the starting offer** (raw-artifact digs · larger offers · mixed offers · cross-run dig
  **meta-bias**), **starting biomes beyond the three homelands** (Cave/4th race), a full **run-choice
  map**, and Hub **art production** + voice **audio**. *(design + art)*

---

## Character System

Known points (from `character-system.md`):
- [ ] `[perf]` Runtime skinned-mesh combining is not implemented (design keeps it possible — the
  controller owns the live per-slot renderers a combiner would consume).
- [ ] `[debt]` `PartSwapExecutor`, `SocketMounter`, and the factory are verified only manually (demo
  bootstrap + preview window); consider play-mode test coverage.

New work:
- [x] `[arch]` **P2-1 — Independent body-plans + skeleton-swap runtime.** ✅ Shipped 2026-07-04
  (`character-system.md` R19–R24, supersedes the old R3/R4/R10 single-superset reading; brief
  `product-requirements/body-plan-skeleton-swap.md`). Frame-changing parts (`GovernsBodyPlan` +
  priority) re-form the body on their own skeleton; structural cross-frame fit; confirm-and-shed
  to the new part inventory; losing governors carried dormant; serpent + spider placeholder
  frames prove both shed directions and the priority test. See CHANGELOG. Follow-ups below.
- [ ] `[arch]` **P2-1 follow-ups (body plans).**
  (a) **Re-install-from-inventory flow** + an explicit `RemovePart`/unequip API — today shed parts
  are visible in the stash but cannot be re-equipped, and a governor leaves only by slot
  replacement; until it ships, going back to the base frame leaves a legless biped.
  (b) **DormantInstall UX feedback** — a losing frame-changer installs silently (log only); needs
  a toast/voice line.
  (c) **Dormant-part semantics review** — dormant parts contribute no abilities/race markers by
  design; revisit once designers author real frame-changers (is a dormant serpent spine truly
  inert?).
  (d) **Per-frame host fit** — the hero's `CharacterController` capsule and the visual's local TRS
  offsets are frame-invariant (a serpent hovers at biped pelvis height); needs per-skeleton
  placement/collider data when production frames land (P5-6).
  (e) ~~Frame/stash persistence~~ — **done (P2-2, 2026-07-05)**: the run save carries the governing
  skeleton + equipped/dormant slot→part map (`HeroBodyRestorer`) and the shed-part stash; see
  `save-persistence.md`.
- [ ] `[content]` **M5 — Production body-part assets.** Replace placeholder box parts with
  production-ready meshes/materials across all slots. Includes the per-race **signature marker parts**
  (Ibex horns/hooves/coat · Lizard scales/frill/tail · Fox tail/ears/fur) named in
  `product-requirements/race-roster-and-passport.md` + `design/narrative/races.md`; each authored part
  also carries its **race tag** (the passport input).
- [ ] `[arch]` **M5 — Part/animation integration workflow.** A documented, repeatable workflow to
  add a new body part or animation: socket placement/adjustment, bone-name baking
  (`PartDefinition._boneNames` / "Bake Bone Names From Prefab"), animation hookup, and validation.

---

## Mutation Subsystem

The M1 core loop (see `mutation-subsystem.md` for the implemented data surface):
- [x] `[content]` **M1 — Artifact archetype attributes.** Extend `ArtifactDefinition` with a
  creature-archetype weight map (e.g. Reptile, Insect, Aquatic, Mammal, Avian). Each eaten artifact
  contributes to one or more archetype axes. Archetypes are data-driven (an authorable set), not
  hard-coded. *(Done — see CHANGELOG; `mutation-subsystem.md`.)*
- [x] `[arch]` **M1 — Per-stage mutation tally.** Accumulate archetype weights from artifacts fed
  during the current mutation stage; the tally decides this stage's mutation options and resets when
  the player mutates to the next stage. Body-part swaps from prior stages persist (the character
  still evolves across the run). *(Done — `IMutationTally`/`MutationTally`, now filled by the feeding
  UI; see CHANGELOG; `mutation-subsystem.md`. `Reset()` exists but is not auto-triggered yet — the
  stage-up choice below drives it.)*
- [x] `[arch]` **M1 — Digestion progress + ready-to-mutate signal.** Per-stage `IDigestionProgress`
  counts artifacts fed vs. the authored `MutationConfig.DigestionThreshold` and exposes
  `IsReadyToMutate`. *(Done via the feeding UI; now consumed by the stage-up choice below.)*
- [x] `[arch]` **M1 — Stage-up mutation choice.** When `IDigestionProgress.IsReadyToMutate`, offer
  up to `MutationConfig.MaxMutationOptions` options derived from the dominant archetype(s); the player
  picks one, which swaps a body part via `IModularCharacter.SwapPart` and resets the tally **and**
  digestion progress for the new stage. *(Done — `MutationChoicePresenter` + `MutationOptionBuilder`;
  see CHANGELOG; `mutation-subsystem.md`. **Ability update on swap is deferred** — see the Ability
  Subsystem M1 item below.)*
- [x] `[content]` **M1 — Archetype → body-part-set mapping.** *(Done in M1, then **superseded and
  removed** by the M2 part-driven-affinity item below. `ArchetypePartSetDefinition` and the part sets
  are deleted; archetype affinity now lives on `PartDefinition` and selection is scored. See CHANGELOG.)*
- [x] `[arch]` **M1 — Mutation swap updates abilities.** *(Done — combat rebuilds the unit's active +
  passive ability set from the live equipped parts at combat start (`PartAbilityResolver`,
  `CharacterCombatInitializer`), so a stage-up swap changes the next combat's abilities without the
  swap path pushing anything. See CHANGELOG; `ability-subsystem.md` §2.6.)*
- [x] `[arch]` Exclude the character's *starting* parts from stage-1 options. *(Done — the adapter's
  equipped-part query reads the live `IModularCharacter.EquippedParts` snapshot, so every equipped
  part is excluded; the redundant swap cache was removed. See CHANGELOG; `mutation-subsystem.md` §2.4.)*

New work:
> **Superseded source of mutations (2026-07-02) — cutover complete.** The feed→tally→stage-up flow
> below **was replaced and deleted**; mutations come from **Socketed Blanks** (see
> `## Crafting & Mutation`, brief `product-requirements/crafting-mutation-socketed-blanks.md`):
> socketing artifacts into a Part-Blank and unsealing a **variant menu**. The checked M1/M2 items
> below are history of the removed loop (their substance lives in the CHANGELOG). The M4
> ability-aware panels item below becomes the **mutation cards** shown on that unseal menu.
- [x] `[arch]` **M2 — Part-driven archetype affinity + scored mutation selection.** *(Done — archetype
  affinity, rarity (`MutationRarity`), and choice icon now live on `PartDefinition`; `MutationPartCatalog`
  builds `MutationCandidatePart`s from the part catalog; `MutationOptionBuilder` scores all candidates
  against the feed tally — `(affinity·tally) × (1 + RarityWeight·tier·unlock)` — and takes the top-N.
  `ArchetypePartSetDefinition` + the option catalog/mapper/provider are removed; balance is tuned via
  `MutationConfig.RarityWeight` / `RarityUnlockPointsPerTier`. Hosting decision: on `PartDefinition`
  (single-asset authoring) — the layering trade-off is recorded as a known limitation in
  `mutation-subsystem.md` §6 rather than being eliminated. See CHANGELOG.)*
- [x] `[arch]` **Mutation choice cards (the unseal variant menu).** *(Done — verified PO brief
  `product-requirements/mutation-choice-cards.md` shipped in full: card front (part picture +
  active/passive ability icons, resolved via the combat `IPartAbilityResolver` for combat parity),
  corner flip → replaced part / "nothing replaced", ability-icon hover tooltip, part-picture hover
  → mini 3D hero-model popover (`MutationModelPreviewRig`, RenderTexture at a far world offset,
  tunables in `MutationConfig.Preview`), card grammar colour=belonging / glow-brightness=tier
  (placeholder glow treatment — VFX polish below), and a two-step select→confirm commit. Prefabs
  hand-authored (`MutationCard`/`MutationAbilityIcon`/reworked `MutationChoicePanel`); the
  `MutationChoiceUISetup` editor generator is deleted. See CHANGELOG; `mutation-subsystem.md`
  §2.4.)* *(mutation + crafting UI)*

---

## Character Locomotion

Known limitations (from `character-locomotion.md` §6):
- [ ] `[content]` Run clip is a procedural placeholder; no walk tier (blend is idle↔run only).
- [ ] `[arch]` `Speed` has no acceleration smoothing — it tracks input instantly; add damping if the
  blend looks abrupt.
- [ ] `[arch]` Velocity reflects movement intent (`moveDir * moveSpeed`), so the character runs in
  place when pushing into a wall; consider deriving from actual displacement.
- [ ] `[arch]` Idle does not resync the solver's yaw from forced facing, so a turn-to-camera pose is
  not preserved as the new heading once movement resumes.
- [ ] `[debt]` The Hero is composed by an editor menu (`Place Moving Hero In Scene`), not shipped as a prefab.

---

## Ability Subsystem

- [x] `[arch]` **M1 — Abilities granted by body parts.** *(Done — `PartDefinition._activeAbilities` /
  `_passiveAbilities`; `PartAbilityResolver` composes the combat ability set from the equipped parts;
  `CharacterCombatInitializer` consumes it (HeroDefinition is now a fallback only). See CHANGELOG;
  `ability-subsystem.md` R23–R26, §2.6.)*
- [x] `[arch]` **M1 — Passive (always-on) abilities.** *(Done — `PassiveAbilityDefinition` SO applies a
  Buff/Debuff `StatusEffectDefinition` as a standing modifier for the whole combat (infinite duration
  via `StatusEffectDurations.Tick`); `DamageSystem.CalculateFinalDamage` consumes Buff/Debuff modifiers
  for outgoing damage. Gives the `Ability` reward type a home. See CHANGELOG.)*
- [ ] `[debt]` **Part ability/mutation data couples CharacterSystem → Combat (and hosts Mutation
  concepts).** `PartDefinition` (`CharacterSystem.Data`) references `Combat.Data.Definitions` ability
  SOs and now also carries the Mutation affinity/rarity/icon, breaking the inward-only layering rule
  (CLAUDE.md §2). The M2 part-driven-affinity item revisited this and **kept the data on
  `PartDefinition`** by user decision (single-asset authoring over strict layering); affinity/rarity
  add no *type* dependency on Mutation (id strings + plain enum). A future cleanup could move it to a
  Mutation-layer companion SO keyed by part id.
- [x] `[arch]` **Passive modifiers beyond outgoing damage (P3-13).** *(Done 2026-07-07 with
  Track S · S3 — the modifier model is a flat signed `Magnitude` × `StatTarget`
  (OutgoingDamage / IncomingDamage), consumed two-sided by `DamageSystem.CalculateFinalDamage`;
  healing-over-time is the HoT status kind (`Status_Regen` as a passive = permanent regen), not a
  stat target. See CHANGELOG; `combat-status-effects.md` R10/R26.)*
- [ ] `[arch]` **Max-HP stat target is not wired.** `StatTarget` covers outgoing/incoming damage
  only; a passive that raises max HP has no consumption point. (Track S residue.)
- [ ] _seed remaining items from `ability-subsystem.md` "Known limitations" on next pass._

## Combat Status Effects (Track S — shipped 2026-07-07; residues)

**S1+S2+S3 shipped 2026-07-07** (new system doc `combat-status-effects.md`; consumed brief
`product-requirements/combat-status-effects.md`); ⚠ **awaiting the owner's gameplay pass** (on-unit
row / card badge / popover feel). Open residues:

- [ ] `[arch]` **Ghost preview ignores statuses.** `AbilityOutcomeCalculator` predicts damage only —
  the one-shot/hover ghost does not show "will apply Burn (2)". Extend the outcome model + ghost
  labels.
- [ ] `[arch]` **Self-buffs are not expressible.** Ability areas exclude the caster's own cell, so
  `Ability_WarCry` (Empowered) buffs adjacent units, never the caster. Needs an "includes self"
  shape/targeting flag on `AbilityDefinition`.
- [ ] `[feature]` **Cleanse / immunity thin flag (brief FR11).** Expiry is the only removal today;
  the authored-only-where-needed cleanse ability / immunity refusal flag is not built.
- [ ] `[content]` **Status glyphs are placeholder art** (code-drawn white shapes at
  `Resources/Combat/StatusEffects/Glyphs/`). Designer swap = overwrite the PNG bodies; GUIDs stable.
- [ ] `[art]` **Status VFX / tick animation** — application flash, per-tick cue, expiry puff
  (render-look / VFX-language pass; ties P5-10).
- [ ] `[feature]` **AI does not reason about statuses** (avoiding a DoT, valuing a stun) — Track K
  smarter-AI scope (P2-4).
- [ ] `[design]` **S4 status interactions / combos** (wet→fire…) — parked by the brief; statuses
  stay flat.
- [ ] `[design]` **S5 elemental damage-type system** — parked planning placeholder (owner
  2026-07-07); "Physical" = the neutral `glyph_untyped` mark, no element/resistance layer.
- [ ] `[debt]` **TurnStart / OnApply / OnRemove / OnThreshold trigger flags are unexercised** — no
  starter content uses them; only TurnEnd is the canonical tick. Either consume or prune on a later
  pass.

## Combat Experience

**Track C shipped (2026-07-03)** — the three verified briefs (`combat-hero-facing.md`,
`combat-turn-intent-phase.md`, `combat-ability-ghost-telegraph.md`) were built as one combat pass;
see the new system doc **`combat-round-and-telegraph.md`** + CHANGELOG.
- [x] `[arch]` **M4 — Turn structure: enemy intent phase (Plan → Act → Resolve).** *(Done —
  `RoundPhase` + `EnemyIntent` on `CombatState`; `EnemyIntentPlanner` decides every enemy up front
  in UnitId order with seeded decision makers (`combat-ai:{enemyId}`, same seed → same plans);
  `EnemyIntentResolver` fires committed cells/facing verbatim — dodged blows whiff, blocked moves
  fizzle, never re-targets; player-then-enemies order; `EnemyRoundController` paces the resolve.
  Also fixed the pre-existing gap where AI-scheduled abilities never fired. See CHANGELOG;
  `combat-round-and-telegraph.md` R1–R7.)*
- [x] `[arch]` **M4 — Hero/unit facing drives ability direction (global facing).** *(Done —
  `Unit.FacingDirection` (`HexDirection`) is the single directional state; aim input rotates the
  unit via free unlimited `ChangeDirectionAction` (validator free-action gate); execution reads the
  live facing so turning re-points the whole volley; `AbilityTarget`/`RetargetAbilityAction`
  deleted; AI turn-and-schedules via `FacingToSet`; `UnitFacingRotator` for model legibility.
  See CHANGELOG; `ability-subsystem.md` R4–R8, R11.)*
- [x] `[arch]` **M4 — Ability-queue display (icons above the unit).** *(Done — per-unit overhead
  icon rows for the player's queue in order AND every enemy's committed intent from the Plan-phase
  reveal (`»` glyph for moves); `UnitPlanIconsPresenter` + `UnitOverheadIconsView` +
  `AbilityDefinitionCatalog` + `CombatUnitViewRegistry`. See CHANGELOG;
  `combat-round-and-telegraph.md` R9.)*
- [x] `[arch]` **M4 — Ability ghost telegraph.** *(Done — one-shot translucent full-outcome ghost
  on queue-submit + hover-to-replay on any plan icon (enemy intents included), computed by the
  pure `AbilityOutcomeCalculator` whose predictions provably match execution; displacement entered
  the data model as `_pushDistance` (push away from caster) with `DisplacementResolver` shared by
  execution and preview; the cell highlight stays as the "where" layer. Caster ghost is a static
  clone — `AnimationTrigger` playback is the tech-art follow-up below. See CHANGELOG;
  `combat-round-and-telegraph.md` R10–R14.)*
- [ ] `[arch]` **M4 — Smarter ability-using enemy AI.** Improve `TacticalAI` to choose and aim
  abilities well (target selection, area value, direction), beyond the current scoring — today it
  scores all six facings equally, so committed facings break ties toward the first direction.

Follow-ups from Track C (doc §6):
- [ ] `[arch]` **More displacement kinds: pull / dash / hook.** Push (away from caster, Line only)
  is the only displacement; pull-toward, caster dashes, and hooks need their own data semantics +
  executor/preview support. *(combat)*
- [ ] `[arch]` **Ring-shape push semantics.** `_pushDistance` on a Ring ability is ignored (no
  line direction); define radial push if a design wants it. *(combat)*
- [x] `[content]` **Ghost/ability animation** — ✅ **D3 (2026-07-05)** ships a code **placeholder**
  cell-sweep (animated ghost + live playback, player & enemy). **Still open (art):** production clips/VFX
  and consuming the ability's `AnimationTrigger` for a real per-ability skeletal animation — ties the
  render-look bible. *(combat + tech-art — Track M / render-look)*
- [ ] `[arch]` **Queue-simulation preview.** Ghosts run against the current board (brief-accepted);
  a "dry-run the queue then preview" upgrade would make chained previews exact. *(combat)*
- [ ] `[arch]` **Speed-based resolution order + multi-round lead policy.** The **initiator-first**
  opening round shipped with D2 (2026-07-05); still deferred: a per-unit **speed** stat letting a fast
  enemy leap ahead of the initiator, and the **multi-round** lead policy (alternate/persist/re-roll).
  *(combat — Track K P3-12)*
- [ ] `[content]` **Move-intent presentation.** ✅ **D3 (2026-07-05)** replaced the `»` glyph with a
  board **direction arrow** toward the committed destination. **Still open:** a full step-by-step
  **path** line (see `combat-move-destination-telegraph.md`). *(combat)*
- [ ] `[debt]` **`TelegraphStyle` constants → config SO.** Icon sizes/heights and ghost fade
  timings are code constants; promote to an authorable asset on the next combat-UI pass. *(combat)*

Track D — Bandit Camp & Combat Legibility II (verified PO briefs, 2026-07-05):
- [x] `[arch]` **Track D · Initiative, turn-order queue & facing input** (brief
  `product-requirements/combat-initiative-and-turn-queue.md`) — ✅ **shipped 2026-07-05 (D2)**.
  **Initiator acts first** — `CombatInitiator` captured at the engagement sites (player Attack vs.
  dialogue-turned/ambush enemy) and threaded to `CombatController.StartRound`, which resolves enemy
  intents before the player's Act phase for an enemy-led opening round (`RoundLeadPolicy`, round 1 only).
  A code-built **horizontal top-right turn-order strip** (PvE only; Arena keeps `IArenaResolutionOrder`)
  reads who acts and in what order, leader-first, updating as the round runs. **Aim/fire input:**
  **hold Enter** turns the hero toward the **mouse cursor** (re-points the whole queued volley), **release
  Enter executes** along the final facing, **right-click aborts**. Presentation + turn-flow only;
  abilities/determinism unchanged. See `combat-round-and-telegraph.md` R4/R4a/R4b + `ability-subsystem.md`
  R8 + CHANGELOG. *(combat)*
- [x] `[content]` **Track D · Ability animation, animated ghost & enemy-action read** (brief
  `product-requirements/combat-ability-animation.md`) — ✅ **shipped 2026-07-05 (D3)**. A code-authored
  **placeholder cell-sweep** spanning each ability's affected area (`AbilityCellFlash`/`AbilityAreaSweep`,
  shape-driven), played **translucent** as the now-animated ghost (queue-submit / icon-hover) and **opaque**
  on live execution via a shared executor `AbilityFiredCue` sink — **player queue + the enemy's paced resolve**,
  so an enemy action animates within its beat (readable, not instant). An enemy's committed **move** shows a
  board **direction arrow** (`EnemyIntentTelegraphView`) replacing the `»` glyph. The **readiness cue** marks an
  armed enemy: **restless plan icons** (jitter/pulse) + a placeholder **wind-up pose** (transform lean/scale/bob;
  enemies only, uniform, cleared on resolve). Presentation only; outcomes/cells/turn-order unchanged. Production
  art / per-ability choreography / queue-simulation / full path line stay deferred. See
  `combat-round-and-telegraph.md` R15–R18 + CHANGELOG. *(combat + tech-art)*
- [ ] `[arch]` **Track D · Clear dead units from the board on death** (D5; task description is enough, no brief).
  Today a killed unit stays in its cell until the **whole** fight ends — it still reads as occupying the cell
  (`CombatState.GetUnitAt` matches any unit at a position regardless of `IsAlive`, so movement's "skip if
  occupied" and targeting treat a corpse as a blocker) and clutters the multi-enemy board. Remove a unit the
  moment it dies: **free its cell** (movement/targeting) and drop the model, so the "fast chess" stays legible
  (Pillar 4). **Placeholder = instant removal now; a death animation comes later** (rides D3's code-authored
  placeholder approach). The **corpse-loot / "you are what you eat" drop must still fire** on death (the
  separate combat/mutation channel — removal must not skip it). *(combat)*

Track D play-test follow-ups on the shipped D2/D3 (2026-07-05; task description is enough, no brief):
- [ ] `[arch]` **D6 — Combat unit uniformity: one code path for all units.** A bug cluster with a shared
  likely root — **divergent per-origin combat code** (hero vs boss vs seeded crew vs ambient enemy).
  **Symptoms:** the D3 board **move-direction arrow does not render for the bandit crew and some single
  enemies**; the **crew spawns stacked in a single cell** (violates one-unit-per-cell); the **hero can
  spawn onto an already-occupied cell** at battle start. **Requirement:** every unit resolves **spawn
  placement, facing, and intent-telegraph through one uniform path** — no two units ever share a cell,
  and **every unit orients toward its next move/action** (uniform facing, not just some). *Diagnose and
  unify the paths; the arrow/stacking/facing defects are symptoms.* Hardens `combat-round-and-telegraph.md`
  R4/R15–R18 + the one-unit-per-cell invariant. *(combat)*
- [ ] `[arch]` **D7 — Aim input redesign: short-press fires, long-press aims.** Owner change to D2's
  input, **superseding** hold-to-aim/release-to-execute (`combat-round-and-telegraph.md` R4b). **New
  scheme:** a **short Enter tap** discharges the queued volley immediately **along the hero's current
  facing** (no aim step); **holding Enter** enters **aim/orientation mode** (turn the hero toward the
  cursor, re-pointing the whole queue + ghosts); **releasing Enter** discharges along the aimed facing.
  **Right-click still cancels.** Press-duration threshold tunable. Deterministic; no ability change.
  *(combat)*
- [ ] `[content]` **D8 — Combat presentation bug fixes** (two independent read-the-board defects).
  **(a) Smeared enemy move animation** — an enemy's move reads as a **teleport** (the figure vanishes in
  the source cell and pops into the neighbour, recoiling slightly) instead of gliding; replace with a
  **smooth cell-to-cell move** (rides D3's paced enemy resolve, R15–R18; if the crew moves via a divergent
  path this may share D6's root). **(b) Planning-outline not reset** — the plan-phase **cell outline
  sometimes fails to reset to the default (black)** after planning ends, leaving cells **stuck yellow for
  the rest of the fight**; the highlight must always clear back to default when the plan phase closes.
  *(combat)*

## Arena Mode (Multiplayer)

**Verified PO build brief: `product-requirements/arena-mode-mvp.md`.** Design intent in
`design/arena-mode.md`. A **secondary combat mode** on Pillar 4 only (no narrative/mutation/crafting):
a main menu with two modes and a networked free-for-all fight on one platform, reusing the combat
engine and the early Netcode-for-GameObjects scaffold already under
`Scripts/Combat/Networking/` (`CombatNetworkManager`, `NetworkCombatStateSync`, `NetworkActionSender`,
`ActionSerializer`; the combat domain already models a `Players` list, not player-vs-AI).

- [x] `[arch]` **Main menu + mode flow.** *(Done — Phase 1: the game boots into the hand-authored
  `MainMenu.unity` (build index 0); **Journey** loads the unchanged `Area` scene, **Arena** loads
  the Arena scene (added with the session phase). New `Core.SceneFlow`
  (`ISceneLoader`/`SceneLoader`/`SceneNames` — the project's first runtime scene-switch seam),
  `MainMenuPresenter` MVP + `MainMenuInstaller`; stale `Demo.unity` build entry removed. See
  CHANGELOG; `arena-mode.md`.)* *(menu + area)*
- [x] `[arch]` **Arena FFA session.** *(Done — Phase 2 + Phase 3: one arena platform generated
  from the match seed (`ArenaPlatformBuilder` on the Combat shape profile + battlefield minimum),
  a **default hero** per player at deterministic distinct spawn cells (`ArenaHeroSpawner` +
  `ArenaSpawnPlanner`, roster-assigned unit ids). **2–4 players, host / join by `ip[:port]`** on
  NGO: connect panel → host approval (max players, closed once started) → Start Match broadcasts
  seed + seat roster; every client builds the identical world locally, lockstep rides NGO custom
  named messages behind the unchanged `IArenaTransport` (no NetworkObjects, no scene sync).
  Offline dummies remain the `_offlineMode` dev fallback. No lobby/matchmaking/ranking/reconnect,
  per the brief. See CHANGELOG; `arena-mode.md` §2.3.)* *(networking + combat)*
- [x] `[arch]` **Round = hidden simultaneous commit → simultaneous resolve.** *(Done — Phase 2:
  `ArenaCombatController` behind the unchanged `ICombatController` seam (full PvE presentation
  reuse); planning is hidden by construction (commits exist only on their owner's machine until
  lock; Move/ExecuteQueue intercepted, never executed locally); `ArenaMatchHost` gathers all alive
  players and broadcasts the canonical bundle; every client normalizes + resolves through the
  PvE `EnemyIntentResolver` — whiffs/fizzles/skip-dead verbatim. **Deterministic**: rotating
  initiative behind the replaceable `IArenaResolutionOrder` (PO decision), all seeds from the
  match seed, per-round `ArenaStateHash` lockstep digest, determinism-replay test green. See
  CHANGELOG; `arena-mode.md` §2.3–§2.5.)* *(combat)*
- [x] `[arch]` **Win = last hero standing.** *(Done — `LastHeroStandingWinCondition` (armed after
  the full roster spawns): one player with a living unit wins, zero is a draw; remaining intents
  are not resolved after match end. Phase 4 added the defeated-player **spectate** presentation,
  the winner/draw banner + **Leave → menu**, and disconnect/desync surfacing
  (`ArenaMatchHudPresenter`). See CHANGELOG; `arena-mode.md` §2.2, §2.7.)* *(combat)*

**Arena MVP complete (2026-07-03)** — all four phases shipped (main menu, symmetric round loop,
NGO host/join, match HUD/spectate). 39 arena edit-mode tests green; PvE untouched (54 green).
The items below are post-MVP polish/hardening.
- [ ] `[content]` **Deferred (out of the MVP brief).** Deck of run-snapshot heroes + character
  selection (a separate design pass — conflicts with the Hades death/reform frame, see
  `design/arena-mode.md`); PvP-specific balance / whether Arena bodies are PvE snapshots or a separate
  roster; matchmaking/lobby/ranking/reconnect/late-join; a dedicated Hub scene for the Journey branch;
  FFA variants beyond last-standing (rounds, teams, scoring); spectator polish. *(design + arch)*

Character layer & draft (post-MVP):
- [x] `[content]` **P4-5 — parts draft + tasted-forms catalog (model).** *(Done 2026-07-06 —
  consumed brief `product-requirements/arena-part-draft-and-catalog.md`: deterministic
  host-composed board (authored floor + seeded sample of the participants' catalog union),
  snake order with denial, Meta-fact catalog `world.<partId>.arena_tasted` written passively in
  Journey, lockstep draft over four new named messages, host-only deadlines/auto-pick. See
  CHANGELOG; `arena-mode.md` §2.9.)* *(combat + persistence)*
- [x] `[ui]` **G4 — parts-draft screen (UI & presentation).** *(Done 2026-07-06 — consumed brief
  `product-requirements/arena-draft-ui.md`: slot-grouped 3D board + live-assembling local
  monster on one stage RenderTexture, whose-pick/snake-order/timer reads, opponents as name +
  parts text, remote-pick flights, part-info popover with legality-gated Draft + visible
  rejection reasons, the "your monster" beat, and the **shared ability-preview popover**
  (`ability-preview-popover.md`) also wired into the mutation cards. See CHANGELOG;
  `arena-mode.md` §2.9.)* *(ui)*

Parts-draft follow-ups (2026-07-06):
- [ ] `[arch]` **Draft-loadout hash checkpoint.** A diverged draft replica currently surfaces
  only as a local `REPLICA DIVERGENCE` log + round 1's `ArenaStateHash` mismatch; piggyback a
  loadout hash on draft completion so the host catches it before the fight. *(combat)*
- [ ] `[ui]` **Real part thumbnails on the draft board.** The board shows bind-pose meshes
  bounds-normalised (odd silhouettes possible) with an icon-sprite fallback; production
  thumbnails / posed captures are the art-quality pass. *(ui + art)*
- [ ] `[debt]` **Relocate `PointerHoverRelay` out of `Mutation.View`** into a shared UI
  namespace — the arena part-info popover now consumes it cross-system. *(debt)*
- [ ] `[debt]` **Meta-file trailing-newline hygiene sweep.** ~900 `.meta` files lack a trailing
  newline; Unity tolerates most but hard-failed 8 on a clean import (fixed 2026-07-06, see
  CHANGELOG) — sweep the rest in a dedicated no-op commit so clean checkouts can never mint
  fresh GUIDs. *(debt)*
- [ ] `[content]` **Widen the "tasted" trigger?** Catalog records carried parts
  (install-at-minimum per the brief); whether merely *seeing* a part in the world qualifies is
  a tuning decision once the board wants more variety. *(design)*

Deferred implementation follow-ups (2026-07-03 implementation plan):
- [ ] `[debt]` **Rename `EnemyIntent` → `CommittedIntent` (+ `RoundPhase` members → Plan/Act/Resolve).**
  The committed-intent machinery is player-agnostic and Arena reuses it for every unit; the names are
  PvE-shaped. Wide mechanical rename deferred (no compiler in env). *(combat)*
- [ ] `[arch]` **Seeded-shuffle resolution-order alternative.** Arena ships rotating initiative behind
  `IArenaResolutionOrder`; a per-round seeded-shuffle strategy is the drop-in alternative if rotation
  feels exploitable. *(combat)*
- [ ] `[arch]` **Per-step simultaneous damage batching.** Mutual blows resolve sequentially (a lethal
  earlier blow prevents the return — the PvE skip-dead semantic); true simultaneous damage application
  is a deliberate MVP non-goal. *(combat)*
- [ ] `[arch]` **Host-side commit validation (anti-cheat).** MVP trusts peers: commits are relayed, not
  re-validated against the canonical state. *(networking)*
- [ ] `[arch]` **Arena camera pass.** Fixed combat camera in the Arena scene; framing/feel at arena
  scale pending. *(camera)*

## Inventory Subsystem

- [x] `[arch]` **M1 — Feeding / digestion UI.** A feeding mode (toggled in the open cauldron) where
  pot artifacts are selected into a feeding tray with a cumulative archetype-attribute readout, the
  current digestion progress, and the dominant archetype(s) this stage; the Feed button maps each
  artifact via `ArtifactArchetypeMapper.ToProfile` into `IMutationTally.Add` and advances
  `IDigestionProgress`. *(Done — see CHANGELOG.)*
  **Superseded and removed (2026-07-02):** the Socketed Blanks model replaced feeding with
  operating-table socketing and the feeding UI was deleted — see the migration items in
  `## Crafting & Mutation` and the CHANGELOG `Removed` entry.

**The Cauldron View — Track F (presentation over the shipped inventory; 2026-07-06):**
- [x] `[arch]` **F1 — spatial spine + bubble = submerged rule + ribbon re-home.** *(Done — brief
  `cauldron-view-spatial-spine.md`: crafting-top · brew-centre · medallion-ribbon-bottom stack;
  bubble shown iff in the liquid (staged/result/socketed = bare); the rack moved to the ribbon under
  the pot. `inventory-subsystem.md` R27–R29.)*
- [x] `[arch]` **F2 — liquid you can feel + fullness by fill level.** *(Done — brief
  `cauldron-liquid-and-fullness.md`: translucent surface + cut-away curtain + bright waterline
  (`LiquidBandMeshBuilder`, `M_PotLiquid`/`M_PotWaterline`); fill height = artifact count
  (`LiquidFillCalculator`), snapshotted on open, always above the topmost bubble; fixed-size mesh.
  `inventory-subsystem.md` R30.)*
- [x] `[arch]` **F3 — stable brew layout + event physics + continuous result flow.** *(Done — brief
  `cauldron-brew-layout-and-physics.md`: stable bottom-up spots (`BrewSpotLatticeBuilder`/
  `BrewLayoutModel`), drop-in splash + settle, and the hovering result auto-commits into the brew on
  the next combine (retires R19's rejection). **FR4 revision 2026-07-07 (implemented):** a removal
  settles the gap downward **column-scoped** — the lattice forms vertical columns and only the column
  resting above the removed bubble falls (`SettleColumn`); everything beside and below stays put,
  replacing the brief's original "a removal never moves the others". `inventory-subsystem.md`
  R10/R19/R31.)*
- [x] `[content]` **F5 — stomach-interior backdrop seam (P5-5).** *(Done — screen-space
  `StomachBackdrop` quad + muted `M_StomachBackdrop`; art is a designer asset swapped by repointing
  the material/texture. `inventory-subsystem.md` R32.)*
- [ ] `[content]` **F6 — prettier cauldron mesh (art).** A more attractive pot silhouette/charm,
  fixed-size (F2 assumes whatever mesh is current) — the one net-new authored asset in the track;
  the code seam (`CauldronView` geometry + `ICauldronGeometry`) is in place. *(designer art)*
- [ ] `[content]` **Cauldron View tuning residue.** Play-mode passes on the bowl scale/capacity,
  splash/settle spring feel (`InventoryConfig` Brew Event Physics), liquid alpha/waterline read, and
  the stomach backdrop art. *(inventory)*
- [ ] _seed remaining items from `inventory-subsystem.md` "Known limitations" on next pass._

---

## Crafting & Mutation — Socketed Blanks

**Verified PO build brief: `product-requirements/crafting-mutation-socketed-blanks.md`.** Design
intent in `design/crafting/model.md` (rewritten 2026-07-02). This **supersedes** the earlier
trait-tally / feeding coupling (`design/needs-code.md` 2026-06-21) — see the migration items below.
The model: **Part-Blanks** carry form/species + sockets; **artifacts** carry function only (no
species tag) with a raw→crafted quality gradient; the **cauldron** fuses artifacts and a separate
**operating table** sockets artifacts into a blank and **unseals** it into a mutation. Feeding is cut.

- [x] `[arch]` **Artifact trait model (incl. tier).** *(Done — `ArtifactDefinition` carries
  Substance/Property `TraitDefinition` refs + an int tier; the trait vocabulary is an authorable SO
  set (`Resources/Artifacts/Traits/`, 10 shipped); startup validation via `ArtifactContentValidator`.
  Archetype weights remain temporarily for the legacy feeding path and go with the migration below.
  See CHANGELOG; `inventory-subsystem.md` §4.)*
- [x] `[arch]` **Two-tier emergent artifact fusion (no failure).** *(Done — `FusionResolver`:
  signature `RecipeBook` match first, else `EmergentFusionCalculator` (trait union → authored
  `FusionRuleDefinition`s in ordinal rule-id order → amplify/tier) + `ArtifactByTraitSelector`
  deterministically picks the best authored artifact (inputs excluded); the craft fail path is
  deleted. See CHANGELOG; `inventory-subsystem.md` R16–R17, §2.2.)*
- [ ] `[content]` **Signature-vs-emergent craft presentation.** `OnCraftSucceeded` reports
  `isSignature` but the puff/pop-in is identical for both; give signature results a distinct effect
  so authored highlights read as special. *(inventory)*
- [ ] `[content]` **Emergent-fusion authoring density.** With only 7 authored artifacts the
  nearest-match emergent output can read semantically odd (unrelated inputs snap to the least-bad
  artifact); author more artifacts across the trait space so emergent results stay legible.
  *(inventory)*
- [x] `[arch]` **Part-Blank item type + operating table (the new mutation source).** *(Done —
  domain: `PartBlankDefinition` SO + `PartBlankCatalog`; `BlankRack`; `SocketingModel`
  (socket/unsocket over the inventory; ripen threshold = all sockets filled; filling the last
  socket raises `OnBlankReady` and commits); `BlankVariantBuilder` (variant menu = authored
  `PartDefinition`s of the blank's slot, trait-affinity-scored against the socketed profile run
  through the cauldron's fusion grammar — slot interaction included; deterministic). UI: the rack
  left of the cauldron on the one inventory screen (`BlankRackView`/`BlankRackPresenter`,
  `InventoryStage.prefab` `BlankRackArea`), drag-to-socket via `StageDragRouter`, variant cards on
  the reused choice panel via `MutationVariantPresenter`, install via `SwapPart` +
  consume-on-pick. See CHANGELOG; `mutation-subsystem.md` §2.6–§2.8.)*
  *(crafting + mutation + inventory)*
- [x] `[content]` **Medallion socket UI + confirm-before-unseal (P1-3 / Track F · F4).** *(Done
  2026-07-06 — brief `product-requirements/medallion-socket-ui.md`: a racked blank renders as a
  **medallion** (part centred on a species disc, sockets as **rim gems**, **rim = progress ring**
  closing as sockets fill — `MedallionMeshBuilder`, reworked `BlankEntryView`), moved into the
  **ribbon under the cauldron** (`InventoryStage.prefab` `MedallionRibbon`), and the commit moved
  from "the last drop" to a **confirm-before-unseal** click (`IBlankRackView.OnUnsealClicked` →
  `MutationVariantPresenter`; `SocketingModel.TryUnsocket` now reopens a full blank). See CHANGELOG;
  `mutation-subsystem.md` §2.3/§2.4.)* *(mutation + inventory)*
- [ ] `[content]` **Medallion look polish (residue of P1-3 / F4).** Procedural disc/rim meshes +
  code-built labels; no blank icons authored, no drag ghost, no gem/rim VFX beyond the ready pulse,
  no tier-glow on the medallion ("tier by glow" PO axis). Exact medallion art / closing-ring VFX are
  the render-look/tech-art call. *(mutation + inventory)*
- [x] `[content]` **Separate scarce Blank Rack.** *(Done — `IBlankRack`/`BlankRack`, capped by
  `MutationConfig.BlankRackCapacity` (default 3), its own instance-id space, never mixed into the
  cauldron's artifact inventory; seeded from `MutationConfig.StartingBlanks` (dev seed — blank loot
  drops are a follow-up below). See CHANGELOG; `mutation-subsystem.md` §2.6.)*
- [ ] `[content]` **Blank drops as loot.** Blanks currently only enter the rack via the
  `MutationConfig.StartingBlanks` dev seed; fold Part-Blank drops into the loot path (monster
  remains, finds, relics — `design/crafting/model.md`). *(loot + mutation)*
- [ ] `[content]` **Cauldron-voice trend telegraph on socketing.** As artifacts are socketed, the
  cauldron voice hints at the *trend* (not the exact menu) — the hint channel that replaces a stats
  panel (`design/narrative/cauldron-voice.md`). **The domain seam is in place** —
  `ISocketingTrendSource`/`SocketingTrendEvaluator` publishes the post-grammar trait trend per
  socket change (`mutation-subsystem.md` §2.6); this item is the bark *delivery* that subscribes
  to it. *(crafting + narrative)*

Migration — retire the old feed loop (the Socketed Blanks brief §18 replaces it):
- [x] `[arch]` **Remove the feed→tally→stage-up mutation coupling.** *(Done — `IMutationTally`/
  `IDigestionProgress`/`ArtifactArchetypeProfile`/`MutationOptionBuilder`/`ArtifactArchetypeMapper`/
  `ArchetypeWeight`/`ArchetypeAffinity` deleted; the variant menu reuses the choice presenter shell
  as `MutationVariantPresenter`; `MutationCandidatePart` is trait-only; species now lives on the
  blank (`ArchetypeDefinition` kept for markers/tints). See CHANGELOG; `mutation-subsystem.md`.)*
- [x] `[arch]` **Retire the feeding/digestion UI.** *(Done — feeding session/presenter/view, the
  mode switch, the HUD feed toggle, the stage `FeedingArea`/feeding camera framing, and the
  `FeedingUISetup` editor tool are deleted; crafting and the operating table share one screen.
  See CHANGELOG; `inventory-subsystem.md`.)*
- [ ] `[content]` **Deferred — cauldron-will stochastic surprise.** Volatile/high-tier inputs adding
  a readable, non-griefing twist at unseal; stays deferred (as with the corruption meter). *(crafting)*

---

## Developer Tools

See `dev-tools.md` for the implemented overlay.
- [x] `[arch]` **Dev state overlay (quests + director facts).** *(Done — key-toggled IMGUI overlay
  (F1, editor/dev-build only) with generic sections; `DevStatePresenter`/`IDevStateSource` +
  `DevOverlayView` + `DevToolsInstaller`. See CHANGELOG; `dev-tools.md`.)*
- [ ] `[content]` **More dev sections.** Inventory contents, the blank rack / socketing state
  (racked blanks, socketed reagents, the current trend), the streaming planner's
  window/committed-horizon + `ILiveActorRegistry` live actors, and live quest objective progress
  (advanced/target) read from `ILiveQuestRegistry.LiveQuests`.
- [ ] `[arch]` **Mutating dev controls.** Beyond read-out: force a fact value, force a quest to
  complete/fail, or grant an artifact from the overlay (currently read-only).
- [ ] `[arch]` **Configurable toggle key.** The toggle is hardcoded to F1; make it configurable.

---

## Backlog (unsorted)

- [x] `[arch]` ~~Run-state persistence/save-load~~ — **done (P2-2, 2026-07-05)**: the whole-run
  continue image (incl. the progression record and mutation-side rack/socketing state) persists at
  platform-entry savepoints; see `save-persistence.md` and the CHANGELOG.
- [ ] `[content]` **Currency model + its save section.** The P2-2 brief lists currency in the
  continue image, but no wallet/currency model exists anywhere (quest rewards note the same gap);
  when it lands, add a `PlayerStuffSnapshot` section for it. *(inventory + persistence)*
- [ ] `[debt]` **Migrate `PlatformEvents` static event bus to Zenject signals.** The autosave (P2-2)
  is the third consumer of the static seam (after the streaming coordinator and the area view);
  signals would make the subscriptions container-scoped and testable without static teardown.
  **W2 audit correction (2026-07-07): the seam is broader than "three consumers" — 7 publisher/subscriber
  files across 6 systems; unblocked by Audit Refactor A3 (platform registry → DI).** *(architecture)*
- [ ] `[content]` Mutation preview on the live character model before the player confirms a choice.
  *(The **mini-model** popover on hovering a mutation card **shipped** with
  `product-requirements/mutation-choice-cards.md`; this backlog item is the **full live-hero**
  in-world preview beyond that.)*
- [x] `[debt]` Generalise the mutation card's `AbilityTooltipView` into a shared UI tooltip
  service once a second consumer appears. *(Done 2026-07-06 — the arena draft was the second
  consumer: superseded by the **shared ability-preview popover** (`ability-preview-popover.md`,
  `UI.AbilityPreview`), which shows description + a 3D hero demonstrating the cast;
  `AbilityTooltipView` deleted. See CHANGELOG.)*
- [ ] `[content]` Mutation card VFX polish (tech-art): a real tier-glow shader (today: frame
  brightness by rarity), a flip animation (today: instant face toggle), tooltip styling.
- [ ] `[content]` Animated idle pose for the mutation card's mini-model preview (today: unanimated
  bind pose); consider a persistent re-swap clone if per-hover assembly ever stutters.
- [x] `[arch]` Enemy-intent telegraph (show enemies' planned abilities) for combat readability.
  *(Done — delivered by Track C: the Plan phase reveals every enemy's committed intent as an icon
  above it, and hovering the icon replays the committed cast's ghost. See CHANGELOG;
  `combat-round-and-telegraph.md` R9–R11.)*
- [ ] `[content]` Biome ↔ archetype affinity: bias biome loot so a biome nudges the player toward
  certain archetypes, tightening the biome → artifact → mutation loop.
- [ ] `[content]` Quest log UI surfacing the progression record (active/completed/failed).
- [ ] _new ideas land here, then get sorted into a system above._
