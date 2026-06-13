# Procedural Narrative System – Design Spec

## 1. Project Goals
- Unity roguelike game
- Procedural narrative using Ink
- Replayable, parameterized stories
- Maintainable and extensible architecture

## 2. Core Concepts
### 2.1 NPCs
- NPCDefinition vs NPCInstance
- NPCs may change roles over time

### 2.2 Narrative Templates
- Stories authored in Ink
- Parameterized with NPC, Location, Reward

### 2.3 Quests
- Runtime instances created from story templates
- Track gameplay state and outcomes

## 3. Narrative Generation Pipeline
Generate Area  
↓  
Generate NPC Pool  
↓  
Evaluate Player and World State  
↓  
Assemble Narrative Context  
↓  
Select StoryTemplates  
↓  
Bind Parameters  
↓  
Instantiate Story  
↓  
Generate QuestInstances  
↓  
Attach Ink Runtime

## 4. Main Story vs Side Stories
- Main Story provides narrative spine
- Side Stories are optional and procedural
- Same pipeline, different constraints

## 5. Runtime Binding
- NPC instances
- Locations
- Rewards

## 6. Ink Integration
- Variable injection
- Tags and knots for branching
- Outcome reporting

## 7. Maintainability Goals
- Data-driven configs
- Modular systems
- Clear separation of concerns



# Implemetation notes

## 1. Architecture Flow

Area Generation
↓
NarrativeGenerator.GenerateForArea()
↓
NpcPool.PopulatePool() → NPCInstance[]
↓
StoryTemplateSelector.SelectTemplates() → StoryTemplateSelection[]
↓
ParameterBinder.BindParameters() → BoundStory
↓
QuestManager.CreateQuest() → QuestInstance
↓
StorySession → InkRuntime execution

## 8. Detailed Runtime Flow

### 8.1 Entry Point: NarrativeGenerator.GenerateForArea()

**Trigger:** Area generation complete, platforms defined

**Input:** INarrativeContext containing:
- CurrentAreaId, CurrentTheme, CurrentChapterNumber
- TotalPlatforms, CurrentPlatformIndex
- WorldState (StoryState)
- PlayerData (alignment, faction relationships)

---

### 8.2 Phase 1: NPC Pool Initialization

```
NpcPool.PopulatePool(npcDefinitions, context)
│
├─► For each NpcDefinition:
│   ├─► Create NpcInstance with unique InstanceId
│   ├─► Set initial state: Available
│   └─► Register in pool dictionaries (by instanceId, by definitionId)
│
└─► NpcPool.DecrementCooldowns()
    └─► All NPCs on cooldown: cooldownPlatforms--
        └─► If cooldown reaches 0: state → Available
```

**Decision Point:** NPCs with cooldown > 0 remain unavailable for this area

---

### 8.3 Phase 2: Story Count Determination

```
mainStoryCount = 1  (always)
sideStoryCount = max(0, (TotalPlatforms / 3) - 1)
totalStories = mainStoryCount + sideStoryCount
```

| TotalPlatforms | Main | Side | Total |
|----------------|------|------|-------|
| 1-5            | 1    | 0    | 1     |
| 6-8            | 1    | 1    | 2     |
| 9-11           | 1    | 2    | 3     |
| 12+            | 1    | 3+   | 4+    |

---

### 8.4 Phase 3: Main Story Generation

**StoryType: Chapter**

