# Narrative Generation — Requirements & Design

> ⚠️ **STALE — describes the deleted legacy pipeline.** The as-implemented system in §1–§3
> (`LevelNarrativeGenerator`, `StoryDefinition`/`NpcDefinition`/`RewardDefinition`,
> `LevelNarrativeConfig`, `RewardResolver`, `CompositeDialoguePresenter`, `NarrativeInstaller`,
> `ScenarioGenerator`, `PlatformGraphGenerator`) was **replaced** by the story-first streaming director
> in the Phase-3 cutover and **no longer exists** (see ROADMAP "Delete legacy (Phase 3)"). The current
> engine is documented in `narrative-procedural.md` (as-implemented) and `narrative-director-requirements.md`
> (forward-looking). This doc is retained only for the §4 planned-design (P1–P4) reference, itself now
> reframed by the streaming director; do not treat §1–§3 as accurate. Refresh tracked in ROADMAP `[debt]`.

Part of the **Narrative System**. This document describes level narrative generation as implemented: how stories and NPCs are selected and paired when a level is formed, how rewards are resolved, and how the resulting assignments drive platform content and dialogue.

Status: current as of 2026-06-10.

---

## 1. Requirements

### 1.1 Functional requirements

**Story selection**

- R1. Stories are authored as `StoryDefinition` ScriptableObject assets and provided to the generator through a pool (`IStoryPool`).
- R2. A story is eligible for a level only if it passes **all** filters: not on cooldown, has compiled Ink content, matches the level theme (`AllowedThemes`; empty = any), matches the level difficulty (`MinDifficulty`/`MaxDifficulty`; 0/0 = any), has every required tag from the config, and has no excluded tag.
- R3. The number of stories picked per level is random within `LevelNarrativeConfig.MinStories..MaxStories`, capped by the number of eligible stories.
- R4. Story repeatability is enforced via cooldowns: a used repeatable story with `CooldownRuns > 0` becomes ineligible for that many runs (`StoryPool.TickCooldowns()` decrements); a used non-repeatable story becomes permanently ineligible for the pool's lifetime.

**NPC assignment**

- R5. Each selected story is assigned **exactly one** NPC; an NPC appears at most once per level (assignment tracking in `NpcPool`).
- R6. A story with `CanTransitionToCombat` must be assigned a combat-capable NPC (`NpcDefinition.CanBecomeEnemy`). Pairing a non-combat NPC would produce an enemy-less fight.
- R7. Combat stories are matched to NPCs **before** non-combat stories, so a non-combat story can never consume the last combat-capable NPC and orphan a combat story. Non-combat stories may still receive a combat-capable NPC if any remain after the combat pass.
- R8. If no eligible NPC exists for a story (combat-capable or otherwise), the story is skipped with a warning; generation continues with the remaining stories.
- R9. The order of assignments in the output follows the story selection order, regardless of the internal two-pass matching. Downstream consumers (`ScenarioGenerator`) read the list sequentially.

**Density / character-only NPCs**

- R10. The total NPC count per level is random within `LevelNarrativeConfig.MinNpcs..MaxNpcs`, capped by pool size. Slots not consumed by story assignments are filled with **character-only** NPCs (`NpcAssignment.CharacterOnly`): they present only their character Ink (greeting/personality) and carry no story or rewards.

**Rewards**

- R11. Each story assignment resolves rewards from the story's `RewardSlot[]`: every slot rolls independently against its `Probability`, and a granted reward's quantity is rolled within the reward's `QuantityRange`.

**Dialogue composition**

- R12. An NPC with a story presents **two layered Ink files**: the story Ink (`StoryDefinition.InkJsonAsset`, entered at `StartingKnot`) and the NPC's character Ink (`NpcDefinition.CharacterInkJson`, entered at `CharacterStartKnot`). Character Ink speaks first; choices from both are merged into one list.
- R13. If an NPC's character Ink is the same asset content as the story Ink, the character Ink is disabled for that dialogue (prevents duplicate playback). If an assignment has no Ink at all, the dialogue ends immediately with the `Exit` outcome.
- R14. Story Ink may trigger combat through bound external functions; the NPC then transitions to an enemy using its `EnemyDefinition`.

