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
- **M2 — Progression + Quests + progression-driven narrative.**
- **M3 — Platform/hex rework + content-aware platform shape.**
- **M4 — Combat readability (ability telegraphing) + smarter enemy AI.**
- **M5 — Production character art/animation workflow + world/landscape visuals.**

---

## Cross-cutting

- [ ] `[arch]` **Non-item reward types have no receiving system.** `Currency`, `Experience`,
  `Ability` rewards are rolled by the narrative system and skipped by the loot granter (logged only).
  Planned owners: `Experience` → Character Progression; `Ability` → Ability Subsystem (part-granted);
  `Currency` still open. Wire each receiving system as its owning section lands. *(narrative + loot)*

---

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

---

## Data-Driven Procedural Narrative

Deferred design (from `narrative-procedural.md` §6):
- [ ] `[arch]` **R8 — First-class threads.** Promote `_threadId` from a string label to a `Thread`
  entity (id + stage) the director balances and the player can read; express "a choice in thread A
  affects thread C" as A's effect read by C's precondition (already the mechanism — this adds the
  first-class entity + balancing).
- [ ] `[arch]` **Director pacing.** Add pacing/quotas/thread-balancing to `RunDirector.SelectNext`
  (currently eligibility filter + seeded pick only).
- [ ] `[arch]` **R11 — Reactive-rule cascade layer.** Optional central layer that derives cross-category
  cascades from fact reads/writes; cascades are explicit authored effects until then.
- [ ] `[arch]` **OR/boolean precondition composition.** Preconditions are AND-only; add OR/grouping.
- [ ] `[arch]` **R14 — Save/load file IO.** The serializable boundary (`INarrativeSaveService`, snapshot
  DTOs incl. PRNG state) and the W3-1 suspended-non-savepoint rule exist and are tested; add the file
  writer/reader and the full run-state aggregate (assemble quests/castings/sessions). Optional W3-1
  option-b: persist a suspended dialogue + pending-external descriptor for mid-excursion saves.
- [ ] `[arch]` **Dialogue view adapter.** Wire a Unity `IDialogueView` adapter to `DialogueRunner`'s
  events in the slice installer (runner currently exposes events but no view is bound).
- [ ] `[content]` **PerLocation-scope content.** Data shape supports per-location world facts (A1);
  author content that uses it (e.g. `world.<locationId>.burned` cascades).
- [ ] `[content]` **Optional ambient/bark channel.** The legacy dual-Ink bark channel was dropped; if
  ambient lines are wanted, add a separate non-narrative system rather than overloading the session.
- [ ] `[debt]` **Legacy cutover.** Migrate `DialogueActiveState`/`NpcContent` to castings; delete the
  old `NpcDefinition`/`StoryDefinition`/`CompositeDialoguePresenter`/`NpcAssignment` path and assets.

---

## Character Progression

New work (no system doc yet — author `character-progression.md` when implemented):
- [x] `[arch]` **M2 — Run progression record.** *(Done — pure-C# `CharacterProgression/Core`:
  `IRunProgressionRecord` (read) / `IRunProgressionRecorder` (write) / `RunProgressionRecord`
  tracking quests (active/completed/failed), NPCs encountered, and key choices, plus
  `RunConditionEvaluator`. Recorded from the dialogue flow (NPC encounter + Ink `start_quest`);
  consumed by `RewardResolver` to evaluate `RewardSlot.Condition`. See CHANGELOG;
  `character-progression.md`. Quest **completion/failure** recording and `StoryNodeRequirement`
  gating remain open — see follow-ups below.)*
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

New work (no system doc yet — author `quest-subsystem.md` when implemented):
- [ ] `[arch]` **M2 — Separate quests from stories.** Introduce a `QuestDefinition` SO (objectives,
  state, rewards, tags) distinct from `StoryDefinition`; link stories ↔ quests by reference. A story
  can offer/advance/complete one or more quests; a quest can be referenced by multiple stories.
  Replace the placeholder `QuestContent`.
