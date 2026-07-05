# Loot Subsystem

> ⚠️ **STALE (predates the streaming Phase-3 cutover).** This doc references legacy narrative pieces
> that have since been **deleted**: `RewardResolver` / `ResolvedReward`, `StoryDefinition` reward
> slots, `NpcAssignment.Rewards`, `RewardDefinition`, `NarrativeInstaller`, `ScenarioGenerator`,
> `PlatformGraphGenerator` (see ROADMAP "Delete legacy (Phase 3)"). Consequences: (1) the **live**
> quest-reward path is in `quest-subsystem.md` — `QuestRewardGranter` grants the active quest's fixed
> `QuestRewardCore` (id+count), **no roll**; the §2 description of the granter is wrong. (2) **Platform
> loot is live on the streaming path (world-content-density, 2026-07-02):** loot-platform *presence* is
> now decided by the density planner (`WorldContentAllocator` ambient draw, `narrative-procedural.md`
> §2.6) — `PlannedPlatformKind.Loot` maps to `PlatformContentType.Loot` and
> `AreaGenerator.CreateLootContent(nodeId)` rolls the biome `_platformTable` deterministically; the
> pickup runtime (`PlatformLootSpawnCoordinator`/`WorldArtifactView`) is unchanged. Consequently
> `ShouldPlaceLootOnPlatform` and `BiomeLootDefinition._platformLootChance` are **dead on the active
> path** (no callers — presence lives in the density config; removal is a ROADMAP cleanup). Treat §1.6
> (R6), the §2 "Generation integration" gating description, and the granter description as **outdated**
> until this doc is refreshed (ROADMAP `[debt]`). The seed/`WeightedPicker`/biome-table **Core** is
> still valid.

Status: current as of 2026-06-12.

Data-driven artifact rewards integrated with procedural level generation. Artifacts
(the same definitions the pot inventory uses) are acquired through three paths:
platform discovery, enemy defeat, and quest completion.

## 1. Requirements

### Acquisition paths
- **R1** Artifacts can be discovered on generated platforms: generation decides
  per platform (biome-configured probability) whether it carries loot, and which
  artifacts, at generation time. Pickups are interactable as soon as the platform
  is entered.
- **R2** Defeated enemies can drop artifacts at their death location. Drops wait
  in the world for player interaction. Drops happen only when the local player
  wins the combat.
- **R3** Completing a quest (a story NPC platform reaching its completed state)
  grants artifacts directly to the inventory — no world entity, no pickup
  animation.

### Biome mapping and loot tables
- **R4** Each biome (`LevelTheme`: Forest, Desert, Mountain, Cave) maps to its own
  weighted artifact tables and probabilities, separately per acquisition path
  (platform / enemy drop / quest), authored as one `BiomeLootDefinition` asset per
  theme.
- **R5** Enemies may define their own loot pool in enemy data
  (`EnemyDefinition.LootSlots`): independently-rolled slots referencing artifact
  ids with probability, quantity range, and a bonus marker for low-probability,
  high-value extras. An enemy with no slots falls back to the biome enemy drop
  table.
- **R6** Quests additionally deliver the fixed rewards already resolved by the
  narrative system (`StoryDefinition` reward slots → `NpcAssignment.Rewards`);
  this is the fixed/bonus reward channel. Item-type rewards reference artifacts
  via `RewardDefinition.ItemId == ArtifactDefinition.Id`.