```
GenerateSingleStory(context, StoryType.Chapter)
│
├─► [SELECTION] StoryTemplateSelector.SelectBestTemplate()
│   │
│   ├─► Filter: Eligibility Checks (see 8.6)
│   │   ├─► HasInkContent == true
│   │   ├─► MinimumChapter <= CurrentChapter <= MaximumChapter
│   │   ├─► Theme match (if RequiresSpecificTheme)
│   │   ├─► Player alignment within AlignmentRange
│   │   ├─► Not recently played (if !IsRepeatable)
│   │   ├─► Prerequisites met (stories, quests, NPCs)
│   │   └─► RequiredNpcCount <= AvailableNpcs
│   │
│   ├─► Score: Scoring Factors (see 8.7)
│   │   ├─► BasePriority (from template)
│   │   ├─► +10 theme match bonus
│   │   ├─► +20 key progression bonus
│   │   ├─► +15 variety bonus (not recently played)
│   │   ├─► +N attribute match bonus
│   │   └─► +progress bonus (chapter position)
│   │
│   └─► Return: StoryTemplateSelection (best score)
│
├─► [VALIDATION] ParameterBinder.CanBindAllParameters()
│   └─► Count required NPC slots, verify pool has enough
│
├─► [BINDING] ParameterBinder.BindParameters()
│   │
│   ├─► For each TemplateParameterSlot:
│   │   ├─► Npc: Find/reserve matching NPC
│   │   ├─► Location: Use current area
│   │   ├─► Reward: Weighted selection from definitions
│   │   ├─► ItemName: Generate placeholder
│   │   ├─► Quantity: Random within range
│   │   └─► Custom: Use slot default
│   │
│   ├─► For each TemplateRewardSlot:
│   │   └─► Create RewardInstance (probability check, scaling)
│   │
│   └─► Return: BoundStory
│
├─► [INSTANTIATION] new StorySession(boundStory, storyManager)
│
├─► [QUEST] QuestManager.CreateQuest(boundStory)
│   └─► Create QuestInstance with objectives, NPCs, rewards
│
├─► [COOLDOWN] NpcPool.SetCooldown(npc, template.CooldownPlatforms)
│   └─► For each bound NPC
│
└─► [RECORD] context.RecordCompletedStory(storyId)
```

---

### 8.5 Phase 4: Side Story Generation (Loop)

**StoryType: SideStory**

```
For i = 0 to sideStoryCount:
│
├─► GenerateSingleStory(context, StoryType.SideStory)
│   │
│   └─► Same flow as Main Story (8.4) with differences:
│       ├─► Template pool filtered to StoryType.SideStory
│       ├─► Lower priority in scoring (no key progression)
│       ├─► Typically shorter CooldownPlatforms
│       └─► Often theme-flexible (no RequiresSpecificTheme)
│
└─► If generation fails:
    └─► Warning logged, continue to next
```

---

### 8.6 Decision Point: Template Eligibility Filters

```
IsTemplateEligible(template, context) → bool
│
├─► REJECT if !HasInkContent
├─► REJECT if CurrentChapter < MinimumChapter
├─► REJECT if MaximumChapter > 0 && CurrentChapter > MaximumChapter
├─► REJECT if RequiresSpecificTheme && RequiredTheme != CurrentTheme
├─► REJECT if PlayerAlignment outside AlignmentRange
├─► REJECT if !IsRepeatable && WasRecentlyPlayed
├─► REJECT if Prerequisites not met:
│   ├─► RequiredCompletedStories
│   ├─► RequiredCompletedQuests
│   ├─► RequiredActiveQuests
│   └─► RequiredEncounteredNpcs
├─► REJECT if RequiredNpcCount > AvailableNpcs.Count
│
└─► ACCEPT
```

---

### 8.7 Decision Point: Template Scoring

```
ScoreTemplate(template, context) → float
│
├─► score = BasePriority
│
├─► Theme Match (if !RequiresSpecificTheme):
│   └─► IF template.Theme == context.CurrentTheme: +10
│
├─► Progression Relevance:
│   └─► IF IsKeyProgression: +20
│
├─► Variety:
│   └─► IF !WasRecentlyPlayed: +15
│
├─► Attribute Match:
│   └─► FOR each matching attribute: +weight * 10
│
├─► Chapter Progress:
│   └─► IF MinimumChapter > 0: +ChapterProgress * 5
│
└─► RETURN score
```

---

### 8.8 Decision Point: NPC Selection

```
BindNpcParameter(slot, context, npcPool) → BoundParameter
│
├─► IF slot.HasSpecificNpc:
│   ├─► npc = pool.GetNpc(SpecificNpcId)
│   └─► FAIL if npc == null || !npc.IsAvailable
│
├─► ELSE: Search by criteria
│   ├─► criteria.RequiredFaction (if EnforceFaction)
│   ├─► criteria.RequiredRole (if != None)
│   ├─► criteria.RequiredTraits
│   ├─► criteria.MaxResults = 5
│   │
│   ├─► matches = pool.FindMatchingNpcs(criteria)
│   └─► selectedNpc = matches[random(0, min(3, count))]
│
├─► IF selectedNpc == null: FAIL
│
├─► pool.ReserveNpc(npc.InstanceId, role, areaId)
│   └─► npc.State → Assigned
│
└─► RETURN BoundParameter(slot, npc, displayName)
```