**Determinism**

- R15. The generator and the reward resolver accept an injected `System.Random`, so a seeded run produces reproducible output for tests.

### 1.2 Non-functional requirements

- N1. Generation logic (`LevelNarrativeGenerator`, `StoryPool`, `NpcPool`, `RewardResolver`) is pure C# and unit-testable without play mode. Edit-mode tests exist in `Assets/__Project/Tests/EditMode/` (`LevelNarrativeGeneratorTests`, plus pool/resolver tests).
- N2. All dependencies are wired through Zenject in `NarrativeInstaller`; no service locators.
- N3. `StoryDefinition`, `NpcDefinition`, `LevelNarrativeConfig`, and `RewardDefinition` are data-only ScriptableObjects; eligibility helpers on them are pure predicates.

---

## 2. Architecture

### 2.1 Pipeline

```
AreaSceneEntrypoint.GenerateArea()
  └─ LevelNarrativeGenerator.Generate(LevelNarrativeConfig)   → LevelNarrative (NpcAssignment[])
  └─ ScenarioGenerator.GenerateScenario(GameContext, LevelNarrative) → ScenarioData (platform requirements)
  └─ PlatformGraphGenerator                                    → PlatformGraphData (connectivity)
  └─ AreaGenerator                                             → platforms with NpcContent / EnemyContent
```

| Stage | Key files |
|---|---|
| Orchestration | `Assets/__Project/Scripts/Area/AreaSceneEntrypoint.cs` |
| Narrative generation | `Assets/__Project/Scripts/Narrative/Generation/LevelNarrativeGenerator.cs`, `StoryPool.cs`, `NpcPool.cs`, `RewardResolver.cs` |
| Data definitions | `Assets/__Project/Scripts/Narrative/Data/Definitions/StoryDefinition.cs`, `NpcDefinition.cs`, `LevelNarrativeConfig.cs`, `RewardDefinition.cs` |
| Scenario / platforms | `Assets/__Project/Scripts/LevelGeneration/Scenario/ScenarioGenerator.cs`, `Assets/__Project/Scripts/LevelGeneration/Area/AreaGenerator.cs` |
| Dialogue | `Assets/__Project/Scripts/Narrative/Dialogue/CompositeDialoguePresenter.cs` |
| DI | `Assets/__Project/Scripts/Core/DI/NarrativeInstaller.cs` |

### 2.2 Data definitions

**`StoryDefinition`** (`Narrative/Story/Story` asset menu)

| Field | Meaning |
|---|---|
| `StoryId`, `DisplayName`, `Description` | Identity |
| `InkJsonAsset`, `StartingKnot` | Compiled Ink content and entry knot (default `start`) |
| `AllowedThemes[]` | Theme filter; empty = matches any theme |
| `MinDifficulty`, `MaxDifficulty` | Difficulty window; 0/0 = matches any |
| `Tags[]` | Free-form tags for required/excluded filtering |
| `IsRepeatable`, `CooldownRuns` | Repeatability; non-repeatable = once per pool lifetime |
| `CanTransitionToCombat` | Story can start a fight → requires a combat-capable NPC |
| `Rewards[]` (`RewardSlot`) | Reward + probability + optional condition string |

**`NpcDefinition`** (`Narrative/NPCs/NPC` asset menu)

| Field | Meaning |
|---|---|
| `NpcId`, `DisplayName` | Identity (empty `NpcId` excludes the NPC from the pool) |
| `Prefab`, `Portrait` | Visuals |
| `CharacterInkJson`, `CharacterStartKnot` | Personality Ink and entry knot (default `greeting`) |
| `Tags[]`, `Faction` | Filtering metadata (`NpcFaction`: Neutral/Friendly/Hostile/Merchant/QuestGiver) |
| `CanBecomeEnemy`, `EnemyDefinition` | Combat capability and the enemy stats used on transition |

