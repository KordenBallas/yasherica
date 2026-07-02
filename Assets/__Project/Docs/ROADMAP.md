# Roadmap

Planned work and open gaps, grouped by system, plus a cross-cutting backlog. Checkbox items.

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
3. In parallel after step 2: **Sites & landscape** (`world-sites-and-landscape.md`, also needs step 1)
   and **Biome visual styles** (`biome-visual-styles.md`) — both now unblocked.

**Track C — Combat (one epic — build together, not piecemeal)**
1. **Hero facing** (`combat-hero-facing.md`) + **enemy intent phase** (`combat-turn-intent-phase.md`) —
   the structural reshape (facing-relative aiming + Plan→Act→Resolve with locked enemy intent).
2. **Ability ghost telegraph** (`combat-ability-ghost-telegraph.md`) — presentation on top (needs the
   intent phase for the enemy half, and facing for direction).
   > Half-states misbehave (facing without the intent phase; ghost without intent), so take Track C as a
   > single combat pass, well-tested. It reshapes `TurnManager` (round-robin → phase model) + the aiming
   > model together.

Tracks A / B / C are independent and can proceed in parallel; Track A already has work in flight.

---

## Cross-cutting

- [ ] `[arch]` **Non-item reward types have no receiving system.** `Currency`, `Experience`,
  `Ability` rewards are rolled by the narrative system and skipped by the loot granter (logged only).
  Planned owners: `Experience` → Character Progression; `Ability` → Ability Subsystem (part-granted);
  `Currency` still open. Wire each receiving system as its owning section lands. *(narrative + loot)*

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

Planned design (from `narrative-generation.md` §4):
- [ ] `[arch]` **P1 — Story↔NPC compatibility scoring.** Score valid story+NPC pairs (tag overlap,
  faction fit, combat capability); assign by best score instead of randomly. Hard requirements prune,
  preferences only weight.
- [ ] `[arch]` **P2 — Rule-based outcome quotas.** Config quotas (min combat stories, min peaceful);
  satisfy quotas first, then fill by compatibility score; report shortfalls.
- [ ] `[arch]` **P3 — Progression-driven selection.** Feed `GameContext` (`CharacterLevel`,
  `Progress`, `StoryState`) into generation; derive difficulty/theme filters and weighting from progression.
- [ ] `[content]` **P4 — Soft cooldowns.** Fold cooldown/repeatability into scoring (recently seen →
  lower score) so a small pool degrades gracefully instead of producing empty levels.
- [ ] `[arch]` **M2 — Progression- & experience-driven composition.** Extend P3 so generation reads
  the run progression record and character experience/mutation state to compose level content —
  combining stories, quests, NPCs, and rewards by rules — producing a unique but coherent experience
  per run. (Supersedes the standalone P3 once Character Progression + Quests exist.)

Known limitations (§5):
- [ ] `[arch]` Skipped story (no candidate NPC) is not backfilled, so a level may fall below `MinStories`.
- [ ] `[arch]` `GameContext` not consulted by `LevelNarrativeGenerator`; `ScenarioGenerator` hardcodes difficulty to 10.
- [x] `[content]` `RewardSlot.Condition` is stored but never evaluated. *(Done — `RewardResolver`
  evaluates it against the run progression record via `RunConditionEvaluator`; see CHANGELOG,
  `character-progression.md`. Only single-predicate conditions are supported — composition is a
  Character Progression follow-up.)*
- [ ] `[rule]` Generator logs via `Debug.Log/LogWarning` directly — violates the logger-abstraction rule (§9).
- [ ] `[debt]` **Reconcile/retire `narrative-generation.md`.** §1–§3 describe the deleted legacy pipeline
  (replaced by the streaming director — `narrative-procedural.md` / `narrative-director-requirements.md`).
  Decide: fold any still-relevant P1–P4 planned design into the streaming docs and retire/trim this file.
  Stale-banner added; reconciliation pending.

---

## Data-Driven Procedural Narrative