---

### 8.9 Main Story vs Side Story Differences

| Aspect | Main Story | Side Story |
|--------|------------|------------|
| **StoryType** | Chapter | SideStory |
| **Count per Area** | Always 1 | 0+ based on platform count |
| **Priority** | Higher base priority | Lower base priority |
| **Progression** | Often IsKeyProgression | Rarely key progression |
| **Prerequisites** | May require prior chapters | Minimal prerequisites |
| **Theme** | May require specific theme | Usually theme-flexible |
| **Failure Handling** | Critical warning | Non-critical, continue |
| **NPC Pool Impact** | Primary reservation | Secondary/remaining pool |
| **Cooldown** | Longer CooldownPlatforms | Shorter cooldowns |

---

### 8.10 Template Selection vs Instantiation Boundary

```
┌─────────────────────────────────────────────────────────────┐
│                    TEMPLATE SELECTION                       │
│         (Stateless, Scoring-based, Reversible)              │
├─────────────────────────────────────────────────────────────┤
│  • Eligibility filtering (no state changes)                 │
│  • Scoring and ranking                                      │
│  • Returns: StoryTemplateSelection                          │
│  • Can be repeated without side effects                     │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  VALIDATION GATE                            │
├─────────────────────────────────────────────────────────────┤
│  • CanBindAllParameters() - verifies binding feasibility    │
│  • Last chance to reject before committing resources        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    INSTANTIATION                            │
│         (Stateful, Resource-consuming, Irreversible)        │
├─────────────────────────────────────────────────────────────┤
│  • BindParameters() - reserves NPCs, creates instances      │
│  • new StorySession() - creates runtime session             │
│  • CreateQuest() - creates QuestInstance                    │
│  • SetCooldown() - marks NPCs unavailable                   │
│  • RecordCompletedStory() - updates context                 │
│                                                             │
│  State Changes:                                             │
│  • NpcInstance.State: Available → Assigned                  │
│  • NpcInstance.Cooldown: set to CooldownPlatforms           │
│  • Context.RecentStoryIds: updated                          │
│  • QuestManager: new active quest                           │
└─────────────────────────────────────────────────────────────┘
```

---

### 8.11 Complete Pipeline Sequence Diagram

```
NarrativeGenerator           NpcPool              TemplateSelector        ParameterBinder         QuestManager
      │                         │                        │                       │                      │
      │ GenerateForArea()       │                        │                       │                      │
      │─────────────────────────│                        │                       │                      │
      │                         │                        │                       │                      │
      │ PopulatePool()          │                        │                       │                      │
      │────────────────────────►│                        │                       │                      │
      │◄────────────────────────│                        │                       │                      │
      │                         │                        │                       │                      │
      │ DecrementCooldowns()    │                        │                       │                      │
      │────────────────────────►│                        │                       │                      │
      │                         │                        │                       │                      │
      │ [MAIN STORY]            │                        │                       │                      │
      │                         │ SelectBestTemplate()   │                       │                      │
      │────────────────────────────────────────────────►│                       │                      │
      │◄────────────────────────────────────────────────│ StoryTemplateSelection│                      │
      │                         │                        │                       │                      │
      │                         │                        │CanBindAllParameters() │                      │
      │────────────────────────────────────────────────────────────────────────►│                      │
      │◄────────────────────────────────────────────────────────────────────────│ true/false           │
      │                         │                        │                       │                      │
      │                         │                        │ BindParameters()      │                      │
      │────────────────────────────────────────────────────────────────────────►│                      │
      │                         │◄───────────────────────────────────────────────│ ReserveNpc()        │
      │◄────────────────────────────────────────────────────────────────────────│ BoundStory           │
      │                         │                        │                       │                      │
      │                         │                        │                       │ CreateQuest()        │
      │─────────────────────────────────────────────────────────────────────────────────────────────►│
      │◄─────────────────────────────────────────────────────────────────────────────────────────────│
      │                         │                        │                       │                      │
      │ SetCooldown()           │                        │                       │                      │
      │────────────────────────►│                        │                       │                      │
      │                         │                        │                       │                      │
      │ [SIDE STORIES - repeat] │                        │                       │                      │
      │ ...                     │                        │                       │                      │
      │                         │                        │                       │                      │
      │ GenerationResult        │                        │                       │                      │
      │◄────────────────────────│                        │                       │                      │
```