### Determinism
- **R7** Rewards are procedurally determined: the same quest/enemy/platform can
  yield different artifacts in different runs, but results are reproducible
  within a run. One run seed (the entrypoint's `seed` field; time-derived when 0)
  is hashed with a stable context key (`platform:{nodeId}`,
  `enemy:{platformId}:{enemyId}`, `quest:{storyId}:{npcId}`) into a per-roll
  `System.Random`, making rolls independent of execution order and of
  `UnityEngine.Random` state.
- **R8** Story/NPC tags influence quest rewards: table entries with matching
  `biasTags` get their weight multiplied by `LootConfig.TagBiasMultiplier`.

### Pickup interaction and visuals
- **R9** Walking into a world artifact plays a placeholder pickup animation
  (grow-then-shrink scale, optional fade, optional drift toward the player) of
  configurable duration (default 0.5 s), then removes the entity and adds the
  artifact(s) to the inventory.
- **R10** World artifacts read as volumes, not flat sprites: a rear sprite-stack
  (progressively darkened layers pushed back on the depth axis, same pattern as
  the inventory `BubbleView`), a blob shadow beneath, and a subtle idle bob.
- **R11** If the inventory cannot accept an item, the pickup is refused with
  shake feedback. The pot inventory is currently unlimited, so this path is
  structurally present but unreachable (see Known limitations).

### Configurability
- **R12** All content tuning is asset-only: biome tables and probabilities
  (`BiomeLootDefinition`), enemy slots (on `EnemyDefinition`), animation timings
  and visual parameters (`LootConfig`), with no code changes.
- **R13** Future progression filtering is structurally supported but not
  enforced: table entries carry `minPlayerLevel`, `requiredAchievements`,
  `requiredPastQuests`; rolls receive a `PlayerLootState`; eligibility runs
  through an `ILootEntryFilter` pipeline (currently a single pass-through
  filter).

## 2. Design

### Layers (Scripts/Loot/)
- **Core** (pure C#, no UnityEngine): `LootSeed` (FNV-1a seed derivation),
  `RunSeedProvider` (per-run context, set at install time) /
  `CurrentThemeProvider` (**set per biome stretch** by the biome journey's
  `BiomeStretchDirector` — see `biome-journey.md`; loot rolls follow the active
  stretch's biome table), `BiomeLootData` / `LootEntryData` / `LootSlotData` (plain
  snapshots), `LootRollContext` / `LootRollResult`, `WeightedPicker`,
  `ILootEntryFilter` + `PassThroughLootFilter`, and `LootRollService` with the
  four operations: `ShouldPlaceLootOnPlatform`, `RollPlatformLoot`,
  `RollEnemyDrops` (slot rolls or biome fallback), `RollQuestRewards`.
- **Data**: `BiomeLootDefinition`, `LootConfig`, `WeightedArtifactEntry`,
  `ArtifactLootSlot` (ScriptableObjects/serializables, data only);
  `BiomeLootCatalog` and `LootSlotMapper` convert assets to Core snapshots at
  install time. `EnemyDefinition` gained `_lootSlots`;
  `EnemyData.LootSlots` carries the mapped slots through `IEnemyDataProvider`.
- **Application**: `QuestRewardGranter` (bridges `ResolvedReward` item rewards +
  procedural biome quest roll into `IInventoryModel`, idempotent per platform),
  `EnemyLootDropper` (rolls and spawns drops at death positions),
  `PlatformLootSpawnCoordinator` (spawns discovery pickups on
  `PlatformEvents.OnPlatformEntered`), `WorldArtifactPresenter` (per pickup:
  player filtering, capacity check, animation, inventory add),
  `IInventoryCapacityPolicy` + `UnlimitedCapacityPolicy`.
- **View**: `WorldArtifactView` (MonoBehaviour adapter: trigger collider,
  coroutine animations, depth stack + shadow + bob) and `WorldArtifactSpawner`
  (wraps the Zenject `WorldArtifactView.Factory`, wires presenter to view).
  Prefab: `Resources/Prefabs/Loot/WorldArtifact.prefab`.

### Generation integration
- `ScenarioGenerator` gates `PlatformContentType.Loot` per platform requirement
  via `ShouldPlaceLootOnPlatform(theme, index)` (requirement order matches graph
  node ids).
- `AreaGenerator.CreateLootContent(nodeId)` rolls the actual artifacts and
  stores them on `LootContent` (items + per-item collected flags; nothing
  respawns on re-entry).
- `AreaSceneEntrypoint` consumes `IRunSeedProvider.RunSeed` for
  `Random.InitState`; `ICurrentThemeProvider` is written by the biome journey's
  `BiomeStretchDirector` per stretch (the entrypoint no longer sets it), and
  `AreaGenerator` reads it live per loot roll.

### Runtime hooks
- Enemy drops: `CombatActiveState.HandleCombatEnded` calls
  `IEnemyLootDropper.DropFor(platform, playerWon)` **before** changing to the
  completed state, because the state change destroys dead enemy GameObjects and
  with them the death positions.
- Quest grants: `PlatformCompletedState.OnEnter` calls
  `IQuestRewardGranter.GrantFor(platform)` — one hook covers both completion
  routes (dialogue ended, combat won).
- `ContentSpawner` returns no prefab for `ContentType.Loot`; pickups are spawned
  exclusively by `PlatformLootSpawnCoordinator`.

### DI and seeding
`LootInstaller` (registered in the Area scene's `SceneContext`) binds everything
and computes the effective run seed **at install time** from the entrypoint's
`seed` field (time-derived fallback when 0). This matters because
`NarrativeInstaller` now seeds `RewardResolver` and `LevelNarrativeGenerator`
with `System.Random` instances derived from the run seed, and those are
constructed during injection — before `GenerateArea` runs. Assets auto-load from
`Resources/Loot/Biomes`, `Resources/Configs/LootConfig` and
`Resources/Prefabs/Loot/WorldArtifact` when inspector fields are empty.

### Adding content without code
- New biome tuning: edit the `Biome_*.asset` under `Resources/Loot/Biomes`
  (Create → Loot → Biome Loot for new themes).
- Enemy drops: add slots to the enemy's `EnemyDefinition` Loot section.
- Animation/visual tuning: edit `Resources/Configs/LootConfig.asset`.
- Fixed quest rewards: add `RewardDefinition` assets (type Item, `ItemId` set to
  an artifact id) to `StoryDefinition` reward slots.

## 3. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:
- `LootSeedTests` — hash stability and sensitivity.
- `WeightedPickerTests` — empty/zero-weight handling, distribution sanity,
  determinism per seed.
- `LootRollServiceTests` — run/context determinism, probability edges, quantity
  ranges, biome fallback, tag bias, filter pipeline, bonus passthrough
  (pure C#, runnable outside Unity).
- `QuestRewardGranterTests` — item rewards reach the inventory, non-item types
  and unknown artifact ids are skipped, procedural rolls are deterministic
  (uses ScriptableObject test fixtures, runs in the editor Test Runner).

## 4. Known limitations / open points

- "Quest completion" means the platform reached `PlatformCompletedState`;
  a dialogue the player walks away from still completes the platform and grants
  rewards. An explicit Ink completion signal is a possible refinement.
- Filler platforms added by `PlatformGraphGenerator` (beyond the scenario's
  requirements) never carry loot; only requirement-driven platforms roll it.
- Non-item `RewardType`s (Currency, Experience, Ability, ...) have no receiving
  system; the granter logs and skips them.
- Inventory capacity feedback (R11) is unreachable while the inventory is
  unlimited; `UnlimitedCapacityPolicy` is the rebinding point.
- Progression gating fields (R13) are serialized and surfaced in the roll
  context but no filter consumes them yet.
- `WorldArtifactView` duplicates the depth-stack construction of
  `BubbleView`; extracting a shared builder is a possible cleanup once a third
  consumer appears.
- World pickups are not despawned when leaving a platform; they persist until
  collected or scene unload.