**`LevelNarrativeConfig`** (`Narrative/Level Narrative Config` asset menu)

| Field | Meaning |
|---|---|
| `MinStories`, `MaxStories` | Story density bounds (default 1–3) |
| `MinNpcs`, `MaxNpcs` | Total NPC density bounds (default 1–4) |
| `Theme`, `Difficulty` | Filter inputs passed to the story pool |
| `RequiredTags[]`, `ExcludedTags[]` | Tag filters applied to stories; `RequiredTags` also filters NPCs |

### 2.3 Generation algorithm

`LevelNarrativeGenerator.Generate(config)` runs four steps:

1. **Filter stories** — `StoryPool.Filter(theme, difficulty, requiredTags, excludedTags)` applies R2.
2. **Pick story count** — random within `MinStories..MaxStories`, capped by eligible count, then Fisher-Yates selection.
3. **Assign NPCs, two passes** (R6/R7): the combat pass walks the selected stories and matches each `CanTransitionToCombat` story to a random NPC among the combat-capable ones still unassigned; the non-combat pass then matches the remaining stories to any unassigned NPC. Results are stored per story index, and assignments are emitted in original selection order (R9) together with resolved rewards and `StoryPool.RecordUsage`. A story with no candidate NPC is skipped with a warning (R8).
4. **Fill with character-only NPCs** — picks a total NPC target within `MinNpcs..MaxNpcs` and adds `NpcAssignment.CharacterOnly` entries from the unassigned remainder (R10).

### 2.4 Pools

- **`StoryPool`** — holds the level's `StoryDefinition` list. `Filter(...)` applies cooldown/content/theme/difficulty/tag checks. `RecordUsage(id)` starts a cooldown (`CooldownRuns` for repeatable stories, permanent for non-repeatable). `TickCooldowns()` advances cooldowns by one run.
- **`NpcPool`** — holds the level's `NpcDefinition` list. `Filter(requiredTags, excludedNpcIds)` returns unassigned NPCs matching the tags; `MarkAssigned(id)` removes an NPC from further consideration; `ResetAssignments()` is called at the start of every `Generate`.

### 2.5 Rewards

`RewardResolver.Resolve(story)` first gates each `RewardSlot` by its `Condition` string against the current run progression (`RunConditionEvaluator` + `IRunProgressionRecord`; see `character-progression.md`), then rolls the surviving slots against their probability and rolls a quantity within `RewardDefinition.QuantityRange`, producing `ResolvedReward` entries stored on the `NpcAssignment`. An empty condition always passes; a malformed/unknown condition fails closed. When the resolver is constructed without a record (the seeded-`Random`-only constructor used by some tests), condition gating is disabled.

### 2.6 Scenario integration

