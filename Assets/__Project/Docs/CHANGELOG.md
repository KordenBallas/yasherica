# Changelog

All notable changes to the project are recorded here. Format follows
[Keep a Changelog](https://keepachangelog.com/): newest first, grouped Added / Changed / Fixed /
Removed, each entry tagged with the affected system and (where useful) requirement ids.

Every functional change appends an entry **in the same change as the code** (CLAUDE.md §8.2).

## [Unreleased]

### Fixed
- **Heat — a soft-cap refusal is never silent** [heat-ascension R3; owner playtest 2026-07-08]:
  taking all four demo modifiers hit the pact's soft cap (9 < the menu's 11) and the last step
  ("Thinner Medallions") was refused with **zero feedback** — the card didn't change and the run
  launched without the most visible effect the owner then looked for. Now the capped card's hover
  reads "next step over the cap (M) — lower another pain first" (`HubHeatModel.NextStepIsCapped`)
  and the seal card shows "Heat N / cap M" while a cap is active. **Owner call:** the demo
  `_softCapTotalHeat` is lifted to 0 (uncapped) so the whole demonstrator menu is takeable during
  playtests; re-cap by data when the menu grows. Diagnosis note: the heat=9 run's other effects
  DID apply (tier lift + high-water record in the log); the pact is per-run and resets at the Hub.
  Tests: `HubHeatModelTests` (capped-marker cases) · `HeatConfigMapperTests` updated.

### Added
- **Heat / Ascension — player-chosen difficulty as the cauldron's dare (Track Y)**
  [heat-ascension R1–R15; consumed brief `heat-ascension-difficulty.md`]: the reiterability engine —
  an optional **pact of independent rules-modifiers** (each 1–N rank steps with authored Heat
  values; total = the sum) taken at a new **cauldron F-spot on the Hub** (ember pot mirroring the
  keeper; skipped when unauthored), presented on the shared mutation card panel with **zero view
  changes** (rank = the rarity glow, pips + Heat in the name, the rank's rule text as a
  passive-ability hover, a closing "Seal the pact" card) under a new `HubPanelArbiter` (fixes the
  shared-panel cross-talk with the part offer); locked for the run at the portal commit.
  **Rules, not stats** — four code seams consumed via per-system neutral records (no system
  references `Heat.*`): `EnemiesActFirst` (`RoundLeadPolicy` + `PveCombatFlow` + the turn-order
  strip — the strip consults the same rule as the flow), `RaisedCreatureFloor` (the D19 tier lifted
  at `BiomeStretchDirector`'s one fact-write site — planner tier-bands + monster pools follow for
  free), `StingyCauldron` (unseal variant options cut, floor 1), `FewerSockets` (a
  `SocketAdjustedBlankSource` decorator on `IPartBlankDataSource` — socketing, the medallion gem
  ring, and quest pools all see one reduced count, floor 1). **Reward entirely through Track R:**
  per-token **min-Heat gates** (`MetaGatingAuthoring._minHeat`/`_heatKey`, keyed to the current
  pact or the persisted record) + tier run-floor **relief** + a **dig bias lift** riding
  `HeatDialAdjuster` — the never-guarantee ceiling untouched by construction; R exposes only the
  narrow `IHeatLens` seam (dependency points Heat → MetaProgression). A pact change at the hub
  **re-deals the dig deterministically** from the same seed. **Persistence:** the pact rides
  `run-setup.json` and `run.json` as additive fields at **unchanged snapshot versions** (old saves
  = Heat 0, never quarantined) and degrades gracefully against a re-authored menu; the **hottest
  clear** persists as the Meta fact `world.heat_high_water` (new `ISavepointObserver` seam on
  `AutosaveService` → `HeatHighWaterRecorder`, written before the meta flush; MVP "cleared" =
  a savepoint at window ≥ the configured floor). **Voice:** three new authored cauldron moments
  (dare / sealed / declined). **Data:** one `HeatConfig` asset (the embedded modifier menu + the
  R-mapping dials + soft cap + clear floor + ember tint) with a four-modifier demonstrator set;
  min-Heat demonstrator on `Recipe_NeedleBacteria_Stinger` (taste + Heat ≥ 2); Heat 0 reproduces
  today's game and the **Arena is untouched** (never installed there). New system doc
  `heat-ascension.md`. Tests: `HeatPactTests` · `HeatRulesTests` · `HeatDialAdjusterTests` ·
  `HeatConfigMapperTests` · `MetaVocabularyHeatTests` · `HubHeatModelTests` ·
  `SocketAdjustedBlankSourceTests` · `HeatHighWaterRecorderTests` · `HeatPactPersistenceTests` ·
  extended `RoundLeadPolicyTests` / `BiomeStretchDirectorTests` / `MutationVariantPresenterTests` /
  `MetaGatingDemonstratorTests`.
- **Meta-progression spine — unlock the vocabulary, never the power (Track R core)**
  [meta-progression R1–R14; consumed brief `meta-progression-spine.md`]: the cross-run Isaac-model
  layer. **Gating:** every gateable content SO (`PartDefinition` / `ArtifactDefinition` /
  `RecipeDefinition` / `PartBlankDefinition`) carries a `MetaGatingAuthoring` block (mark / deed /
  unlock tier / draw weight; unmarked = base, FR15); deeds reuse the authored fact-predicate
  grammar (`$self` = the token id) over Meta-horizon facts — the two MVP channels are tasting a
  form (`world.<partId>.arena_tasted`) and reveal-spine milestones (`spine_seen`, `run_count`); a
  deed satisfied unlocks immediately, paced by per-tier run floors (owner forks 2026-07-07).
  **Vocabulary:** `MetaVocabulary` (pure, frozen per scene from `meta.json` + the effective run
  count) answers `IsUnlocked`; consulted by all five draw surfaces — the hub dig pool (now
  **Tasted ∩ Eligible**, run-1 bare launch unchanged), biome loot tables (new `MetaGateLootFilter`
  in the existing `ILootEntryFilter` chain), quest-reward pools (extracted
  `QuestRewardPoolsBuilder`), the recipe book (a locked recipe simply doesn't exist; token id =
  output artifact id), and mutation variant candidates. **Direction bias:** a per-run ledger
  (`MetaRunLedgerSnapshot` — an additive `MetaMemorySnapshot` field, version stays 1 so old files
  never quarantine) records installed part ids (`PartsChanged`) and consumed reagent ids (new
  `ISocketingModel.OnSocketsConsumed` at the unseal commit); `DirectionTally` reads its sliding
  window on the two data axes (race marker / substance+property traits) into a `DirectionProfile`
  that weights the dig's tie-break — `pow(drawWeight·(1+strength·score), dilution)` with every
  weight clamped below the **never-guarantee ceiling < 100%** (uniform weights = the legacy pick,
  byte-identical). **Tuning:** one `MetaProgressionConfig` asset (floors, window, axis weights,
  bias + ceiling, dig shape incl. offer size — the old `OfferSize = 3` const retired — dilution,
  retention) slides generous↔minimal without a rebuild. **Demonstrator set (FR16):**
  `Part_LegsSpider` (run ≥ 3), `Part_SpineSerpent` (spine milestone), `Recipe_FireWater_Snake` /
  `Recipe_NeedleBacteria_Stinger` (taste-the-form deeds). New system doc `meta-progression.md`;
  in-run currency stays the separate companion brief. Tests: `MetaVocabularyTests` ·
  `SnapshotPredicateEvaluatorTests` · `MetaProgressionConfigMapperTests` · `RunLedgerTests` ·
  `MetaMemorySnapshotCompatTests` · `DirectionTallyTests` · `StartingPartSelectorBiasTests` ·
  `MetaGatingSurfaceTests` · `MetaGatingDemonstratorTests`.