=========================================================================================


Core Domain Entities - Narrative System

Based on NARRATIVE.md Sections 2 (Core Concepts) and 5 (Runtime Binding).

 ---
Entity Overview
┌─────────────────────────┬──────────────────┬──────────────────────────────────────────┐
│         Entity          │       Type       │                 Purpose                  │
├─────────────────────────┼──────────────────┼──────────────────────────────────────────┤
│ NpcDefinition           │ ScriptableObject │ Static NPC configuration                 │
├─────────────────────────┼──────────────────┼──────────────────────────────────────────┤
│ NpcInstance             │ Pure C#          │ Runtime NPC state                        │
├─────────────────────────┼──────────────────┼──────────────────────────────────────────┤
│ StoryTemplateDefinition │ ScriptableObject │ Parameterized story template             │
├─────────────────────────┼──────────────────┼──────────────────────────────────────────┤
│ BoundStory              │ Pure C#          │ Fully resolved story ready for execution │
├─────────────────────────┼──────────────────┼──────────────────────────────────────────┤
│ StorySession            │ Pure C#          │ Runtime Ink story execution              │
├─────────────────────────┼──────────────────┼──────────────────────────────────────────┤
│ QuestInstance           │ Pure C#          │ Runtime quest tracking                   │
├─────────────────────────┼──────────────────┼──────────────────────────────────────────┤
│ RewardDefinition        │ ScriptableObject │ Static reward configuration              │
├─────────────────────────┼──────────────────┼──────────────────────────────────────────┤
│ RewardInstance          │ Pure C#          │ Runtime reward with calculated values    │
└─────────────────────────┴──────────────────┴──────────────────────────────────────────┘
 ---
1. NpcDefinition

Purpose

Static configuration data for an NPC archetype. Defines who the NPC is, not their runtime state.

Key Fields

- NpcId (string) - Unique identifier
- DisplayName (string) - UI display name
- Description (string) - NPC description
- Prefab (GameObject) - Visual representation
- Portrait (Sprite) - Dialogue UI image
- DefaultDialogueKnot (string) - Ink entry point
- Faction (NpcFaction) - Allegiance: Neutral, Friendly, Hostile, Merchant, QuestGiver
- AssociatedStories (List) - Stories this NPC participates in
- CanBecomeEnemy (bool) - Can transition to combat
- EnemyDefinition (EnemyDefinition) - Combat stats if hostile

Lifecycle

- Creation: Authored in Unity Editor as ScriptableObject asset
- Mutation: Immutable at runtime (configuration only)
- Destruction: Never destroyed; exists for game lifetime

Relationships

- Referenced by NpcInstance (1 Definition → N Instances possible)
- Links to BaseStoryDefinition via NpcStoryAssociation
- May reference EnemyDefinition for combat transition

 ---
2. NpcInstance

Purpose

Runtime state for a specific NPC encounter. Tracks availability, assignment, relationships, and cooldowns.

Key Fields

- InstanceId (string) - Unique runtime ID (8-char GUID)
- Definition (NpcDefinition) - Reference to static data
- CurrentRole (NpcRole) - Role in current story: None, QuestGiver, Companion, Antagonist, Merchant, Informant, Target, Bystander
- State (NpcInstanceState) - Availability: Available, Assigned, OnCooldown, Encountered, Retired
- AssignedStoryId (string) - Currently bound story
- AssignedLocationId (string) - Current location binding
- CooldownRemaining (int) - Platforms until available
- RelationshipScore (int) - Player relationship (-100 to 100)

Lifecycle

- Creation: NpcPool.PopulatePool() creates instances from definitions at area start
- Mutation:
    - Assign() → State becomes Assigned, role/story/location set
    - Release() → State reverts, cooldown may apply
    - SetCooldown() → State becomes OnCooldown
    - DecrementCooldown() → Called per platform transition
    - UpdateRelationship() → Changed by player actions
- Destruction: Pool cleared on area transition; instances are transient

Relationships