- [ ] `[content]` **M2 — Quest rewards live on the quest.** Move quest-completion rewards from the
  story's `RewardSlot[]` onto the quest, so story rewards and quest rewards are authored separately.
- [ ] `[arch]` **M2 — Explicit quest-completion signal from Ink.** Complete quests on an explicit Ink
  signal rather than on walking away from dialogue (mirrors the Loot §4 known limitation).

---

## Loot Subsystem

Known limitations (from `loot-subsystem.md` §4):
- [ ] `[content]` Explicit Ink "quest completed" signal — today walking away from a dialogue still
  completes the platform and grants rewards.
- [ ] `[arch]` Filler platforms (beyond scenario requirements) never carry loot; only requirement-driven ones roll it.
- [ ] `[arch]` Non-item `RewardType`s have no receiving system (see Cross-cutting).
- [ ] `[arch]` Inventory capacity feedback (R11) is unreachable while inventory is unlimited;
  `UnlimitedCapacityPolicy` is the rebinding point.
- [ ] `[content]` Progression gating (R13: `minPlayerLevel`, `requiredAchievements`, `requiredPastQuests`)
  is surfaced in the roll context but no `ILootEntryFilter` consumes it yet.
- [ ] `[debt]` `WorldArtifactView` duplicates `BubbleView`'s depth-stack construction; extract a shared
  builder once a third consumer appears.
- [ ] `[arch]` World pickups are not despawned when leaving a platform; they persist until collected or scene unload.

---

## Platform & Area Generation

New work (no system doc yet — author `platform-generation.md` when implemented):
- [ ] `[arch]` **M3 — Hex-composed platform top surface.** Build the platform top-surface mesh from
  the battlefield hex tiling so the combat grid lays perfectly on the platform. The current rect +
  edge-jitter interior (`PlatformMeshBuilder`, `AreaGeneratorConfig.edgeVertexCount` / `edgeJitter`)
  is replaced by a hex-cell field; the combat grid (`PointyHexGrid`,
  `HexGridBase.CalculateCellsInBoundary`) is derived from the same source of truth instead of being
  re-snapped at combat time.
- [ ] `[arch]` **M3 — Natural edges over a complete hex interior.** Edges may follow an organic,
  non-hex silhouette, but every full hex cell must lie on the top surface. Biome features (river,
  mountains) align to hex-cell boundaries — feature regions are placed in hex space so a river's
  banks and a mountain's footprint snap to cells.
- [ ] `[arch]` **M3 — Content-aware platform size & shape.** Platform extent/form is chosen from its
  content: combat-capable content (enemy, or NPC that `CanBecomeEnemy`) requires a battlefield-sized
  hex field meeting combat requirements; loot-only or empty platforms can be smaller. Drive from
  `GraphNode.ContentTypes` / `StoryPlatformData` in `AreaGenerator` rather than the current random
  `platformSizeMin/Max`.
- [ ] `[content]` **M3 — Biome-driven platform appearance & features.** Platform generation consumes
  the level biome (`LevelTheme`: Forest/Desert/Mountain/Cave) for mesh treatment, materials, and
  which hex-aligned features may appear, in addition to the narrative content above.

---

## World & Environment

New work (no system doc yet):
- [ ] `[content]` **M5 — Landscape around platforms.** Generate surrounding terrain/visuals that
  support the platforms and form a cohesive world feel (skybox, distant geometry, biome dressing).
  Read-only relative to gameplay (no grid/collision impact on platforms).

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
- [x] `[arch]` **M2 — Part-driven archetype affinity + scored mutation selection.** *(Done — archetype
  affinity, rarity (`MutationRarity`), and choice icon now live on `PartDefinition`; `MutationPartCatalog`
  builds `MutationCandidatePart`s from the part catalog; `MutationOptionBuilder` scores all candidates
  against the feed tally — `(affinity·tally) × (1 + RarityWeight·tier·unlock)` — and takes the top-N.
  `ArchetypePartSetDefinition` + the option catalog/mapper/provider are removed; balance is tuned via
  `MutationConfig.RarityWeight` / `RarityUnlockPointsPerTier`. Hosting decision: on `PartDefinition`
  (single-asset authoring) — the layering trade-off is recorded as a known limitation in
  `mutation-subsystem.md` §6 rather than being eliminated. See CHANGELOG.)*