Deferred design (from `narrative-procedural.md` §6):
- [x] `[arch]` **Director Priority-1 — actor/faction eligibility (D16) + recurring-actor casting (D11).**
  *(Done — `RunWindowPlanner` classifies each story: world-only stories keep the actor-less path + fresh
  mint; actor/faction-scoped stories resolve as a casting query over `ILiveActorRegistry` and hard-pin
  the satisfying live actor for recast (continuation semantics). New pure-C# `ILiveActorRegistry`/
  `LiveActorRegistry` bound `AsSingle`. Proven by the `RunWindowPlannerTests` raider-arc + negative
  tests. See CHANGELOG; `narrative-procedural.md` §2.2/§2.6.)*
- [ ] `[arch]` **D7 — Spine reserved lane + per-run reveal cap.** Place spine reveal-beats first by
  their own quota (mirroring the combat minimum), capped at ≤1–2/run, gated on mastery milestone + soft
  floor; they never compete for the narrative density budget by weight. Decoupled from win/lose.
- [ ] `[arch]` **D19 — Escalation tier gating.** Read an altitude/tier fact and shift the eligible
  story pool + tonal register (and optionally density) as the run climbs.
- [ ] `[arch]` **D20 — Meta-scoped fact horizon.** Distinguish run-scoped facts (reset on death) from
  meta-scoped facts (persist across runs); long arcs (spine cursor, mirror-lore flags, cauldron memory)
  ride the meta horizon. The director reads both.
- [ ] `[arch]` **R8 — First-class threads.** Promote `_threadId` from a string label to a `Thread`
  entity (id + stage) the director balances and the player can read; express "a choice in thread A
  affects thread C" as A's effect read by C's precondition (already the mechanism — this adds the
  first-class entity + balancing).
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
- [ ] `[content]` **Biome selection along the run.** The streaming generator still runs a fixed biome
    (`AreaSceneEntrypoint` hardcodes Forest); fold progression/biome selection into the planner so the
    biome (and with it the monster pool + loot table) changes as the run advances.
- [ ] `[debt]` **Remove the dead platform-loot chance API.** Loot-platform *presence* is owned by the
    density allocator now; `LootRollService.ShouldPlaceLootOnPlatform` and
    `BiomeLootDefinition._platformLootChance` have no callers — delete them (and their
    `BiomeLootData` field) on the next loot pass. *(loot)*
- [ ] `[arch]` **Director pacing — thread balancing.** Cross-window thread continuity/quotas beyond the
  planner's within-window thread preference (ties into R8 first-class threads).
- [ ] `[content]` **Demo fact-web sharpeners.** The deeper two-thread demo (`narrative-procedural.md` §4)
  defers, to sharpen director fact-analysis coverage later: (a) **numeric-threshold facts** — an int fact
  (e.g. `village_hunger`) exercising `Gt/Gte/Lt/Lte` (today all demo facts are Bool/`Eq`); (b) replace the
  card-set `world.reads_as_frogfolk` **passport stand-in** with the real mutation→fact projection (D15);
  (c) a **single NPC offering several resolution cards at once** (`npc-encounter-cards.md` §3), which needs
  multiple quest slots per story (one today).
- [x] `[content]` **Quest-carried item rewards.** *(Done — item rewards live on `QuestDefinition._rewards`
  and are granted on completion by `QuestRewardGranter` (no longer a no-op) via the `PlatformCompletedState`
  hook. Item rewards only; currency/experience/ability kinds still lack a receiving system. See CHANGELOG;
  `quest-subsystem.md`.)*
- [ ] `[arch]` **Window/horizon save-state.** `RunNarrativeSnapshot` does not yet capture the streaming
  planner's window/committed-horizon state, the `ILiveActorRegistry` live-actor set (recurring-actor
  continuity, D11), **nor the `ILiveQuestRegistry` live-quest set** (cross-dialogue quest continuity, R8),
  **nor the `WorldContentAllocator` quest-spacing counter** (world-content-density spacing across a
  reload); add all four (ties to R14 file IO).