- References NpcDefinition (N:1)
- Reserved by BoundStory when assigned
- Tracked by QuestInstance as InvolvedNpc
- Managed by NpcPool for availability

 ---
3. StoryTemplateDefinition

Purpose

Authoring asset for parameterized stories. Defines structure, constraints, and slots for runtime binding.

Key Fields

- StoryId (string) - Unique identifier
- DisplayName / Description (string) - Metadata
- StoryType (StoryType) - Chapter, SideStory, Dialogue, Event
- InkJsonAsset (TextAsset) - Compiled Ink story
- StartingKnot (string) - Ink entry point
- ParameterSlots (List) - Slots requiring binding
- RewardSlots (List) - Potential rewards
- PlatformType (StoryPlatformType) - Where story appears
- IsRepeatable (bool) - Can be selected multiple times
- CooldownPlatforms (int) - Minimum platforms between uses
- MinimumChapter / MaximumChapter (int) - Chapter constraints
- AlignmentRange (Vector2Int) - Player alignment requirement
- RequiresSpecificTheme (bool) / RequiredTheme (LevelTheme) - Theme constraints
- Prerequisites (StoryPrerequisites) - Required prior completions

Lifecycle

- Creation: Authored in Unity Editor with Ink JSON reference
- Mutation: Immutable at runtime
- Destruction: Never destroyed

Relationships

- Selected by StoryTemplateSelector based on context
- Resolved into BoundStory by ParameterBinder
- Associated with NpcDefinition via NpcStoryAssociation

 ---
4. BoundStory

Purpose

A fully resolved story with all parameters bound. Ready to create a StorySession and QuestInstance.

Key Fields

- Template (StoryTemplateDefinition) - Source template
- BoundParameters (Dictionary<string, BoundParameter>) - Resolved parameter values
- BoundNpcs (List) - Reserved NPCs
- BoundLocationId (string) - Resolved location
- BoundRewards (List) - Instantiated rewards

Lifecycle

- Creation: ParameterBinder.BindParameters() after template selection
- Mutation: Immutable once created
- Destruction: Transient; passed to StorySession/QuestManager for instantiation

Relationships

- Created from StoryTemplateDefinition (1:1)
- Contains reserved NpcInstance references
- Contains RewardInstance objects
- Used to create StorySession and QuestInstance

 ---
5. StorySession

Purpose

Runtime execution context for an Ink story. Manages dialogue state, variable injection, and story progression.

Key Fields

- SessionId (string) - Unique runtime ID
- BoundStory (BoundStory) - Source data
- State (StorySessionState) - Created, Active, Paused, Completed
- InjectedVariables (Dictionary<string, object>) - Ink variables set from bindings
- VisitedKnots (List) - Dialogue nodes visited
- CurrentKnot (string) - Current position in story
- AssociatedQuest (QuestInstance) - Linked quest
- StartedAt / EndedAt (DateTime) - Timing

Lifecycle

- Creation: new StorySession(boundStory, storyManager) after binding
- Mutation:
    - Start() → State becomes Active, variables injected
    - RecordKnotVisit() → Tracks progression
    - SetVariable() → Updates Ink state
    - Pause() / Resume() → Suspends/continues execution
    - End(outcome) → State becomes Completed
- Destruction: Session ends; state may be saved for persistence

Relationships

- Created from BoundStory (1:1)
- Links to QuestInstance for gameplay tracking
- Managed by StoryManager

 ---
6. QuestInstance

Purpose

Gameplay-facing quest tracking. Manages objectives, progress, and outcomes independently of story dialogue.

Key Fields

- QuestId (string) - Unique identifier
- DisplayName / Description (string) - UI text
- BoundStory (BoundStory) - Source story
- Status (QuestStatus) - NotStarted, Active, Completed, Failed, Abandoned
- Outcome (QuestOutcome) - Success, PartialSuccess, Failure, Abandoned
- Objectives (List) - Tasks to complete
- InvolvedNpcs (List) - NPCs in this quest
- Rewards (List) - Potential rewards
- StartedAt / CompletedAt (DateTime) - Timing
- CustomData (Dictionary<string, object>) - Extensible data storage

Lifecycle

- Creation: QuestManager.CreateQuest(boundStory) after story binding
- Mutation:
    - Start() → Status becomes Active
    - UpdateObjective() / CompleteObjective() → Progress tracking
    - Complete(outcome) → Status becomes Completed, rewards evaluated
    - Fail() → Status becomes Failed