- [ ] `[arch]` **M4 — Ability-aware mutation choice panels.** Weight the stage-up choice
  by ability, not just part name/icon/archetype. For the slot being mutated, show a
  before→after comparison per option: the **old** body part with its currently granted
  active + passive abilities vs. the **new** body part with the abilities it would grant,
  highlighting the contrast (added / removed / changed) so the swap's combat consequence
  is legible at choice time.
  - Extend `MutationChoiceViewData` (today `DisplayName`/`Icon`/`Tint`) and the
    `MutationOption → ToViewData` mapping in `MutationChoicePresenter` to carry per-option
    ability info. Resolve the **old** part via `IMutationCharacter.TryGetEquippedPartId(slotId)`
    and the **new** part via the option's `PartId`; read `PartDefinition.ActiveAbilities` /
    `PassiveAbilities`, surfacing each ability's `Name`/`Description`/`Icon` (both
    `AbilityDefinition` and `PassiveAbilityDefinition` expose these).
  - Reuse the `PartAbilityResolver` / `PartAbilitySet` gather+dedupe shape so the panel
    reflects exactly the ability set combat will compose. Keep the presenter pure-C# and
    the view thin (MVP §3); ability metadata is read-only display data.
  - Depends on the **M2 part-driven archetype affinity** item (which moves ability
    references + choice icon onto parts). Complements the backlog "Mutation preview on the
    live character model" item (model preview vs. ability readout).

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
- [ ] `[arch]` **M4 — Ability-queue display.** Show the unit's queued abilities (the existing
  `Unit.AbilityQueue` / `ScheduledAbility`) on the combat HUD with order and per-ability state.
- [ ] `[arch]` **M4 — Ability telegraphing on the battlefield.** Communicate each ability's
  direction and nature (advance/jump-forward, single-target damage, area damage, hook/pull, ring)
  on the grid so the player can make a weighted decision on their turn. Group ability effects into
  categories and surface the grouping in the battlefield/combat UI.
- [ ] `[arch]` **M4 — Smarter ability-using enemy AI.** Improve `TacticalAI` to choose and aim
  abilities well (target selection, area value, direction), beyond the current scoring.

## Inventory Subsystem

- [x] `[arch]` **M1 — Feeding / digestion UI.** A feeding mode (toggled in the open cauldron) where
  pot artifacts are selected into a feeding tray with a cumulative archetype-attribute readout, the
  current digestion progress, and the dominant archetype(s) this stage; the Feed button maps each
  artifact via `ArtifactArchetypeMapper.ToProfile` into `IMutationTally.Add` and advances
  `IDigestionProgress`. *(Done — see CHANGELOG; `inventory-subsystem.md` R24–R26; `mutation-subsystem.md`.)*
- [ ] _seed remaining items from `inventory-subsystem.md` "Known limitations" on next pass._

---

## Backlog (unsorted)

- [ ] `[arch]` Run-state persistence/save-load (needed once the progression record + per-run
  mutation state matter across sessions).
- [ ] `[content]` Mutation preview on the live character model before the player confirms a choice.
- [ ] `[arch]` Enemy-intent telegraph (show enemies' planned abilities) for combat readability.
- [ ] `[content]` Biome ↔ archetype affinity: bias biome loot so a biome nudges the player toward
  certain archetypes, tightening the biome → artifact → mutation loop.
- [ ] `[content]` Quest log UI surfacing the progression record (active/completed/failed).
- [ ] _new ideas land here, then get sorted into a system above._
