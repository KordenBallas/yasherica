# Loot Subsystem

> **Refreshed 2026-07-06 to the streaming/density path (P6-1).** The legacy narrative pipeline this doc
> once leaned on was deleted in the Phase-3 cutover — `RewardResolver` / `ResolvedReward`,
> `StoryDefinition` reward slots, `NpcAssignment.Rewards`, `RewardDefinition`, `NarrativeInstaller`,
> `ScenarioGenerator`, `PlatformGraphGenerator` no longer exist (`narrative-generation.md` is the
> tombstone). This document now describes the loot subsystem **as implemented**. Two Core operations
> (`LootRollService.ShouldPlaceLootOnPlatform`, `RollQuestRewards`) and one field
> (`BiomeLootDefinition._platformLootChance`) are **dead — no callers** — kept only until the P6-4
> cleanup; they are flagged inline. The seed / `WeightedPicker` / biome-table Core and the pickup
> runtime are current.

Status: current as of 2026-07-06.

Data-driven artifact rewards integrated with procedural level generation. Artifacts
(the same definitions the pot inventory uses) are acquired through three paths:
platform discovery, enemy defeat, and quest completion.

## 1. Requirements

### Acquisition paths
- **R1** Artifacts can be discovered on generated platforms. Whether a platform
  carries loot is decided by the **world-content-density allocator** (`WorldContentAllocator`'s ambient
  weighted draw, `narrative-procedural.md` §2.6) — **not** a per-biome platform-loot probability. A
  `Loot` slot maps to `PlatformContentType.Loot`, and `AreaGenerator.CreateLootContent(nodeId)` rolls
  **which** artifacts from the biome `_platformTable` at generation time. Pickups are interactable as
  soon as the platform is entered.
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
- **R6** Completing a quest grants a **declared, rolled** reward (Track H, P1-5). A `QuestRewardCore`
  is now a declaration — `(tier, belonging, payload kind)`, never an item id; on completion
  `QuestRewardGranter` asks `IQuestRewardRoller` (`Scripts/Loot/Core/QuestRewardRoller.cs`) for a
  concrete artifact or Part-Blank (belonging a hard filter, tier a nearest-tier bias, deterministic
  under the run seed) and routes it by kind — artifact → inventory, blank → rack. See
  `quest-subsystem.md` R7/§2.4. (The legacy `StoryDefinition` reward-slot / `RewardResolver` /
  `RewardDefinition` channel was deleted in the Phase-3 cutover.)