- Destruction: Moved to completed/failed collection; persisted for history

Relationships

- Created from BoundStory (1:1)
- Contains QuestObjective children
- References NpcInstance participants
- Contains RewardInstance for outcomes
- Managed by QuestManager

 ---
7. RewardDefinition

Purpose

Static configuration for a reward type. Defines what the reward is and how it scales.

Key Fields

- RewardId (string) - Unique identifier
- DisplayName / Description (string) - UI text
- RewardType (RewardType) - Currency, Experience, Item, Equipment, Ability, Reputation, Unlock
- BaseValue (int) - Starting value
- ChapterScaling (float) - Exponential scaling factor per chapter
- Icon (Sprite) - UI visual
- ItemId (string) - Reference if item reward
- QuantityRange (Vector2Int) - Min/max quantity
- Rarity (RewardRarity) - Common, Uncommon, Rare, Epic, Legendary
- DropWeight (float) - Weight in random selection
- MinimumChapter (int) - Chapter requirement
- RequiredQuestId (string) - Prerequisite quest

Lifecycle

- Creation: Authored in Unity Editor
- Mutation: Immutable at runtime
- Destruction: Never destroyed

Relationships

- Referenced by TemplateRewardSlot in templates
- Instantiated as RewardInstance at runtime

 ---
8. RewardInstance

Purpose

Runtime reward with calculated values based on context (chapter, multipliers, conditions).

Key Fields

- InstanceId (string) - Unique runtime ID
- Definition (RewardDefinition) - Source configuration
- CalculatedValue (int) - Final scaled value
- Quantity (int) - Item quantity
- Condition (RewardCondition) - Always, OnSuccess, OnPartialSuccess, OnPeacefulResolution, OnCombatVictory
- IsClaimed (bool) - Whether player received reward

Lifecycle

- Creation: ParameterBinder creates during story binding with scaling applied
- Mutation:
    - ShouldGrant(outcome) → Evaluates condition against quest outcome
    - Claim() → Marks as claimed, triggers reward delivery
- Destruction: Persisted with quest; claimed flag prevents re-grant

Relationships

- Created from RewardDefinition (N:1)
- Contained by BoundStory and QuestInstance
- Evaluated against QuestOutcome for granting

 ---
Entity Relationship Diagram

┌─────────────────────────────────────────────────────────────────────┐
│                      DEFINITION LAYER (Immutable)                    │
├─────────────────────────────────────────────────────────────────────┤
│  NpcDefinition ──────────────────┐                                   │
│       │                          │                                   │
│       │ associates               │ references                        │
│       ▼                          ▼                                   │
│  StoryTemplateDefinition ◄─── RewardDefinition                      │
│       │                                                              │
│       │ contains                                                     │
│       ▼                                                              │
│  TemplateParameterSlot, TemplateRewardSlot                          │
└─────────────────────────────────────────────────────────────────────┘
│
│ instantiates
▼
┌─────────────────────────────────────────────────────────────────────┐
│                      INSTANCE LAYER (Mutable)                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  NpcPool ─────► NpcInstance ◄───────┐                               │
│                     │               │                                │
│                     │ reserved by   │ tracked by                     │
│                     ▼               │                                │
│               BoundStory ───────────┼──► RewardInstance             │
│                     │               │                                │
│        creates      │               │                                │
│        ┌────────────┴───────────┐   │                                │
│        ▼                        ▼   │                                │
│  StorySession            QuestInstance ◄┘                           │
│        │                        │                                    │
│        │ manages                │ contains                           │
│        ▼                        ▼                                    │
│   Ink Runtime             QuestObjective                            │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘

 ---
Summary

The narrative system uses a Definition/Instance pattern:

1. Definitions (ScriptableObjects) are immutable configuration authored in the Editor
2. Instances (Pure C#) are mutable runtime state created during gameplay
3. Binding connects templates to specific NPCs, locations, and rewards
4. Lifecycle progresses: Definition → Selection → Binding → Session/Quest → Completion

This separation enables:
- Replayability through parameterization
- Clean testability (pure C# instances)
- Data-driven authoring (ScriptableObjects)
- Clear state management (explicit lifecycle methods)