- [ ] `[arch]` **R11 — Reactive-rule cascade layer.** Optional central layer that derives cross-category
  cascades from fact reads/writes; cascades are explicit authored effects until then.
- [ ] `[arch]` **OR/boolean precondition composition.** Preconditions are AND-only; add OR/grouping.
- [ ] `[arch]` **R14 — Save/load file IO.** The serializable boundary (`INarrativeSaveService`, snapshot
  DTOs incl. PRNG state) and the W3-1 suspended-non-savepoint rule exist and are tested; add the file
  writer/reader and the full run-state aggregate (assemble quests/castings/sessions). Optional W3-1
  option-b: persist a suspended dialogue + pending-external descriptor for mid-excursion saves.
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
  reward tier-glow/belonging color (gated on Crafting), the full Monster verb, several-offers-per-NPC, and
  deletion of the old UI remain as the separate items below.)*
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

See `quest-subsystem.md` for the implemented quest fragment, lifecycle, and rewards.

> **Priority — the quest-as-reward economy** (`design/narrative/quest-as-reward.md`,
> `quest-subsystem.md` §6). Make a quest *offer* feel like a prize. **Gated:** the reward **tier**
> (card glow + roll-by-tier) needs the artifact trait/tier model — see **`## Crafting`** (prerequisite).
> Suggested order:
> - *Can precede the tier model:* (1) cross-dialogue quest continuity (enabler below);
>   (2) competing same-tier offers + mutual-exclusion facts on a thread; (3) the encounter card-hand
>   scaffold (situation bubble + typed card hand) that replaces the branching dialogue UI, incl.
>   several offers per NPC and the attack card.
> - *Gated on `## Crafting` (tier):* (4) reward rolled by `tier + archetype-bias`; (5) the card's
>   tier glow + belonging color; (6) the cauldron tempter bark.
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
- [ ] `[content]` **M2 — Quest log UI.** Surface the active/completed/failed quests and objective
  progress (ties into the backlog quest-log item + the progression record).

Quest-as-reward (priority — `design/narrative/quest-as-reward.md`, `quest-subsystem.md` §6):
- [ ] `[arch]` **Reward = rolled `tier + archetype-bias`, not a literal id.** Replace the fixed
  `(artifactId, count)` on `QuestRewardCore` / `QuestRewardSerial` with a declared **reward tier +
  archetype-bias**; on completion `QuestRewardGranter` **rolls** the artifact against loot tables by
  tier + bias instead of granting a literal id. Keeps the offer-card glow honest and the world
  non-catalog ("direction + floor, not vending"). Item rewards only. *(quests + loot)*
- [ ] `[arch]` **Competing / mutually-exclusive same-tier offers, separated in time.** Two offers on a
  shared-actor thread (director D10–D12) where accepting/declining the first gates a **same-tier** second
  offer placed a couple platforms later; the choice reads as *whose side / which facts*, not "better
  loot." The cross-dialogue quest-continuity prerequisite is now **done** (above); this still needs
  mutual-exclusion facts **and** same-tier offer placement. *(quests + narrative)*
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
- [ ] `[arch]` **Several quest offers per NPC/storylet (new authoring shape).** A single story/NPC may
  present **multiple quest-offer cards at once** (several resolution paths to one situation), same tier +
  "different currency, not more". Today a story carries one Quest slot; let a story/casting carry several
  offers and the card hand present them. Distinct from the time-separated cross-actor fork above. *(quests + narrative)*
- [ ] `[content]` **Quest-offer card visual treatment.** *(Verified PO brief:
  `product-requirements/quest-offer-card.md`.)* Upgrade the encounter card-hand's placeholder per-type
  tint into the **ornate framed offer card**: the job (title+summary, already shipped) + a **mystery
  reward slot** that **glows by tier**, is **tinted by belonging colour**, and shows the item as a
  **hidden silhouette/"?"** (exact item never named). **Inspect** reveals the quest detail (reward
  stays hidden). Reuses the artifact / mutation-card grammar (glow=tier, colour=belonging). **Now
  unblocked** by the shipped artifact tier; the honest glow still assumes the "reward rolled by
  tier+archetype-bias" item above. *(quests + narrative)*
