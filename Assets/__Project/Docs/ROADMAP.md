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
- [ ] `[content]` `RewardSlot.Condition` is stored but never evaluated.
- [ ] `[rule]` Generator logs via `Debug.Log/LogWarning` directly — violates the logger-abstraction rule (§9).

---

## Character Progression

New work (no system doc yet — author `character-progression.md` when implemented):
- [ ] `[arch]` **M2 — Run progression record.** A per-run record of quests completed / failed /
  in-progress and other run-meaningful state (NPCs encountered, key choices), queryable by the
  narrative system. Backs the existing-but-unused fields `StoryNodeRequirement.RequiredActiveQuests`
  / `RequiredEncounteredNpcs` and unblocks `RewardSlot.Condition` evaluation (Narrative §5).
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
- [x] `[content]` **M1 — Archetype → body-part-set mapping.** Author which body parts/variants each
  archetype can offer at each slot, so a mutation choice resolves to concrete `PartDefinition`s.
  *(Done — `ArchetypePartSetDefinition` SO; `Resources/Mutation/PartSets/`. Shipped content has no
  part-set assets yet — designer authors them.)*
- [ ] `[arch]` **M1 — Mutation swap updates abilities.** When the stage-up choice swaps a part, also
  update the part's active + passive abilities. Blocked on the Ability Subsystem M1 items (abilities
  granted by parts); the swap path is ready to call it.
- [ ] `[arch]` Exclude the character's *starting* parts from stage-1 options. `IMutationCharacter`'s
  adapter only knows parts it swapped in this run, so a starting part can be re-offered once; add an
  equipped-part query to `IModularCharacter` to make exclusion exact.

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

- [ ] `[arch]` **M1 — Abilities granted by body parts.** Associate an ability (and passive
  modifiers) with each `PartDefinition`. Equipping a part makes its active ability available in
  combat and applies its passive buffs; swapping the part out removes both. Wire into the existing
  `HeroDefinition` / `Unit.Abilities` path so combat consumes a part-derived ability set.
- [ ] `[arch]` **M1 — Passive (always-on) abilities.** Add a passive ability/buff kind alongside the
  active Line/Ring abilities: not queued or aimed, applied as standing modifiers for the duration of
  combat. Gives the `Ability` reward type a home (see Cross-cutting).
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
