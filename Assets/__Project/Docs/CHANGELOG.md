# Changelog

All notable changes to the project are recorded here. Format follows
[Keep a Changelog](https://keepachangelog.com/): newest first, grouped Added / Changed / Fixed /
Removed, each entry tagged with the affected system and (where useful) requirement ids.

Every functional change appends an entry **in the same change as the code** (CLAUDE.md §8.2).

## [Unreleased]

### Added
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