- [ ] `[arch]` **Attack card — the Monster verb.** A combat card present on **eligible NPCs only** (NPC
  may also self-initiate) that, on pick: closes the actor's thread (director D13), routes corpse-loot to
  the **separate** combat/mutation loot channel (`LootRollService` — never balance the fork on it), writes
  Conquest/path facts, and fires the cauldron tempter bark. `design/narrative/npc-encounter-cards.md` §4. *(quests + narrative + loot)*
- [ ] `[content]` **Cauldron-voice tempter hook on the dark offer.** A bark slot fired when a
  power-archetype (Monster-path) offer **or the attack card** is presented, so the temptation rides fiction
  rather than weighting the loot (`design/narrative/cauldron-voice.md`).

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
- [ ] `[debt]` **Refresh `loot-subsystem.md` to the streaming cutover.** The doc (2026-06-12) describes the
  deleted legacy reward path (`RewardResolver`/`ResolvedReward`, `StoryDefinition` reward slots,
  `NpcAssignment.Rewards`, `NarrativeInstaller`/`ScenarioGenerator`/`PlatformGraphGenerator`) and a wrong
  `QuestRewardGranter`; rewrite §1.6/§2 to the live state (fixed `QuestRewardCore` grant; `LootRollService`
  dormant until "Streaming-path loot" lands). Stale-banner added; full rewrite pending.

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
  alignment has its data seam (`PlatformHexSurface.BlockedCells`, unused) — the feature content is
  the biome-visual-styles brief. See CHANGELOG; `platform-generation.md`.)*
- [x] `[arch]` **M3 — Content-aware platform size & shape.** *(Done — per-content-kind
  `ShapeProfile`s + battlefield minimum (12 cells) on the one `PlatformShapeConfig` SO; kind resolved
  from the graph node (`Type == Combat` covers ambient + story-with-required-combat); per-platform
  seeded streams (`LootSeed.Derive(runSeed, "platform-shape:{id}")`) make same-seed→same-platforms
  hold, replacing the last `UnityEngine.Random` uses on the platform path. See CHANGELOG;
  `platform-generation.md`.)*

Follow-ups from the platform-hex rework (doc §6):
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
- [ ] `[content]` **Sites & landscape — content-driven footprint (setting scale).** Two axes:
  **Biome** (`LevelTheme`, ground/race homeland) × **Site** (settlement scale on top: wild / camp /
  village / city). Content-first: a settlement-scale beat pulls a **Site** into being (ambient content
  is Wild), so Sites are rare and the world is mostly wilderness. A Site's footprint is a **cluster of
  N adjacent platforms** (city ~5, village ~2, camp 1) that **holds several beats** and reads as one
  place via shared dressing + connective visuals (connected islands, Windblown feel kept). The
  deferred companion to the "World content density" brief. Vocabulary is **extensible by schema**
  (one authored asset per site type) with **two families** — Settlements (camp / village / city:
  faction-occupied, passport-gated, quest-bearing) and Landmarks (ruin / lair: wild, no passport,
  the spatial homes of the naturalistic loot sources). Beta backlog: hamlet, grove/shrine, crater,
  biome-locked variants, and the Order's-Seat apex (open question). Each Site's **capacity recipe**
  (how it fills its footprint) is **anchor + weighted fill + connective**, references content
  kinds/tags (not literal assets), with a **family-default + per-Site override** and additive/optional
  attributes so it stays revisable without breaking authored Sites. Content kinds are one shared
  **base × flavor** vocabulary (4 base kinds Empty/Loot/Combat/NPC × open flavor tags; quest &
  hostility derived from an NPC beat) used by both the density budgets and site recipes — see
  `design/world/content-kinds.md`. **Verified PO build brief:
  `product-requirements/world-sites-and-landscape.md`** (consolidates the model + vocabulary +
  recipes + content-kinds catalog; builds on the density brief). Design background:
  `design/world/sites-and-landscape.md` + `content-kinds.md`. *(Engine concern for the code track:
  reserving a multi-platform Site block across a director planning window; balance numbers.)*