`ScenarioGenerator.GenerateScenario(context, levelNarrative)` converts every `NpcAssignment` into a `PlatformRequirement` with `StoryPlatformData` (`NpcId`, `DialogueKnot` from the story's starting knot, `IsKeyNode = HasStory`, `NpcCanBecomeEnemy`, `EnemyId`). `AreaGenerator.CreateNpcContent` later looks the assignment back up by `NpcId` and attaches it to the platform's `NpcContent`.

### 2.7 Dialogue composition

`CompositeDialoguePresenter.StartDialogue(assignment)` runs two `IStoryManager` (Ink) instances in parallel: one for the story Ink, one for the NPC's character Ink. Character Ink speaks first; the story Ink is silently primed to its first choice point so both choice sets merge into a single list (R12). Identical story/character Ink disables the character layer (R13). External functions bound by `IInkExternalFunctionBinder` let story Ink trigger combat or quests (`OnCombatTriggered`, `OnQuestTriggered`).

### 2.8 DI wiring

`NarrativeInstaller` (scene component) provides the content: inspector lists of `StoryDefinition` and `NpcDefinition` assets become the `StoryPool`/`NpcPool`, plus the `LevelNarrativeConfig` (a default instance is created if unassigned). It binds `ILevelNarrativeGenerator`, two `IStoryManager` instances (ids `"story"` and `"npc"`, `AsCached`), the dialogue view, and `CompositeDialoguePresenter` (also bound as `IDisposable` for teardown).

---

## 3. Data-driven authoring

Adding narrative content requires **no code changes**:

1. **New story**: author an `.ink` file with a `start` knot (or set a custom `StartingKnot`), compile it to JSON, create a `StoryDefinition` asset (`Create → Narrative → Story → Story`), assign the Ink JSON, set filters/behavior/rewards, and add the asset to the `NarrativeInstaller` story list in the scene.
2. **New NPC**: create an `NpcDefinition` asset (`Create → Narrative → NPCs → NPC`), assign prefab, portrait, and a character Ink JSON with a `greeting` knot, and add it to the installer's NPC list.
3. **Pairing rule for authors**: every story with `CanTransitionToCombat` needs at least one NPC with `CanBecomeEnemy` (and a valid `EnemyDefinition`) in the same level's NPC list, or the story will be skipped at generation time. The character Ink asset must be distinct from the story Ink asset (R13).

---

## 4. Planned design (NOT implemented)

> Everything in this section is a forward-looking requirement set for procedural narrative selection. None of it exists in code yet.

**P1. Story↔NPC compatibility scoring.** Stories declare *preferred* and *required* NPC traits (tags, faction) in addition to the existing hard combat constraint. The generator scores every valid story+NPC pair (tag overlap, faction fit, combat capability) and assigns pairs by best score instead of randomly, so a merchant-flavored story lands on a merchant-flavored NPC whenever one is available. Hard requirements prune pairs; preferences only affect the score.

**P2. Rule-based outcome quotas.** `LevelNarrativeConfig` gains quotas such as *minimum stories that can lead to combat* and *minimum peaceful stories*, derived from level settings (planned battle count, NPC count). The generator satisfies quotas first — guaranteeing minimum combat outcomes per level — then fills remaining slots by best compatibility score. Quota shortfalls (not enough eligible content) are reported, not silently dropped.

**P3. Progression-driven selection.** `GameContext` (`CharacterLevel`, `Progress`, `StoryState`) becomes an input to narrative generation, not only to scenario generation: difficulty windows and theme filters derive from progression, and story weighting can prefer content appropriate to the player's stage of the game.

**P4. Soft cooldowns.** Cooldown and repeatability fold into the scoring (recently seen stories score lower) instead of acting purely as hard filters, so a small content pool degrades gracefully rather than producing empty levels.

---

## 5. Known limitations / open points

- No story↔NPC affinity exists today: any non-combat story may be paired with any NPC (e.g., a merchant story on a bandit), and combat-capable NPCs may receive non-combat stories once all combat stories are served. Addressed by P1.
- The two-pass allocation is correct only for the single combat/non-combat constraint; it does not generalize to multiple competing constraints (that requires the P1 scoring/matching model).
- A skipped story (no candidate NPC) is not backfilled with another eligible story, so a level may end up below `MinStories`.
- `GameContext` is not consulted by `LevelNarrativeGenerator`; `ScenarioGenerator` uses it only for theme rotation and platform count, with difficulty hardcoded to 10.
- `RewardSlot.Condition` supports only a single predicate (`quest_completed:` / `quest_active:` / `quest_failed:` / `npc_encountered:`); boolean AND/OR composition is not yet supported (see `character-progression.md`).
- The generator logs via `Debug.Log`/`Debug.LogWarning` directly, which violates the project rule that domain code should use the logger abstraction (pre-existing).