- **Arena anti-cheat + P4-3 resolution/damage alternatives (Track X · X2)** [arena-mode §2.7 /
  §2.11]: the host no longer trusts relayed commits. **Host-side commit validation:**
  `ArenaCommitValidator` (pure) re-checks every accepted commit against the host's canonical
  round-start state (`IArenaCanonicalStateSource` = the controller over the flow's retained anchor)
  — ownership/seat, alive+not-stunned, defined facing, shape (one move XOR a volley within the
  queue-size cap **and** the seat's banked scheduling rounds via `ArenaQueueCreditLedger`), move
  legality (origin/range/on-platform), ability legality (known + off cooldown), and the
  load-bearing **cell re-derivation** through the shared `ArenaCommitBuilder.RebuildAbilityIntent`
  (extracted public — one source, no drift) with exact equality: tampered cells cannot enter the
  bundle. An invalid commit is **replaced with a pass** (reusing X1's `MarkPassed`) + loud
  `CommitRejected` — no kick (safe under a false positive); gated by `_validateCommits` (on by
  default), the host's own commit self-checks. **P4-3a `SeededShuffleResolutionOrder`:** a
  config-selected per-round Fisher–Yates (`LootSeed.Derive(seed, "arena-resolve:{round}")`) behind
  the existing `IArenaResolutionOrder` seam — unpredictable but lockstep-identical; default stays
  `RotatingInitiative`. **P4-3b per-step damage batching** (config-gated, default off): a new
  `ICombatRoundFlow.OnIntentResolved` hook + optional `IIntentEligibility` on `EnemyIntentResolver`
  (null everywhere = PvE byte-identical) + `ArenaStepBatcher` / `ArenaBatchEligibility` +
  `LastHeroStandingWinCondition.Suspend/Resume` let the round resolve step-major with the win check
  gated to batch boundaries — a mutual lethal exchange kills **both** (a real draw) instead of the
  sequential skip-dead survivor. New config fields on `ArenaMatchConfig`
  (`_resolutionOrderMode` / `_validateCommits` / `_simultaneousDamageBatching`). Tests:
  `SeededShuffleResolutionOrderTests` · `ArenaCommitValidatorTests` ·
  `ArenaCommitValidationFlowTests` (forged envelope → pass, hashes converge) ·
  `ArenaDamageBatchingTests` (mutual-kill draw vs sequential survivor) — **1761/1761 EditMode green**
  (clone-project batch run) with the shipped defaults (rotation + sequential + validation on).
- **Arena online robustness — reconnect + disconnect grace + desync recovery + host migration
  (Track X · X1)** [arena-mode R15–R18 · §2.10]: a dropped or diverged peer no longer ends (or
  silently corrupts) a networked match. **Grace/auto-pass (R15):** `ArenaSeatLedger` (Connected /
  Gracing / Resyncing / Departed) — a vanished connection is auto-passed by the host for
  `_disconnectGraceRounds` round-opens (its unit stays alive in place; the pass is a bundle-level
  fact `AutoPassedPlayerIds`, normalization unchanged) and only then departs. **Rejoin (R16):**
  derived `ArenaRejoinToken` (off the match seed — no storage, any authority validates) claims the
  gracing seat at connection approval (`ArenaConnectPayload` in NGO ConnectionData →
  `IArenaRejoinGate`); the host ships an `ArenaRejoinPackage` (setup + draft loadouts +
  **round-start `ArenaStateSnapshot`** — the exact hash field set; queues/acted/intents excluded
  by design — + the round's bundle if already broadcast); `ArenaSnapshotRestorer` restores by
  overlay (statuses rebuilt by id via `IArenaStatusReconstructor` → status catalog + factory;
  any mismatch fails the whole restore), the flow adopts it (`AdoptState`), and the collector
  reinstates the seat into the still-open round. **Desync heal (R17):** `DesyncDetected` now
  pushes a targeted resync snapshot instead of only warning; the diverged client adopts, re-plans,
  and reports the host's hash from then on. **Host migration (R18, best-effort):** one client-side
  drop machine (`ArenaReconnectClient`, tick-driven, pure) retries the known host, then every
  survivor elects deterministically (`ArenaHostElection` = lowest connected PlayerId over the
  bundle-fed `ArenaSeatStatusMirror`); the winner re-hosts (`MatchStarted` kept, fresh seat book
  with all others in grace — the dead host may return as a rejoiner), rolls the round back to its
  planning anchor (`ReopenCurrentRound`; in-flight commits died with the old host), and losers
  dial it off the self-reported address book (`LanEndpointSource`; LAN best-effort until X3).
  Five new named messages (`yash.arena.rejoin/resync/resyncack/endpoint/addrbook`), four config
  dials on `ArenaMatchConfig`, HUD reconnect overlays + seat notices (drop is terminal only on
  `ReconnectFailed` or pre-combat), engine additions kept surgical (`CombatState.WithTurnNumber`,
  resolve-cursor reset in `EnterEnemyResolveWithIntents`, additive
  `IStatusEffectFactory.CreateStatusEffect(def, duration, stacks)` — PvE byte-identical). Tests:
  `ArenaSeatLedgerTests` · `ArenaSnapshotTests` (capture→restore→hash invariant) ·
  `ArenaReconnectFlowTests` (auto-pass/expiry/rejoin-before-and-after-broadcast/desync-heal, all
  hash-converging) · `ArenaHostMigrationTests` (election, promote-self to victory, terminal
  fallbacks) + wire-codec round-trips — green as part of the **1761/1761 EditMode** suite (with X2).
  ⚠ Awaiting the owner's two-machine LAN checklist (arena-mode §5) for play verification.
- **Smarter ability-using enemy AI + configurable difficulty (P2-4 · Track K)** [new
  `combat-enemy-ai.md` · combat-round-and-telegraph R7 · arena-mode §2.6]: enemies now decide by
  **simulating candidates against the real board** instead of flat payload scoring. New pure-C#
  pipeline `Scripts/Combat/Player/AI/`: `AICandidateEnumerator` (ability × facing, (Q,R)-sorted
  move cells capped at 64, end turn — fixed order), `AIActionScorer` (folds
  `IAbilityOutcomeCalculator` outcomes: damage/kill on hostiles up, friendly fire down, heals worth
  only missing HP, statuses only against targets not already carrying them; moves earn a
  positioning gradient + **lookahead improvement** — the best next-round shot from the destination
  minus from here, discounted — so units that can hit attack and units that can't walk toward the
  shot), `AIDecisionQualityFilter` (mistake roll → per-candidate noise → top-N pick; the pipeline's
  only RNG, argmax at perfect dials), `SimulationTacticalAI` (`IAIDecisionMaker` orchestrator).
  **Difficulty is configuration on two layers**: `AIProfileDefinition` gains decision-quality dials
  (`_scoreNoise`/`_pickFromTopN`/`_mistakeChance`) + priority weights (`_focusWoundedWeight`,
  `_friendlyFirePenaltyWeight`, `_aggressionWeight`, `_selfPreservationWeight`) with back-compat
  defaults (legacy assets = sharp pre-P2-4 behavior), and the new **`DifficultyDefinition`** SO
  (`Create → Combat → AI → Difficulty`) modulates every enemy at once — quality dials add,
  priority scales multiply; Easy/Normal/Hard presets authored under `Resources/Combat/Difficulty/`,
  bound via installer field → `NormalDifficulty` Resources fallback → Neutral, behind the
  `IAIDifficultySource` seam (a future settings screen swaps the source). Hostility became an
  injected `IHostilityPolicy`: `TeamHostilityPolicy` (PvE) / `FreeForAllHostilityPolicy` (Arena);
  one `AIDecisionMakerFactory` builds the same brain for PvE enemies and Arena offline dummies.
  `IAbilityOutcomeCalculator` gains a hypothetical-origin `ComputeForFacing` overload (telegraph
  callers untouched). Determinism preserved: same seed + state → same pick even with noise/top-N.
  Tests: `SimulationTacticalAITests` (aiming/kill-securing/friendly-fire/heal/lookahead/
  determinism/difficulty-monotonicity), `AIDecisionQualityFilterTests`, `AITuningTests`,
  `AIDefinitionMapperTests`, outcome-origin parity — 81/81 combat closure green, full game +
  all-test compile clean.
- **Cross-Device Input Foundation** [new `input-foundation.md` · npc-proximity-interaction R3 ·
  dev-tools R6 · inventory R4] (brief `cross-device-input-foundation.md`): the game is now played
  through a fixed **named-action vocabulary** (Move / Interact / Confirm / Cancel / Navigate / Aim /
  Fire + CombatMoveMode / CombatChangeDirection / AbilitySlot1–6) with every action bound on
  **keyboard+mouse, gamepad, and touch simultaneously** — no platform switching. New pure-C# core
  (`Scripts/GameInput/Core`): `InputBindingCatalog` (declarative action × source table + prompt
  cues), `InputCoverageValidator` (an unbound pair fails the build unless explicitly allowlisted as
  a deferred design gap — and a stale allowlist entry fails too), `InputSourceClassifier` (last-used
  device wins; stick-drift threshold; the touch overlay's **virtual** gamepad counts as Touch), and
  `PromptCueProvider` (action → cue for the ACTIVE source, `CuesChanged` on device switch).
  Infrastructure: `GameActionsProvider` loads the single `Resources/Input/GameActions.inputactions`
  (the project's template asset **moved with its GUID intact** — Hero.prefab's move/dash references
  keep resolving — extended with combat actions: MoveMode LT, VolleyFire RT, ChangeDirection X,
  Ability1–6 on d-pad + bumpers, Aim on stick, Interact retuned e→f + gamepad north);
  `ActiveInputSourceTracker` over `InputSystem.onEvent`; `TouchControlsView` — a procedural
  on-screen stick (Move) + interact button, shown only when a touchscreen exists; `InputInstaller`
  installed by Area/Hub/Arena. **Prompts are device-aware everywhere** (npc-proximity R3): the NPC
  overhead prompt ("[F] Talk" ↔ "[Y] Talk" ↔ "[Tap] Talk"), the Hub keeper/portal prompts, and the
  combat action-panel keybind labels all read `IPromptCueProvider` and re-render the instant the
  player switches device. Dev overlay gains an **Input section** (active source + per-action cues,
  gaps marked). Tests: catalog/coverage/classifier/cue-provider suites + an asset drift guard
  (`InputActionsAssetConsistencyTests`) + dev-overlay section tests — 27 new/updated green, full
  1000+-file game compile clean.

- **Combat status effects — the whole Track S (S1 core · S2 legibility · S3/P3-13 one modifier
  model)** [new `combat-status-effects.md` · ability-subsystem R15/R25/R26 ·
  combat-round-and-telegraph R6 · arena-mode §2.5] (brief `combat-status-effects.md`): combat gains
  its persistent condition layer — a **status** is a data-authored condition (new
  `StatusEffectDefinition` fields + `Resources/Combat/StatusEffects/` catalog) that applies through
  the existing ability targeting, resolves at the ONE deterministic round-end point (PvE = Arena,
  owner call: "end of the afflicted unit's turn" ≡ end of round in this phase-based model), and
  expires visibly. Per-status **stack rules** (`StackRule`: refresh default / stack-to-cap —
  duration refreshes even at cap / ignore); **control kinds** (`ControlKind`: stun = loses the turn,
  root = pinned but acts, slow = −penalty·stacks movement) enforced through the new
  `MovementRange.EffectiveFor` single move-budget source (validator + rules + all AIs + committed
  moves fizzle under a post-commit root/slow); **S2**: per-unit world-space status row
  (`UnitStatusIconsView`/`UnitStatusIconsPresenter`, glyph + remaining turns, ×N stacks, both
  scenes), the mutation card's effect badge (`StatusBadge` on `MutationAbilityIcon.prefab`,
  `glyph_untyped` neutral mark when no status) and the shared "Applies: …" line in the
  ability-preview popover (`StatusEffectPreviewText`); **S3/P3-13**: ONE modifier model —
  `DataDrivenModifierEffect` now a flat signed `Magnitude` × `StatTarget`
  (OutgoingDamage/IncomingDamage), consumed two-sided in `CalculateFinalDamage` (integer-only,
  lockstep-safe); a part passive is the same asset at infinite duration (Hardened ships timed AND
  as `Passive_HardenedHide`). Content: 10 starter statuses (Burn/Poison/Bleed/Stun/Root/Slow/
  Weakened/Empowered/Hardened/Regen) + 11 placeholder glyphs + 8 demo abilities/passives wired
  into the demo parts and `TestEnemyDefinition`. **1666/1666 EditMode green via the clone-batch
  run.** ⚠ Awaiting owner gameplay pass (on-screen feel).

### Changed
- **Enemy decision-maker creation path (Combat · Arena)** [combat-enemy-ai]:
  `EnemyCombatIntegrator.CreateDecisionMaker` builds Tactical enemies through the injected
  `AIDecisionMakerFactory` (profile mapped via `AIProfileMapper`, no-profile enemies get the sharp
  `AIBehaviorProfile.Default`); `ArenaSceneEntrypoint.SeatOfflinePlayers` seats dummies with the
  same factory. `SimpleRandomAI` unchanged. `Resources/Enemies/AIProfiles/TestAIProfile.asset`
  body updated (drops `_killThresholdPercent`, adds the seven new dials at default values,
  **`_basePersonality` SimpleRandom → Tactical** — the profile's personality routes an enemy into
  the simulation AI, so the old value left every test enemy on random actions; GUID/meta
  untouched). `AreaInstaller`/`ArenaInstaller` gain a `_difficulty` inspector field.

- **A1 · Combat core reconvergence — one shared round engine** [Combat · Arena ·
  `combat-round-and-telegraph.md` §2.1/§2.3/§2.5/§5 · `arena-mode.md` §2.3/§5 ·
  `combat-status-effects.md` R3] (W2 audit F1 P0 / `audits/W6.md` Theme R1): `ArenaCombatController`
  was a near-verbatim copy of the PvE `CombatController` (35 consumers of `ICombatController`) — every
  combat-core change had to be written and unit-tested twice or the Arena silently diverged. Extracted
  the shared round/state core into one pure-C# **`CombatRoundEngine`** (unit-list mutation, battlefield
  lifecycle, phase sequencing, the resolve loop, the end-of-round lifecycle, and the win check), with
  the three divergence axes behind an injected **`ICombatRoundFlow`**: **`PveCombatFlow`** (initiator
  lead + player/enemy interleave, local `EnemyIntentPlanner` intents, execute-locally actions,
  eliminate-all-enemies win) and **`ArenaCombatFlow`** (host gather + hidden planning, the
  execute-or-intercept-into-`ArenaCommit` action path, the bundle→`IArenaResolutionOrder` resolution
  source, the end-of-round `ArenaStateHash` publish, last-hero-standing win). Both controllers are now
  thin `ICombatController` adapters that internally compose the engine + their flow; **constructor
  signatures, DI bindings (`AreaInstaller`/`ArenaInstaller`/`CombatControllerFactory`), and the
  Arena-only surface (`SetHostRole`/`ArmWinCondition`/`LastRoundHash`) are unchanged**, so no consumer,
  installer, or existing test moved. The shared win-check now guards against a second `OnGameEnded`
  firing once the game is over (previously only Arena did). Behaviour otherwise preserved — round order,
  phase sequencing, win semantics, and the Arena lockstep hash are byte-for-byte identical.
  Gates **Track X** (online robustness sits on this seam) and **Track K / P2-4** (combat depth).
  New `CombatRoundEngineTests` covers the extracted core directly (closing the gap that the PvE
  controller had no unit test — play-mode-only). **1674/1674 EditMode green** via the clone-project
  batch run (incl. `ArenaLockstepTests`/`ArenaRoundFlowTests`/`ArenaEdgeCaseTests` determinism as the
  safety net). `CombatActiveState` bootstrap split (F5) + unit-spawn unification (D6) stay separate.

- **`PCInputController` → `CombatInputController` (Combat)** [input-foundation R3]: the combat
  gesture state machine is preserved tap/hold-identical, but the device seam now polls the shared
  actions (so every gesture works on gamepad out of the box) and aim direction moved behind
  `IAimDirectionResolver` — cursor→ground raycast on pointer, left stick in camera space on gamepad,
  selected by the active source. `InputConfig` slimmed to gesture thresholds only
  (`volleyAimHoldThresholdSeconds`, `inputDeadzone`) — all dead KeyCode/axis/platform fields removed
  from the SO and `Resources/Configs/InputConfig.asset`. `NpcInteractionInput` is now a plain class
  polling the shared Interact action instead of a MonoBehaviour reading `Keyboard.current.fKey`.

### Removed
- **`TacticalAI` and `ConfigurableTacticalAI` (Combat)**: the flat-scoring tactical AIs are
  deleted — `SimulationTacticalAI` with the default profile subsumes both (three near-duplicate
  tactical brains violated KISS). `AIProfileDefinition._killThresholdPercent` removed (dead field —
  never read; kill detection is simulation-driven now).

### Fixed
- **PvE enemies no longer treat each other as targets (Combat)** [combat-enemy-ai R7]: each enemy
  owns a separate `AIPlayer` (distinct `Owner.Id`), so the old `Owner.Id != unit.Owner.Id`
  hostility check scored fellow enemies as victims. Hostility is now the injected
  `TeamHostilityPolicy` (AI side vs everyone else) in PvE, `FreeForAllHostilityPolicy` in the
  Arena (where everyone-vs-everyone is correct).
- **Enemies no longer fire blanks or default to the first facing (Combat)** [combat-enemy-ai R1]:
  ability candidates that hit no one now score below ending the turn, and facing selection is
  outcome-driven — the pre-existing "all six facings score equally, ties break toward the first
  direction" limitation is gone.

- **`JoystickInputController` and `MobileInputController` (Combat)**: the never-bound gamepad
  controller and the stubbed mobile controller are deleted — their intent is absorbed by the merged
  binding model (all devices live on one controller) and the touch overlay; the installers' hardcoded
  "TODO: platform detection" is gone with them.

### Fixed
- **Inventory Escape-close was silently dead** [inventory R4]: `InventoryHud.prefab`'s
  `_cancelAction` was `{fileID: 0}` (never wired), so only the close button worked. Now points at
  the shared asset's `UI/Cancel` (Escape / right-click / gamepad east), discovered via
  binary2text on the Library artifact so the sub-asset fileID is exact.
- **A hybrid ability's status application silently undid its own damage** [ability-subsystem R14]:
  `AbilityExecutor.ApplyStatusEffect` rebuilt the unit from the caller's pre-damage reference, so
  a damage+status ability restored the target's HP while attaching the status. Now re-reads the
  live unit from the state. Caught by the new Arena lockstep status test.
- **Status-ability duration override never applied** [ability-subsystem]: `AbilityFactory` computed
  `_durationOverride` into the display field but built `EffectToApply` with the status default —
  the applied instance now carries the authored duration (status + hybrid arms).
- **A DoT kill at round end was only caught a frame later** [combat-round-and-telegraph R6 ·
  arena-mode §2.5]: both `EndRound()`s now run an explicit win check right after the round-end
  status tick, so a decided fight never opens another round (and Arena settles it identically on
  every peer).
- **Hub portals collapsing into one (O1)** [hub]: the three biome portals sat on a narrow 60°
  arc at a small radius and each snapped **independently** to the nearest cell, so two adjacent
  portals landed on the **same** cell (only two portals reachable). Fix: widen the arc
  (−5°/45°/95°) + larger radius, and — the guarantee — each placed spot (keeper + portals) now
  **reserves its cell** so `HubSceneEntrypoint` snaps every next spot to the nearest unblocked
  **and unclaimed** cell. `HubProximityPresenter`/domain unchanged.

### Removed
- **Legacy hardcoded status effects** [combat]: `StunEffect` / `PoisonEffect` /
  `RegenerationEffect` and `RoundLifecycleProcessor.ApplyLegacyStatusEffects` deleted — one
  data-driven code path (`Unit.ActionState` now reads `DataDrivenControlEffect`;
  `PoisonStrikeAbility` re-payloaded onto the data-driven DoT). The dead
  `AreaInstaller._statusEffectDefinitions` inspector field is gone too (superseded by the
  `StatusEffectDefinitionCatalog` Resources load).

### Changed
- **Brew gravity settle is now column-scoped (Track F · F3, FR4 revision 2026-07-07)**
  [inventory-subsystem R10/R31] (brief `cauldron-brew-layout-and-physics.md`, revised section):
  the first settle implementation re-packed **all** survivors onto the lowest spots, which slid
  same-row neighbours sideways — exactly the "arbitrary re-sort" the revised brief forbids. Now the
  lattice rows share one X grid so spots form **vertical columns** (`BrewSpot.Row`/`Column`; the
  brick offset is gone — the ≥ diameter × margin spacing keeps towers overlap-free), and
  `BrewLayoutModel` settles **only the removed bubble's column** (`SettleColumn`): the bubbles that
  were resting above it fall one place each, everything **beside and below stays put** — like
  pulling a marble from a jar. Insertion (lowest free spot), the fill-level read, the view's
  spring glide, and determinism are unchanged; `BrewLatticeTests`/`BrewLayoutModelTests` now pin
  column alignment, per-column row contiguity, and the beside-stays-put acceptance.
- **Quest-as-Reward economy — the whole Track H (P1-5 rolled reward + P0-3·b belonging · P1-6 offer
  card · P1-7 Monster verb · P1-8 competing fork · P1-9 several offers · P1-10 cauldron barks · P1-11
  quest log)** [quest-subsystem · encounter-dialogue-ui · loot-subsystem · mutation-subsystem · new
  `cauldron-barks.md`] (briefs `quest-reward-rolled.md`, `quest-offer-card.md`,
  `multiple-and-competing-offers.md`, `attack-card-monster-verb.md`, `cauldron-voice-barks.md`,
  `quest-log-and-saga.md`): a quest *offer* now lands as a prize, not a wall of obligation — the
  reward is declared and rolled, the card telegraphs it, and the world remembers the choices.
  **⚠ Code-complete + edit-mode green (1605/1605 via the clone-batch run) but NOT yet play-tested by
  the owner** — the on-screen feel (offer-card mystery slot, quest-log panel, cauldron barks) is
  unconfirmed in play mode; the behaviour is implemented, the presentation awaits a gameplay pass.
  - **P1-5 + P0-3·b — rolled reward (`quest-subsystem.md` R7, §2.4/§2.5):** `QuestRewardCore` is now
    a **declaration** — `(tier, belonging, payload kind)`, never an item id. A pure `QuestRewardRoller`
    (Loot.Core) rolls a concrete artifact or Part-Blank on completion, deterministic under
    `(runSeed, questId:index)`; **belonging is a hard filter** (an artifact's reward-family, a blank's
    race — the card's colour can't promise a family the roll won't deliver), **tier is a bias** that
    degrades to the nearest authored tier. `QuestRewardGranter` routes by kind (artifact → inventory,
    blank → rack; a full rack forfeits the blank, never bypassing the cap; an empty pool warns + marks
    paid). New SO `RewardFamilyDefinition` (power/utility, `Resources/Rewards/`) + `BelongingTintCatalog`
    merging race and family colours; new fields `ArtifactDefinition._rewardFamilyId`,
    `PartBlankDefinition._raceId`. All 4 demo quests re-authored to declarations.
  - **P1-6 — offer card (`encounter-dialogue-ui.md` R10/R11):** a quest card that declares a reward now
    shows a **mystery reward slot** — the item hidden as `?`, the slot **glowing by tier**, a chip
    **tinted by the belonging colour**; **hover-inspect** swaps the summary for the job detail
    (objectives + giver) while the reward stays hidden. `EncounterCardView.prefab` gains the reward slot.
  - **P1-7 — the Monster verb (`quest-subsystem.md` R11, §2.6):** a talkable NPC dying in a
    dialogue-routed fight (attack card **or** self-initiation, one path via `DialogueRunner.OnCombatResolved`)
    **forecloses its thread** (`ThreadRetirementReason.Foreclosed` + indicator fact, no beat), writes
    `actor.<id>.slain` (the planner never recasts a slain actor) and increments `world.path_conquest`.
    Corpse-loot stays on the separate combat channel.
  - **P1-8 / P1-9 — the two offer shapes:** Shape A (several Quest slots on one story, resolved by
    `offer-quest: <tag>` on choice + branch; same tier, different belonging) and Shape B (two offers on
    opposed threads — committing to one fails the other on fact-conflict via the shipped maintenance).
    Demo: the frog elder's two-offer hand; the barn `barn_raid` vs new `raider_pact` fork.
  - **P1-10 — cauldron barks (new `cauldron-barks.md`):** the live reactive voice — slots (temptation ·
    dark offer/attack · restraint · socketing trend) × path lean (`path_conquest` vs `path_restraint`,
    **no new meter**), deterministic under the run seed, data-authored in one `CauldronBarkLinesConfig`;
    fired from the mutation/encounter presenters + the `ISocketingTrendSource` seam.
  - **P1-11 — quest log (`quest-subsystem.md` R12, §2.7):** a read-only **J**-toggled panel projecting
    the live registry + thread ledger — quests grouped into **sagas** by thread with the lifecycle
    verdict (live / completed / failed-by-conflict / foreclosed / expired), each showing state +
    objectives + giver + reward **tier + belonging only** (the rolled item never named). Quest origin
    (giver + thread) added to `QuestInstance` + the save snapshot.
  - New pure-C# Core is UnityEngine-free and edit-mode tested: `QuestRewardRollerTests`,
    `QuestRewardGranterTests` (rewritten), `MultipleOffersTests`, `CompetingOffersForkTests`,
    `MonsterVerbConsequencesTests`, `QuestLogModelBuilderTests`, `CauldronBarkServiceTests`, plus a
    `RunWindowPlannerTests` slain-actor case. Fixed a pre-existing `DressingKitMapperTests` reflection
    helper (didn't see the base-class `_kitId` since Track E) surfaced by the full run.
  - **Deferred (unchanged):** P1-12 non-item reward sinks (currency/XP/ability) — no currency model yet.
- **The Cauldron View — the whole Track F (F1 spatial spine · F2 liquid & fullness · F3 stable brew &
  event physics · F4 medallion socket UI · F5 stomach backdrop)** [inventory-subsystem ·
  mutation-subsystem] (briefs `cauldron-view-spatial-spine.md`, `cauldron-liquid-and-fullness.md`,
  `cauldron-brew-layout-and-physics.md`, `medallion-socket-ui.md`; absorbs **P1-3** medallion UI +
  **P5-5** stomach backdrop): the pot/inventory look-and-feel carved into one presentation pass over
  the shipped inventory/crafting/socketing — **no mechanic or outcome change** except the two
  interaction beats noted below.
  - **F1 — spatial spine + bubble rule (`inventory-subsystem.md` R27–R29):** the view now reads as a
    vertical **crafting-top · brew-centre · medallion-ribbon-bottom** stack. One rule — **bubble =
    suspended in the liquid**: artifacts in the crafting slots and the hovering result render **bare**
    (`BubbleView.SetShellVisible(false)`), only pot artifacts wear a bubble. The socketing rack moved
    from **left of the cauldron** to a **horizontal ribbon under it** (`InventoryStage.prefab`
    `MedallionRibbon`, anchors spread along X), anchored to the frame so it never jumps with the fill
    level.
  - **F2 — liquid you can feel + fullness (R30):** `M_PotLiquid` is now translucent; the surface is a
    full disc at the session's fill height, the pot's open wedge is filled by a **cut-away liquid
    curtain** shaped to the bowl's inner profile (`LiquidBandMeshBuilder`) so submerged bubbles read
    *through* the liquid, and a **bright waterline band** (`M_PotWaterline`) marks the meniscus. The
    **fill level is a function of the artifact count** (`LiquidFillCalculator`, between a configured
    min/max), **snapshotted on open** and held for the session, and **always sits above the topmost
    bubble**. The cauldron mesh itself is never resized.
  - **F3 — stable brew layout + event physics + continuous result flow (R10 rewrite, R19 rewrite,
    R31):** artifacts hold **stable spots** on a deterministic bottom-up lattice
    (`BrewSpotLatticeBuilder` → `BrewLayoutModel`) — adding one takes the lowest free spot, so
    untouched bubbles don't reshuffle. A drop-in **splashes** and nudges neighbours (a damped spring
    that settles back), a removal gives a small bob. **Continuous result flow:** starting the next
    combine while a result hovers **auto-commits it into the brew** (`CraftingSession.TrySelect`
    collects the pending result first) — retires R19's "new selections rejected until the result is
    collected".
  - **F4 — medallion socket UI (`mutation-subsystem.md` R4/R6 rewrite, §2.3):** a racked blank now
    renders as a **medallion** — the part centred on a species-tinted disc, its sockets as **gems
    around the rim**, the **rim a progress ring** that closes as sockets fill
    (`MedallionMeshBuilder`, `BlankEntryView` reworked). **Confirm-before-unseal:** the completed
    medallion pulses and shows "Unseal"; the **click is the commit** (`IBlankRackView.OnUnsealClicked`
    → `MutationVariantPresenter`), replacing the old auto-open on the last socket. Re-slotting is now
    free **until** that confirm — `SocketingModel.TryUnsocket` no longer rejects a full blank.
  - **F5 — stomach backdrop seam:** a screen-space `StomachBackdrop` quad behind the diorama with a
    new muted `M_StomachBackdrop` material (no scene swap); the art is a designer asset swapped by
    repointing the material/texture.
  - **Cauldron enlarged + gravity settling (owner revision, same pass):** the bowl grew (radius
    0.5→0.68, depth 0.5→0.6; ~7→~18 submerged bubbles) so a full inventory fits without spilling over
    the rim, with the liquid/steam/crafting anchors and camera tilt re-seated. Removing a lower
    bubble now settles the gap downward, and the view glides the fallers there via the same spring —
    replacing the original brief's "a removal never moves the others" (settle scope refined below).
  - Retired `BubbleLayoutCalculator`/`BubbleLayoutSettings` (phyllotaxis spiral) and its tests,
    superseded by the stable lattice. New Core types are UnityEngine-free and edit-mode tested
    (`BrewLatticeTests`, `BrewLayoutModelTests`, `LiquidFillCalculatorTests`; `CraftingSessionTests`
    and `SocketingModelTests` updated for the two new beats; `MutationVariantPresenterTests` driven by
    the unseal confirm).
- **Environment Dressing — the whole Track E (E1 kit contract · E2 biome kits · E3 site/camp kits ·
  E4 backdrop scatter · E5 footprint/edge-fit)** [environment-dressing · platform-generation ·
  world-landscape · world-sites] (briefs `dressing-kit-binding-and-swap.md`,
  `biome-decoration-kits-demo.md`, `site-camp-dressing-kits-demo.md`, `world-backdrop-fill-demo.md`,
  `decoration-footprint-and-edge-fit.md`; builds the `biome-visual-styles.md` P1-2 placement model;
  new system doc `environment-dressing.md`): the world's decoration layer behind one **swappable
  dressing-kit contract**.
  - **E1 — the contract (R1–R6):** data-only kit SOs (`DressingKitDefinition` base +
    `BiomeFeatureKitDefinition` + `SiteDressingKitDefinition`; a backdrop kind is one more
    subclass later), **whole-kit binding** (`BiomeAppearanceDefinition._featureKit`;
    `SiteStamp.DressingThemeId` → kit — the world-sites seam is now live), **demo quarantine**
    (`Resources/World/Dressing/Demo/`; a living guard test asserts no pack asset is referenced
    outside it), **bind-time tone treatment** (`ToneMaterialCache`: every kit material rebuilt on
    URP Lit, albedo lerped to the biome's muted `_toneTint` — pack Standard materials never render
    raw/magenta), and the **fail-safe base layer** (no/unknown/broken kit → `Empty` plan, warn
    once, never a generation failure). Wiring: `AreaInstaller.InstallDressingBindings()` →
    `IEnvironmentDressingPlanner` + `IEnvironmentDressingSpawner` threaded through
    `AreaGenerator` as optional ctor seams (the `landmarkSpawner` pattern).
  - **E2 — biome features (R7–R11):** the `PlatformHexSurface.BlockedCells` seam went **live**
    (ctor-supplied + `WithBlockedCells`; `SurfaceHexGrid` + `PlatformAnchor` exclude blocked
    cells — movement, targeting, spawns, and landings honour obstacles by construction). Pure
    `BiomeFeaturePlanner`: sparse whole-cell **blockers** (per-100-cells density, spacing ≥ 2,
    battlefield-minimum cap, lane + center protection, connectivity BFS) + **homogeneous
    decorative clusters** (copse/outcrop/tuft patch, 2–5 members, several per cell, no cell
    consumption) with rim/rear (+Z) bias. Demo kits authored: **Desert** (Low Poly Desert
    Environment rocks/cacti + dry tufts, sand ground) and **Forest** (RPG Poly Pack trees/bushes/
    rocks/tufts/flowers, grass ground); Mountain/Cave stay base-layer by data.
  - **E3 — site & camp dressing (R12–R16):** pure `SiteDressingPlanner` reads the persisted
    `SiteStamp` — settlement structures placed rear-third on a **block-shared skyline band**
    with shared scale (instance-seeded), fronts to the camera; **gate** on the anchor platform's
    approach edge; **camp** = focal fire (owner-picked `rpgpp_lt_hanger_wood_02` + stones + log)
    on one blocked rear-center cell with a facing **prop ring**; site ground overlay wins over
    biome ground. Demo kits: **Settlement** (`settlement-kit` — buildings 01–05, banner+fence
    gate, crates/barrels/well/wagon, street ground; Village + City re-keyed to share it) and
    **Camp** (`camp-kit` — awnings/sacks/barrels/crates, packed-dirt ground). Site dressing
    replaces biome features on site platforms; all placements platform-local (no gap-crossing
    possible). Determinism rides `biome-features:{nodeId}` / `site-dressing:{instanceId}(:{index})`
    seed streams — restore replays identical dressing, **no save-format change**.
  - **Decoration footprint & edge-fit (Track E polish, R17–R19)** (brief
    `product-requirements/decoration-footprint-and-edge-fit.md`): wide props no longer spill past
    the platform edge. Every feature entry carries a **rough footprint radius** (kind defaults
    0.35/0.9/1.0 + optional `_footprintOverride`); cluster members are **deterministically nudged
    inward** to fit fully within the silhouette (new pure `PlatformEdgeFit` —
    signed-clearance-to-outline + inward nudge; skip only when genuinely unfittable), blocking
    obstacles skip a cell their footprint doesn't clear. The **rim-framing overhang became
    opt-in**: only `_mayOverhang`-flagged entries (demo: the desert `Tree_01`) anchor near the rim
    and lean past the edge, with tighter member jitter so the base keeps its foothold. Density,
    clustering, dials, and determinism unchanged.
  - **E4 — world backdrop fill (R20–R22)** (brief `world-backdrop-fill-demo.md`): the distant
    hazed horizon behind the field, as the **third kit kind** — `BackdropKitDefinition` bound
    whole-kit from `BiomeAppearanceDefinition._backdropKit` (the contract's additive backdrop
    subclass, exactly as E1 foretold). Pure `BackdropScatterPlanner`: the world X axis is sliced
    into fixed slots, each drawn from a private `backdrop-scatter:{slot}` stream so contiguous
    streaming windows fill every slot exactly once (same seed → same horizon); low density
    (`_backdropScatterPer100Units`), a depth band behind the corridor and in front of the ridge
    rig. `BackdropScatterSpawner` places the silhouettes **world-fixed** (real parallax, unlike
    the hero-anchored ridges) along the **camera's yawed depth axis** dropped by `depth·tan(pitch)`
    (the same ortho compensation the ridge rig applies), washed heavily toward the biome haze tint
    (new `ToneMaterialCache.GetHazed` — the quietest layer), colliders stripped (never walkable).
    Rides the `AreaGenerator` landmark-scan hook (`IBackdropScatterSpawner.Fill` over the same
    half-open span). Demo kits: **Desert** (cliffs as mesas + big rocks + cactus cluster),
    **Forest** (hills + mountain + tree-clumps + crag); Mountain/Cave bind none (ridge rig only).
  - **Fixed (E5 extended to site kits, playtest): camp rings & gates no longer spill over the
    edge.** Site placements had no edge-fit — the camp's prop ring and the anchor's gate pieces
    could hang past the silhouette. Now small props and gate pieces are **nudged inward**
    (`PlatformEdgeFit`) like biome decoration, and structure/focal **cells are chosen with an
    edge-clearance margin** (wide houses and the fire's ring move a ring inward), with a fall-back
    to the plain rear band when a blob is too small. Also tightened the `_mayOverhang` rim anchor
    to sit near the walkable outline (the rim strip droops below Y = 0, so a mid-rim anchor floated
    above the slope).
  - **Tests:** 46 dressing tests green headless (`BiomeFeaturePlannerTests`,
    `SiteDressingPlannerTests` incl. the silhouette-fit invariant, `EnvironmentDressingPlannerTests`,
    `PlatformHexSurfaceBlockedCellsTests`, `PlatformEdgeFitTests`, `BackdropScatterPlannerTests`) +
    in-editor `DressingKitMapperTests` and the `DemoPackQuarantineTests` clean-swap guard;
    hexsurface (32) + combat (55) regressions green.
  - **Fixed (same pass, playtest): platform ground now actually reads as the biome.** Two defects
    kept the platform texture unchanged: the legacy per-platform **debug color variation**
    (`colorVariation`, on in `Area.unity`) created a material instance and overwrote its color
    with a rainbow HSV tint — stomping the kit ground; and a planner collapsed a **featureless
    platform's plan to `Empty`**, losing the ground with it. Now a dressed ground suppresses the
    debug tint (and the Area scene ships with `colorVariation: 0`), and a bound kit always yields
    a kit-carrying plan — biome sand/grass and site street/dirt grounds apply to every platform,
    features or not.
- **The Hub — staging scene, start-of-run choices & death return (O1)** [hub · persistence ·
  biome-journey · character-system · main-menu] (brief
  `product-requirements/hub-staging-and-launch.md`, consumed with the 2026-07-06 owner revisions:
  derived tasted-pool offer instead of 3 fixed organs · bare launch always allowed · all three
  homelands authored at tier 1; new system doc `hub-staging.md`): the **Junkyard Hub** is a real
  scene for the first time — Journey stages there, the run launches from it, and **death returns
  to it** (the Hades reform point; no game-over screen). **Reworked the same day (owner change)
  from a button screen into a walkable world**: the Hub is one regular hex platform in its own
  biome look, the picks are walk-up F-interactions.
  - **Hub world:** authored `Scenes/Hub.unity` (SceneContext = new `HubInstaller` +
    `CharacterSystemInstaller`; camera + light + canvas + the free-walking scene hero) whose
    world is built at boot — `HubPlatformBuilder` (the `ArenaPlatformBuilder` treatment: shared
    shape profile, fixed seed from the new **`HubSceneConfig`** SO, mesh + walkable colliders +
    perimeter walls, ground material = the config's "own biome" look), the **junk-keeper NPC**
    (placeholder humanoid assembly, slightly left of the platform centre) and one **labelled
    portal disc per homeland** on the far arc, each with a billboarded overhead name + F prompt
    (`HubOverheadLabelView`); `HubSceneEntrypoint` assembles it, `HubProximityPresenter` +
    pure `HubProximity` (nearest in-radius spot) drive the prompts and the F key
    (`IInteractionInput`, the NPC-interaction input). Presenter pairs:
    `HubStagingPresenter`/`HubStagingView` (chosen-part readout) and
    `CauldronVoicePresenter`/`HubVoicePlaqueView`.
  - **Starting-part offer (R3–R5):** new pure-C# `StartingPartSelector` — a deterministic,
    variety-greedy (unseen race > unseen slot > flavored > plain) draw of up to 3 cards from the
    **tasted-forms pool** (`ArenaTastedCatalogReader` reused via `HubStartingPoolSource`;
    base-skeleton fits only), salted by the upcoming run's index (`world.run_count` + 1);
    **dealt by talking to the keeper** (F) on the **shared mutation card panel** with
    race-belonging tints, the mini-model preview and the ability popover — re-openable, the
    pick can be revised until launch. Picking is optional — **bare launch is always allowed**;
    the cold start (nothing tasted) is the same state.
  - **Entry-homeland choice (R6–R7):** walking into a **portal** + F (the prompt is the biome's
    name) launches into that homeland — independent of the part's race (cross allowed, no soft
    hint yet); `BiomeJourney` gained an optional starting-theme override that forces **only the
    window-0 stretch** (burn-the-draw — the entry stretch keeps its seeded length; fail-safe on
    an unhonorable theme); the climb stays the shipped seeded pool. The portal launch is
    guarded (one commit per visit).
  - **Cross-scene carriers (disk, no ProjectContext):** new one-shot `run-setup.json`
    (`RunSetupSnapshot`/`IRunSetupStore`, consume-on-read at the Area boot) and
    `hub-arrival.json` (`IHubArrivalStore` death-return marker); new `RunStartConditions`
    resolves restore-vs-fresh (restore wins, stale setup deleted). `RunSaveSnapshot` gained
    `StartingBiome` and bumped to **version 2** (v1 dev saves become fresh runs — accepted).
  - **Run-start install (R8):** new `StartingPartApplier` (Area, mirrors `HeroBodyRestorer`)
    installs the chosen part once after `CharacterAssembled` on fresh runs; the passport's
    1-marker "tolerated freak" and the tasted record emerge through the unchanged binders.
  - **Cauldron voice (R9):** new data-only SO **`HubVoiceLinesConfig`**
    (`Resources/Hub/HubVoiceLines`, menu `Hub → Cauldron Voice Lines`) + UnityEngine-free
    `CauldronVoiceLines`/`CauldronVoiceSelector` (stable FNV-1a pick per moment/race/run index;
    empty pools = quiet) — part-pick per race with a generic fallback, empty-offer, launch, and
    death-return lines; shaped for later absorption by the P1-10 bark channel.
  - **Death → Hub (R10):** `RunLifecycleService` extended (optional, null-tolerant
    `ISceneLoader` + `IHubArrivalStore`): Defeat = meta flush → run consume → arrival mark →
    load Hub.
  - Tests: all Hub suites green via the clone-project batch runner (new
    `StartingPartSelectorTests`, `HubPersistenceTests`, `CauldronVoiceTests`,
    `HubStagingPresenterTests`, `CauldronVoicePresenterTests`, `StartingPartApplierTests`,
    `HubProximityTests`; extended `BiomeJourneyTests`, `BiomeProgressionConfigMapperTests`,
    `RunStateServiceTests`, `RunLifecycleTests`, `MainMenuPresenterTests`).

### Changed
- **The Hub is just another biome — one normal platform, the world's camera & controls (O1
  corrective rework)** [hub · platform · camera · biomes] (brief
  `product-requirements/hub-as-a-normal-platform.md`; `hub-staging.md` re-written): the Hub's
  parallel-world implementation is gone — standing on the Hub is now indistinguishable from a
  normal world platform except in style and content.
  - **World camera**: the Area's Cinemachine rig cribbed verbatim into `Hub.unity` (orthographic
    Main Camera + Brain, IsometricCamera vcam at euler 30/45/0, distance 10, damping 1/1/1,
    tracking the hero) — the bespoke static yaw-180 perspective camera (which inverted WASD
    relative to the screen) is deleted; no `CameraService` (its combat/belly consumers never
    run here).
  - **World locomotion**: `CharacterLocomotionInstaller` added to the Hub SceneContext — run
    blend + facing now match the Area exactly.
  - **World platform**: `HubPlatformBuilder` (the ArenaPlatformBuilder copy) **deleted**; new
    `HubPlatformAssembler` is pure orchestration over the world path —
    `PlatformSurfaceGenerator` → `IEnvironmentDressingPlanner` (blockers folded before the
    mesh) → **`PlatformView`** (new standalone `Initialize(surface, topBoundary)` overload; the
    `IPlatform` overload delegates — mesh/material/collider logic in exactly one place) →
    `IEnvironmentDressingSpawner`. The Hub island reshapes once (seed path changed) — cosmetic.
  - **The Hub is a first-class biome**: `LevelTheme.Hub` appended (excluded from the run
    rotation by data — no `BiomeProgressionConfig` entry, the Cave convention); authored
    `BiomeAppearance_Hub.asset` + `Demo_BiomeFeatureKit_Hub.asset` + `Demo_Ground_Hub.mat`
    drive the toned junkyard ground and decor through the Track-E dressing chain
    (`HubInstaller.InstallHubBiomeDressing` mirrors the Area bindings with theme/seed providers
    pre-set). `HubSceneConfig` dropped `_platformMaterial` — the look is biome data now.
  - **Placement verified against the real camera**: keeper = screen-left (world −X,+Z) of the
    centre cell, portals on the screen-far arc; every F-spot snaps to the nearest unblocked
    cell (`PlatformAnchor.NearestCellWorld`).
  - Tests: all Hub/biome/journey fixtures green in the clone-project batch runner (55/55 on the
    affected fixtures; the 5 failing `DressingKitMapperTests` are the parallel dressing
    session's in-flight refactor — its `Set` reflection helper misses private base-class
    fields after `_kitId` moved to the abstract `DressingKitDefinition` — unrelated).
- **Journey routing & the new-run commit point (O1, revises the P2-2 PO note)** [main-menu ·
  persistence]: the menu's Journey now loads the **Hub** and no longer deletes the run save —
  the delete moved to the Hub's **launch**, so backing out of the Hub keeps Continue alive.
- **Biome progression re-authored + (theme, tier) dedupe (O1)** [biome-journey ·
  world-biomes]: `BiomeProgressionConfig` is now **five entries** (Forest/Desert/Mountain @
  tier 1 — the homeland/entry pool — plus Desert/Mountain @ tier 2, the shipped climb pool);
  `BiomeProgressionConfigMapper` dedupes on the **(theme, tier) pair** (post-normalization)
  instead of theme alone, making same-theme-at-several-tiers legitimate authoring.
  `run_escalation_tier` still climbs 1 → 2. Behavior change: **without** a Hub pick (direct
  editor Area play), window 0 is now a seeded pick over the three tier-1 homelands (was: always
  Forest).
- **Arena — parts draft, tasted-forms catalog & draft screen (P4-5 + G4)** [arena ·
  character-system · mutation · persistence · UI] (briefs
  `product-requirements/arena-part-draft-and-catalog.md` +
  `product-requirements/arena-draft-ui.md`; `arena-mode.md` §2.9/§3/§4 · new
  `ability-preview-popover.md` · `character-system.md` §2.3 · `save-persistence.md` §4): every
  Arena match now opens with a **snake-order parts draft** off a **shared, host-composed
  board** — the MVP's identical default hero is retired; each seat assembles the monster it
  fights with. The round loop, transport contract, resolution order, and win condition are
  untouched (presentation + a read-only catalog; the Arena budget guard holds).
  - **Draft model (pure C#, P4-5 reqs 5–10):** `ArenaDraftModel` + `ArenaSnakeOrder` +
    `ArenaDraftBoardComposer` (`Combat.Arena.Core.Draft`) — deterministic board = the authored
    **common floor** in full (duplicates are copies; viability per seat, req 6) + a seeded
    sample (`LootSeed.Derive(seed, "arena-draft-board")`) of the participants' **catalog
    union** (single-copy — real denial, req 9); snake order with the double pick at the turn;
    the full pick-legality matrix; deterministic auto-pick (lowest loadout-slot order, then
    entry id).
  - **Tasted-forms catalog (P4-5 reqs 1–4):** new Meta fact `world.<partId>.arena_tasted`
    (`MetaFact_ArenaTasted`, new `FactScope.PerPart`, `WorldFacts.ArenaTasted`) written by the
    new `TastedFormsRecorder` (Area; every carried part — equipped or dormant, any install
    path — idempotent, passive, no currency/grind) riding the existing meta flush; read in the
    Arena scene by `ArenaTastedCatalogReader` straight off `meta.json` (`PersistenceInstaller`
    now installed there; no fact-store bootstrap), frame-changers + unknown ids dropped
    (base-plan-only draft). Read-only for Arena — never feeds back into Journey.
  - **Networking (mirrors setup/commit/bundle):** four named messages —
    `yash.arena.tasted` (catalog → host on session start, `ArenaTastedCatalogSender` /
    `ArenaTastedCatalogRegistry`), `yash.arena.draftstart` (the composed board + slot loadout +
    timer), `yash.arena.draftpick` (request → host), `yash.arena.draftapplied` (canonical pick +
    piggybacked departures) — wire structs (+ `SerializeStringArray`), codec round-trips, the
    local-raise loopback trick preserved. **Lockstep by construction:** the host
    (`ArenaDraftHost`) validates requests against its own model; every replica
    (`ArenaDraftFlow`, the host's own included) advances only on applied broadcasts.
  - **Never hangs (G4 req 13):** host-only pick deadlines over the new `IArenaDraftClock` seam —
    humans get the generous soft timer (auto-pick on expiry), offline AI dummies a short pacing
    delay, departed seats fill immediately; mid-draft drops are seeded into `ArenaMatchHost`
    (new `SeedDeparted`) so round 1's bundle kills their deterministically-spawned units on
    every client.
  - **Draft screen (G4):** `ArenaDraftPresenter` (pure C#) over `ArenaDraftView`
    (`Resources/Prefabs/UI/ArenaDraftPanel`) + a procedural 3D stage (`ArenaDraftStageRig`: one
    far-offset camera → RenderTexture; slot-grouped pedestal grid of bind-pose part models with
    icon fallback; the local monster assembling live per pick) — whose-pick banner with an
    unmistakable "YOUR PICK" state, snake-order line, cosmetic countdown, dual local readout
    (3D + name/parts text), per-opponent name + parts readouts (no opponent 3D — owner call),
    remote-pick flights, click a part model (board or hero) → `ArenaPartInfoPopover` with
    ability rows and a Draft button only when legal (illegal shows its reason, req 7), and the
    "your monster" beat (Continue or auto after `_beatSeconds`). Missing prefabs degrade to a
    headless draft — never a crash.
  - **Drafted body → combat:** `ArenaHeroSpawner` now takes per-seat loadouts — abilities via
    the PvE `IPartAbilityResolver` path (actives → instances, passives → permanent effects),
    visuals via `SwapPart` on the modular rig; `HeroDefinition` keeps MaxHP + the loud
    fallback. `ArenaSceneEntrypoint.StartMatch` split into `BeginDraft` → (confirmed result) →
    `BeginCombat`; combat input stays disabled through the draft.
  - **Shared ability-preview popover** (new module `UI.AbilityPreview` + doc
    `ability-preview-popover.md`; closes `mutation-choice-cards.md` FR5's "one preview
    mechanism, two surfaces"): hover an ability icon → name + description **plus a 3D hero on
    a mock ground demonstrating the cast** — the D3 `AbilityAreaSweep` over the ability's
    shape (`AbilityPreviewShape`, pure hex math), the animator trigger when the controller has
    it, idle for a passive, text-only degrade when no hero exists. Per-scene hero seam
    `IAbilityPreviewHeroSource` (Mutation = live hero; Arena = base assembly + drafted parts —
    deliberately never the scene's ambiguous `ModularCharacterVisual`). New
    `AbilityPreviewConfig` SO + `AbilityPreviewPopover.prefab`; new `LogCategory.UI`.
  - **New SO + assets:** `ArenaDraftConfig` (slot loadout · floor stock · catalog sample size ·
    pick/AI/beat pacing · base assembly; mapper validates + warns) at
    `Resources/Arena/ArenaDraftConfig`; draft panel + part-info popover prefabs.
  - Tests: new `ArenaDraftModelTests` (14), `ArenaDraftBoardComposerTests` (10),
    `ArenaDraftWireCodecTests` (6), `ArenaDraftFlowTests` (8), `TastedFormsCatalogTests` (6),
    `AbilityPreviewShapeTests` (7 cases). **1407/1407 EditMode green** (clone batch-mode,
    2026-07-06).

### Changed
- **Mutation — ability tooltip → shared ability-preview popover** [mutation · UI]: the card
  hand's ability-icon hover now opens the shared popover (description + the hero casting) via
  the new fields on `MutationAbilityInfo`/`MutationAbilityIconViewData` (shape + animator
  trigger threaded from `AbilityDefinition`); `MutationChoicePanel.prefab`'s tooltip subtree
  removed.

### Fixed
- **Arena named messages overflowed the unfragmented MTU** [arena]: `yash.arena.draftstart`
  (the composed board, ~1.7 KB) threw `OverflowException` on a real network session —
  `NgoArenaTransport` now sends every named message **ReliableFragmentedSequenced** instead of
  `ReliableSequenced`. Also pre-empts the same latent overflow on a full 4-player round bundle;
  one shared delivery pipeline keeps all lockstep messages mutually ordered (a fragmented
  draft-start can never be overtaken by the small applied-pick that follows it).
- **8 `.meta` files from the P0-3 races session failed Unity's YAML parse on a clean import**
  [character-system · debt] (`Race_{Ibex,Fox,Lizard}.asset.meta`, `Scripts/Editor/World.meta`,
  `Scripts/World/Races{,/Core,/Data,/Integration}.meta`): missing trailing newline — surfaced
  by this change's clean-clone batch test run ("Parser Failure at line 8"); a clean checkout
  would have minted fresh GUIDs for the race assets. One byte appended to each; a repo-wide
  hygiene sweep for the ~900 other (tolerated) newline-less metas is filed on the ROADMAP.

### Removed
- **`AbilityTooltipView`** [mutation]: the text-only, mutation-local tooltip is deleted —
  superseded by the shared ability-preview popover (the "generalise into a shared tooltip"
  backlog item ships with it).
- **Director — D20 meta-scoped consumers (P3-3)** [narrative-procedural · persistence] (brief
  `product-requirements/director-meta-consumers.md`; `narrative-procedural.md` R15 + §2.6 step 2 +
  §4 recipes; `save-persistence.md` §4): the world now **remembers across runs, in the fiction** —
  the persisted meta horizon (P2-2) gets its three reading consumers on P3-1's verified lane. **PO
  decision recorded (2026-07-06): the cross-run cursor counts a reveal as *seen*, not placed** —
  within a run placed = spent (P3-1 unchanged), but a placed-and-never-entered beat returns to the
  pool after death; only a delivered reveal is gone for good.
  - **Spine cursor** — new Meta fact `world.<storyId>.spine_seen` (`MetaFact_SpineSeen`, Bool, new
    scope `FactScope.PerStory` + `WorldFacts.SpineSeen`; mirrors the `thread_retired`
    subject-parameterized pattern). Written only by the new **`SpineSeenRecorder`**
    (`Narrative.Runtime.Core`, bound beside `StoryResolutionRelay`) when a spine beat's dialogue
    ends (any outcome incl. `leave`; a mid-dialogue quit never fires, so the encounter re-begins
    unseen). `RunWindowPlanner`'s spine channel-split additionally excludes seen beats — a pure
    pool filter before the seeded pick (D21 intact); the per-run cap stays run-ledger-based, so
    past-run reveals never spend a fresh run's cap. Rides the existing meta flush/bootstrap —
    zero new persistence plumbing; a losing run's reveal sticks (defeat flushes meta first).
  - **Authored cursor gates (data-only):** because the cursor is an ordinary fact, preconditions
    read it with a **literal story-id subject** — "must have seen Y" floors and echo
    sibling-exclusion need no code (recipes in `narrative-procedural.md` §4).
  - **Mirror-lore echoes (demo):** new Meta deed flag `world.raider_pact_sworn`
    (`MetaFact_RaiderPactSworn`; set by `RaiderMotive.ink` beside the run-scoped
    `raider_offer_taken`) + the cross-excluded spine pair `DemoSpine_TyrantEcho_Conquest`
    (gated `raider_pact_sworn`) / `DemoSpine_TyrantEcho_Alliance` (gated the shipped
    `barn_bounty_honored`), both floor `run_count ≥ 3` — a conquest history yields different
    tyrant lore than an alliance one, from a bounded authored table (never generated).
  - **Cauldron memory (demo):** `DemoSpine_CauldronMemory` (floor `run_count ≥ 4` **and**
    `spine_seen(story_spine_cauldron_hint)`) — the voice's past-hosts aside deepens across runs on
    the lane's schedule, gourmand-tempter register, never states what the cauldron is (Tier-0
    mystery intact); **not** the P1-10 bark channel.
  - Graceful degrade for free: an empty/absent meta store means the gated beats are simply
    ineligible (registry defaults) and plans stay bit-identical to an echo-free pool; a corrupt
    memory quarantines (P2-2).
  - Tests: new `RunWindowPlannerSpineCursorTests` (8), `SpineSeenRecorderTests` (4),
    `SpineCursorPersistenceTests` (2). **1355/1355 EditMode green** (clone batch-mode, 2026-07-06).
  - Known limitation filed: echo sibling exclusion binds on *seen*, so with cap ≥ 2 both variants
    of a pair can place in one run before either is entered (`narrative-procedural.md` §6);
    production spine likely runs cap 1, or author distinct floors on the pair.
- **Director — spine reserved lane + per-run reveal cap (D7 / P3-1)** [narrative-procedural ·
  persistence] (brief `product-requirements/director-spine-reveal-lane.md`;
  `narrative-procedural.md` R15 + §2.6 step 2 + recipe): the curated reveal spine now delivers at
  the lore-pacing tempo — spine beats place **first by their own quota**, ≤1–2 per run, never too
  early, on won and lost runs alike, as a **gated pool, not a linear queue**.
  - `RunWindowPlanner`: `_isSpine` stories (authored since R13, consumed for the first time) are
    routed out of the quest/ambient channels into a spine pool (same gates: tier band, preconditions
    incl. the casting query, not placed/resolved, thread not retired); a pre-slot-loop lane places
    at most **one** eligible beat per window into a seeded slot while the per-run cap allows,
    bypassing the quest rarity/spacing gate and the `MaxLiveThreads` ceiling (owner-approved
    reserve-don't-compete exception — the cap bounds the load). The reserved slot still ticks the
    allocator (spacing/site-block state advances uniformly); an inactive lane draws nothing, so
    spine-free windows stay draw-identical (D21).
  - **Zero new run-state:** "revealed" = placed in the `StoryRunLedger` (already rides `run.json`),
    so the cap and never-re-reveal survive save/continue for free; cross-run persistence of the
    revealed set (the spine cursor) stays **P3-3**.
  - `RunPacingConfig`/`RunPacingSettings` gain `_maxSpineRevealsPerRun` (default **2**, `0` = lane
    off).
  - **Run counter (soft-floor input):** new meta fact `world.run_count` (`MetaFact_RunCount`, Int,
    Horizon = Meta, in `DemoFactKeyRegistry` + `WorldFacts.RunCount`) written once per **fresh**
    boot by the new `RunCounterService` (`Core.Persistence`, order −90, after `MetaMemoryBootstrap`;
    a continue never counts). A "not before run N" floor is an ordinary `Gte` precondition — no new
    subsystem (FR7).
  - **Placeholder demo beats** (real reveal lines come with the spine content):
    `DemoSpine_CauldronHint` (floor `run_count ≥ 2`) and `DemoSpine_MirrorGlimpse` (floor
    `run_count ≥ 3`) + dialogues/ink under `Resources/Narrative/…` + `Resources/Stories/Slice/`.
  - Tests: new `RunWindowPlannerSpineTests` (13: reserved placement despite a denying quest gate,
    cap 0/1/2 across windows, ≤1 per window, never-twice, restored-ledger cap, soft floor, quest-slot
    exclusion, same-seed identity, inactive-lane draw purity, tier-band/retired-thread gates, ceiling
    bypass) + `RunCounterServiceTests` (2). **1341/1341 EditMode green** (clone batch-mode,
    2026-07-06). Follow-up filed: P3-3 decides placed-vs-*seen* for the cross-run cursor.
- **Run escalation — tier gating (D19 / P3-2)** [narrative-procedural · combat] (brief
  `product-requirements/run-escalation.md`; `narrative-procedural.md` §2.6 + SO Reference): the run's
  published altitude (`run_escalation_tier`) is now **consumed** so a run *feels* like it climbs — the
  eligible content pool shifts by tier without inflating stats or density.
  - New shared **`RunTierBand`** value type (`Narrative.Director.Core`, `{MinTier, MaxTier}`, `MaxTier ≤
    0` = open upward, default `(0,0)` = every tier) + its authoring struct **`RunTierBandAuthoring`**
    (`Narrative.Director.Data`, `ToCore()`), following the `FactPredicateSerial` pattern.
  - `StoryTemplate` and `EnemyDefinition` gain a `_tierBand` field (mapped to Core via
    `StoryTemplateMapper` / `BiomeMonsterPoolMapper` → `MonsterPoolEntry.Band`). Unbanded content
    (missing field / older assets) defaults to open — **no asset migration**.
  - `RunWindowPlanner` reads `run_escalation_tier` once per window and gates **every** story's
    eligibility by its band (register shift, quest + ambient colour alike). `IBiomeMonsterPoolCatalog`
    gains tier-aware `GetPool(theme, tier)` / `GetPool(theme, flavor, tier)` overloads; the tier is
    threaded through `IWorldSlotAllocator.AllocateSlot(questAvailable, currentTier)` so the ambient
    (`WorldContentAllocator`) **and** site/camp (`SiteAwareSlotAllocator`) combat draws pull only
    in-band creatures — a fully out-of-band pool downgrades the slot to Empty.
  - **Pool-shift only:** no per-tier stat multiplier and no density modulation (both deliberately cut
    per the brief); the three registers (Backwater / Courts / Divine-Apex) are a documented tier-range
    convention over the 1-based tier, not a new type. Deterministic (band is a predicate before the
    existing seeded draw; no new RNG or run-state).
  - Tests: new `RunTierBandTests`; tier coverage added to `BiomeMonsterPoolCatalogTests`,
    `WorldContentAllocatorTests`, and `RunWindowPlannerTests`; existing allocator/restore/site suites
    updated for the new signature. **1325/1325 EditMode green** (clone batch-mode).

### Changed
- **Doc reconcile — retired `narrative-generation.md`, refreshed `loot-subsystem.md` (P6-1)** [docs]
  (CLAUDE.md §8; no code change): brought two stale docs back in line with the shipped streaming
  director.
  - `narrative-generation.md` → **tombstone**: the §1–§3 as-implemented description of the deleted
    legacy pipeline (`LevelNarrativeGenerator`/`StoryDefinition`/`NpcDefinition`/`RewardResolver`/
    `CompositeDialoguePresenter`/`NarrativeInstaller`/`ScenarioGenerator`/`PlatformGraphGenerator`) is
    replaced by a RETIRED banner pointing at `narrative-procedural.md` (as-implemented) +
    `narrative-director-requirements.md` (forward). The §4 planned-design (P1–P4) is **preserved as the
    P3-8 "residue"** with per-item status (P2/P3 superseded by density budgets + D19/passport facts; P4
    soft cooldowns + optional P1 compat-scoring = the live residue).
  - `loot-subsystem.md` → **refreshed to as-implemented**: loot-platform *presence* is decided by the
    density allocator (not a per-biome chance); quest rewards are a **fixed** `QuestRewardGranter` grant
    of `QuestRewardCore` (no roll); the deleted `RewardResolver`/`RewardDefinition`/`StoryDefinition`
    reward channel is called out as gone. Flagged the dead-no-callers `LootRollService.ShouldPlaceLootOnPlatform`
    / `RollQuestRewards` and `BiomeLootDefinition._platformLootChance` for the **P6-4** cleanup; the
    seed/`WeightedPicker`/biome-table Core and pickup runtime confirmed current.
  - `Docs/README.md` index updated (narrative-generation marked retired); ROADMAP Narrative-Generation
    and Loot sections reconciled (P6-1 checked off, moot legacy limitations struck, P3-8 residue statuses recorded).

### Fixed
- **Bandit camp D1 — first-playtest fixes (capsules · enemy radii · camp frequency)** [combat +
  npc-interaction + world-sites] (PO playtest 2026-07-05; docs updated in place):
  (1) **Capsules still spawning** — the legacy `ContentSpawner` prefab path double-spawned a capsule
  (and an NPC placeholder) over the real humanoid bodies on every platform entry; its Enemy/Npc
  cases are retired (same pattern as the earlier Loot retirement). `EnemyVisualSpawner` additionally
  falls back to the **default humanoid assembly** (`PlaceholderAssembly_A`) for any enemy without an
  authored one, and applies a shared **demo enemy red** when no tint is authored — **capsules and
  untinted (blue) enemies no longer exist**.
  (2) **Combat started by landing; enemies had no radius** — landing now NEVER starts a fight for
  any content (PO decision, extends brief req 10 beyond camps): `CombatAutoStartRule` re-enters
  combat only for an engaged platform (`EnemyContent.Engaged`, latched by every engagement path —
  enemy radius / boss circle / dialogue combat — so a re-landing resumes a begun battle). Lone
  ambient monsters now register **hostile proximity handles** of their own
  (`INpcInteractionService.BindEnemy` from `CombatIdleState`): name + `!` overhead, global aggro
  radius, platform-scoped; crossing one engages the whole platform's fight and consumes the sibling
  handles. Camp crews behind a boss are never bound — the boss's circle owns the trigger
  (`npc-proximity-interaction.md` R2/R13).
  (3) **Camps effectively unreachable** — ambient sites rolled ~1 per 14 platforms and the Camp
  competed 3 : 3 : 3 with Ruin/Lair (≈1 camp per 42 platforms). Tuned for the demo:
  `WorldContentDensityConfig` `_averagePlatformsPerAmbientSite` 14→8, `_minPlatformsBetweenSites`
  6→4, and `Camp._triggerWeight` 3→6 (≈1 camp per ~16 platforms; ruins/lairs stay rarer).
  `TestEnemyDefinition` renamed to **Wild Beast** + given the enemy red tint.
  Updated `CombatAutoStartRuleTests` to the Engaged semantics — full suite 1290/1290 green via the
  clone-project batch runner.

### Added
- **Combat ability animation + animated ghost + move arrow + enemy readiness cue — D3 of Track D
  "Bandit Camp & Combat Legibility II"** (verified brief `product-requirements/combat-ability-animation.md`;
  docs: `combat-round-and-telegraph.md` R15–R18):
  - **Placeholder cell-sweep animation.** A code-authored motion that spans an ability's whole affected
    area — each struck cell flashes/pops, swept outward along the caster's line for a **Line** and
    together for a **Ring** (`AbilityCellFlash` + `AbilityAreaSweep`, shape-driven; `_animationTrigger`
    stays the real-clip seam). Played **translucent** as the ghost preview (now *moving* — extended
    `GhostPlaybackPlan`/`Builder`/`View` with the affected-cell positions) and **opaque** on live
    execution.
  - **Live playback for player + enemy.** The shared executor (`AbilityExecutor.ExecuteAbilityAtCells`)
    emits a read-only `AbilityFiredCue` via an `[InjectOptional] IAbilityFiredSink` (mirrors
    `ICombatOutcomeRelay`); `LiveAbilityAnimationView` plays the sweep. Player-queue and enemy-resolve
    both flow through it, so an **enemy action now animates within its paced beat** (visibly not
    instant). No change to outcomes, cells, damage, or timing.
  - **Enemy move-direction arrow.** A **short** board `LineRenderer` pointer from the hex centre toward
    the shared edge in the move direction (half the centre-to-centre distance) replaces the overhead `»`
    glyph (`EnemyIntentTelegraphView`, driven by the pure `EnemyIntentTelegraphPresenter`).
  - **Enemy readiness cue.** An enemy holding a committed intent reads as "armed": its plan icons go
    **restless** (jitter/pulse in `UnitOverheadIconsView`) **and** it holds a placeholder **wind-up pose**
    (a transform lean/scale/bob on the visual root — the rig has no attack state). The telegraph shows
    during the **plan** phase and **clears when the round enters `EnemyResolve`** (the enemy acts/moves);
    the pose is applied as an additive offset (never an absolute position), so it never pins or fights the
    enemy's movement. Enemies only, uniform.
  - PvE (Area) only for the wired views (matching D2); the animated-ghost + no-move-glyph changes live in
    the shared views. New edit-mode suites `AbilityFiredCueTests`, `EnemyIntentTelegraphPresenterTests`,
    plus extended `GhostPlaybackPlanTests` / updated `UnitPlanIconsPresenterTests` — full suite
    1310/1310 green via the clone-project batch runner.
- **Combat initiative + turn-order strip + aim/fire input — D2 of Track D "Bandit Camp & Combat
  Legibility II"** (verified brief `product-requirements/combat-initiative-and-turn-queue.md`; docs:
  `combat-round-and-telegraph.md` R4/R4a/R4b, `ability-subsystem.md` R8):
  - **Initiative — the initiator leads the opening round.** A new `CombatInitiator` (`Player`/`Enemy`)
    is captured at the three engagement sites (defaulting to `Enemy`): the encounter's Attack card
    (system **or** a `card: attack` Ink choice, via a one-shot player-combat latch on `DialogueRunner`)
    reads `Player`; a plain `start-combat:` tag (an NPC turning hostile) and an ambush/aggro cross read
    `Enemy`. It rides `EnemyContent.Initiator` → `CombatActiveState` → `ICombatController.Initialize`,
    and `CombatController.StartRound` reorders the opening round so an enemy-initiated fight resolves
    committed enemy intents **before** the player's Act phase (`RoundLeadPolicy.EnemyLeadsThisRound`,
    round 1 only). Deterministic: same seed + same initiator → same order.
  - **Turn-order strip** — a code-built screen-space overlay (top-right `HorizontalLayoutGroup`,
    no new prefab) listing the round's actors in order, leader-first per the initiative, with the
    current / already-acted side marked and dead units dropped (`TurnOrderStripView` +
    `TurnOrderStripPresenter`, created per-fight in `CombatActiveState`). **PvE (Area) only** — the
    Arena keeps its own `IArenaResolutionOrder` (Track G).
  - **Aim & fire input** — **Enter** is now hold-to-aim / release-to-execute: holding Enter turns the
    hero toward the mouse cursor (re-pointing the whole queued volley via the shipped global-facing
    model), releasing fires the queue along the final facing, and **right-click while holding aborts**
    (`PCInputController` hold/release + `OnVolleyAimStarted`/`OnVolleyAimCancelled`,
    `CombatAbilityPresenter.BeginVolleyAim`/`UpdateVolleyAim`/`EndVolleyAim`).
  - No change to what any ability does, the plan/commit model, or determinism. New edit-mode suites
    `RoundLeadPolicyTests`, `TurnOrderStripPresenterTests`, plus initiator assertions in
    `DialogueRunnerTests` / `EncounterCardHandPresenterTests` — full suite 1301/1301 green via the
    clone-project batch runner.
- **Humanoid bandit camp — D1 of Track D "Bandit Camp & Combat Legibility II"** (verified brief
  `product-requirements/bandit-camp-humanoids.md` reqs 1–12; docs updated: `world-sites.md`,
  `npc-proximity-interaction.md`, `character-system.md`, `narrative-procedural.md`):
  - **Humanoid enemies (reqs 1–2)** [combat + character-system]: `EnemyDefinition` gains
    `_assembly` (`CharacterAssemblyDefinition`) — enemies now spawn as the **shared modular
    humanoid** through the new `EnemyVisualSpawner` (single spawn path replacing the duplicated
    `InstantiateEnemy` bodies of `CombatIdleState`/`CombatActiveState`; assembly-first, authored
    prefab fallback, legacy capsule as logged last resort; deterministic ring offsets so a crew
    doesn't stack). Combat reuses the same GameObject, so the look is identical in world and
    battle. `DemoEnemy_BanditBrute` + `TestEnemyDefinition` now carry assemblies (the brute also
    gained the test ability + AI profile so it can act).
  - **Demo role tints (reqs 3–4)** [character-system]: new `IDemoRoleTintApplier` /
    `DemoRoleTintApplier` (`MaterialPropertyBlock` `_BaseColor`+`_Color`; alpha 0 = untinted) +
    `_demoTint` fields on `NpcArchetype` and `EnemyDefinition`. Villagers/frogfolk read green,
    barn raider + camp crew lighter maroon, camp boss deep maroon (`DemoEnemy_BanditBoss` id 9002 +
    `DemoArch_BanditBoss` are new).
  - **Boss-led camp in the director (reqs 5–6, 12)** [world-sites + narrative]: `SiteDefinition` /
    `SiteFamilyDefinition` gain the boss-anchor block (`_bossStoryFlavor`, `_bossCrewMin/Max`);
    `SiteAwareSlotAllocator.AllocateCampAnchor` converts a boss-led site's Combat anchor into
    `WorldSlotKind.Camp` with seeded `CrewEnemyIds`; `RunWindowPlanner.PlaceCampBoss` fills it with
    a seeded pick among boss-flavor **ambient-colour** stories (the authored pool IS the
    quest-or-not ratio; never a quest-slot candidate; no boss story → plain crew fight).
    `PlannedPlatformKind.Camp` realises as boss `NpcContent(isCampBoss)` + crew `EnemyContent`s on
    one platform; `WindowNodeSnapshot.CrewEnemyIds` rides the run save (old saves stay valid;
    missing boss story on restore degrades to a plain fight). `Camp.asset`: `bandit-boss`, crew 2–4.
  - **Boss engagement (reqs 7–9)** [npc-interaction]: `NpcInteractionConfig._bossEngagementRadius`
    (default 6) — a camp boss (`NpcContent.IsCampBoss`) uses it for **both** intents and
    **auto-engages on cross**: hostile boss starts the camp fight, job-bearing boss opens his
    dialogue with no F press. **All** NPC engagement is now **platform-scoped** (PO decision
    2026-07-05): `NpcProximityPresenter` tracks the player's platform via `PlatformEvents` and
    off-platform NPCs are ineligible. Dev overlay draws boss rings in a third colour.
  - **No aggro on landing (reqs 10–11)** [platform]: new pure `CombatAutoStartRule` — a platform
    with enemies auto-fights on activation only when no un-engaged NPC gates it;
    `NpcEncounterStarter` latches `NpcContent.EncounterStarted` on talk/aggro. The crew joins the
    boss's **one** fight (`CombatActiveState` already sweeps every `EnemyContent` on the platform).
  - **Demo content**: `DemoStory_CampBossHostile` (required combat ⇒ `!` boss),
    `DemoStory_CampBossJob` (+ `DemoDlg_CampBossJob`/`DemoDlg_CampBossThreat` ink pairs +
    `DemoQst_CampJob` — an explicit placeholder until the P3-17 shady-offer content).
  - **Tests**: `ProximityEvaluatorTests` (boss radius / auto-talk / platform scope),
    `CombatAutoStartRuleTests`, `DemoRoleTintApplierTests`, camp cases in
    `SiteAwareSlotAllocatorTests` / `RunWindowPlannerTests` / `WorldRestoreTests` — full suite
    1289/1289 green via the clone-project batch runner.

### Fixed
- **Persistence — first-playtest fixes (P2-2 follow-up)** (`save-persistence.md` updated in place):
  three bugs found by the owner's first save/continue playtest.
  (1) **Stale save on quit** — progress made on the current platform after entering it (a finished
  conversation, its facts and quest stages) was lost, because the only savepoint was platform
  entry: new `QuitSavepointHook` + `AutosaveService.SaveGraceful()` take a **graceful-exit
  savepoint** on `Application.quitting` (player quit AND editor play-mode stop), skipped while any
  dialogue is open so a mid-conversation quit still resumes at the platform's clean start (FR7).
  (2) **Artifacts duplicating on every load** — `InventoryPresenter.SeedStartingInventory()` added
  the dev starting set on every Area boot with no guard: now skipped on a continue boot (and when
  the inventory is already non-empty), mirroring the blank-rack guard.
  (3) **Current-window content vanishing on load** — the world capture marked "all windows but the
  last" consumed, but the next window is planned when the player leaves the FIRST platform of the
  current one, so the window the hero stood in was wrongly stripped (his NPC and the neighbouring
  platform's NPC disappeared): consumption is now **per node, marked when its platform is exited**
  (sticky) — the standing platform re-begins (FR7), unvisited neighbours keep their content, and
  left-behind platforms still never respawn (FR8).
  Also added a one-line run-seed diagnostic on seed creation (`[LootInstaller] Run seed …
  fresh/restored`) — platform geometry is a pure function of that seed — plus a capture-side test
  asserting `RunSaveSnapshot.RunSeed` rides the save. New/extended tests:
  `GracefulQuitSave_WritesOnlyWhenNoDialogueIsOpen`, the RunSeed assertion in
  `RunStateServiceTests`; full suite green via the clone-project batch runner.

### Added
- **Persistence — Save / Continue a run + cross-run memory (P2-2, closes narrative R14)** (verified
  brief `product-requirements/save-continue-run.md` FR1–FR14; new system doc `save-persistence.md`):
  a run now **survives the session boundary** and the world **remembers across deaths**. New
  `Core.Persistence`: `JsonSaveFile` (versioned, temp-file + atomic swap, parse-or-discard —
  a corrupt/stale `run.json` is deleted, a corrupt `meta.json` is **quarantined** to `.corrupt`;
  FR13/FR14), `RunSaveStore`/`MetaMemoryStore` over `persistentDataPath/Saves`, the whole-run
  `RunSaveSnapshot` (root `RunSeed` + narrative sections + `WorldStateSnapshot`/`HeroBodySnapshot`
  /`PlayerStuffSnapshot`), the explicit `RunStateService` aggregator (capture normalizes a
  mid-staging cauldron session to plain items without touching live play; restore replays
  facts → actors → quests (recorder bridge repopulates progression) → stuff → re-socketing →
  staged hero body), `RunRestoreContext` (the lazy "is this boot a continue?" decision — the file
  on disk is the cross-scene carrier; **no ProjectContext, no statics**), `RunRestoreCoordinator`
  (execution order −200, before `MetaMemoryBootstrap` at −100 so the always-on memory wins),
  `AutosaveService` (savepoint = **platform entry**, post-planning state; W3-1 suspended dialogues
  skip the boundary) and `RunLifecycleService` (**Defeat → meta flush → run-save delete** — death
  consumes the save, no scumming; FR2). **World resume (D5, FR4/FR12)**: `RunStreamingCoordinator`
  records every realized window at plan time and `BeginRestored` rebuilds them **by id with zero
  draws** from the shared stream (castings re-resolved via the new `Actor/Quest/CastingSnapshotMapper`s,
  the context bag rebuilt by the factory's exact recipe; consumed windows restore content-free with
  their surface shape pinned by `GraphNode.ShapeKindOverride`; loot re-rolls its stateless per-node
  context; the biome journey needs no state — `ApplyForWindow(0..k)` replays idempotently); the
  quest-spacing counter and the site-allocator block queue/counters ride
  `WorldStatePersistenceBridge`; the run seed provider (`LootInstaller`) seeds from the save on a
  continue. **Hero body**: `HeroBodyRestorer` re-applies the saved frame + equipped/dormant parts
  through the factory's staged all-or-nothing overload once the rig assembles. **Combat**: per-fight
  controllers fan outcomes into the new `ICombatOutcomeRelay` (wired by `CombatControllerFactory`).
  **Menu**: Continue button (`MainMenu.unity` authored directly) shown iff `run.json` exists;
  Journey = an explicit **new run** (deletes the save; PO decision); model/interface additions:
  `IInventoryModel`/`IBlankRack` gain `NextInstanceId` + `RestoreFrom`, `IPartInventoryModel.RestoreFrom`,
  `IRunProgressionRecord.Choices`, `INarrativeSaveService.Restore`, `CastingSnapshot` gains
  `StoryId`/`ThreadId`, `QuestInstanceSnapshot` gains `RewardsGranted`, new `LogCategory.Persistence`.
  **Cross-run memory (FR9–11)**: `meta.json` carries the `FactHorizon.Meta` partition, flushed at
  savepoints + death and loaded into the fact store at every Area boot; proven end-to-end by the
  demo meta fact **`world.barn_bounty_honored`** (new `MetaFact_BarnBountyHonored` key asset,
  Horizon = Meta, + an `OnCompleteEffects` entry on `DemoQst_BarnBounty`) — asset-only authoring,
  recipe in `save-persistence.md` §4. New suites `PersistenceStoreTests`, `RunStateServiceTests`,
  `WorldRestoreTests`, `RunLifecycleTests` + extended `MainMenuPresenterTests`; full suite
  **1264/1264 green** via the clone-project batch runner. Deferred per §0 into the ROADMAP:
  W3-1 option-b mid-dialogue saves, the currency model (+ its save section),
  `PlatformEvents` → Zenject signals debt.
- **Narrative — first-class threads + cross-window continuity + run/meta fact boundary (R8 / P2-3)**
  (verified brief `product-requirements/director-threads-and-continuity.md` FR1–FR12;
  `narrative-procedural.md` §2.6/§3/§4): a thread is now a **managed entity**, not a string label.
  New `Narrative.Threads.Core`: `ThreadCatalog` (authored vocabulary), `ThreadLedger` (run-scoped
  lifecycle: live/resolved/failed + reason, stage, expiry clock, replay-stable order),
  `ThreadMaintenanceService` (per-window tick: advance fold → authored **resolution** → **premise
  conflict** (incl. arcs — the mutual-exclusion mechanism) → **expiry** (ephemeral only); retirement
  = state + one `world.<threadId>.thread_retired` indicator fact, never a closure beat). New
  `StoryRunLedger` (`Narrative.Stories.Core`): a beat placed/resolved this run — or on a retired
  thread — is **never re-placed** (FR9, the stale re-placement fix). `RunWindowPlanner` runs the
  tick first, filters through the run ledgers, applies the **live-thread ceiling**
  (`RunPacingConfig._maxLiveThreads`, default 3; at the cap the quest slot degrades to ambient and
  waits — never force-drops) and prefers **advancing a live thread over opening a new one** (FR3);
  availability and the pick share one placeability predicate. `StoryResolutionRelay` folds
  `DialogueRunner.OnDialogueEnded` into the ledgers (`Casting`/runner now carry
  `StoryId`/`ThreadId`; a `leave` outcome resolves the story but does not advance the thread).
  **Authoring**: new `ThreadDefinition` SO (`Narrative/Threads/Thread`: kind ephemeral/arc, premise,
  resolution conditions, lifespan) auto-loaded from `Resources/Narrative/Threads`; a bare
  `_threadId` label runs as an implicit ephemeral default. **D20 boundary**: `FactKeyDefinition`
  gains `_horizon` (Run/Meta; default Run), `FactScope` gains `PerThread`, and the save snapshot
  partitions `Facts`/`MetaFacts` by horizon and captures/restores both ledgers — the cross-run
  store + file IO stays P2-2, D7 spine lane P3-1, D19 P3-2, meta consumers P3-3. Demo assets:
  `DemoThread_BarnRaid` (ephemeral, lifespan 4), `DemoThread_FrogMarsh` (**arc**),
  `Fact_ThreadRetired` (+ registry entry). New suites `ThreadLedgerTests`, `StoryRunLedgerTests`,
  `ThreadMaintenanceTests`, `RunWindowPlannerThreadTests`, `StoryResolutionRelayTests` + snapshot
  partition/ledger round-trip tests; full suite **1234/1234 green** via the clone-project batch
  runner. Scope additions flagged per §0: authored `_resolutionConditions` (owner-approved
  2026-07-05) and `FactScope.PerThread` — both data-only.
- **Character System — body-plan demo scene + part-selection dev console** (dev tooling;
  `character-system.md` §3.1): new `Tools/Character System/Build Body-Plan Demo Scene` builds and
  saves `Scenes/BodyPlanDemo.unity` — a small walled platform (walls on the hero's wall layer so
  dashes stay penned), the Hero prefab (WASD movement → per-frame gait is visible), an
  `EventSystem` + Input System UI module, and a left-side console with **one TMP dropdown per
  authored slot** listing every catalog part (`[frame]` / `(dormant)` tags). Picks route through
  `BodyPlanSwapCoordinator` — instant same-frame swaps, real confirm-and-shed for frame-changers,
  shed parts visible in the part-stash readout. New `BodyPlanDemoConsoleView` /
  `BodyPlanDemoConsolePresenter` (MVP) + `BodyPlanDemoInstaller` (logger via `LoggingInstaller`,
  `ICharacterRegistry`, stash trio, console; character system + locomotion from their own
  installers). `YashericaEditor.asmdef` gains a `Unity.InputSystem` reference (the builder places
  the UI input module). Compile green (main + editor) via Rider MSBuild.
- **Character System — independent body-plans + skeleton-swap runtime (P2-1)** (verified brief
  `product-requirements/body-plan-skeleton-swap.md` FR1–FR11; `character-system.md` R19–R24,
  supersedes R3/R4/R10's single-fixed-superset reading): a rare part marked **`GovernsBodyPlan`**
  (+ authored **`BodyPlanPriority`**) pulls in its own skeleton — the body tears down and re-forms
  on the governing frame under the same host root, ordinary parts with no structural home shed to
  the new **part inventory**, and losing frame-changers stay **equipped-but-dormant** (governance
  falls back to them when the winner leaves). New pure Core: `BodyPlanResolver` (highest priority
  wins, ordinal tie-break, order-independent — FR3/FR11) + `BodyPlanChangePlanner`
  (`InstantSwap`/`DormantInstall`/`FrameChange`/`Incompatible`; structural fit = every skinned bone
  and contributed-socket parent resolves — FR4/FR6) + `IShedPartSink` port. Runtime:
  `BodyPlanSwapCoordinator` (build-before-destroy transaction: the new body is fully staged
  inactive before the live rig is torn down — a failed build changes nothing), factory overload
  `Create(skeleton, activeParts, dormantParts, …)` (all-or-nothing),
  `ModularCharacterVisual.ReplaceCharacter` (re-points `Character`/`Animator`, binds the frame's
  `SkeletonDefinition.AnimatorController`, re-fires `CharacterAssembled` so the passport binder
  re-binds), controller dormancy (`EquipDormant`/`DormantParts`/`EquippedPartDefinitions`), and
  `CharacterLocomotionView` animator re-resolution. **Confirm-and-shed flow (FR7)**: new
  `BodyPlanConfirmPanel.prefab` + `BodyPlanConfirmView`/`BodyPlanConfirmPresenter`
  (`IBodyPlanConfirmPrompt`); declining leaves body, blank, and sockets untouched.
- **Inventory — part stash for shed body parts (P2-1 FR8)** (`inventory-subsystem.md` §2.4b):
  `PartInventoryModel` (pure multiset of part ids) + `PartInventorySink` (implements the character
  system's `IShedPartSink`) + read-only corner readout (`PartInventoryPanel.prefab`,
  `PartInventoryPresenter`, names via `IPartCatalog`, hidden while empty). Re-install-from-stash is
  a ROADMAP follow-up.
- **Mutation — body-plan-aware unseal (P2-1)** (`mutation-subsystem.md` R6/R6b):
  `IMutationCharacter.SwapPart` → **`RequestSwapPart`** (`Applied | PendingConfirmation |
  Rejected`) + `SwapRequestResolved`; the card pick commits the unseal only on an
  applied/confirmed install, a declined frame change keeps the cards, blank, and sockets intact;
  offers are pre-filtered through `CanInstall` and a slot's dormant occupant is excluded like an
  equipped part. Adapter now routes through the `BodyPlanSwapCoordinator`.
- **Placeholder frames — serpent + spider demonstrators (P2-1 FR9/FR10)**: the generator is
  restructured around per-frame data tables (`PlaceholderFrameLibrary`: base biped 27 bones ·
  legless **serpent** = −6 leg bones +`Tail.3..5` · **spider** = −legs +8 radial
  `SpiderHip/SpiderTip` chains; shared bones keep identical names + rest TRS so exactly the legs
  shed) and now regenerates **in place** (GUIDs and hand-authored part fields survive; the old
  delete-folder regen would have severed `Hero.prefab`/blank references). Per frame: rig prefab,
  sway-table idle/run clips + `Speed` blend controller (slither / scuttle), skeleton def (+ display
  name + controller), frame-changer part (`part.spine.serpent` slot.tail prio 20 ·
  `part.legs.spider` new **`slot.legs.cluster`** prio 10 — both equippable at once for the FR3
  priority test), preview assemblies. Hand-authored equip triggers `Blank_SerpentSpine` /
  `Blank_SpiderCluster` + `Slot_LegsCluster`. **Run `Tools/Character System/Generate Placeholder
  Assets` once to materialize the frames.**
- **Races & Passport — races as data, part race-tags, acceptance-tier fact (P0-3)** (new
  `races-passport.md`; brief `product-requirements/race-roster-and-passport.md` R1–R8): the world's
  peoples exist as data and read the player's body. New `Scripts/World/Races/` — pure core
  (`RaceData`, `IRaceRoster`/`RaceRoster`, `RaceAcceptanceCalculator`: tier = count of a race's
  tagged parts equipped, clamped {0 outsider, 1 tolerated, 2+ kin}, kindless/unknown ids never
  count), data (`RaceDefinition` SO — `Create → World → Race`: id, display name, home biome,
  belonging colour — + `RaceRosterMapper`, auto-loaded from `Resources/World/Races` in
  `AreaInstaller`), and integration (`RacePassportProjector` — the **single writer** of the new
  per-faction Int fact **`faction.<raceId>.reads_as_tier`** (one key covers every race via its
  subject; `FactionFacts.ReadsAsTier` + `Fact_ReadsAsTier` registered) — and `RacePassportBinder`,
  `NonLazy` in `NarrativeSliceInstaller`). `PartDefinition` gains a **`_raceId` string tag**
  (empty = kindless; `[RaceId]` drop-down drawer over the authored races — data-only extension, no
  enum), and the body now surfaces change events: `CharacterAssemblyController.PartsChanged` (via
  `IModularCharacter`) + `ModularCharacterVisual.CharacterAssembled` — every swap recomputes all
  roster tiers synchronously (R7). Authored the three starting races (Ibex/Mountain slate-blue,
  Lizard/Desert sun-gold, Fox/Forest russet); the hero's `_A` starting set is kindless, the `_B`
  parts carry demo tags (2× ibex / 2× lizard / 2× fox so tier 2 is reachable). Wearing parts of two
  races reads tier-1 to both — no hard conflict (R5). Tests: `RaceRosterMapperTests`,
  `RaceAcceptanceCalculatorTests` (6/6 green via the Roslyn runner), `RacePassportProjectorTests`
  incl. the precondition-gating integration; plus a 10-check Roslyn end-to-end smoke (real
  projector + `FactStore` + `PreconditionEvaluator`, literal race-id subjects). Full compile +
  test compile green.

### Changed
- **Character System — `AssemblyValidator.SkeletonMismatch` demoted Error → Warning (P2-1 FR4)**:
  a part's `TargetSkeleton` is authoring provenance, not a gate; cross-frame fit is enforced
  structurally by the `MissingBone`/`SocketParentBoneMissing` Errors (a base part whose bones all
  resolve legitimately rides another frame; a serpent spine on the base rig still fails on
  `Tail.3..5`). `ModularCharacterVisual` binding moved `MutationInstaller` →
  `CharacterSystemInstaller` (its home system). Tests: `BodyPlanResolverTests` +
  `BodyPlanChangePlannerTests` + `PartInventoryModelTests` + validator/presenter updates — 35/35
  new-suite green via the Roslyn runner; full compile (main + editor + tests assemblies) green.
- **Narrative demo — the marsh passport is now the real body-derived tier, not a card flag**
  (`races-passport.md` §4.3, narrative-procedural.md §4): `DemoStory_FrogElderOpen` gates on
  `faction.fox.reads_as_tier ≥ 1` (`Gte`, literal subject `fox` — the first authored int-threshold
  precondition), `DemoStory_FrogElderClosed`/`DemoStory_MarshPool` on `< 1`; `MarshPool.ink` no
  longer writes any fact — the hermit now *teaches* the rule (wear the fox's marks), and the elder
  prose reads the fox markers (`FrogElderOpen/Closed.ink` + hand-recompiled JSONs;
  `DemoDlg_MarshPool` fact-write footprint emptied). Equipping one fox-tagged part via a mutation
  flips the marsh from closed to open — the passport loop end-to-end.

### Removed
- **`world.reads_as_frogfolk` (the D15/D16 card-set passport stand-in)** — superseded by
  `faction.<raceId>.reads_as_tier`; `DemoFact_ReadsAsFrogfolk.asset` deleted and unregistered from
  `DemoFactKeyRegistry`. (`RunWindowPlannerTests` keeps a synthetic bool passport to isolate planner
  flip mechanics; the tier-side flip is covered by `RacePassportProjectorTests`.)

### Fixed
- **`TypedFactsTests.DriftCheck_FlagsMissingRef` hard-coded "exactly 1 missing curated ref"** and so
  broke whenever `TypedFacts.All()` grew — it was already latently red after `run_escalation_tier`
  (P0-2) and surfaced with `reads_as_tier` (P0-3). The expected warning count is now derived from
  `TypedFacts.All()` minus the refs the test declares. Full editor EditMode suite on a project
  clone: 1156/1156 green after the fix (1155/1156 before, this test the only failure).

### Added
- **Biome Journey — biome selection along the run (P0-2)** (new `biome-journey.md`; brief
  `product-requirements/biome-selection-along-the-run.md` R1–R11): the run's biome is no longer a
  hardcoded Forest. A new pure core `LevelGeneration.Journey` (`BiomeJourney` + `BiomeStretch` +
  `BiomeStretchDirector`) advances the biome in authored, seeded, **tier-climbing stretches** of
  planning windows: stretch *s* draws a weighted pick from the *s*-th lowest authored escalation
  tier (clamped at top; previous biome excluded when the tier offers an alternative, so a boundary
  is a visible crossing) — journeys **diverge by seed** (D18) and are deterministic on their **own
  random stream** (`LootSeed.Derive(runSeed, "biome-journey")`), so stretch draws never perturb the
  shared narrative-slice stream. `RunStreamingCoordinator` applies the stretch **before** planning
  each window; crossing a stretch switches the live `ICurrentThemeProvider` (monster pools + loot
  follow automatically — `AreaGenerator` now reads the provider live instead of a frozen ctor
  theme), swaps landmark dressing (`RouteLandmarkSpawner.ApplyBiome`), rebuilds the world backdrop
  (hard cut; transition art is M5), and publishes the new world fact **`run_escalation_tier`**
  (Int; the D19 seam — nothing consumes it yet; registered in `TypedFacts` + the fact registry).
  New data surface: **`BiomeProgressionConfig`** SO (`Create → World → Biome Progression`;
  tier/weight/stretch-windows per biome) + mapper, bound in `AreaInstaller`
  (`Resources/World/Biomes/BiomeProgressionConfig`); authored Forest t1 · Mountain t2 · Desert t2
  (weight 1, stretch 3–4 windows), **Cave excluded by data** (no entry). Placeholder
  `MonsterPool_Mountain`/`MonsterPool_Desert` assets reuse the two demo enemies so ambient combat
  survives outside Forest. Missing/empty config degrades to fixed Forest (warned). The route
  model's landscape *shape* stays frozen to the entry biome (known limitation → M5). Tests:
  `BiomeJourneyTests` (11) + `BiomeStretchDirectorTests` (4) — 15/15 green via the Roslyn runner —
  plus `BiomeProgressionConfigMapperTests`; full compile + test compile green.

- **World Landscape Read — routed path, elevation tiers, world backdrop (P5-2)** (new
  `world-landscape.md`; brief `product-requirements/world-backdrop-and-elevation.md` FR A–D): the
  traversal field now reads as a landscape you route through. **Routed path**: platform depth (Z)
  follows a pure seeded route function — bounded low-frequency baseline wander + sparse `cos²`
  feature arcs bulging toward the camera around a **midground routing landmark** placed at the arc
  apex on the far side (corridor and hero-never-occluded invariants enforced structurally in
  `BiomeLandscapeSettings`); forward (X) stays the monotonic layout cursor. **Elevation tiers**:
  platform Y is a quantized low-frequency swell (tier count × step, ≤ 1 tier between neighbors at
  defaults) — visual only, gaps stay clean hops. **World backdrop**: a hero-anchored rig (two hazed
  ridge strips from `BackdropSilhouetteModel` + sky gradient band) reads as an infinitely distant
  biome horizon under the unchanged fixed isometric camera. New pure core
  `LevelGeneration.Route` (`RunRouteModel`, `BiomeLandscapeSettings`, `BackdropSilhouetteModel`),
  new biome data path `World.Biomes` (**`BiomeAppearanceDefinition`** SO — the biome appearance
  config P1-2 will extend — + mapper + catalog, bound in `AreaInstaller`, auto-loaded from
  `Resources/World/Biomes`), new views `World.Landscape` (`RouteLandmarkSpawner`,
  `WorldBackdropBuilder`/`View`, procedural muted placeholder silhouettes until the P5-4 asset
  pass; SO kit lists are the swap-in seam). Authored `BiomeAppearance_Forest/Desert/Mountain/Cave`
  assets with distinct landscape characters. Tests: `RunRouteModelTests` (11),
  `BiomeLandscapeSettingsTests` (7), `BackdropSilhouetteModelTests` (5) — 23/23 green via the
  Roslyn runner — plus `BiomeAppearanceMapperTests`; full compile + test compile green.

### Fixed
- **Backdrop vertical correction had the wrong sign; landmarks left the frame with tier height**
  (`world-landscape.md` §3 scale note). Under the tilted **orthographic** camera there is no
  perspective convergence: a ground-height point `D` away projects `0.5·D` **above** screen
  centre — the previous "raise by `D·tan(pitch)`" pushed the horizon further out of frame.
  `WorldBackdropBuilder` now **lowers** every part by `D·tan(pitch)`, making a part's on-screen
  height depend only on its authored base offset. Same geometry constrains landmarks: a +Z
  offset shifts them up-screen, so bases now sit at a fixed low height (no tier coupling in
  `RunRouteModel`), offsets pulled to the no-occlusion floor (≈ 14), baseline amplitudes eased
  to ~5 to keep that floor small, and **arc depths raised** (7–8) so the feature arcs — which
  bulge toward the camera and are not floor-constrained — carry the felt turns. Defaults + all
  four biome assets retuned; route suite 23/23 + full compile green.

### Fixed
- **World backdrop was invisible and the weave/tiers unreadably subtle at game scale**
  (`world-landscape.md` §3 scale note). Two causes found on the first play-mode pass: (1) under
  the tilted isometric camera a horizon placed at ground height 180–340 units away projects
  ~125 world units **below** the visible window — `WorldBackdropBuilder` now raises every part by
  `distance × tan(cameraPitch)` (pitch passed from `CameraConfig.IsometricRotation`), and
  `WorldBackdropView` follows the hero's Y with slow smoothing so tier climbs re-center the
  horizon without jump-bob; (2) the traversal camera is a tight **orthographic** window
  (~25×14 world units) over 8–16-unit platforms, so amplitude-5 weave / 1.5-unit tiers read as a
  straight line — landscape dials bumped to platform-commensurate values in the code defaults
  (corridor 14, amplitude 8, arc depth 5, tier step 2.5, landmark scale 12–18) and all four
  biome assets (with tier wavelengths keeping the ≤ 1-tier-per-platform guarantee). Route suite
  23/23 + full compile green.

### Fixed
- **`CameraConfig` Zenject binding pointed at the wrong Resources path** (`AreaInstaller`):
  `FromResource("CameraConfig")` vs the actual `Resources/Configs/CameraConfig.asset`. Latent
  since the binding was added — nothing resolved it lazily until the world backdrop injected
  `CameraConfig` for the isometric yaw, which surfaced as a `ZenjectException` (resource not
  found) followed by a `NullReferenceException` in `AreaSceneEntrypoint.CreateWorldBackdrop`
  and an aborted area generation. Now binds `Configs/CameraConfig`.

### Removed
- **The `_heightDeviation` drunk walk and the Perlin height map** (`platform-generation.md` §3):
  the dial — which despite its name drove an **unbounded per-platform lateral (Z) random walk**,
  not height — is deleted from `PlatformShapeConfig`/`PlatformShapeSettings`/mapper and the asset;
  `PerlinNoiseMap` (the old Y source) is deleted with it. Both axes now come from `RunRouteModel`.
  The `AreaSceneEntrypoint.seed` inspector override now seeds the route instead of the height
  noise; `noiseScale`/`noiseOctaves` fields removed.

### Added
- **Arena Mode — Phase 4: match HUD, spectate, disconnect handling (MVP complete)**
  (`arena-mode.md` §2.2): `ArenaMatchHudPresenter`/`ArenaMatchHudView` (MVP) drive the in-match
  HUD off the controller events — round/lock-in status line, **defeat → Spectating** (local input
  disabled, the player watches the match end — brief R14), winner/draw banner with **Leave → main
  menu** (session shutdown first), "connection to the host was lost" on a joined client whose host
  vanished, and a **desync warning** when a client's reported lockstep hash disagrees with the
  host's (`ArenaMatchHost.DesyncDetected`, R10). `LoopbackArenaTransport.SimulateDeparture` lets
  tests exercise R9. The Arena scene's HUD gained the spectate/desync labels and the Leave button.
  New edge-rule sweep `ArenaEdgeCaseTests` (6) locks the conflict corners — and corrected two rule
  descriptions to the true behavior: a move onto an occupied cell is **rejected at plan time** (so
  swaps/chases are never committed — the only same-hex conflict is two units racing to a common
  empty cell), and a true simultaneous mutual-kill is **not a draw** under sequential skip-dead
  resolution (the earlier unit wins). Arena suites now 39, all green; full compile + PvE
  regression green.

### Added
- **Arena Mode — Phase 3: networked host/join over NGO** (brief `arena-mode-mvp.md` R4–R5;
  `arena-mode.md` §2.3): the Arena scene boots into a **connect panel** — one player **hosts**
  (connection approval caps players at the config max and rejects joins once started), others
  **join by `ip[:port]`**; the host's **Start Match** seats every connected client (ids 1..N in
  connection order), rolls the match seed, and broadcasts the setup every client builds the
  identical world from. **Lockstep over NGO custom named messages** (`NgoArenaTransport` behind
  the unchanged `IArenaTransport` seam) — no NetworkObjects, no scene sync, no per-unit
  replication; joined clients never assemble rounds (`ArenaCombatController.SetHostRole`). New:
  wire structs + `ArenaWireCodec` (buffer-free domain↔wire mapping), `ArenaSessionService`,
  `ArenaMatchLauncher`, `ArenaConnectPresenter`/`ArenaConnectView` (MVP), `ArenaPlayerDirectory`;
  Arena.unity gains the hand-authored NetworkManager + UnityTransport and the connect panel
  (incl. a TMP_InputField); `Yasherica.asmdef` references `Unity.Networking.Transport` (its API
  surfaces `NetworkEndpoint`). Offline dummies stay as the `_offlineMode` config-flag dev fallback.
  Local multi-client testing uses the editor-as-host + standalone dev-build joiners on
  `127.0.0.1` (Unity's Multiplayer Play Mode is intentionally not a dependency — it pulls a
  Newtonsoft-JSON package the registry flags with an invalid signature). Tests:
  `ArenaWireCodecTests` (round-trips) + `ArenaLockstepTests` (two independent client sims over one
  transport → identical positions/HP/state hashes; arena suites now 33, all green).

### Removed
- **Dead pre-Arena networking scaffold** (never constructed): `NetworkGameStateSync`,
  `NetworkActionSender`, `CombatNetworkManager`, `MessageType`, and — superseded by the arena
  wire format, which must carry the lock-time committed cells — `ActionSerializer`/`ActionData`.
  `NetworkPlayer` stays (remote seats in the arena roster).

### Added
- **Arena Mode — Phase 2: the symmetric round, playable offline** (brief `arena-mode-mvp.md`
  R6–R14; `arena-mode.md` §2): the Arena scene now runs the full **hidden simultaneous commit →
  simultaneous resolve** loop against 1–3 seeded AI dummies on a match-seed-generated platform.
  - **Round domain** (pure C#, `Combat.Arena.Core`): `ArenaCommit`/`ArenaCommitBuilder` (lock-time
    snapshot — per-ability committed cells frozen from position + final facing; move step; empty
    commitment for schedule/pass), `ArenaCommitCollector`, `ArenaRoundBundle`,
    `IArenaResolutionOrder` + **`RotatingInitiativeOrder`** (initiative passes between players
    each round — PO decision, replaceable strategy), `LastHeroStandingWinCondition` (win/draw,
    armed after spawn), `ArenaStateHash` (per-round FNV-1a lockstep digest), `ArenaSpawnPlanner`
    (deterministic farthest-point spawn cells), `IArenaTransport` + `LoopbackArenaTransport`.
  - **`ArenaCombatController` : `ICombatController`** — the whole PvE presentation stack (action
    panel, plan icons, ghost telegraph, `EnemyRoundController` pacing) works against it
    untouched; terminal Move/ExecuteQueue are intercepted into commits (never executed locally),
    the host (`ArenaMatchHost`) gathers all alive players and broadcasts the canonical bundle,
    every client normalizes and resolves it through the unchanged `EnemyIntentResolver`
    (whiff/fizzle/skip-dead semantics literally the PvE ones). `ArenaAICommitSource` drives the
    offline dummies through the same commit path with `LootSeed`-derived AI seeds.
  - **Arena scene & assets** (hand-authored): `Scenes/Arena.unity` (SceneContext with the new
    `ArenaInstaller` + `CharacterSystemInstaller`, fixed camera, the CombatActionPanel canvas
    cribbed from Area with wiring intact, HUD status line), `ArenaMatchConfig` SO +
    `Resources/Arena/ArenaMatchConfig.asset`, `ArenaPlatformBuilder` (standalone seeded platform:
    `PlatformSurfaceGenerator` + mesh + walkable-outline colliders), `ArenaHeroSpawner` (N ×
    Hero.prefab, roster-assigned unit ids — never `UnityEngine.Random`).
  - Tests (26, green via the Roslyn runner): `ArenaCoreTests`, `ArenaCommitBuilderTests`,
    `ArenaRoundFlowTests` — incl. the PRD acceptance semantics (whiff on dodge, step-into-cells,
    same-hex conflict by initiative, lethal-first skips the return, last-hero-standing/draw) and
    the **determinism replay** (same commits twice → identical `ArenaStateHash`).

### Changed
- **Combat — round bookkeeping extracted (behavior-preserving):** `CombatController`'s private
  round-end/reset/round-start effect ticking moved verbatim into pure
  `Combat.Core.RoundLifecycleProcessor` (constructed internally — no signature change anywhere);
  the Arena round loop reuses it for the identical cadence. `WinConditionType` gains
  `LastHeroStanding`. `CharacterCombatCoordinator`'s `CharacterCombatInitializer` injection is
  now optional with an explicit ability-definition `Initialize` overload (the Arena spawner has
  no initializer; PvE call sites unchanged). PvE combat regression suite green (54 tests).

### Added
- **Arena Mode — Phase 1: main menu + scene flow** (brief `arena-mode-mvp.md` R1–R3; new system
  doc **`arena-mode.md`**): the game now boots into `MainMenu.unity` (build index 0) with two mode
  buttons — **Journey** loads the unchanged `Area` scene, **Arena** loads the (next-phase) `Arena`
  scene. New `Core.SceneFlow` seam (`ISceneLoader`/`SceneLoader`/`SceneNames` — first runtime
  scene-switch abstraction in the project), `MainMenuPresenter` + `IMainMenuView`/`MainMenuView`
  (MVP), `MainMenuInstaller`; hand-authored `MainMenu.unity`. Build settings reordered
  (MainMenu 0, Area 1) and the stale nonexistent `Demo.unity` entry removed. Tests:
  `MainMenuPresenterTests` (3, green via the Roslyn runner).
- **Combat — Track C epic: hero facing + enemy intent phase + ghost telegraph** (verified PO briefs
  `combat-hero-facing.md`, `combat-turn-intent-phase.md`, `combat-ability-ghost-telegraph.md`;
  new system doc **`combat-round-and-telegraph.md`**), built as one pass:
  - **Plan → Act → Resolve round** (R1–R7): new `RoundPhase` + `EnemyIntent` on `CombatState`;
    pure `EnemyIntentPlanner` (every enemy decides up front, UnitId order, cells/facing
    snapshotted) and `EnemyIntentResolver` (fires committed intents verbatim — dodged blows
    whiff, blocked moves fizzle, never re-targets; cooldown starts at resolve);
    `CombatController` orchestrates `BeginRounds`/`StartRound`/`ResolveNextEnemyIntent`/`EndRound`
    with new `OnRoundPhaseChanged` + `OnEnemyPlansRevealed` events; `EnemyRoundController` paces
    the resolve coroutine; `CombatActiveState` sequences character → enemies → `BeginRounds` so
    the first plan sees the full board. Enemy decision makers are now **seeded from the run seed**
    (`combat-ai:{enemyId}`) — same seed, same plans. Fixes the pre-existing gap where AI-scheduled
    abilities never fired (no AI ever executed its queue).
  - **Facing-relative aiming** (`ability-subsystem.md` R4–R8, R11): `Unit.FacingDirection` is now
    a `HexDirection` driving all directional abilities — aim input rotates the unit (free,
    unlimited `ChangeDirectionAction`, legal after acting via the validator's free-action gate),
    turning re-points the whole queued volley, cells are computed from the live facing at
    execution; AI emits `ScheduleAbilityAction.FacingToSet` (turn-and-schedule); new pure
    `FacingGeometry`; `UnitFacingRotator` makes facing legible on the model (hero + enemies).
  - **Push displacement** (`ability-subsystem.md` R13a): `AbilityDefinition._pushDistance` (Line
    only) + `IDisplacementAbility` + pure `DisplacementResolver` (stop before invalid/occupied);
    executor pushes survivors farthest-first after damage; authored demo asset
    `Resources/Abilities/Data/TestPushAbility.asset` wired into `TestEnemyDefinition` and the
    `TestHeroDefinition` fallback.
  - **Outcome preview** (R13 ghost honesty): pure `AbilityOutcomeCalculator` +
    `AbilityOutcome`/`UnitOutcome` mirror executor semantics without mutating state — predicted
    damage and landing cells equal execution on an unchanged board (test-asserted).
  - **Overhead plan icons** (R9): every unit shows its plan above it — player queue in order,
    enemy committed intent from the Plan-phase reveal (ability icon via new
    `AbilityDefinitionCatalog`, `»` glyph for moves) — `UnitPlanIconsPresenter` +
    `UnitOverheadIconsView` + `CombatUnitViewRegistry`, code-built and billboarded, each icon
    hover-raycastable via `AbilityIconMarker`.
  - **Ghost playback** (R10–R14): queue-submit auto-plays a one-shot translucent full-outcome
    ghost (caster clone facing the volley + displaced-unit clones at predicted destinations +
    damage/heal labels), hovering any plan icon — enemy icons included — replays it against the
    current board; `GhostPlaybackPresenter`/`GhostPlaybackPlanBuilder` (pure) +
    `GhostPlaybackView`/`GhostVisualCloner` (inactive-holder cloning, shared alpha-blend ghost
    material, fade in→hold→out) + `AbilityIconHoverController`.
  - Tests (all pure, green): `UnitFacingTests`, `FacingGeometryTests`,
    `ActionValidatorFacingTests`, `AbilityExecutorFacingTests`, `EnemyIntentPlannerTests`,
    `EnemyIntentResolverTests`, `CombatStateRoundTests`, `DisplacementResolverTests`,
    `AbilityExecutorPushTests`, `AbilityOutcomeCalculatorTests`, `UnitPlanIconsPresenterTests`,
    `GhostPlaybackPlanTests`.

### Changed
- **Combat — round bookkeeping cadence:** status-effect TurnStart/TurnEnd triggers, duration
  ticking, cooldown decrement, and acted-flag reset now tick **once per round for all units** at
  round end (was per-player-turn under round-robin — equivalent cadence for a two-party fight).
  `TurnManager` is degenerate under the phase round: `CurrentPlayer` pinned to the human,
  `NextTurn()` = round counter. Player actions are validator-gated to `RoundPhase.PlayerAct`.
  *(combat)*

### Removed
- **Combat — per-ability aiming:** `AbilityTarget` and `RetargetAbilityAction` deleted
  (`ScheduledAbility` is direction-free; the queue has ONE facing by design — PO brief
  `combat-hero-facing.md`); `AITurnController` replaced by `EnemyRoundController` (enemies no
  longer take round-robin turns). *(combat)*

### Fixed
- **Platform — hero stuck on/outside invisible walls (regression `7201bb2`):** three-part fix, the
  drooping rim look is kept (PO decision after the play-test). (1) **Stitched walkable edge with a
  continuous floor** — new pure `OutlineStitcher` sews the shallow between-cell V-notches of the
  hex-union outline (reflex vertices at most 0.75·hexSize deep are bridged; deeper bays keep their
  shape, winding-agnostic) and emits flat fill triangles paving the sewn spans;
  `PlatformSurfaceGenerator` stitches before growing the rim, so `Surface.Outline` (and
  `TopBoundary` = walls, `PlatformRegistry` point-in-polygon, AI boundary math) is the smooth sewn
  edge, the mesh's new `NotchFills` keep real floor under the hero across the notches (no running
  on air), and the rim droops from the stitched edge outward. The hex-cell pattern stays the
  combat grid — fills are walkable dressing, never cells. (2) **Guaranteed in-pen landing** —
  new `PlatformAnchor` (center-cell + nearest-cell world anchors over the surface): neighbor jumps
  land on the **nearest walkable cell center** (a full hex inradius inside the walls; the old
  probes measured the neighbor's mesh edge — i.e. the decorative rim beyond the walls — or the
  boundary line itself, and could strand the hero outside the pen), instant teleports and the area
  spawn use the **center cell** (the raw centroid can fall outside every cell on a concave union).
  Replaced the per-dash mesh-triangle scan in `CharacterMovementController` and the polygon
  intersection in `AICharacterMovementController`; dead code removed. (3) `_rimDropHeight` stays
  0.4 and the mesh keeps the sloped rim strip (zero-height skirt guard retained). New
  `OutlineStitcherTests` + `PlatformAnchorTests` (pure, green);
  `PlatformHexSurfaceMeshBuilderTests` lock the drooping profile. Doc: `platform-generation.md`
  §2.2/§2.3/§3/§4/§5/§6. *(platform + character movement)*
- **Combat — units sink waist-deep into the battlefield (regression `9d2f909`):** new
  `UnitGrounding` (`Combat.Battlefield`) grounds units explicitly — surface top from
  `HexToWorld` + a feet/pivot offset **derived from the unit's own authored collider**
  (`CharacterController`, else `CapsuleCollider`; Hero/Enemy capsules h=2/c=0 → 1.0) — applied at
  the five unit placement/animation sites (`CharacterCombatComponent`, `EnemyCombatComponent`,
  `EnemyCombatIntegrator`, `CombatEntryAnimator` entry target, `CharacterCombatAnimator` — whose
  0.1 movement threshold otherwise loops on the permanent Y gap). `SurfaceHexGrid` is untouched:
  its contract stays "true surface top", now asserted in `SurfaceHexGridTests` (Y never doubled).
  New `UnitGroundingTests` (5). No per-prefab magic numbers — new data-driven enemies ground for
  free. *(combat + platform)*

### Added
- **World Sites — content landing (phase 4, completing the world-sites brief; acceptance criteria):**
  `GraphNode` gains `Site` (`SiteStamp` — the M5 dressing seam on every platform) + `ContentFlavor`;
  `RunStreamingCoordinator.MapWindow` copies them from the plan; `AreaGenerator.CreateLootContent`
  passes the flavor into `LootRollContext.Tags`, so `Loot·market/stash/chest/relic` beats **bias**
  the biome platform table through the existing `BiasTags × tagBiasMultiplier` loot machinery
  (authored: forest `water` → market/stash, `bacteria` → chest/relic). **Townsfolk chatter
  content**: `DemoStory_TownsfolkGossip` + `DemoStory_TownsfolkGrumbler` (story tag `townsfolk`,
  one dialogue slot, no quest/combat → derived `Plain` intent) + `DemoDlg_*` + hand-compiled Ink
  (`TownsfolkGossip`/`TownsfolkGrumbler` .ink+.json); `arch_villager` gains the `townsfolk`
  archetype tag so villagers are preferred to play them. Enemy flavor tags authored:
  `TestEnemyDefinition` → wild-beast/den-monster, `DemoEnemy_BanditBrute` → bandit/guard. New
  `WorldSitesAcceptanceTests` (3): seeded 600-slot histogram at shipped density — Wild majority,
  sites rare + contiguous 0..N-1 blocks on both trigger channels, City busier than Village,
  landmarks townsfolk-free, guard fights draw the tagged enemy, same seed → same world. ROADMAP:
  the Sites & landscape item checked off (follow-ups filed: deferred passport/tier/biome fields,
  quest-bearer-as-fill design pass, per-flavor loot tables, real per-flavor enemies; the
  window-save-state item now covers the site allocator's pending queue). Docs: `world-sites.md`
  complete; `narrative-procedural.md` §2.6 sites layer + save-state note; `platform-generation.md`
  seam note. *(world/sites + level generation + loot + narrative content)*

### Changed
- **World Sites — reservation goes live (phase 3 of the world-sites brief; R2/R3/R8/R9):** the
  planner now runs on the site-aware allocator — sites appear in the streamed run.
  `RunWindowPlanner` depends on `IWorldSlotAllocator` + `ISiteCatalog`: eligible stories are
  partitioned into quest-eligible vs **ambient colour** (tagged with an `NpcFillFlavors` entry —
  e.g. `townsfolk`); ambient-colour stories fill site `Npc` slots by flavor (unused preferred, a
  small chatter pool repeats with a fresh actor, none → Empty + warn-once) and **never satisfy a
  Quest slot**; a landed quest calls `TryReserveSettlement(story.StoryTags)` and the quest platform
  is stamped as the block's anchor; Combat/Loot/Empty slots carry their flavor + `SiteStamp` onto
  `PlannedPlatform`. Monster pools are now **tagged entries** (`MonsterPoolEntry` = id +
  `EnemyDefinition.EnemyTags`; `BiomeMonsterPoolMapper` maps them): `IBiomeMonsterPoolCatalog.
  GetPool(theme, flavor)` filters case-insensitively; the allocator's site combat pick falls back
  to the unfiltered pool when no enemy carries the tag (warn-once per flavor).
  `NarrativeSliceInstaller` binds `IWorldSlotAllocator → SiteAwareSlotAllocator` (sharing the
  director stream) and threads it + the catalog into the planner. Townsfolk platforms plan as
  ordinary Story encounters, so `NpcIntentResolver` derives `Plain` with no interaction-layer
  change. Tests: `BiomeMonsterPoolCatalogTests` (6, new), `SiteAwareSlotAllocatorTests` +2
  (flavored pick + fallback), `RunWindowPlannerTests` +3 (townsfolk fill/exclusion/degrade) — and
  the 16 pre-site planner tests run unchanged over the wrapped allocator (planner-level passthrough
  proof). Doc: `world-sites.md` §2.3/§2.4/§4/§5. *(world/sites + narrative director + combat data)*

### Added
- **World Sites — SO schema, mapper, and the authored site vocabulary (phase 2 of the world-sites
  brief; R6/R7):** new `World.Sites.Data` — `SiteFamilyDefinition` (`Create → World → Sites → Site
  Family`: family-default anchors + fill budget/table) and `SiteDefinition` (`… → Site Definition`:
  id, family ref, footprint range, trigger weight, dressing theme, and **explicit override
  toggles** — off = inherit, so a later-added attribute defaults to "inherit" and existing assets
  stay valid). `SiteCatalogMapper` is the one Data→Core bridge: merges family default + site delta
  into effective `SiteDefinitionData`, validates (missing/duplicate id, no effective anchor →
  warn + skip). `WorldContentDensityConfig` gains the three site dials
  (`_averagePlatformsPerAmbientSite` 14 / `_minPlatformsBetweenSites` 6 / `_wildQuestWeight` 40) —
  mapper passes them through. `NarrativeSliceInstaller` binds `ISiteCatalog` (inspector list,
  auto-load fallback `Resources/World/Sites`) + `SiteBlockBuilder`; **the allocator is not yet
  swapped — behavior-identical until phase 3.** Authored assets: `SettlementFamily`/`LandmarkFamily`
  + `Camp` (anchor `Combat·bandit`, ambient channel), `Village` (inherits the settlement default),
  `City` (fill 2–3: townsfolk 5 / market 3 / guard 2), `Ruin` (anchor `Loot·relic`), `Lair`
  (inherits landmark); quest-channel weights Village 40 / City 20 vs Wild 40 (≈40% lone-wanderer
  quests, PO decision). Tests: `SiteCatalogMapperTests` (7). Doc: `world-sites.md` §3/§4 (SO
  reference + "Add a site" / "Revise a family" / "site:<id>" recipes). *(world/sites)*
- **World Sites — site domain + block reservation (phase 1 of the world-sites brief; R1–R10):**
  new pure-C# `World.Sites.Core` — the shared `base·flavor` content vocabulary (`ContentBaseKind`
  Empty/Loot/Combat/Npc × open flavor strings, `ContentBeat`/`WeightedBeat`), `SiteDefinitionData`
  (effective capacity recipe: footprint range, anchor beats, weighted fill budget/table, dressing
  theme; trigger channel derived from the first anchor's kind — NPC → quest roll, Combat/Loot →
  ambient roll), `SiteCatalog` (channel split + `NpcFillFlavors`), `SiteBlockBuilder` (anchor-first
  block build, fill clamped to footprint, connective Empty remainder, per-slot `SiteStamp`), and
  `SiteStamp` (siteId/instanceId/index/footprint/dressingThemeId — the M5 dressing seam). New
  `Narrative.Director.Core.IWorldSlotAllocator` + `SiteAwareSlotAllocator`: wraps the untouched
  `WorldContentAllocator` (empty catalog = bit-exact passthrough, regression-tested); run-scoped
  pending queue lets a 4–5 platform block span window boundaries; ambient site gate mirrors the
  quest gate (`MinPlatformsBetweenSites` spacing from block end + 1-in-`AveragePlatformsPerAmbientSite`
  roll); `TryReserveSettlement` resolves a landed quest to Wild-vs-settlement (`WildQuestWeight`,
  `site:<id>` story tag = hard request). `SlotAllocation`/`PlannedPlatform` gain defaulted
  `Flavor` + `Site`; `WorldSlotKind` gains `Npc`; `WorldContentDensitySettings` gains the three
  site dials (defaulted). **Not yet bound in DI — behavior-identical until phase 3.** Tests:
  `SiteBlockBuilderTests` (8) + `SiteAwareSlotAllocatorTests` (11). New system doc
  **`world-sites.md`** (added to `Docs/README.md`). *(world/sites + narrative director)*

### Changed
- **Combat/Platform — the combat grid is now derived from the platform's hex surface (phase 4,
  completing the platform-hex rework; brief §1 / R1):** new `SurfaceHexGrid : IHexGrid` takes its
  cells and local positions **1:1 from `PlatformHexSurface`** — `IsCellInBoundary` is set
  membership and `WorldToHex` is the exact inverse of cell placement; no boundary scan, no re-fit,
  no re-snap: the battlefield cells ARE the ground tiles. Signature cutover:
  `IHexGrid.Initialize(surface, center)`, `IBattlefield`/`Battlefield.Initialize(surface, center,
  hexConfig)` (hex size/orientation ride on the surface), `ICombatController.InitializeBattlefield
  (surface, center)`, `CombatActiveState` passes `Visual.Surface`; `CombatController`(+Factory)
  drop their now-unused `CombatConfig` dependency (`ActionValidator` keeps it for the queue size).
  `IHexGrid` gains `GetCellPosition` (local offset), removing the `HexGridBase` pattern-matches in
  `Battlefield`/`BattlefieldView`. Cell visuals (`BattlefieldView`/`HexCellView`), cell states, and
  all combat rules are untouched — the grid they render now coincides with the ground by
  construction. Tests: new `SurfaceHexGridTests` (6). New system doc **`platform-generation.md`**
  (added to `Docs/README.md`); the three M3 ROADMAP items are checked off with follow-ups filed
  (muted→crisp tech-art pass, arena-scale camera pass, `ContentSpawner` on concave islands,
  `Core.Hex` extraction debt).

### Removed
- **Combat — the boundary-scan hex grids:** `FlatHexGrid`, `PointyHexGrid`, `HexGridBase`
  (bbox-scan + point-in-polygon `CalculateCellsInBoundary`), `HexGridFactory`/`IHexGridFactory`,
  and their `AreaInstaller` factory bindings. Their center/rounding math lives on as `HexMetrics`
  (exact port, test-pinned by `HexMetricsTests`).

### Changed
- **Platform & Area Generation — traversal cutover to hex-composed platforms (phase 3 of the
  platform-hex rework; brief §1–§8):** platforms are no longer random ellipse blobs.
  `AreaGenerator.CreatePlatformFromNode` grows each platform's `PlatformHexSurface` from a
  **per-platform seeded stream** (`LootSeed.Derive(runSeed, "platform-shape:{nodeId}")` — decoupled
  from the director stream, order-independent across windows), sized by its **content kind's shape
  profile** (`PlatformContentKindResolver` over the graph node) with the **battlefield-minimum floor
  for combat-capable platforms**; `PlatformVisual` gains `Surface` and its `TopBoundary` is now the
  hex-union outline, **final from birth** (`PlatformView` no longer overwrites it). New
  `PlatformHexSurfaceMeshBuilder` replaces the deleted `PlatformMeshBuilder`: per-cell shallow-dome
  tops (the muted traversal tiling — cell borders read as soft valleys; `CellInset` 0 = flat), the
  drooping jittered **rim strip** (dressing only — wall colliders sit on the walkable outline, so the
  rim is physically unreachable), side skirt, and a mirrored concave-safe bottom cap. The last
  platform-path `UnityEngine.Random` uses are gone: height deviation draws from the per-platform
  stream and the Perlin height seed derives from the run seed (inspector `seed` is now an override).
  `AreaGeneratorConfig` slims to material/colorVariation; the removed `AreaSceneEntrypoint` shape
  inspector fields moved to the `PlatformShapeConfig` SO. Combat unchanged this phase: the grid still
  scan-fits inside the (now hex-shaped) boundary — the shared-source grid is the next phase.

### Fixed
- **Platform — `PlatformRegistry.GetPlatformAtPosition` tested the world-space point against the
  platform-local `TopBoundary`,** so the polygon test never matched and detection silently rode the
  ≤5u nearest-platform fallback — invisible at 3–6u blobs, breaking at arena-sized hex platforms.
  The query point is now brought into platform-local space first. *(platform)*

### Added
- **Platform & Area Generation — `PlatformShapeConfig` SO + one-source-of-truth hex tiling (phase 2
  of the platform-hex rework):** new data-only SO **`PlatformShapeConfig`**
  (`Create → Level Generation → Platform Shape Config`; asset at
  `Resources/LevelGeneration/PlatformShapeConfig.asset`) carrying the hex tiling (cell size 2,
  flat-top), the four per-content-kind `ShapeProfileData` blocks (Empty 2–4 / Loot 3–5 /
  Combat 12–18 / NPC 4–7 cells + 0–8 compactness), the **battlefield minimum (12 cells)**, the rim
  tunables (width 1.2 / jitter 35% / drop 0.4), and the body/layout values that will move off
  `AreaGeneratorConfig` (thickness / gap / height deviation) plus the muted-tiling `_cellInset`.
  `PlatformShapeConfigMapper` is the only SO→Core bridge (null → code defaults, identical to an
  unedited asset — asserted by `PlatformShapeConfigMapperTests`). `AreaInstaller` binds the mapped
  `PlatformShapeSettings` (inspector field with a Resources fallback) and now builds **`CombatConfig`
  from those settings** instead of the hardcoded `2f`/`Flat` literals, so the combat grid and the
  future hex ground share one authored source for cell size/orientation (value-identical today).

### Added
- **Platform & Area Generation — hex-surface domain (phase 1 of the platform-hex rework; verified
  brief `product-requirements/platform-hex-surface-and-shape.md`; ROADMAP Track B step 2, M3):**
  the pure-C# foundation that makes a platform's top surface and its combat grid one thing. New in
  `Combat.Battlefield` Core: **`HexMetrics`** (shared axial↔local math — exact port of the legacy
  `FlatHexGrid`/`PointyHexGrid` center formulas, edge-aligned neighbor order, cube rounding) and
  **`PlatformHexSurface`** (the immutable source of truth: whole-cell set, centroid-recentered local
  cell positions, `CenterCell`, the walkable hex-union `Outline`, the index-aligned
  `SubdividedOutline`/`RimRing` pair, and the empty `BlockedCells` seam reserved for biome features).
  New in `LevelGeneration.Surface`: **`PlatformSurfaceGenerator`** (deterministic weighted blob
  growth to a per-profile cell count with a 0–8 `Compactness` dial, hole-filling so the interior is
  complete whole cells — brief §3, and a `guaranteedMinCells` floor for combat platforms — brief §6),
  **`HexOutlineExtractor`** (border-segment stitching via quantized endpoints into one CCW loop),
  **`PlatformRimBuilder`** (subdivided outline pushed outward by a jittered rim width + tangential
  wobble — the organic silhouette, dressing only), **`PlatformShapeSettings`**/**`ShapeProfile`**
  (UnityEngine-free dials: hex size/orientation, four per-content-kind profiles, battlefield minimum
  **12 cells**, rim tunables, and the layout values that will move off `AreaGeneratorConfig`), and
  **`PlatformContentKind`**(+`Resolver` deriving Empty/Loot/Combat/Npc from what `GraphNode` already
  carries — `Type == Combat` wins, covering ambient monsters and story-with-required-combat). All
  seeded via `IRandomSource`; same seed → same cells/outline/rim (brief §8). Nothing consumes the
  domain yet — the generation/combat cutover is the next phases. Tests: `HexMetricsTests`,
  `HexOutlineExtractorTests`, `PlatformRimBuilderTests`, `PlatformSurfaceGeneratorTests`,
  `PlatformShapeSettingsTests`, `PlatformContentKindResolverTests` (38 total; the pure-C# 32 verified
  green outside Unity).

### Added
- **Narrative/World — world content density: a rare, breathing world (verified brief
  `product-requirements/world-content-density.md`; `narrative-procedural.md` §2.6; ROADMAP Track B
  step 1):** the streaming director stops filling windows to the narrative weight budget; each
  platform slot is allocated one of the **four content kinds** (`design/world/content-kinds.md`) —
  **Empty/traversal** (the budgeted majority), **Loot·scattered** (simple low-tier finds from the
  biome `_platformTable`, rolled deterministically by `AreaGenerator` and spawned by the existing
  pickup runtime — the streaming loot path is live), **Combat·wild-beast** (ambient aggressive
  monsters drawn from a per-biome pool at flat difficulty, spawned as `EnemyContent` with no quest or
  dialogue — the main combat source), and **NPC·quest-bearer** (rare: a seeded
  1-in-`AveragePlatformsPerQuest` roll behind a hard `MinPlatformsBetweenQuests` spacing carried
  across windows; an unfillable quest slot degrades to the ambient draw without resetting spacing).
  New pure-C# `WorldContentAllocator` + `WorldContentDensitySettings` +
  `IBiomeMonsterPoolCatalog`/`BiomeMonsterPoolCatalog` (`Narrative.Director.Core`); new SOs
  **`WorldContentDensityConfig`** (the one world-fullness asset —
  `Resources/Narrative/WorldContentDensityConfig.asset`, defaults 1-in-10 / spacing 4 /
  empty 65 / loot 15 / combat 20) and **`BiomeMonsterPoolDefinition`**
  (`Resources/Combat/MonsterPools/MonsterPool_Forest.asset`, seeded with the test enemy + the bandit
  brute) with mappers, bound in `NarrativeSliceInstaller`. `PlannedPlatform` gains
  `AmbientCombat(enemyId)`/`LootDrop()` (the reserved `Combat`/`Loot` kinds are now emitted);
  `RunStreamingCoordinator` maps them to `EnemyContent`/`PlatformContentType.Loot` nodes.
  `AreaInstaller` now auto-loads enemy definitions from **both** `Resources/Enemies/Definitions` and
  `Resources/Narrative/Enemies` (deduped by id) so pooled and story enemies resolve in combat.
  Tests: new `WorldContentAllocatorTests` (9); `RunWindowPlannerTests` reworked to the density model
  (ambient emission, cross-window spacing, all-kind same-seed determinism).

### Changed
- **Narrative — `RunPacingConfig`/`RunPacingSettings` slimmed to window mechanics** (`_windowSize`,
  `_lookAheadWindows`): `_narrativeBudgetPerWindow`, `_minCombatPerWindow` and `_maxCombatPerWindow`
  are **removed** — superseded by `WorldContentDensityConfig` (no `RunPacingConfig.asset` existed, so
  no asset migration). `RunWindowPlanner` drops the two-phase budget fill; story selection (thread
  preference + seeded tie-break, eligibility, recasting) is unchanged and runs only for quest slots.
  **Determinism note:** the allocator's extra seeded draws shift the shared PRNG stream, so the same
  seed produces a *different* world than pre-change builds (same-seed-same-world still holds within a
  build). `StoryTemplate.Weight` is currently unread (kept for future pacing use).

### Added
- **Mutation — mutation choice cards: the unseal menu is a hand of cards (verified brief
  `product-requirements/mutation-choice-cards.md`; `mutation-subsystem.md` §2.4; ROADMAP Track A
  step 2):** each variant is a **card** — front face = the part pictured centre (its `ChoiceIcon`)
  + the granted active/passive **ability icons** beneath (no stat blocks; crafting traits stay
  hidden); a corner **FLIP** button shows the **replaced part** + its abilities on the back
  ("nothing replaced" for an empty slot); **hovering an ability icon** opens a name+description
  tooltip (`AbilityTooltipView` — first tooltip in the project, deliberately mutation-local);
  **hovering the part picture** opens a **mini 3D model popover** of the hero wearing the offered
  part (`MutationModelPreviewRig`: a hidden hero clone built via `IModularCharacterFactory` at
  `MutationPreviewSettings.RigWorldOffset`, camera → RenderTexture → RawImage; new `Preview`
  settings block on `MutationConfig`, all tunables authored). **Card grammar:** colour = the
  blank's species archetype tint (belonging), frame **glow brightness = rarity tier** (potency,
  placeholder treatment — the ROADMAP tier-glow polish folds in here). **Two-step commit:** the
  first click selects (highlight + "choose again to graft" hint), a second click on the same card
  grafts — a deliberate confirmation, not an idle tap. Data path: `IMutationPartCatalog` gains
  `TryGetCardData` (per-part `MutationPartCardData`: name/icon/tier/abilities), resolved through
  the combat `IPartAbilityResolver` (single-part query) so the card lists **exactly** the ability
  set combat composes; `MutationChoiceViewData` now carries front/back `MutationCardFaceViewData` +
  slot/part ids + tier; the old part resolves via `IMutationCharacter.TryGetEquippedPartId`.
  Presenter contract unchanged (`OnChoiceSelected` = confirmed pick); flip/tooltip/popover/selection
  are view-layer presentation. Prefabs hand-authored: `MutationChoicePanel.prefab` reworked in place
  (+tooltip/popover nodes), `MutationChoiceButton.prefab` → **`MutationCard.prefab`** (GUID stable),
  new `MutationAbilityIcon.prefab`. `ModularCharacterVisual` gains a read-only `Assembly` accessor.
  Tests: `MutationPartCatalogTests` card-data cases (real-resolver parity incl. dedupe);
  `MutationVariantPresenterTests` front/back/tier/empty-slot/fallback cases.

### Removed
- **Mutation — `MutationChoiceUISetup` editor generator deleted** (`Tools → Mutation → Setup
  Stage-Up Choice UI`): the card prefabs are hand-authored source of truth now; rerunning the
  generator would have overwritten them with the legacy `Text` button layout.
- **Mutation/Inventory — the feed→tally→digestion→stage-up loop is deleted (Socketed Blanks
  migration, brief §18; ROADMAP "Crafting & Mutation"):** mutations are obtained only through
  blanks now. Deleted: `FeedingSession`/`IFeedingSession`, `FeedingPresenter`,
  `IFeedingView`/`FeedingView`/`ArchetypeReadoutEntry`, the `InventoryMode`/`IInventoryModeState`
  mode switch (crafting and the operating table share one screen), the `FeedingUISetup` editor
  tool, `IMutationTally`/`MutationTally`, `IDigestionProgress`/`DigestionProgress`,
  `ArtifactArchetypeProfile`, `IMutationOptionBuilder`/`MutationOptionBuilder`,
  `MutationScoringParameters`, `ArtifactArchetypeMapper`, `ArchetypeWeight`, `ArchetypeAffinity`,
  and their test fixtures. Trimmed: `ArtifactDefinition._archetypeWeights` (species lives on the
  blank now), `PartDefinition._archetypeAffinities` (trait affinities remain),
  `MutationConfig._digestionThreshold`/`_maxMutationOptions`/`_rarityUnlockPointsPerTier`,
  `InventoryConfig` feeding + fail-return fields, the HUD feed toggle
  (`IInventoryHudView`/prefab subtree), `IInventoryStageView.SetFeedingFraming`, and the
  `FeedingArea` + `PuffFail` subtrees in `InventoryStage.prefab` (all internal references
  validated). `MutationCandidatePart` is trait-only; `MutationContentValidator` validates trait
  affinities + blanks (artifact traits are the Inventory validator's job).
  `ArchetypeDefinition`/`IArchetypeCatalog` stay — blanks' species markers and card tints consume
  them. Docs: `mutation-subsystem.md` fully rewritten around Socketed Blanks;
  `inventory-subsystem.md` R24–R26 removed and rewired to the one-screen layout;
  `character-system.md` PartDefinition reference updated.

### Added
- **Mutation/Inventory — operating-table UI: the Socketed Blanks loop is playable (Phase 3;
  `mutation-subsystem.md` §2.7–§2.8; ROADMAP "Crafting & Mutation"):** the open cauldron screen now
  hosts crafting **and** the operating table (no mode switch). A world-space **blank rack** sits
  left of the cauldron (`BlankRackArea` authored into `InventoryStage.prefab`: anchors + disabled
  entry/socket templates; `BlankRackView`/`BlankEntryView`/`SocketView` + pure-C#
  `BlankRackPresenter` seeding `MutationConfig.StartingBlanks`). **Drag & drop:** `StageClickRouter`
  became **`StageDragRouter`** (same meta GUID — prefab wiring untouched): sub-threshold
  press/release = click (`IStageClickable`); dragging a pot bubble moves it on its camera plane
  (pot drift skips `BubbleView.IsDragged`; its collider disables so the release raycast sees the
  socket) and releasing over a socket (`IArtifactDropTarget`) sockets the artifact; clicking a
  filled socket unsockets it. **Filling the last socket unseals**: `MutationVariantPresenter`
  (renamed from `MutationChoicePresenter`; now triggered by `ISocketingModel.OnBlankReady`, with a
  ready-queue) shows the variant cards on the existing `MutationChoicePanel`, and the pick installs
  via `IMutationCharacter.SwapPart`, consumes the reagents (`ConsumeSockets`), and spends the blank
  (`IBlankRack.Remove`); a failed swap keeps the cards up. Socket state persists across
  inventory open/close (multi-track incubation). Demo content: `Trait_Wood`; raw `Artifact_Stick`/
  `Artifact_Needle` and crafted `Artifact_Mace` (stone+wood/heavy, t2) / `Artifact_Stinger`
  (chitin/sharp+toxic, t2) with signature recipes rock+stick→mace, needle+bacteria→stinger;
  starting inventory extended. Tests: `MutationVariantPresenterTests` (replaces
  `MutationChoicePresenterTests`) — fill→cards→pick→swap+consume+spend, failure keeps state,
  equipped excluded, raw-only still offers, ready-queue.

### Changed
- **Mutation — the feeding loop is now a dead end (pending removal):** the digestion ready signal
  has no consumer since the stage-up choice presenter became the unseal variant presenter. Feeding
  still fills the tally/digestion bars; the whole path is deleted in the Socketed Blanks migration
  phase (ROADMAP).
- **Mutation — Socketed Blanks domain (Phase 2, not yet player-facing; `mutation-subsystem.md` §2.6;
  ROADMAP "Crafting & Mutation"):** the pure-C# heart of the new mutation source. New
  `PartBlankDefinition` SO (*Create → Mutation → Part Blank*, auto-loaded from
  `Resources/Mutation/Blanks/`; slot + species/passport archetype + socket count; 3 shipped:
  skull/claw-arm/haunch) with `PartBlankCatalog` (`IPartBlankCatalog`/`IPartBlankDataSource`).
  Domain: `BlankRack` (capped by `MutationConfig.BlankRackCapacity` — the multi-track incubation
  tension), `SocketingModel` (socket pulls the artifact from the inventory, unsocket returns it,
  **filling the last socket raises `OnBlankReady` and commits** — no rearrange after full;
  `ConsumeSockets` destroys reagents on pick, `ReturnAll` refunds on close), `BlankVariantBuilder`
  (unseal menu: socketed profiles combined through the **same cauldron fusion grammar** so sockets
  interact, then non-equipped parts of the blank's slot scored by trait-affinity overlap × a
  tier-driven rarity gate; deterministic, no zero-score filter), and the cauldron-voice **seam**
  `SocketingTrendEvaluator`/`ISocketingTrendSource` (post-grammar trend per socket change; no
  consumer yet). `PartDefinition` gains `TraitAffinities` (new `TraitAffinity` beside
  `ArchetypeAffinity`), carried into `MutationCandidatePart.TraitAffinity` by `MutationPartCatalog`;
  the 6 Head/ArmL/LegL part assets are trait-tagged. `MutationConfig` gains
  `BlankRackCapacity`/`MaxVariantOptions`/`TierUnlockPerRarityTier`/`StartingBlanks`;
  `MutationContentValidator` now also checks part trait affinities and blanks (slot, species,
  ≥2 candidates per slot). Wired in `MutationInstaller`. Edit-mode tests: `BlankRackTests`,
  `SocketingModelTests`, `BlankVariantBuilderTests`, `SocketingTrendEvaluatorTests` (+
  `MutationPartCatalogTests` trait cases) — 88 domain tests green. The feeding loop remains the
  live mutation source until the operating-table UI lands (next phase).
- **Inventory/Crafting — artifact trait model + two-tier emergent fusion (Socketed Blanks Phase 1;
  `inventory-subsystem.md` R16–R17; ROADMAP "Crafting & Mutation"):** artifacts now carry **function**
  — `Substance`/`Property` trait tags (new `TraitDefinition` SO, *Create → Inventory → Trait*,
  auto-loaded from `Resources/Artifacts/Traits/`; shipped vocabulary: stone/water/fire/chitin/rot +
  sharp/heavy/toxic/focusing/fiery) and an integer **tier** (0 = raw find). The cauldron combine is
  now **two-tier and never fails**: an authored signature `RecipeDefinition` wins; otherwise a pure-C#
  emergent grammar (`EmergentFusionCalculator`: trait union → authored `FusionRuleDefinition`s applied
  in ordinal rule-id order (combine/transmute, cascading) → tier from max input + rule deltas +
  amplify-on-duplicates) computes a target profile and `ArtifactByTraitSelector` deterministically
  picks the best-matching authored artifact (inputs excluded; ordinal tie-break). New
  `IFusionResolver`/`FusionResolver`, `IArtifactTraitSource`/`ArtifactTraitIndex` (SO→Core bridge),
  `ITraitCatalog`/`TraitCatalog`, `FusionRuleSetBuilder`, `FusionSettings` (tunables on
  `InventoryConfig`), and a startup `ArtifactContentValidator` (broken/misplaced trait refs,
  trait-less artifacts, unreachable rule outputs). The 7 shipped artifacts are trait-tagged; 2 demo
  fusion rules ship (`heat_hardens`, `rot_spreads`). Edit-mode tests: `ArtifactTraitProfileTests`,
  `TraitFusionRuleSetTests`, `EmergentFusionCalculatorTests`, `ArtifactByTraitSelectorTests`,
  `FusionResolverTests` (57 domain tests green incl. the reworked `CraftingSessionTests`).

### Changed
- **Inventory/Crafting — craft failure path removed (R18 retired):** `CraftingSession` resolves through
  `IFusionResolver` and always succeeds; `OnCraftFailed`, the last-item-return rule, the dark fail
  puff, and the glide-back animation are gone (`ICraftingSession`, `CraftingPresenter`,
  `ICraftingSlotsView.PlayCraftFailure/PlayFailPuff`, `CraftingSlotsView`). `OnCraftSucceeded` now
  reports `(result, isSignature)`; presentation does not differentiate signature vs emergent yet
  (ROADMAP polish item). `ArtifactDefinition._archetypeWeights` stays temporarily (legacy feeding
  path) until the Socketed Blanks migration phase removes it.
- **Per-system logging (implemented — `logging.md`):** runtime logs can now be muted or raised
  **per system** so a feature can be tested without the flood of unrelated logs. New pure-C# core
  (`LogCategory`, `LogLevel`, `LogLevelPolicy` with edit-mode `LogLevelPolicyTests`), a `LoggingConfig`
  ScriptableObject switchboard (`Resources/Configs/LoggingConfig`, per-system + master verbosity
  ceilings, seeded with every category at Info, code-default "all on" if absent), and a category-aware
  `IGameLogger`/`UnityGameLogger` that consults the config before writing to the Console and prefixes
  each line with `[Category]`. Wired in `LoggingInstaller`. **R1–R6**. `IGameLogger.Info/Warning/Error`
  now require a `LogCategory`; all existing `IGameLogger` call sites (Inventory, Loot, Mutation,
  CharacterSystem) were migrated.
- **Per-system logging — raw `Debug.*` sweep complete (`logging.md`, ROADMAP):** every
  `Debug.Log/LogWarning/LogError` in runtime gameplay code (Combat, Narrative, Dialogue, Platform,
  Character, Camera, LevelGeneration, Area, Inventory, Loot, Mutation, CharacterSystem) now routes
  through the categorized `IGameLogger`, so any system can be muted/raised from `LoggingConfig` while
  testing a feature. Logger threaded via constructor (container-bound), owner (manually-`new`'d leaves:
  presenters, AI strategies, platform content, hex-cell states), or null-safe `[Inject]`
  (scene/prefab MonoBehaviours). Deliberately left on `Debug.*`: `UnityGameLogger` (the adapter),
  `LoggingInstaller` bootstrap, Zenject installer auto-load diagnostics (service-locator at install
  time, §4), and `Scripts/Editor/**` tooling.
- **NPC Proximity Interaction (implemented — `npc-proximity-interaction.md`):** approaching an NPC is now
  deliberate. Talkable NPCs show an **F** prompt inside a global interaction radius and open their
  conversation on **F**; hostile NPCs start their battle on their own when the player crosses a global
  aggro radius — no prompt, no dialogue. **Intent is derived from the placement-time casting + story** (no
  authored hostility flag): a filled quest slot → quest-bearer (`?`), else a **required (non-optional)
  combat slot** → hostile (`!`), else plain (no marker). An *optional* combat slot is a dialogue branch
  (e.g. the raider you can fight or bribe), so that NPC stays talkable rather than auto-engaging.
  Always-visible billboarded `?`/`!` markers and a **name label** sit above
  each NPC. New pure-C# core (`NpcIntentResolver`, `ProximityEvaluator`) with edit-mode tests
  (`NpcIntentResolverTests`, `ProximityEvaluatorTests`); `NpcInteractionConfig` SO for the two radii
  (`Resources/Narrative/NpcInteractionConfig`, code defaults if absent); a dev radius overlay (**F2**,
  editor/dev-build only). Wired via `NpcInteractionInstaller`. Demo: `DemoStory_BarnVictim` → `?`,
  `DemoStory_BarnRaid` → plain/talkable (fight-or-bribe is a dialogue choice — its combat slot is
  optional), dialogue-only stories → plain. The auto-aggro `!` path needs a story with a **required**
  combat slot; none ships in the demo (adding an always-eligible one perturbs the seeded streaming
  threads — see the system doc §6).
- **NPC Proximity Interaction (product requirement authored):** new verified
  product-owner brief `product-requirements/npc-proximity-interaction.md` replacing the implicit
  land-on-platform encounter trigger with **proximity + button**. NPC **intent is derived from the
  fact-state** (no authored hostility flag) into three states: **quest-bearer** (`?`, F prompt →
  dialogue), **hostile** (`!`, auto-battle on entering the aggro radius), and **plain** (no marker, F
  prompt → chat). An NPC is hostile when the facts leave it **no quest to offer** *and* it has **an
  enemy available** — typically a fact-gated story variant whose quest line is closed to the player
  while a combat slot is filled; the same character reads as `?`/talkable under friendlier facts. All
  three signals already exist pre-dialogue (`IPreconditionEvaluator` story eligibility at plan time,
  `Casting.OptionalQuest`, `Casting.CombatAllowed`/`OptionalEnemyId`); the code track computes the
  casting at spawn time and carries it. Always-visible markers; both radii are **global config
  values** with a toggleable **dev debug overlay** that draws the circles in play mode for tuning.
  Brief only — no engine/asset change in this entry; the code track owns implementation. Added to the
  briefs index in `product-requirements/README.md`.
- **Data-Driven Procedural Narrative (deeper demo fact-web — director fact-analysis coverage):** the demo
  slice grows from one thread to **two parallel threads** so the director's eligibility analysis is
  exercised across previously-untested paths (content/asset-only — no engine change). New `frog_marsh`
  thread tests **passport/faction gating** (D15/D16): four stories gated on the passport fact
  `world.reads_as_frogfolk` — `MarshPool` (a card sets the fact, standing in for the mutation→fact
  projection), `FrogElderClosed` (passport-negative, recurs until you pass), `FrogElderOpen` (passport-
  positive, offers `qst_frog_errand`), `FrogMarshThanks` (the thread's 2nd beat — completes it). Running
  alongside `barn_raid` (distinct `_threadId`s eligible together) it exercises **multi-thread within-window
  coherence** (D12/D14). The `barn_raid` thread gains the **cross-actor moral fork** (`quest-as-reward.md`
  §4): `RaiderMotive` is reworked from an arc-closer into the recurring raider's **counter-offer** —
  a quest-offer card OFFERING `qst_raider_run` (power currency, `1× fire`) opposed to the farmer's bounty
  (access currency), same tier, separated in time on the shared actor's thread, writing
  `world.raider_offer_taken` + closing the arc (D13). New assets: 4 `FactKeyDefinition`s
  (`raider_offer_taken`, `reads_as_frogfolk`, `frog_quest_offered`, `frog_quest_accepted`) registered in
  `DemoFactKeyRegistry`; `arch_frogfolk` archetype (faction `marsh_folk`); `qst_raider_run` +
  `qst_frog_errand`; 4 stories (`.ink` + hand-authored compiled `.json` + `StoryTemplate` + `DialogueDefinition`).
  All facts Bool (numeric-threshold facts deferred). See `narrative-procedural.md` §4 (demo slice).
- **Encounter Dialogue UI (Hades-style presentation/feel upgrade — R1–R12):** the NPC encounter is now
  a bottom-centre dialogue **box** with the speaker's portrait + name, the current line revealed **word
  by word** at a tunable global reading speed (tap to instantly complete), choice **cards** centred
  above the box that appear only once the line finishes, and author-marked **`[[ ]]` key words** tinted
  in both lines and cards. The **quest card is labelled with the job** — the offered quest's title
  (`DisplayName`) + objective/summary (`Summary`), not just the reply text. New pure-C#
  `KeywordHighlightFormatter` (`[[word]] → <color>` rich text, markers stripped; unit-tested);
  `EncounterCardViewData` gains optional `QuestTitle`/`QuestObjective`; `DialogueRunner` gains read-only
  `OfferedQuest` / `EncounterArchetypeId` / `EncounterDisplayName` seams (no engine behavior change);
  `IEncounterCardHandView` gains `SetPortrait(archetypeId)`. The view (`EncounterCardHandView` /
  `EncounterCardView`) moves to `TextMeshProUGUI` and reveals via TMP `maxVisibleCharacters` stepped to
  word boundaries (rich-text color spans stay intact), resolves the portrait through the already-bound
  `INpcArchetypeCatalog`, and falls back to a neutral placeholder for portrait-less NPCs. The presenter
  stays UnityEngine-free (forwards the archetype **id**, not a `Sprite`); the conversation engine
  (facts/quests/combat) is unchanged. Demo: `BarnVictim` marks `[[Raiders]]` in the victim's line and
  the bounty quest summary so the key word reads the same in the line and on the quest card. New
  `KeywordHighlightFormatterTests` + `EncounterCardHandPresenterTests` cases. The box layout (bottom-
  centre, portrait `Image`, TMP text, full-box tap catcher, card anchor above the box) is wired in the
  `EncounterCardHandView` / `EncounterCardView` prefabs. See new doc `encounter-dialogue-ui.md`;
  `narrative-procedural.md` §2.7.
- **Data-Driven Procedural Narrative / Quests (Encounter card-hand UI — presentation cutover, MVP):** the
  NPC encounter now presents a **situation bubble + a composed hand of typed cards** instead of the
  line-reading + Ink choice-list panel. New `Narrative.Encounter` MVP slice: `EncounterCardType`
  (`QuestOffer`/`Attack`/`Leave`/`Talk`), the `EncounterCardViewData` DTO, the pure-C#
  `EncounterCardHandPresenter`, and the `IEncounterCardHandView` + thin `EncounterCardHandView` /
  `EncounterCardView` MonoBehaviours. The presenter **composes** the hand off the runner's existing events:
  a `QuestOffer` card per Ink choice; an `Attack` card from an Ink choice tagged `# card: attack` (or,
  only at a decision point, a system-added card when the casting is combat-capable and none was authored);
  and an always-present `Leave` card. The fact/quest/tag engine is unchanged — only presentation + choice
  selection. `DialogueRunner` gains `CombatAvailable`/`CombatEnemyId`, `TriggerCombat()` (player-initiated
  combat reusing the `start-combat:` suspend/resume path), and `Leave()` (graceful `"leave"` end).
  `NarrativeSliceInstaller` binds the card-hand view (the authored `Prefabs/UI/Encounter/EncounterCardHandView`
  + `EncounterCardView` prefab pair; the view auto-creates an `EventSystem` if the scene lacks one) +
  presenter; the old `IDialogueView` / `DialogueRunnerViewPresenter` are left **dormant (unbound)** for the
  separate UI-removal follow-up. The barn demo migrated: `BarnVictim` drops its decline choice (now the
  system Leave card; `world.barn_quest_accepted` stays default-false), `BarnRaid`'s fight choice is tagged
  `# card: attack` (its `combat_won → world.grain_recovered` write-back preserved). New
  `EncounterCardHandPresenterTests` + `DialogueRunnerTests` cases (129 domain tests green). See
  `narrative-procedural.md` §2.7/§6; `quest-subsystem.md` §6.

### Fixed
- **Narrative / Combat (post-combat dialogue branch never ran — quest never closed, grateful farmer never
  appeared):** a `# start-combat` encounter that branches on `combat_won` (e.g. `BarnRaid.ink`'s `resolve`)
  always took the **lose** branch. The runner suspends on the `start-combat` *tag*, which is processed
  *after* `DialogueSession.Continue()`, but Ink's look-ahead in that same `Continue()` already evaluated the
  following `{ combat_won }` conditional with the default `false` and locked the branch — so the win branch
  (which sets `grain_recovered` and fires `# complete-quest`) was unreachable, and `ReportCombatResult(true)`
  on resume came too late. Fixed by inserting a plain stop line at the start of `resolve` (before the
  conditional) so the branch is evaluated only after the runner resumes with the real result; documented the
  authoring rule in the `.ink`. Verified end-to-end via runtime logs (win branch + `complete-quest -> Completed`).
- **Narrative / NPC Proximity (subsequent quests didn't record; threads mis-sequenced):** the streaming
  planner generated the next window on platform **entry**, but proximity defers an encounter's fact-writes to
  the player's F-press, so windows were planned against stale facts (re-placing already-offered quest stories
  whose re-offer no-ops, and placing closed/no-quest variants). `RunStreamingCoordinator` now advances on
  platform **exit**, so an engaging player's choices are written before the next window is planned.
- **Quests (cross-encounter completion + Defeat-type bounty):** `DialogueRunner.HandleCompleteQuest` now
  resolves the target quest by the tag's id from the live `ILiveQuestRegistry` when the current casting does
  not carry it, so an encounter can close a quest offered elsewhere. Made the barn bounty (`qst_barn_bounty`,
  now a **Defeat** objective) complete on the raider's defeat via `# complete-quest` in `BarnRaid.ink`'s
  combat-won branch — no delivery NPC required (the grateful farmer remains a fallback).
- **Data-Driven Procedural Narrative / Quests (offer-quest stories crashed on accept):** accepting a quest
  from an NPC threw `[InkStoryManager] Failed to set variable 'quest_accepted': Cannot assign to a variable
  that hasn't been declared`. `DialogueRunner.HandleOfferQuest` always injects the runner-side
  `quest_accepted` Ink variable, but three `# offer-quest:` stories never declared it (only `GratefulFarmer`
  did). Added `VAR quest_accepted = false` to `BarnVictim`, `RaiderMotive`, and `FrogElderOpen` (`.ink`
  + lockstep compiled `.json`), per the runner-injected-variable contract in `narrative-procedural.md` §4 —
  content/asset-only, no engine change.

### Changed
- **NPC encounters no longer auto-start on land (NPC Proximity Interaction R2):** `PlatformStateFactory`
  no longer maps NPC content to `DialogueActiveState` when a platform is entered; it adds
  `CreateDialogueState()` for the on-demand F path. `DialogueActiveState` now **reuses the placement-time
  casting** (`DialogueRunner.Begin(NpcContent.Casting)`) instead of re-casting via
  `EncounterDirector.BeginPlanned`, so the casting is computed once (deterministic) and the shown intent
  matches the encounter that plays. `RunStreamingCoordinator` casts + resolves intent at window generation.
- **Encounter Dialogue UI (Continue gate removed — lines auto-reveal into choices, R6):** the encounter
  no longer gates each line behind a **Continue** button. A narration line now shows **alone** (no cards
  while it types); once it finishes revealing the presenter **auto-advances** so the choice cards appear
  automatically. Picking a quest/talk card shows the branch's closing reply (its `offer-quest`/`fact`
  tags applied as before) and a tap then closes the box; the always-present **Leave** card now appears
  only in the choice hand, not during the opening line. Implemented in presentation only — `DialogueRunner`
  and the fact/quest/tag engine are unchanged: `IEncounterCardHandView` drops `ShowContinueAffordance` and
  gains `OnRevealCompleted`; `EncounterCardHandView` raises it on reveal-complete (the Continue button is
  removed from `EncounterCardHandView.prefab`); `EncounterCardHandPresenter` clears the hand on a line,
  auto-advances pre-choice lines, and holds a closing reply (`_closingReplyPending`) for the dismiss tap.
  `EncounterCardHandPresenterTests` updated. See `encounter-dialogue-ui.md` §2.3.
- **Data-Driven Procedural Narrative (demo story text localized to Russian):** all 9 demo stories'
  player-facing text — situation lines, choice-card labels, outcomes — translated to Russian in the
  `.ink` sources and their lockstep compiled `.json` (Russian guillemets «» replace the escaped speech
  quotes; engine tags `# fact:`/`# offer-quest:`/`# speaker:` and Ink structure/variables untouched).
  Quest `DisplayName`/`Summary`/objective descriptions (`BarnBounty`, `RaiderRun`, `FrogErrand`) and NPC
  display-name pools (`arch_villager`, `arch_barn_raider`, `arch_frogfolk`) localized too; the `[[ ]]`
  keyword (`[[Raiders]]`→`[[Налётчики]]`) stays consistent between `BarnVictim`'s line and the bounty
  quest summary. Content/asset-only, no engine change. All `.json` re-validated. **Note:** the TMP font
  asset must carry Cyrillic + «» glyphs for in-game rendering (editor-side check).
- **Loot / Quest Subsystem (reward granting reads the live quest registry):** `QuestRewardGranter` now
  constructor-injects `ILiveQuestRegistry` (not `DialogueRunner`) and `GrantFor` scans `LiveQuests` for any
  `Completed`, not-yet-paid quest, granting and marking each (idempotent). This makes the payout land on
  whichever platform sees a quest completed (cross-dialogue), and models more than one completed quest per
  platform — removing the prior single-`ActiveQuest` fragility. `QuestRewardGranterTests` updated to drive
  through the registry. See `quest-subsystem.md` §2.3–§2.4.

### Removed
- **Data-Driven Procedural Narrative (Demo slice — old branches pruned to barn-only):** removed the
  "Razor Pass" (bandit) and "Gorge Toll" (sellsword) demo branches so the slice ships only the latest barn
  arc. Deleted stories `DemoStory_RazorPassToll`, `DemoStory_GratefulCaravan`, `DemoStory_GorgeToll`,
  `DemoStory_RewardedWarden`; dialogues `DemoDlg_TollShakedown`, `DemoDlg_CaravanThanks`, `DemoDlg_GorgeToll`,
  `DemoDlg_RoadReward` (+ `RazorPassToll`/`CaravanThanks`/`DemoDlg_GorgeToll`/`DemoDlg_RoadReward` ink/json);
  archetypes `DemoArch_RoadBandit`, `DemoArch_CaravanMerchant`, `DemoArch_Sellsword`; the quest
  `DemoQst_ClearPass` (the barn-victim story's optional Quest slot + `BarnVictim.ink`'s `offer-quest:` were
  dropped with it); and fact keys `world.pass_cleared`, `world.pass_blocked`, `world.gorge_cleared`,
  `actor.hostile`, `faction.reputation` (deregistered from `DemoFactKeyRegistry`). Re-pointed the curated
  `TypedFacts` vocabulary (and `TypedFactsTests`) from the retired pass/hostile/reputation keys to the
  surviving barn facts (`world.barn_raided`, `world.grain_recovered`, `actor.looted_barn`) so the startup
  D3 drift check stays green. `DemoEnemy_BanditBrute` is kept (barn-raid combat slot). See
  `narrative-procedural.md` §4.

### Added
- **Quest Subsystem (M2 — cross-dialogue quest continuity; R8):** a live `QuestInstance` now outlives the
  dialogue that offered it, so a quest can be offered on platform A and completed on a later platform B.
  New pure-C# run-scoped `ILiveQuestRegistry` / `LiveQuestRegistry` (`Narrative/Quests/Core`, idempotent
  `Register` on quest id, `TryGet`, ordered `LiveQuests`), bound `AsSingle` in `NarrativeSliceInstaller`
  beside `ILiveActorRegistry`. `DialogueRunner.Begin` now **restores** the registered instance when the
  casting carries a quest already offered this run (matched by id via its Quest slot) instead of nulling
  its active quest; `HandleOfferQuest` registers a newly minted instance and reuses a restored one (no
  second `Start`). The live registry is the run-scoped counterpart of the progression record (which still
  holds only status flags, kept in sync via `QuestInstance`'s recorder calls). Demo proof: split the barn
  bounty across two windows — `story_barn_victim` (window 1) now carries the `bounty` Quest slot and offers
  `qst_barn_bounty` on the "bring your grain back" branch; `story_grateful_farmer` (window 2) restores and
  completes it (its `GratefulFarmer.ink` no longer offers). Tested by new `LiveQuestRegistryTests` and a
  cross-dialogue case in `DialogueRunnerTests`. See `quest-subsystem.md` (R8, §2); `narrative-procedural.md` §4.
- **Developer Tools (new — in-game dev state overlay):** added a key-toggled IMGUI overlay (default
  **F1**, editor / development-build only) that displays live game state in generic sections. First two
  sections: **Quests** (`DisplayName (questId): Status` grouped Active/Completed/Failed, ids mapped to
  names via `IFragmentLibrary`) and **Director Facts** (`key = value [Type]` from `IFactStore.Snapshot()`).
  Pure-C# `DevStatePresenter` (`IDevStateSource`) builds the sections from `IRunProgressionRecord` +
  `IFragmentLibrary` + `IFactStore`; thin `DevOverlayView` renders them. Wired by `DevToolsInstaller`
  (non-Mono `Installer<T>`, mirrors `LoggingInstaller`), installed from `AreaInstaller` under
  `#if UNITY_EDITOR || DEVELOPMENT_BUILD` so it never ships. Tested by `DevStatePresenterTests`. New
  `dev-tools.md`. See `dev-tools.md`.
- **Quest Subsystem (Demo — barn slice exercises the quest loop end-to-end):** added
  `DemoQst_BarnBounty` (`qst_barn_bounty`, tag `bounty`, objective `obj_return_grain`, reward `1× rock`,
  no fact effects) under `Resources/Narrative/Quests/` (auto-loaded by `NarrativeSliceInstaller`). Gave
  `DemoStory_GratefulFarmer` an optional `bounty` Quest slot (filled by tag) and rewrote
  `GratefulFarmer.ink` to offer/advance/complete the quest via `offer-quest:`/`advance-objective:`/
  `complete-quest:`, so the bounty's item reward is granted on platform completion. Reachable in-engine
  after accepting the barn-victim's plea and winning the raid (the existing window-2 reaction-A path).
  Single-session by design — the runner's `ActiveQuest` is per-dialogue, so offer and complete live in the
  same conversation. See `narrative-procedural.md` §4; `quest-subsystem.md`.
- **Quest Subsystem (M2 — quest lifecycle loop closed; R2–R7):** a quest can now finish, not just
  start. Added three Ink lifecycle tags parsed by `DialogueTagParser` and dispatched by
  `DialogueRunner`: `advance-objective: <objectiveId> [amount]`, `complete-quest:`, `fail-quest:`.
  `DialogueRunner` drives `QuestInstance.AdvanceObjective`/`Complete`/`Fail`, applies the emitted fact
  effects gated against the **quest's own footprint** (not the dialogue session's), records each
  transition via `IRunProgressionRecorder` (closing the Character Progression completion/failure-recording
  gap), and raises new `OnQuestCompleted`/`OnQuestFailed` events. Completion is explicit — advancing
  objectives never auto-completes. Tags firing with no active quest fail closed (warn + no-op).
  Quest-carried **item rewards**: `QuestDefinition._rewards` (`QuestRewardSerial` = artifact id +
  count) → `QuestData.Rewards` (`QuestRewardCore`) via `QuestMapper`; `QuestRewardGranter` (previously a
  no-op) now reads the singleton `DialogueRunner.ActiveQuest` and, when `Completed`, grants each reward to
  `IInventoryModel` through the existing `PlatformCompletedState` hook (covers both dialogue-ended and
  combat-won routes). New tests: `DialogueRunnerTests` (offer→complete/fail, objective advance,
  no-active-quest no-op), `DialogueTagParserTests` (new tags), `QuestRewardGranterTests`, plus
  `QuestInstance`/`QuestMapper` reward coverage. New `quest-subsystem.md`; `narrative-procedural.md`
  tag bridge + quest SO section updated to point to it. See `quest-subsystem.md`.
- **Data-Driven Procedural Narrative (Demo slice — barn two-window reactive demo, exercises D5/D15 +
  D11/D16):** expanded the single-beat barn slice into a two-window partition demo that makes fact-based
  window-2 selection legible end-to-end. Window 1 places two world-gated openers: `DemoStory_BarnVictim`
  (`story_barn_victim`, archetype `arch_villager`/`DemoArch_Villager`, dialogue `DemoDlg_BarnVictim`/
  `BarnVictim.ink`, optional `errand` quest slot) whose choice writes `world.barn_quest_accepted`; and the
  updated `DemoStory_BarnRaid` (now with an **optional Combat slot** req tag `bandit` → `enemy_bandit_brute`,
  thread `barn_raid`) whose `BarnRaid.ink` forks into **fight** (`start-combat:` → on `combat_won` sets
  `world.grain_recovered`, clears `actor.$self.looted_barn`) and **let-go** (sets `world.raider_bribed`,
  leaves `looted_barn` true). Window 2 the director places **exactly one** of three reactions purely from
  those facts — A `DemoStory_GratefulFarmer` (`barn_quest_accepted == true` AND `grain_recovered == true`),
  B `DemoStory_RaiderMotive` (actor-scoped `actor.$self.looted_barn == true`, recasts the **same** raider),
  C `DemoStory_StarvingVillage` (`barn_quest_accepted == false` AND `grain_recovered == true`); `grain_recovered`
  and `looted_barn` are mutually exclusive by the raider choice, so the three partition the space. Four new
  fact keys (`world.barn_quest_offered`, `world.barn_quest_accepted`, `world.grain_recovered`,
  `world.raider_bribed`, all Bool/Global) registered in `DemoFactKeyRegistry`; two new villager dialogues
  (`DemoDlg_GratefulFarmer`/`GratefulFarmer.ink`, `DemoDlg_StarvingVillage`/`StarvingVillage.ink`). Data-only;
  auto-loaded from `Resources/Narrative/*`, no code or scene changes. New ink/added dialogues ship with **seed
  compiled JSON** re-derived on editor import. Test: `RunWindowPlannerTests.BarnDemo_Window2SelectionPartitions_ByWindow1Choices`
  drives all four window-1 combos and asserts the single expected window-2 story (and the same recast raider
  `InstanceId` for B). See `narrative-procedural.md` §4.
- **Data-Driven Procedural Narrative (Demo slice — recurring-actor "raider arc", exercises D11/D16):**
  a third Demo branch that makes the new actor-scoped eligibility + recurring-actor casting visible
  in-engine (the in-engine analogue of the `RunWindowPlannerTests` proof). New facts `world.barn_raided`
  (Bool/Global gate) and `actor.looted_barn` (Bool/**PerActor** arc fact) added to `DemoFactKeyRegistry`;
  archetype `DemoArch_BarnRaider` (`raider`,`can-fight`); stories `DemoStory_BarnRaid` (world-gated
  `barn_raided == false`; tags `barn`/`raider`) and `DemoStory_RaiderMotive` (actor-gated
  `actor.$self.looted_barn == true`; tag `motive`); dialogues `DemoDlg_BarnRaid` (writes `world.barn_raided`
  + `actor.$self.looted_barn`) and `DemoDlg_RaiderMotive` (clears `actor.$self.looted_barn`), with
  `BarnRaid.ink`/`RaiderMotive.ink` and **real compiled** `.json` (fact tags verified by playing the
  compiled story). Flow: the barn story mints a raider and writes its actor fact → the next window recasts
  the **same** raider into the motive story by that fact (the `motive` tag overlaps no archetype, so
  placement proves the hard pin). Data-only; auto-loaded from `Resources/Narrative/*` (installer lists are
  empty), no code or scene changes. See `narrative-procedural.md` §4.
- **Data-Driven Procedural Narrative (director Priority-1 — actor/faction eligibility + recurring-actor
  casting, D16/D11):** `RunWindowPlanner` now gates storylets on **actor- and faction-scoped facts**, not
  only world/global, and **recasts a recurring `NpcInstance`** across stories instead of minting a fresh
  actor every time. Eligibility is classified per story: a **world-only** story (no `$`-context token in
  its preconditions) keeps the prior path — evaluated actor-less, then a fresh actor minted and recorded
  as live; an **actor/faction-scoped** story is resolved as a **casting query** — the planner finds the
  first live actor (deterministic registration order) whose facts satisfy the precondition when bound as
  `$self`/`$faction`, makes the story eligible, and **hard-pins** that actor for the recast (overriding
  the soft P1 tag preference). Continuation semantics: a positive actor-scoped gate only opens once a
  qualifying actor already exists. New pure-C# `ILiveActorRegistry`/`LiveActorRegistry`
  (`Narrative.Actors.Core`) holds the run's minted actors in deterministic order (R12) and is bound
  `AsSingle` in `NarrativeSliceInstaller`, injected into the planner. Unblocks the passport loop, character
  arcs, and the mirror antagonist. Tests: `RunWindowPlannerTests` gains the raider-arc proof
  (`RecurringActor_RecastIntoMotiveStory_ByActorScopedFact`), `ActorScopedStory_WithoutALiveActor_IsIneligible`,
  and `WrongActor_DoesNotSatisfyActorScopedGate`. Still open (ROADMAP): D7 spine lane, D13/D14 first-class
  threads, D19 escalation tier, D20 meta-scoped horizon, and save-capture of the live-actor registry. See
  `narrative-procedural.md` §2.2/§2.6/§5/§6; `narrative-director-requirements.md` (Priority-1 marked done).
- **Data-Driven Procedural Narrative (Demo slice expanded — two branches, tag-based actors):** the
  `Resources/Narrative/*` Demo set now drives two independent starting encounters that each write a
  different fact and open a different follow-up, exercising eligibility, fact-gating, and tag-based actor
  selection. New facts `world.gorge_cleared` (added to `DemoFactKeyRegistry`); archetypes
  `DemoArch_Sellsword` (`mercenary`,`can-fight`) and `DemoArch_CaravanMerchant` (`merchant`,`trader`)
  (all archetypes incl. `DemoArch_RoadBandit` now use `PlaceholderAssembly_A` for a visible body);
  dialogues `DemoDlg_GorgeToll` (writes `gorge_cleared`) and `DemoDlg_RoadReward`; stories
  `DemoStory_GorgeToll` (→ sellsword) and `DemoStory_RewardedWarden`. `DemoStory_RazorPassToll`
  precondition changed to `pass_cleared == false` (was `pass_blocked == true`, which never flipped → the
  story repeated) and tagged `road`/`bandit`; `DemoStory_GratefulCaravan` tagged `trade`/`merchant`. The
  two new dialogues carry placeholder compiled JSON (copies of the toll/thanks `.json`); their `.ink` must
  be compiled and pasted into the matching `.json` for the authored text. Data-only; no code changes.
  See `narrative-procedural.md` §4.
- **Data-Driven Procedural Narrative (streaming cutover — the new engine now drives gameplay):** platform
  encounters now run through the fact-driven engine instead of the legacy dialogue path. New
  `RunStreamingCoordinator` (`LevelGeneration`) plans + generates platforms **window-by-window** as the
  player advances: it generates window 0 at start and, on each `PlatformEvents.OnPlatformEntered` into the
  current frontier, locks that window and plans the next against the **live** fact store (R7). Each planned
  story platform is realised as an `NpcContent` carrying the planner's minted `NpcInstance` + committed
  `StoryTemplateData` (new ctor; spawns its visual via `IModularCharacterFactory` from the archetype's
  assembly). `AreaGenerator` gains a `PrebuiltContent` path (and `GraphNode.PrebuiltContent`) so the
  coordinator attaches narrative content directly. `EncounterDirector.BeginPlanned(story, actor)` casts +
  begins a pre-selected encounter (selection happened at plan time). `DialogueActiveState` is rewritten as
  a thin entry adapter: it calls `BeginPlanned`, sets the portrait from the archetype, and routes runner
  outcomes — combat → spawn `EnemyContent` + `CombatActiveState`; normal end → completed. `CombatActiveState`
  feeds the suspended runner `ReportCombatResult(playerWon)` so post-combat lines/facts replay before the
  platform completes. New `INpcArchetypeCatalog`/`NpcArchetypeCatalog` (id → archetype SO for visuals).
  `AreaSceneEntrypoint` drops the legacy `LevelNarrativeGenerator`/`ScenarioGenerator` narrative path and
  drives the coordinator; `NarrativeSliceInstaller` binds the planner/pacing/catalog (it was already in the
  Area SceneContext). The legacy `NarrativeInstaller` stays installed but **dormant** (its `IDialogueView`
  is reused; its presenter/generator bindings are unused) and is removed in the next stage. *Known gaps
  this stage:* loot is not placed on the streaming path (fillers are empty), biome is fixed (Forest), and
  encounters require new-engine content whose archetype tags overlap story tags. See
  `narrative-procedural.md` §2.6.
- **Data-Driven Procedural Narrative (story-first windowed director — planner core):** first slice of the
  streaming, budgeted director that replaces actor-first per-encounter selection. New pure-C#
  `RunWindowPlanner` (`IRunWindowPlanner`, `Narrative.Director.Core`): `PlanWindow(windowIndex, facts)`
  selects a budgeted set of stories for a window against the **live** fact store (R6/R7), then matches an
  actor archetype to each. It (1) filters stories whose preconditions pass and that have a matching
  archetype, (2) places combat-bearing stories until `MinCombatPerWindow` is met, (3) fills the rest
  within the narrative weight budget while combat stays under `MaxCombatPerWindow`, (4) pads to
  `WindowSize` with empty fillers. Combat is its own budget dimension, separate from narrative weight;
  selection prefers continuing a thread already chosen this window, then a seeded pick (B2 determinism).
  New Core types `WindowPlan`/`PlannedPlatform`/`RunPacingSettings`; new `RunPacingConfig` SO
  (`Create → Narrative → Director → Run Pacing Config`) + `RunPacingConfigMapper`. `StoryTemplate`/
  `StoryTemplateData` gain a `Weight` (pacing cost; a story is still one platform). Not yet wired into
  generation (the streaming coordinator + cutover is the next stage). Tests: `RunWindowPlannerTests`
  (budget cap, combat min/max, determinism, R7 eligibility shift across windows, archetype match/skip,
  actor assignment). See `narrative-procedural.md` §2.6.
- **Data-Driven Procedural Narrative (encounter orchestration core):** the production entry point that
  turns a placed actor into a running dialogue — the `SelectNext → Cast → Begin` chain the slice tests
  proved but no gameplay code drove yet. New pure-C# `EncounterDirector` (`Narrative.Director.Core`):
  `BeginEncounter(NpcInstance)` binds the actor's `$self`/`$faction`, asks `RunDirector.SelectNext` for an
  eligible storylet against the live fact store (R6/R7), `CastingFactory.Cast`s it onto the actor
  (R3/R5), and starts the `DialogueRunner`; returns `false` (runner untouched) when the actor is null, no
  storylet is eligible, or the storylet can't be cast. New `IActorInstanceFactory`/`ActorInstanceFactory`
  (`Narrative.Actors.Core`) mints a per-run `NpcInstance` from an `NpcArchetypeData` — run-unique
  deterministic instance id + seeded name-pool draw + faction (R12). Both bound `AsSingle` in
  `NarrativeSliceInstaller`. Still no gameplay trigger (the platform-state adapter that calls
  `BeginEncounter` and routes combat/quest/ended signals is the next cutover stage); documented in
  `narrative-procedural.md` §2.5. Tests: `ActorInstanceFactoryTests`, `EncounterDirectorTests` (incl. the
  R7 fact-coupling proof end-to-end through the orchestrator).
- **Data-Driven Procedural Narrative (dialogue view adapter + continue-gated pumping):** the fact-driven
  `DialogueRunner` is now bound to the game UI. New MVP presenter `DialogueRunnerViewPresenter` (pure-C#)
  subscribes to the runner's events and drives the existing `IDialogueView` (speaker, line text, choices,
  hide/show), forwards the view's choice/continue input back into the runner, and re-exposes the
  combat/quest signals for the later encounter integration; wired in `NarrativeSliceInstaller`
  (`BindInterfacesTo<DialogueRunnerViewPresenter>().AsSingle().NonLazy()`), reusing the view bound by the
  Area-scene `NarrativeInstaller`. `LoggingInstaller` (installed by `AreaInstaller`) now owns the single
  `IGameLogger` binding so the additive slice installer can resolve it without a duplicate `AsSingle`.
  `DialogueRunner` gains **continue-gated pumping**: it emits one readable line then parks in the new
  `DialogueRunnerState.AwaitingContinue` until `Continue()` (driven by the view's continue/skip input)
  advances it — so a multi-line knot is read one line at a time instead of collapsing to the last line.
  No-text tag steps (`speaker:`/`fact:`) still flow without gating; combat suspension (`AwaitingExternal`)
  is unchanged and remains the only non-savepoint. `DialogueRunnerTests` updated for the gate + new
  `MultiLineKnot_EmitsOneLineAtATime_GatedByContinue`, `NoTextTagSteps_DoNotGate_OnlyTheTextLineGates`,
  and `Continue_WhenNotGated_IsNoOp`.
- **Data-Driven Procedural Narrative (vertical slice — R1–R14):** new recombinable narrative system in
  which actor identity, dialogue, quest, enemy, and story are orthogonal fragments matched into typed
  story slots by semantic tags, composed at runtime by a casting layer, and coupled **only** through one
  unified namespaced fact store. New pure-C# `Narrative.Facts.Core` (`FactStore`/`IFactStore` with the
  B4 presence-vs-default contract and stable-ordered snapshot; `FactKey`/`FactValue`; typed
  `FactKeyRegistry`; `PreconditionEvaluator`; `FactEffectApplier` — a footprint-gated write chokepoint
  comparing by namespace+key+unresolved subject token; `SubjectResolver` with open `$<contextKey>`
  tokens; typed accessors `WorldFacts`/`ActorFacts`/`FactionFacts` + drift check). Fragment Core +
  SO + static mapper for each of `NpcArchetype`, `DialogueDefinition`, `QuestDefinition`(+objectives),
  `StoryTemplate`(+slots); authoring structs `FactPredicateSerial`/`FactEffectSerial`/`FactKeyShape`
  and SO vocabulary `FactKeyDefinition`/`FactKeyRegistry`. Runtime: `NpcInstance`, `DialogueSession`
  (fresh-start-vs-restore variable injection), `QuestInstance` (lifecycle + legacy
  `IRunProgressionRecorder` bridge), `Casting`/`ContextBag` (role/hostility derived, not stored),
  `FragmentLibrary` + `CastingFactory` (tag-match fill, deterministic tie-break, derived advisory
  footprint), `RunDirector` + serializable `DeterministicRandom` PRNG, and `DialogueRunner` — the Ink
  tag bridge with an explicit `Running/AwaitingExternal/Ended` suspension state machine for async
  combat, empty-optional-slot fail-closed handling, and write-back into Ink variables. Save boundary
  (`INarrativeSaveService` + snapshot DTOs incl. PRNG state; suspended dialogues are non-savepoints).
  Zenject wiring `NarrativeSliceInstaller` + `NarrativeSliceBootstrap` (footprint derivation + ref
  validation); `EnemyDefinition` gains `_enemyTags` for combat-slot matching. Slice content: Ink
  `RazorPassToll.ink` + `CaravanThanks.ink`. New doc `narrative-procedural.md`. 84 edit-mode tests,
  including `CrossStorylet_ChoiceInOneThreadChangesEligibilityInAnother_ViaFacts` — the executable R7
  proof that clearing the pass (thread `road`) makes the caravan storylet (thread `trade`) eligible
  purely through `world.pass_cleared`. Legacy `NarrativeInstaller`/`CompositeDialoguePresenter` and the
  old `Story`/`NPC` SOs remain alongside, untouched, pending a later cutover.
- **Data-Driven Procedural Narrative (Demo content + auto-load):** ready-made `Demo*` asset set under
  `Resources/Narrative/` — four `FactKeyDefinition`s (`pass_blocked`/`pass_cleared`/`actor.hostile`/
  `faction.reputation`) + `DemoFactKeyRegistry`, `DemoArch_RoadBandit`, `DemoDlg_TollShakedown` +
  `DemoDlg_CaravanThanks` (wired to the compiled Ink JSON), `DemoQst_ClearPass`, `DemoEnemy_BanditBrute`
  (tag `bandit`), and `DemoStory_RazorPassToll` + `DemoStory_GratefulCaravan`. `NarrativeSliceInstaller`
  now auto-loads these from `Resources/Narrative/*` when its inspector lists are empty
  (`ResolveAssetsFromResources`), so the slice runs without per-scene wiring.
- **Character Progression / Narrative (M2 — run progression record):** new per-run state service that
  systems can query for what the player has done this run. New pure-C# `CharacterProgression/Core`:
  `QuestStatus` (`Active`/`Completed`/`Failed`), split read/write surfaces `IRunProgressionRecord` /
  `IRunProgressionRecorder` implemented by `RunProgressionRecord` (terminal status wins over active;
  idempotent; null/empty-id safe), and `RunConditionEvaluator` — a single-predicate condition parser
  (`quest_completed:` / `quest_active:` / `quest_failed:` / `npc_encountered:`; empty passes;
  unknown/malformed fails closed and warns). `AreaInstaller` binds the record (both interfaces) +
  evaluator `AsSingle`. Recording hooks added to `DialogueActiveState`: an NPC dialogue start records
  the NPC encounter, and an Ink `start_quest` signal marks the quest active (replacing a bare log).
  Consumer wired: `RewardResolver` now **evaluates `RewardSlot.Condition`** (previously stored but
  ignored), skipping slots whose condition does not hold against the run record before the probability
  roll; `NarrativeInstaller` injects the record + evaluator. New doc `character-progression.md`. Tests:
  `RunProgressionRecordTests`, `RunConditionEvaluatorTests`, `RewardResolverConditionTests`.
- **Mutation Subsystem / Character System (M2 — part-driven affinity + scored selection):** stage-up
  mutation options are now chosen by scoring **every** candidate body part against the cumulative feed
  tally instead of walking per-archetype option lists. `PartDefinition` (CharacterSystem) gains the
  mutation data `_archetypeAffinities` (`ArchetypeAffinity[]`: archetype id + 0..1 weight), `_rarity`
  (`MutationRarity` enum, `Common`…`Mythical`), and `_choiceIcon` (Sprite). New pure-C# Core:
  `MutationCandidatePart`, `MutationScoringParameters`, and a rewritten `MutationOptionBuilder` whose
  score is `(affinity·tally) × (1 + RarityWeight·tier·unlock)` — `unlock` ramping with accumulated
  points so a lower-affinity rare part can overtake a common one over a stage; parts with no affinity
  to anything fed (score 0) and equipped parts are excluded; top-N by descending score, ordinal
  part-id tie-break. New `IMutationPartCatalog`/`MutationPartCatalog` builds the candidate set from the
  CharacterSystem `IPartCatalog` and serves the per-part choice icon. `MutationConfig` gains
  `_rarityWeight` (0.5) and `_rarityUnlockPointsPerTier` (10). Shipped `*_B` parts authored with
  placeholder affinity/rarity/icon (migrated from the removed part sets). Tests:
  `MutationOptionBuilderTests` (rewritten as scoring tests), new `MutationPartCatalogTests`, updated
  `MutationChoicePresenterTests`. `PartDefinition` also gains a `_displayName` (friendly UI label,
  authored on the shipped `*_B` parts) used as the mutation choice-button label, falling back to the
  asset name when blank. *(Supersedes the M1 "Archetype → body-part-set mapping" item.)*
- **Ability Subsystem / Character System / Mutation (M1):** body parts now grant combat abilities,
  closing the mutation core loop end-to-end. `PartDefinition` gains `_activeAbilities`
  (`AbilityDefinition[]`) and `_passiveAbilities` (`PassiveAbilityDefinition[]`). New
  `PassiveAbilityDefinition` SO (*Create → Combat → Abilities → Passive Ability*) references a
  Buff/Debuff `StatusEffectDefinition` applied as a standing modifier for the whole combat. New
  pure-C# bridge in `Combat/Integration`: `IPartAbilityResolver`/`PartAbilityResolver` resolves a
  `PartAbilitySet` (active + passive, deduped) from a character's equipped parts via `IPartCatalog`
  (bound in `AreaInstaller`). `CharacterCombatInitializer` now builds the player `Unit`'s ability set
  from the live equipped parts at combat start (`IModularCharacter.EquippedParts`), applying passives
  as infinite-duration status effects; `HeroDefinition.Abilities` remains a logged fallback when no
  part grants an active ability. `IModularCharacter` gains an `EquippedParts` (slotId→partId) query
  (Ability R23–R26; mutation "swap updates abilities" satisfied by pull-at-combat-init).
  Tests: `PartAbilityResolverTests`, `DamageSystemModifierTests`, `StatusEffectDurationsTests`, and
  `CharacterAssemblyStateTests` (equipped-parts query).

### Changed
- **Platform & Area Generation (incremental groundwork):** `AreaGenerator` is split into `Initialize()`
  (prepare an empty area + reset the layout cursor) and `AppendPlatforms(nodes)` (lay out and instantiate
  a batch of platforms, linking each to the previous one and keeping the cursor across calls). The legacy
  one-shot `Generate()` is now `Initialize()` + `AppendPlatforms(all graph nodes)` — behavior-identical
  (linear chain; the disabled branch-edge path is unchanged) — so the upcoming streaming director can add
  platforms one window at a time. No gameplay change.
- **Combat:** standing Buff/Debuff modifiers now affect outgoing damage. `DamageSystem.CalculateFinalDamage`
  (previously a stub returning base damage, and never called) is implemented to scale damage by the
  attacker's net `StatModifier` × stack count, and `AbilityExecutor` now routes damage through it.
  Status-effect duration ticking moved into the pure, unit-tested `StatusEffectDurations.Tick`, which
  treats a **negative duration as infinite** (never decremented/removed) so part passives persist for
  the whole combat. (Ability R25–R26.)

### Fixed
- **Data-Driven Procedural Narrative (dialogue ended instantly — Ink never entered its start knot):**
  `DialogueSession.StartFresh` skipped `GoToKnot` whenever the declared start knot was literally `"start"`,
  on the false assumption that a fresh Ink story auto-begins there. Ink begins at top-level flow, and the
  slice dialogues are knot-only (no top-level content), so `canContinue` was false and the runner ended
  immediately (`OnDialogueEnded` on enter) — no lines shown. It now always navigates to the declared start
  knot when one is set. (Surfaced only once the real `InkStoryManager` drove gameplay; the slice tests use
  a fake story manager.)
- **Data-Driven Procedural Narrative (streaming director placed no encounters):** `RunWindowPlanner`
  pruned every story whose `StoryTags` didn't overlap an archetype's `ArchetypeTags`, so with the demo
  content (`story_razor_pass_toll` tagged `road` vs. `arch_road_bandit` tagged `bandit`/`can-fight`) no
  story was ever placed and all platforms were empty. Actor↔story matching is now a **soft preference**,
  not a hard filter (P1: hard requirements prune, preferences only weight): a story is eligible regardless
  of archetype tags, and `MatchArchetype` prefers a tag-overlapping archetype but falls back to any (a
  story is only pruned for actor reasons when no archetype exists at all). Tests updated
  (`StoryWithoutTagOverlap_IsStillPlaced`, `NoArchetypesAtAll_PlacesNothing`,
  `OverlappingArchetypePreferredOverNonMatching`). See `narrative-procedural.md` §2.6.
- **Mutation Subsystem:** the stage-up choice now excludes the character's *starting* parts, so a
  stage-1 mutation never re-offers a part the character already wears (a no-op swap).
  `ModularCharacterMutationAdapter.TryGetEquippedPartId` now reads the live
  `IModularCharacter.EquippedParts` snapshot instead of a private write-through cache that only knew
  parts swapped *this run*; the redundant `_equippedBySlot` cache is removed (the swap already
  writes through to the real character). Closes the last M1 Mutation item.
- **Mutation Subsystem:** the stage-up choice no longer crashes the Area scene at Play. The
  `MutationChoiceView` was bound `FromComponentInHierarchy`, which **asserts** when the choice panel
  isn't present (and cascaded into a `ModularCharacterVisual` `NullReferenceException` because the
  failed `SceneContext` resolve left `_factory` un-injected). The view is now **instantiated from a
  prefab** (`FromComponentInNewPrefab`, panel auto-loaded from `Resources/Prefabs/UI/MutationChoicePanel`,
  mirroring `InventoryInstaller`'s HUD view); when the prefab is absent the installer logs a warning and
  skips the view + presenter instead of throwing. **Tools → Mutation → Setup Stage-Up Choice UI** now
  builds the panel as a standalone prefab (plus the button prefab) and clears any panel a prior version
  embedded in `InventoryStage`. Shipped default `ArchetypePartSetDefinition` assets for all five
  archetypes under `Resources/Mutation/PartSets/` (placeholder `.a → .b` swaps) so the loop works out
  of the box.

### Removed
- **Data-Driven Procedural Narrative (legacy narrative engine deleted — Phase 3):** the old generation +
  dual-Ink dialogue path is gone now that the streaming engine drives gameplay. Deleted the whole
  `Narrative/Generation/` (`LevelNarrativeGenerator`/`StoryPool`/`NpcPool`/`RewardResolver`/`LevelNarrative`/
  `NpcAssignment`/`ResolvedReward` + interfaces), the legacy SOs `StoryDefinition`/`NpcDefinition`/
  `RewardDefinition`/`LevelNarrativeConfig` (+ `RewardSlot`/`RewardType`), `CompositeDialoguePresenter`/
  `IDialoguePresenter`, `InkExternalFunctionBinder`/`IInkExternalFunctionBinder`, the `NarrativeInstaller`
  (also removed from the `Area` SceneContext), the `NarrativeSetupEditor` tool, the legacy `Resources`
  assets (NPC/Story definitions, `DefaultLevelNarrativeConfig`) and the legacy-only Ink under
  `Resources/Stories/` (kept `Slice/`). Obsolete tests removed (`LevelNarrativeGeneratorTests`,
  `StoryPoolTests`, `RewardResolverConditionTests`, `CompositeDialoguePresenterTests`,
  `QuestRewardGranterTests`). Kept and decoupled: `NpcContent` (actor/archetype only), `AreaGenerator`
  and `ScenarioGenerator`/`IScenarioGenerator` (dropped the `LevelNarrative` coupling), `CutsceneActiveState`
  (dropped the dialogue-presenter dependency). `IDialogueView` ownership moved to `NarrativeSliceInstaller`
  (instantiated from the `Resources` prefab). `QuestRewardGranter` is now a no-op completion hook
  (item-reward re-homing is a ROADMAP item). Also deleted the now-dead one-shot level-generation pipeline
  (`ScenarioGenerator`/`IScenarioGenerator`, `PlatformGraphGenerator`/`IPlatformGraphGenerator`,
  `GameContext`) and its `AreaInstaller` bindings — the streaming `RunStreamingCoordinator` builds
  platforms per window. `narrative-generation.md` is superseded.
- **Mutation Subsystem (M2):** the per-archetype option model is gone now that affinity/rarity/icon
  live on the parts and selection is scored. Deleted `ArchetypePartSetDefinition` (+ inline
  `MutationOptionEntry`), `MutationOptionMapper`, `IMutationOptionProvider`, `IMutationOptionCatalog`/
  `MutationOptionCatalog`, the five `Resources/Mutation/PartSets/*` assets, and the
  `MutationInstaller._partSetDefinitions` field / `PartSets` load path.

### Added
- **Mutation Subsystem:** stage-up mutation choice + archetype→body-part-set mapping (M1) — closes the
  mutation core loop. When `IDigestionProgress.IsReadyToMutate` flips true, the new
  `MutationChoicePresenter` (NonLazy, on `IDigestionProgress.OnChanged`) offers up to
  `MutationConfig.MaxMutationOptions` (default 3) body-part options derived from the dominant
  archetype(s); picking one swaps the part on the live character via `IModularCharacter.SwapPart` and
  resets **both** `IMutationTally` and `IDigestionProgress` for the next stage. A failed swap (e.g. the
  rig is not yet assembled) or an empty option set leaves the stage untouched so the player keeps
  feeding. New pure-C# Core: `MutationOption`, `IMutationOptionProvider`,
  `IMutationOptionBuilder`/`MutationOptionBuilder` (deterministic across-dominant gather, dedupe by
  part, exclude equipped, cap at max), and the `IMutationCharacter` swap port. New data surface:
  `ArchetypePartSetDefinition` SO (*Create → Mutation → Archetype Part Set*,
  `Resources/Mutation/PartSets/`, one per archetype, inline `MutationOptionEntry` with slot/part id +
  label + icon), `IMutationOptionCatalog`/`MutationOptionCatalog` (fail-fast, `TryGetIcon`), and
  `MutationOptionMapper`. New `ModularCharacterMutationAdapter` (Infrastructure) bridges to the scene's
  `ModularCharacterVisual.Character` lazily and caches swapped-in parts so they are not re-offered. New
  `MutationChoiceView`/`MutationChoiceButton` (+ `MutationChoiceViewData`) thin views. `MutationConfig`
  gains `MaxMutationOptions`; `MutationContentValidator` now also warns on part-set options that
  reference an unknown archetype, part, or mismatched slot. New editor tool **Tools → Mutation → Setup
  Stage-Up Choice UI** (`Scripts/Editor/Mutation/MutationChoiceUISetup.cs`, idempotent) builds the
  choice panel on `InventoryStage.prefab` and the `MutationChoiceButton` prefab. Wired in
  `MutationInstaller`. Tests: `MutationOptionBuilderTests` (10), `MutationChoicePresenterTests` (7).
  **Deferred:** part-derived ability grants (the swapped part does not yet update abilities — Ability
  Subsystem M1); starting parts are not excluded from stage-1 options.
- **Inventory Subsystem:** feeding-mode presentation + one-click setup tool. Feeding now shows **3D
  feeding slots below the pot** (mirroring the crafting slots above) with the cumulative-archetype /
  digestion-progress / dominant-archetype readout and the **Feed** button on a screen-space
  `FeedingReadoutCanvas` anchored to the right of the pot; entering feeding mode **drops the stage
  camera** to reveal the slots below (Inventory R24a). Feeding-tray capacity is configurable
  (`InventoryConfig.FeedingSlotCount`, default 3) and enforced by `FeedingSession`; new
  `InventoryConfig` fields `FeedingSlotCount` / `FeedingCameraDrop` / `FeedingFramingDuration`;
  `InventoryStageView.SetFeedingFraming` performs the camera move, driven by `InventoryPresenter` on
  mode change. New editor tool **Tools → Inventory → Setup Feeding UI**
  (`Scripts/Editor/Inventory/FeedingUISetup.cs`, idempotent) builds the `FeedingArea` (tray anchor +
  readout canvas + wired `FeedingView`) on `InventoryStage.prefab` and the feed-mode toggle on
  `InventoryHud.prefab`, so the prefab setup is a menu click instead of manual authoring. Tests:
  `FeedingSessionTests` gains capacity coverage.
- **Inventory + Mutation Subsystems:** feeding / digestion UI (M1, Inventory R24–R26) — the open
  cauldron now has a **feeding mode** alongside crafting, toggled by a HUD button (`IInventoryModeState`
  arbitrates which mode owns a pot-bubble click; switching modes returns the other mode's staged
  items). In feeding mode, clicking pot bubbles fills a **feeding tray** (`IFeedingSession` /
  `FeedingSession`, pure C#: `TrySelect`/`TryUnselect`/`Consume`/`ReturnAll`), a readout panel shows
  the cumulative archetype weights of the tray plus this-stage digestion progress and the dominant
  archetype(s), and the **Feed** button digests the tray: each artifact maps via
  `ArtifactArchetypeMapper.ToProfile` into `IMutationTally.Add` and advances `IDigestionProgress`.
  New `IDigestionProgress` / `DigestionProgress` (`Mutation.Core`) counts artifacts fed this stage vs.
  the authored `MutationConfig.DigestionThreshold` (`IsReadyToMutate`, `Normalized`, `Reset`);
  `ArtifactArchetypeProfile.Combine` sums several profiles for the cumulative readout. New
  `MutationConfig` SO (*Create → Mutation → Mutation Config*, `Resources/Mutation/MutationConfig.asset`).
  New `FeedingPresenter` and `FeedingView` (+ `ArchetypeReadoutEntry` DTO); `InventoryHudView` gains a
  feed-mode toggle; `InventoryPresenter`/`CraftingPresenter` respect the mode and return their staged
  items on mode switch/close. Wired in `InventoryInstaller` (sessions, mode state, view, presenter)
  and `MutationInstaller` (config + digestion). Tests: `ArtifactArchetypeProfileCombineTests`,
  `DigestionProgressTests`, `FeedingSessionTests`. **Not wired yet:** nothing calls `Reset` on the
  tally/digestion and nothing acts on `IsReadyToMutate` — the stage-up mutation choice (next M1 step)
  drives that; remains on the ROADMAP.
- **Mutation Subsystem:** per-stage mutation tally (M1) — `IMutationTally` / `MutationTally`
  (`Mutation.Core`, pure C#) aggregates the archetype weights of artifacts fed during the current
  **mutation stage**: `Add(ArtifactArchetypeProfile)` sums per archetype, `TotalFor`,
  `Dominant(count)` returns the top-N archetypes (weight desc, ordinal-id tie-break), `OnChanged`
  notifies UI, and `Reset()` starts the next stage. Bound `AsSingle` in `MutationInstaller` (shared
  by the future feeding UI and stage-up mutation choice). The live consumer path is
  `ArtifactArchetypeMapper.ToProfile` → `MutationTally.Add`. Tests: `MutationTallyTests`. Not wired
  yet: nothing calls `Add` (feeding UI) or `Reset` (stage-up choice) — both remain on the ROADMAP.
- **Mutation Subsystem:** new subsystem and its first M1 step — the creature-archetype data surface
  (`mutation-subsystem.md`, Mutation R1–R5). Archetypes are an authorable set of `ArchetypeDefinition`
  ScriptableObjects (*Create → Mutation → Archetype*, auto-loaded from `Resources/Mutation/Archetypes`);
  shipped: reptile, insect, aquatic, mammal, avian. `ArtifactDefinition` gains an `_archetypeWeights`
  array (`ArchetypeWeight` = archetype id + weight) describing how strongly eating an artifact pushes
  the character toward each archetype; the seven shipped artifacts are populated. Pure
  `ArtifactArchetypeProfile` (aggregation: duplicate ids summed, empty/non-positive dropped, ids
  trimmed) with `ArtifactArchetypeMapper` as the SO→Core bridge; `ArchetypeCatalog` (fail-fast on
  empty/duplicate ids); `MutationContentValidator` warns at startup about unknown/empty archetype ids
  on artifacts. New `MutationInstaller` registered on the Area `SceneContext`. Tests:
  `ArtifactArchetypeMapperTests`, `ArchetypeCatalogTests`. The per-stage tally that consumes these
  weights is the follow-up entry above; the feeding UI and mutation choice remain on the ROADMAP.
- **Character Locomotion:** new subsystem driving movement-based animation and facing
  (`character-locomotion.md`). When the Hero moves it blends idle→run via a `Speed` float and yaws
  to face its travel direction along the shortest arc at a configured rate; idle holds the heading
  (Locomotion R1–R5). Pure `LocomotionSolver` (+ `LocomotionSolverTests`), `CharacterLocomotionView`/
  `CharacterLocomotionPresenter`, `LocomotionConfig` SO, and `CharacterLocomotionInstaller`.
  `CharacterMovementController` now exposes `ICharacterVelocityProvider`; `ModularCharacterVisual`
  exposes the assembled rig's `Animator`. Locomotion attaches to the **existing** `Hero.prefab`
  instance in `Area.unity` (already the cameras' `TrackingTarget` and `AreaSceneEntrypoint.characterTransform`):
  `Tools/Character System/Attach Modular Visual To Hero` adds + wires `ModularCharacterVisual` and
  `CharacterLocomotionView` on `Hero.prefab`, and `Tools/Character System/Setup Locomotion In Open
  Scene` registers `CharacterLocomotionInstaller` (+ `LocomotionConfig`) on the SceneContext. Neither
  creates/deletes/moves a Hero, so the camera follow and entry-platform placement stay intact.
- **Docs:** documentation discipline established — `Docs/` is the maintained source of truth, with a
  mandatory per-system template (`_TEMPLATE.md`) requiring a ScriptableObject Reference and an
  Adding Content section. Added `CHANGELOG.md` and `ROADMAP.md`.

### Changed
- **Character System:** placeholder animation tooling now generates a run clip (`PlaceholderRun.anim`)
  alongside the idle clip and a 1D blend-tree controller on a `Speed` parameter
  (`PlaceholderLocomotion.controller`, idle @ 0 / run @ 1) instead of the single-state idle
  controller. `Speed` defaults to 0, so existing characters still idle (Character System §3.1).
- **AI rules:** rewrote `CLAUDE.md` — fixed section numbering, made the AI workflow planning-first
  (plan + doc-impact statement before code), and made data-driven content extension a first-class
  rule.

### Fixed
- **Character Locomotion:** the run animation stopped playing because the assembled rig's Animator
  came up with no controller bound at runtime ("Animator is not playing an AnimatorController"),
  despite the rig prefab referencing a valid controller. `ModularCharacterVisual` now binds the
  controller in code at startup (a serialized `_animatorController`, else the placeholder controller
  from Resources), and `CharacterLocomotionView` skips writing `Speed` until a controller is bound (no
  warning spam).
- **Character Locomotion:** the run gait no longer reads as moving backward. `BuildRunClip` now has a
  `RunGaitSign` (−1) that mirrors the fore/aft swing of the legs/knees/arms/spine so the cycle reads
  forward for the −Z-forward placeholder once it is oriented forward; flip to +1 to reverse. Applied
  via `Rebuild Locomotion Controller` (run-clip content only; no rig regeneration).
- **Character Locomotion:** the character no longer moves backward. The placeholder model faces −Z
  (tail/back on +Z), but the solver aims the rig's +Z at the velocity, so the back led. Fixed by a
  data-driven `ModularCharacterVisual._localRotationEuler` (default `(0,180,0)`) that orients the
  model's front to the host's +Z — correcting both movement facing and forced turn-to-camera facing.
  Runtime-only; no rig regeneration. Real art facing +Z sets it to `(0,0,0)`.
- **Character Locomotion:** the run blend is now applied with a controller-only swap
  (`Tools/Character System/Rebuild Locomotion Controller`) that builds the run clip + blend-tree
  controller and assigns it onto the existing rig prefab via a `LoadPrefabContents`/`SaveAsPrefabAsset`
  round-trip, preserving the rig's root-GameObject fileID that `SkeletonDefinition._rigPrefab`
  references. Previously a full regenerate churned those fileIDs, so the skeleton's rig reference
  resolved to null and `ModularCharacterFactory` aborted ("assembly/skeleton/rig prefab is missing").
  `CharacterLocomotionView` now writes `Speed` only when the controller exposes it.
- **Editor tooling:** SceneContext installer registration used `SerializedObject.FindProperty("_installers")`,
  but the field is `_monoInstallers` (`[FormerlySerializedAs("_installers")]`, which `FindProperty`
  ignores) — it returned null and threw. Now registers through the public `Context.Installers` setter
  (`SceneLocomotionSetup`, `PlaceholderCharacterGenerator`).
- **Character System:** placeholder asset generation no longer wipes every definition to a blank
  shell when generation throws midway. `AssetDatabase.SaveAssets()` ran *after* the `try`, so any
  exception skipped it and left `SerializedObject`-configured definitions (parts, slots, sockets,
  skeleton, assembly) empty on disk — which then crashed `PartCatalog` ("empty id") at scene load.
  `SaveAssets`/`Refresh` and the rig cleanup now run in a `finally`.

### Removed
- _none yet_

<!--
Entry shape to copy:

## [Unreleased]
### Added
- **<System>:** <what changed> (<requirement id, e.g. Loot R7>).
-->