- [ ] `[content]` **M3 — Biome-driven platform appearance & features.** *(Verified PO brief:
  `product-requirements/biome-visual-styles.md`.)* A **per-biome appearance config** — one SO per
  `LevelTheme` (Forest/Desert/Mountain/Cave), mirroring the existing `BiomeLootDefinition` pattern —
  defines ground material/mesh treatment, **palette key** (a muted key modulation of the master
  palette, `design/art/render-look.md` §2), light, and a **feature pool**. Features place on **whole
  hex cells** (platform brief); each is **decorative or blocking** (blocking = the cell is unavailable;
  no cover/LoS), and blocking must not drop a combat platform below its battlefield minimum. Replaces
  the single global `AreaGeneratorConfig.platformMaterial`. Data-authored (new biome / feature = no
  code), deterministic. Biome = base layer only; **site dressing** and **biome placement along the
  run** are separate. *(platform + world)*

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
- [ ] `[arch]` **Live fact-driven marker refresh.** The `?` currently clears when the encounter is started
  (consumed), not by per-frame re-evaluation of quest availability against the fact store.
- [ ] `[arch]` **Per-NPC / per-archetype radius overrides.** Global values only today.
- [x] **No-quest-but-talkable-with-optional-fight now expressible.** Hostility requires a **required**
  (non-optional) combat slot; an optional combat slot is a dialogue branch, so the NPC stays talkable
  (e.g. `DemoStory_BarnRaid`'s fight-or-bribe raider). Resolved in `NpcIntentResolver`.
- [ ] `[content]` **No forced-combat NPC in the demo.** All demo combat is a dialogue branch, so the
  auto-aggro `!` path has no demo subject. A required-combat story would supply one, but see the next
  item — an always-eligible combat story currently perturbs the seeded threads.
- [ ] `[arch]` **Streaming planner does not enforce thread ordering / cross-window continuity (R8).**
  Fact-ordered threads (frog passport→elder, barn accept→combat→complete) rely on a placement order the
  budgeted seeded planner does not guarantee. Proximity made this worse: encounters now write their facts
  on the player's F-press, not on landing, so the planner — which advances **one window ahead** — can
  plan against stale facts and re-place an already-offered quest story (the re-offer no-ops) or a
  closed/no-quest variant. **Mitigated** by advancing the window on platform **exit** instead of entry
  (`RunStreamingCoordinator`), so an engaging player's choices are written before the next window is
  planned; but the look-ahead is still loose (only the exited platform's facts are guaranteed current).
  The real fix is dependency-aware planning (plan/re-plan a window's narrative against the facts as the
  player reaches it).
- [ ] `[content]` **Marker/name art polish + animation.** Currently plain 3D-TMP glyphs built in code.

---

## World & Environment

New work (no system doc yet):
- [ ] `[content]` **M5 — Landscape around platforms.** Generate surrounding terrain/visuals that
  support the platforms and form a cohesive world feel (skybox, distant geometry, biome dressing).
  Read-only relative to gameplay (no grid/collision impact on platforms).
- [ ] `[content]` **M5 — Site dressing ("reads as one place").** Make a Site's island-cluster read as
  one place via **aligned skyline + shared ground/palette + density gradient + a distant backdrop**,
  with a **gate/threshold** at the boundary — **dressing only, gaps stay clean hops** (no walkable
  bridges). Layer **biome base × site dressing kit** (same site, different biome = shared structures,
  different materials). Art direction: `design/art/site-dressing.md`. *(Engine: place skyline/gate
  pieces so adjacent islands align.)*
- [ ] `[content]` **M5 — Render-look & palette bible.** Foundational visual direction
  (`design/art/render-look.md`): **flat low-poly, no outline** (Windblown-side; supersedes the vision
  §5 Gunfire outlined-cel candidate); **warm muted base + reserved saturated gameplay accents**
  (archetype = hue, tier = glow); **balanced charming-grotesque** silhouettes with anatomy legibility.
  Because there is no outline, figure-ground + readability rely on silhouette + value + reserved
  colour. Follow-ups: concrete palette **swatches**, a **flat-shading / figure-ground shader spike**
  (tech-art), animation direction, per-archetype concept sheets (gated on the parked race roster),
  and the tier-glow / ability-telegraph **VFX language** (ties M4 combat readability).

---

## Character System

Known points (from `character-system.md`):
- [ ] `[perf]` Runtime skinned-mesh combining is not implemented (design keeps it possible — the
  controller owns the live per-slot renderers a combiner would consume).
- [ ] `[debt]` `PartSwapExecutor`, `SocketMounter`, and the factory are verified only manually (demo
  bootstrap + preview window); consider play-mode test coverage.

New work:
- [ ] `[content]` **M5 — Production body-part assets.** Replace placeholder box parts with
  production-ready meshes/materials across all slots.
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
- [ ] `[arch]` **Passive modifiers affect only outgoing damage.** `StatModifier` has no stat-target
  dimension, so max-HP / defence / healing passives are not yet consumed. Extend the modifier model
  (stat target) and `DamageSystem` / unit-stat resolution to honour them.
- [ ] _seed remaining items from `ability-subsystem.md` "Known limitations" on next pass._

## Combat Experience

Enhancements to the implemented combat (extend `ability-subsystem.md` / a new combat-UI doc when built):

**Verified PO build brief: `product-requirements/combat-ability-ghost-telegraph.md`** — one mechanism
unifying the first two items below **and** the backlog "Enemy-intent telegraph": a **ghost preview of
an ability's full outcome** plays once on queue-submit (caster action + affected-unit
displacement/damage, against the **current** board) then fades; each unit (player **and** enemy) shows
its **queued abilities as icons above it**; **hovering an icon replays that ability's ghost**
(including enemy icons → read enemy intent).
- [ ] `[arch]` **M4 — Ability-queue display (icons above the unit).** Show each unit's queued
  abilities (`Unit.AbilityQueue` / `ScheduledAbility`) as ordered **icons above the unit** (not only a
  HUD list), for both player and enemy units — the hover-to-replay anchor. (brief §5)
- [ ] `[arch]` **M4 — Ability ghost telegraph.** Replace the "where"-only affected-cell highlight-only
  telegraph with a **ghost of the full outcome** on submit (and on icon-hover replay): caster ghost via
  the ability's `AnimationTrigger` + affected-unit displacement/damage ghosts, computed against the
  current board. Existing aim-time cell highlight (`CombatAbilityPresenter.ShowAffectedCells`) stays as
  the "where" layer. Category iconography (advance/jump/area/hook/ring) is an optional supplement, not
  the primary telegraph. **Open:** ability data must express displacement (push/pull/dash) so the ghost
  can show resulting positions; queue-simulation preview is a later upgrade. (brief §1–§4, §6–§7)
- [ ] `[arch]` **M4 — Hero/unit facing drives ability direction (global facing).** *(Verified PO
  brief: `product-requirements/combat-hero-facing.md`.)* Directional abilities fire relative to the
  **unit's facing**, not a per-ability baked direction: **one facing for the whole queue** — turning
  the unit **re-points the entire volley + all its ghosts**. Facing is **free/unlimited** to change;
  ring abilities are unaffected; enemies carry a **committed facing** in their intent (locked).
  Reuse the existing direction input (`CombatAbilityPresenter.UpdateAimDirection`) to **rotate the
  unit** instead of baking `AbilityTarget.ForDirection` per schedule; `AbilityTarget` for directional
  abilities becomes facing-relative and a `Facing` state moves onto the unit. Anatomy legibility
  (Pillar 4). Per-ability aiming is intentionally dropped; "turn as a queued step" (multi-directional
  volley) is a deferred escape hatch. *(combat; pairs with the intent phase + ghost telegraph)*
- [ ] `[arch]` **M4 — Smarter ability-using enemy AI.** Improve `TacticalAI` to choose and aim
  abilities well (target selection, area value, direction), beyond the current scoring.
- [ ] `[arch]` **M4 — Turn structure: enemy intent phase (Plan → Act → Resolve).** *(Verified PO
  brief: `product-requirements/combat-turn-intent-phase.md`.)* The **structure prerequisite** for the
  ghost telegraph's enemy-intent half. Replace the round-robin, decide-and-act-instantly flow
  (`TurnManager` per-player + `AITurnController` deciding+executing on the AI's turn) with a **phase
  round**: (1) **Plan** — every enemy decides up front and **reveals a locked plan**; (2) **Act** —
  the player queues/executes seeing those plans; (3) **Resolve** — enemy committed actions **fire as
  shown** (locked: they whiff if the player dodged, they do not re-target). **Resolution order:
  player then enemies** (initiative-based order deferred). The AI **scoring** (`TacticalAI`) is
  unchanged — only decide-timing (up front) + commitment (lock+reveal) change. Deterministic per seed.
  *(combat; prerequisite of the Ability ghost telegraph above)*

## Inventory Subsystem

- [x] `[arch]` **M1 — Feeding / digestion UI.** A feeding mode (toggled in the open cauldron) where
  pot artifacts are selected into a feeding tray with a cumulative archetype-attribute readout, the
  current digestion progress, and the dominant archetype(s) this stage; the Feed button maps each
  artifact via `ArtifactArchetypeMapper.ToProfile` into `IMutationTally.Add` and advances
  `IDigestionProgress`. *(Done — see CHANGELOG.)*
  **Superseded and removed (2026-07-02):** the Socketed Blanks model replaced feeding with
  operating-table socketing and the feeding UI was deleted — see the migration items in
  `## Crafting & Mutation` and the CHANGELOG `Removed` entry.
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
- [ ] `[content]` **Operating-table polish.** The rack is a placeholder look (tinted quads,
  code-built TMP labels, no blank icons authored); no drag ghost effects, no unseal animation, no
  tier-glow on bubbles ("tier by glow" PO axis). A confirm step before the auto-unseal commit is a
  possible later tweak (today the last drop is the commit — user decision). *(mutation + inventory)*
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

- [ ] `[arch]` Run-state persistence/save-load (needed once the progression record + per-run
  mutation state matter across sessions).
- [ ] `[content]` Mutation preview on the live character model before the player confirms a choice.
  *(The **mini-model** popover on hovering a mutation card **shipped** with
  `product-requirements/mutation-choice-cards.md`; this backlog item is the **full live-hero**
  in-world preview beyond that.)*
- [ ] `[debt]` Generalise the mutation card's `AbilityTooltipView` into a shared UI tooltip
  service once a second consumer appears (it is deliberately mutation-local today — KISS).
- [ ] `[content]` Mutation card VFX polish (tech-art): a real tier-glow shader (today: frame
  brightness by rarity), a flip animation (today: instant face toggle), tooltip styling.
- [ ] `[content]` Animated idle pose for the mutation card's mini-model preview (today: unanimated
  bind pose); consider a persistent re-swap clone if per-hover assembly ever stutters.
- [ ] `[arch]` Enemy-intent telegraph (show enemies' planned abilities) for combat readability.
  *(Now delivered by the ghost telegraph — enemy queued-ability icons + hover-to-replay ghost — in
  `product-requirements/combat-ability-ghost-telegraph.md` / Combat Experience; kept here only for the
  turn-flow dependency that surfaces the enemy plan ahead of resolution.)*
- [ ] `[content]` Biome ↔ archetype affinity: bias biome loot so a biome nudges the player toward
  certain archetypes, tightening the biome → artifact → mutation loop.
- [ ] `[content]` Quest log UI surfacing the progression record (active/completed/failed).
- [ ] _new ideas land here, then get sorted into a system above._