### Determinism
- **R7** The *rolled* paths (platform discovery, enemy drops) are procedurally
  determined: the same enemy/platform can yield different artifacts in different runs, but results are
  reproducible within a run. One run seed (the entrypoint's `seed` field; time-derived when 0) is
  hashed with a stable context key (`platform:{nodeId}`, `enemy:{platformId}:{enemyId}`) into a
  per-roll `System.Random`, making rolls independent of execution order and of `UnityEngine.Random`
  state. *(Quest rewards are fixed (R6), not rolled — the `quest:{storyId}:{npcId}` context key and
  `RollQuestRewards` are dead, no callers; P6-4 cleanup.)*
- **R8** Story/NPC tags bias the *rolled* reward tables: entries with matching
  `biasTags` get their weight multiplied by `LootConfig.TagBiasMultiplier` (platform/enemy rolls only;
  the quest-reward roll path is retired).

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
  `ILootEntryFilter` + `PassThroughLootFilter`, and `LootRollService`. The two live operations are
  `RollPlatformLoot` (the artifacts on a density-placed loot platform) and `RollEnemyDrops` (slot rolls
  or biome fallback). `ShouldPlaceLootOnPlatform` and `RollQuestRewards` are **dead — no callers**
  (presence is owned by the density allocator; quest rewards are fixed) — kept until the P6-4 cleanup.
- **Data**: `BiomeLootDefinition`, `LootConfig`, `WeightedArtifactEntry`,
  `ArtifactLootSlot` (ScriptableObjects/serializables, data only);
  `BiomeLootCatalog` and `LootSlotMapper` convert assets to Core snapshots at
  install time. `EnemyDefinition` gained `_lootSlots`;
  `EnemyData.LootSlots` carries the mapped slots through `IEnemyDataProvider`.
- **Application**: `QuestRewardGranter` (scans the run-scoped `ILiveQuestRegistry` for a quest that
  reached `QuestState.Completed` and not yet paid out, **rolls** each declared `QuestRewardCore` via
  `IQuestRewardRoller` and routes it by kind — artifact → `IInventoryModel`, blank → `IBlankRack`,
  idempotent per quest — item rewards only),
  `QuestRewardRoller` + `QuestRewardPools` (`Core/`, the P1-5 roll projected from the artifact/blank
  catalogs),
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
- Loot-platform **presence** is decided by the streaming **density allocator**
  (`WorldContentAllocator`'s ambient weighted draw, `narrative-procedural.md` §2.6): a `WorldSlotKind.Loot`
  slot becomes a `PlannedPlatformKind.Loot` platform → `PlatformContentType.Loot`. *(The legacy
  `ScenarioGenerator` gate via `ShouldPlaceLootOnPlatform` is gone — that operation and
  `BiomeLootDefinition._platformLootChance` are dead, P6-4.)*
- `AreaGenerator.CreateLootContent(nodeId)` rolls the actual artifacts from the biome `_platformTable`
  and stores them on `LootContent` (items + per-item collected flags; nothing
  respawns on re-entry).
- `ICurrentThemeProvider` is written by the biome journey's `BiomeStretchDirector` per stretch, and
  `AreaGenerator` reads it live per loot roll (the entrypoint no longer sets the theme).

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
and fixes the effective run seed **at install time** via `IRunSeedProvider` — from the entrypoint's
`seed` field, or the restored `RunSaveSnapshot.RunSeed` on a continue, time-derived when 0. Every
subsystem derives its own per-context stream off that one root seed through `LootSeed.Derive` (loot
rolls, and the narrative slice's own seed in `NarrativeSliceInstaller`), so results stay reproducible
and order-independent. Assets auto-load from `Resources/Loot/Biomes`, `Resources/Configs/LootConfig`
and `Resources/Prefabs/Loot/WorldArtifact` when inspector fields are empty.

### Adding content without code
- New biome tuning: edit the `Biome_*.asset` under `Resources/Loot/Biomes`
  (Create → Loot → Biome Loot for new themes).
- Enemy drops: add slots to the enemy's `EnemyDefinition` Loot section.
- Animation/visual tuning: edit `Resources/Configs/LootConfig.asset`.
- Declared quest rewards: author `(tier, belonging, payload kind)` on the `Quest` asset
  (`QuestRewardSerial` — no item id; the world rolls it). Mark artifacts with `_rewardFamilyId` and
  blanks with `_raceId`; author reward families as `RewardFamilyDefinition` under `Resources/Rewards/`.
  See `quest-subsystem.md` §3/§4.

## 3. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:
- `LootSeedTests` — hash stability and sensitivity.
- `WeightedPickerTests` — empty/zero-weight handling, distribution sanity,
  determinism per seed.
- `LootRollServiceTests` — run/context determinism, probability edges, quantity
  ranges, biome fallback, tag bias, filter pipeline, bonus passthrough
  (pure C#, runnable outside Unity).
- `QuestRewardGranterTests` — a completed quest's **declared** reward is rolled into the inventory
  (artifact) / rack (blank) of the right family/race, the payout is idempotent, an empty pool grants
  nothing but marks paid, and a full rack forfeits the blank. `QuestRewardRollerTests` — the roll's
  belonging filter, nearest-tier bias, determinism, and non-catalog spread (both pure C#, P1-5).

## 4. Known limitations / open points

- "Quest completion" means the platform reached `PlatformCompletedState`;
  a dialogue the player walks away from still completes the platform and grants
  rewards. An explicit Ink completion signal is a possible refinement.
- Only platforms the density allocator marks as `Loot` roll loot; Empty/traversal
  and other content kinds never carry it.
- Quest rewards are **item-only** (a declared `QuestRewardCore` rolls to an artifact or Part-Blank);
  non-item reward *sinks* (Currency, Experience, Ability) have no receiving system yet — deferred
  (Track H P1-12, pending a currency model).
- The quest-reward roll uses **uniform weights** within the nearest-tier-in-family subset — no rarity
  curve yet (Track H tuning follow-up).
- Inventory capacity feedback (R11) is unreachable while the inventory is
  unlimited; `UnlimitedCapacityPolicy` is the rebinding point.
- Progression gating fields (R13) are serialized and surfaced in the roll
  context but no filter consumes them yet.
- `WorldArtifactView` duplicates the depth-stack construction of
  `BubbleView`; extracting a shared builder is a possible cleanup once a third
  consumer appears.
- World pickups are not despawned when leaving a platform; they persist until
  collected or scene unload.
