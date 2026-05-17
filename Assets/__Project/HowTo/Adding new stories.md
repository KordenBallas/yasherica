Story & NPC Extension Workflow

Overview

This document describes the complete workflow for extending the game's story and NPC pool. The system uses a template-based approach with runtime parameter binding, allowing reusable story templates that dynamically bind NPCs, locations, and rewards during gameplay.

 ---
System Architecture Summary

The narrative system follows a Template → Generation → Instantiation pipeline:

StoryTemplateDefinition (ScriptableObject)
↓
NarrativeGenerator (selects + binds NPCs)
↓
BoundStory (template + runtime parameters)
↓
StorySession (active Ink dialogue)
↓
Quest (tracks objectives + rewards)

Key Components:
- StoryTemplateDefinition: Template with parameter slots (NPCs, rewards, locations)
- NpcDefinition: NPC configuration with story associations
- NarrativeGenerator: Selects templates and binds parameters at runtime
- ParameterBinder: Fills template slots with concrete NPCs from pool
- NpcPool: Manages NPC availability and cooldowns
- Resources Folders: Auto-loaded by NarrativeInstaller

 ---
Part 1: Adding New Stories

Step 1: Author Ink Content

Create your Ink narrative file:

// Example: Resources/InkStories/MerchantQuest.ink

=== merchant_quest ===
# npc: merchant
# location: town

Hello, traveler! I need help retrieving a stolen item.

* [Accept quest]
  -> accept_quest
* [Refuse]
  -> refuse_quest

=== accept_quest ===
# outcome: quest_started
# reward: currency:50

Thank you! The item is in the old ruins.
-> END

=== refuse_quest ===
# outcome: quest_refused

I understand. Safe travels.
-> END

Compile to JSON using the Ink compiler (Unity Ink integration or command-line tool).

Step 2: Create StoryTemplateDefinition

In Unity Editor:

1. Right-click in Project window → Create > Narrative > Story > Story Template
2. Name it descriptively (e.g., MerchantQuestTemplate)
3. Configure the template:

Identity & Story:
- Story ID: merchant_quest_01 (unique identifier)
- Display Name: The Merchant's Request
- Description: Brief summary for debugging/UI
- Ink Content: Assign compiled Ink JSON TextAsset
- Starting Knot: merchant_quest (entry point in Ink)
- Story Type: Auto-set to SideStory for templates

Template Configuration:
- Platform Type: Dialogue (where story appears)
- Is Repeatable: false (or true with cooldown)
- Cooldown Platforms: 5 (if repeatable)

Parameter Slots (what gets bound at runtime):

Add a slot for the merchant NPC:
- Parameter ID: merchant_npc
- Parameter Type: Npc
- Ink Variable Name: merchant (binds to {merchant} in Ink)
- Is Required: true
- Required Faction: Merchant
- Enforce Faction: true
- Required Role: QuestGiver

Add a location slot:
- Parameter ID: quest_location
- Parameter Type: Location
- Ink Variable Name: location
- Location Type: Town

Reward Slots:
- Assign RewardDefinition assets (see Part 3)
- Set probability (0-1)
- Set value multiplier (e.g., 1.5x for hard quests)
- Set condition (e.g., OnSuccess)

Context Requirements:
- Requires Specific Theme: false (or true + select theme)
- Minimum Chapter: 1
- Maximum Chapter: 0 (no limit)
- Alignment Range: (-100, 100) (all alignments)

Prerequisites:
- Required Completed Stories: (optional - story dependencies)
- Required Active Quests: (optional)
- Required Encountered NPCs: (optional)
- Min Chapter Number: 1

Step 3: Place in Resources Folder

Critical: Save the StoryTemplateDefinition asset in:
Assets/Resources/StoryTemplates/

The NarrativeInstaller auto-loads all templates from this path:
Resources.LoadAll<StoryTemplateDefinition>("StoryTemplates")

Step 4: Verify Ink Integration

Ensure your Ink file compiles properly:
- Unity Ink plugin compiles .ink → .json
- Assign the .json TextAsset to template's Ink Content field
- Verify Starting Knot matches Ink file (e.g., === merchant_quest ===)

 ---
Part 2: Adding New NPCs

Step 1: Create NpcDefinition

In Unity Editor:

1. Right-click → Create > Narrative > NPC > NPC Definition
2. Name it (e.g., EldaraMerchant)
3. Configure:

Identity:
- NPC ID: eldara_merchant (unique identifier)
- Display Name: Eldara the Merchant
- Description: Brief backstory

Visual:
- Prefab: Assign 3D/2D NPC GameObject
- Portrait: Sprite for dialogue UI

Dialogue:
- Default Dialogue Knot: generic_merchant_greeting (fallback if no story)
- Legacy Dialogue Sessions: (deprecated - leave empty)

Story Associations (NEW SYSTEM):

Add an NpcStoryAssociation entry:
- Story: Drag & drop the MerchantQuestTemplate asset
- Role: QuestGiver (semantic role in story)
- Trigger Condition:
    - Trigger Type: Always (story always available)
    - Or AfterEncounters: requires N encounters first
    - Or QuestCompleted: requires prerequisite quest
    - Or ChapterReached: requires specific chapter
- Priority: 10 (higher = more likely to be selected)

Faction & Behavior:
- NPC Faction: Merchant
- Can Become Enemy: false (or true if combat possible)
- Enemy Definition: (optional - for combat transitions)

Step 2: Place NPC in Installer

In NarrativeInstaller:

Ensure _npcDefinitions list includes your new NPC:
- Drag & drop NpcDefinition into the installer's NPC list (Inspector)
- Or place in Resources/NPCs/ and modify installer to auto-load

Current Implementation (NarrativeInstaller.cs:322):
Container.Bind<INarrativeGenerator>()
.To<NarrativeGenerator>()
.AsSingle()
.WithArguments(_npcDefinitions as IReadOnlyList<NpcDefinition>);

Auto-load Alternative (if you want to avoid manual assignment):
var npcs = _npcDefinitions != null && _npcDefinitions.Count > 0
? _npcDefinitions
: new List<NpcDefinition>(Resources.LoadAll<NpcDefinition>("NPCs"));

Step 3: Verify NPC Pool Integration

The system automatically:
1. Loads NPCs via INpcDataProvider (ScriptableObjectNpcDataProvider)
2. Populates NpcPool during area generation
3. Creates NpcInstance for each NpcDefinition
4. Binds NPCs to story templates via ParameterBinder

No additional code required - the DI container handles everything.

 ---
Part 3: Adding Rewards (Optional)

Step 1: Create RewardDefinition

In Unity Editor:

1. Right-click → Create > Narrative > Reward Definition
2. Configure:
- Reward ID: merchant_quest_gold
- Reward Type: Currency
- Base Value: 50
- Chapter Scaling: (optional - multiply by chapter)

Step 2: Place in Resources

Save to:
Assets/Resources/Rewards/

Auto-loaded by NarrativeInstaller (line 274-277):
var rewards = _rewardDefinitions != null && _rewardDefinitions.Count > 0
? _rewardDefinitions
: new List<RewardDefinition>(Resources.LoadAll<RewardDefinition>("Rewards"));

Step 3: Reference in Story Template

In your StoryTemplateDefinition, add a TemplateRewardSlot:
- Reward Definition: Drag & drop the RewardDefinition
- Probability: 1.0 (100% chance)
- Value Multiplier: 1.0 (use base value)
- Condition: OnSuccess (only on quest completion)

 ---
How the Game Considers New Stories

Runtime Flow

1. Area Generation Start

AreaGenerator.Generate() triggers NarrativeGenerator.GenerateForArea(context):

2. Story Template Selection

StoryTemplateSelector evaluates all templates from Resources/StoryTemplates/:

Eligibility Checks:
- ✓ Ink content exists
- ✓ Chapter in range (MinimumChapter ≤ current ≤ MaximumChapter)
- ✓ Theme matches (if RequiresSpecificTheme)
- ✓ Player alignment in AlignmentRange
- ✓ Prerequisites satisfied (completed stories, active quests, encountered NPCs)
- ✓ Not recently played (if non-repeatable)
- ✓ Required NPC slots can be filled from pool

Scoring (highest wins):
- Base priority (from template)
- +50 theme match bonus (if configured)
- +100 key progression bonus (if IsKeyProgression)
- +20 variety bonus (haven't seen recently)
- +Attribute matching (player history)

3. Parameter Binding

ParameterBinder.BindParameters() fills template slots:

NPC Binding:
- If SpecificNpcId set → validate that NPC is available
- Else → search NpcPool for NPCs matching:
    - Required faction (if enforced)
    - Required role
    - Required traits
    - Not on cooldown
    - Not already assigned
- Select random from top 3 matches (variety)
- Reserve NPC → NpcPool.ReserveNpc() marks as Assigned

Location Binding:
- Uses current area ID from context

Reward Binding:
- Match RewardDefinition by type and chapter
- Apply probability roll
- Apply value multiplier
- Create RewardInstance with calculated values

Output: BoundStory containing:
- Original template
- Dictionary of bound parameters
- List of reserved NPC instances
- Bound location ID
- List of reward instances

4. Story Session Creation

StorySession.Start():
- Loads Ink JSON via IStoryManager
- Injects bound parameters as Ink variables
    - {merchant} = bound NPC's display name
    - {location} = area name
- Navigates to starting knot
- Fires OnStateChanged event

5. Quest Creation

QuestManager.CreateQuest():
- Creates QuestInstance from BoundStory
- Links bound NPCs to quest (for tracking)
- Generates auto-objectives (talk to NPCs, complete story)
- Copies bound rewards
- Registers with NPC tracker

6. Platform Spawning

AreaGenerator.CreateNpcContent():
- Resolves NpcDefinition from INpcDataProvider
- Creates NpcContent with definition
- Binds BoundNpc from GeneratedBoundStory.BoundNpcs
- Platform.Initialize() → NpcContent.Initialize():
    - Binds RuntimeInstance from pool
    - Calls SelectStoryDynamically() (uses NpcStoryProvider)
    - Sets DialogueKnot (from story or default)
    - Spawns NPC visual

7. Dialogue Presentation

When player enters platform:
- DialoguePresenter.StartNpcDialogue(npcId, dialogueKnot)
- DialogueSessionInitializer.InitializeSession():
    - Loads template's Ink JSON
    - Binds external Ink functions
    - Injects BoundParameters into story variables
- Displays dialogue with NPC portrait
- Processes choices/outcomes

 ---
Story Selection Logic

Main Story vs Side Story

Main Stories (Chapter progression):
- Selected first
- Access primary NPC pool
- Higher weight for key progression

Side Stories (Templates):
- Selected after main
- Use secondary NPC pool (NPCs not in main story)
- Continue generation until platform quota met

Repeatability

Non-Repeatable Stories:
- Tracked in CompletedStoryIds
- Never selected again

Repeatable Stories:
- Tracked in StoryCooldowns (platform count)
- Available again after CooldownPlatforms elapsed
- NpcPool.DecrementCooldowns() called each platform

Dynamic NPC Story Selection

If NpcStoryProvider available, NpcContent.SelectStoryDynamically():
1. Gets all stories associated with NPC (NpcDefinition.AssociatedStories)
2. Filters by trigger conditions (encounters, quests, chapter)
3. Scores by:
- Association priority
- Story base priority
- Role bonus (QuestGiver=50, Protagonist=40, Merchant=20)
- Key progression boost (+100)
4. Returns highest-scored story
5. Sets SelectedStory and DialogueKnot

 ---
Directory Structure

Assets/
├── Resources/
│   ├── StoryTemplates/          ← Place StoryTemplateDefinition assets here
│   │   ├── MerchantQuestTemplate.asset
│   │   ├── BanditAmbushTemplate.asset
│   │   └── ...
│   ├── Rewards/                 ← Place RewardDefinition assets here
│   │   ├── GoldReward.asset
│   │   ├── ExperienceReward.asset
│   │   └── ...
│   ├── NPCs/                    ← (Optional) Auto-load NPCs
│   │   ├── EldaraMerchant.asset
│   │   └── ...
│   └── InkStories/              ← Compiled Ink JSON files
│       ├── MerchantQuest.json
│       └── ...
└── __Project/
└── Scripts/
├── Narrative/
│   ├── Data/
│   │   └── Definitions/
│   │       ├── StoryTemplateDefinition.cs
│   │       ├── NpcDefinition.cs
│   │       └── RewardDefinition.cs
│   ├── Generation/
│   │   ├── NarrativeGenerator.cs
│   │   ├── ParameterBinder.cs
│   │   ├── NpcPool.cs
│   │   └── ...
│   └── Core/
│       └── InkStoryManager.cs
└── Core/
└── DI/
└── NarrativeInstaller.cs

 ---
Critical Files Reference
┌────────────────────────────┬──────────────────────────────────────────────────────┬────────────────────────────────┐
│            File            │                         Path                         │            Purpose             │
├────────────────────────────┼──────────────────────────────────────────────────────┼────────────────────────────────┤
│ StoryTemplateDefinition.cs │ Assets/__Project/Scripts/Narrative/Data/Definitions/ │ Template configuration         │
├────────────────────────────┼──────────────────────────────────────────────────────┼────────────────────────────────┤
│ NpcDefinition.cs           │ (Not shown, but referenced)                          │ NPC configuration              │
├────────────────────────────┼──────────────────────────────────────────────────────┼────────────────────────────────┤
│ RewardDefinition.cs        │ Assets/__Project/Scripts/Narrative/Data/Definitions/ │ Reward configuration           │
├────────────────────────────┼──────────────────────────────────────────────────────┼────────────────────────────────┤
│ NarrativeInstaller.cs      │ Assets/__Project/Scripts/Core/DI/                    │ DI container setup             │
├────────────────────────────┼──────────────────────────────────────────────────────┼────────────────────────────────┤
│ NarrativeGenerator.cs      │ Assets/__Project/Scripts/Narrative/Generation/       │ Template selection & binding   │
├────────────────────────────┼──────────────────────────────────────────────────────┼────────────────────────────────┤
│ ParameterBinder.cs         │ Assets/__Project/Scripts/Narrative/Generation/       │ NPC/Location/Reward binding    │
├────────────────────────────┼──────────────────────────────────────────────────────┼────────────────────────────────┤
│ NpcPool.cs                 │ Assets/__Project/Scripts/Narrative/Generation/       │ NPC availability management    │
├────────────────────────────┼──────────────────────────────────────────────────────┼────────────────────────────────┤
│ NpcContent.cs              │ Assets/__Project/Scripts/Platform/Content/           │ NPC platform content           │
├────────────────────────────┼──────────────────────────────────────────────────────┼────────────────────────────────┤
│ AreaGenerator.cs           │ Assets/__Project/Scripts/LevelGeneration/Area/       │ Creates platforms with content │
└────────────────────────────┴──────────────────────────────────────────────────────┴────────────────────────────────┘
 ---
Workflow Summary

To Add a New Story:

1. ✓ Author Ink content (compile to JSON)
2. ✓ Create StoryTemplateDefinition ScriptableObject
3. ✓ Configure parameter slots (NPCs, locations, rewards)
4. ✓ Configure context requirements (chapter, theme, alignment)
5. ✓ Assign Ink JSON to template
6. ✓ Save to Resources/StoryTemplates/
7. ✓ (Optional) Create reward definitions in Resources/Rewards/
8. ✓ Play → System auto-loads and considers template

To Add a New NPC:

1. ✓ Create NpcDefinition ScriptableObject
2. ✓ Configure identity, visual, faction
3. ✓ Add NpcStoryAssociation entries (link to stories)
4. ✓ Set default dialogue knot (fallback)
5. ✓ Add to NarrativeInstaller._npcDefinitions list (or place in Resources/NPCs/)
6. ✓ Play → System auto-loads NPC into pool

No Code Changes Required

The system is fully data-driven:
- New templates are automatically discovered from Resources/StoryTemplates/
- New NPCs are loaded via NarrativeInstaller
- Parameter binding is automatic based on template slots
- Story selection is automatic based on eligibility + scoring

No C# code changes needed to add new stories or NPCs - only ScriptableObject configuration.

 ---
Authoring Best Practices

Story Templates

Parameter Naming:
- Use clear Ink variable names: {merchant}, {quest_giver}, {location}
- Match InkVariableName in parameter slot to Ink variables

Ink Tags:
- Use tags for outcomes: # outcome: quest_started
- Use tags for rewards: # reward: currency:50
- Use tags for NPC reactions: # npc_reaction: merchant:friendly:10

Repeatability:
- Short side quests → IsRepeatable = true with CooldownPlatforms = 5
- Story-driven quests → IsRepeatable = false

NPC Associations

Trigger Conditions:
- First encounter → Always
- Follow-up dialogue → AfterEncounters: 1
- Quest-gated → QuestCompleted: prerequisite_quest_id
- Chapter-gated → ChapterReached: 2

Role Assignment:
- One NPC can have multiple story associations with different roles
- Use Priority to influence selection (higher = more likely)

Chapter Scaling

Use chapter ranges to gate content:
- Tutorial stories → MinimumChapter: 1, MaximumChapter: 1
- Early game → MinimumChapter: 1, MaximumChapter: 3
- Late game → MinimumChapter: 5, MaximumChapter: 0 (no limit)

 ---
Troubleshooting

"Story never appears in game"

Check:
1. ✓ Template saved in Resources/StoryTemplates/
2. ✓ Ink JSON assigned to Ink Content field
3. ✓ Chapter requirements met (MinimumChapter ≤ current chapter)
4. ✓ Theme requirements met (if RequiresSpecificTheme)
5. ✓ Prerequisites satisfied (completed stories, quests)
6. ✓ NPC slots can be filled (NPCs available in pool)
7. ✓ Story not on cooldown (if repeatable)

Debug:
- Check Unity console for [StoryTemplateSelector] logs
- Check [NarrativeGenerator] logs for generation results
- Verify Resources.LoadAll<StoryTemplateDefinition>("StoryTemplates") finds your template

"NPC not appearing in story"

Check:
1. ✓ NPC added to NarrativeInstaller._npcDefinitions
2. ✓ NPC faction matches template's RequiredFaction (if enforced)
3. ✓ NPC not on cooldown
4. ✓ NPC not already assigned to another story
5. ✓ NPC has NpcStoryAssociation for the story (if using dynamic selection)

Debug:
- Check [NpcPool] logs for NPC availability
- Check [ParameterBinder] logs for binding results
- Verify INpcDataProvider.GetNpcById() finds your NPC

"Rewards not granting"

Check:
1. ✓ RewardDefinition saved in Resources/Rewards/
2. ✓ Template's TemplateRewardSlot references correct definition
3. ✓ Probability > 0 (1.0 = 100% chance)
4. ✓ Condition met (e.g., OnSuccess requires successful completion)
5. ✓ Ink story emits correct outcome tag: # outcome: quest_completed

 ---
Next Steps

After adding stories/NPCs, you can:
- Test in Play Mode → Check Unity console for generation logs
- Verify Story Selection → Use debug UI to see available stories
- Monitor NPC Pool → Check which NPCs are available/assigned
- Track Quest State → Verify quests are created from bound stories
- Adjust Scoring → Tune template priority and policy bonuses

The system is designed to be highly configurable without code changes - experiment with different parameter constraints, chapter ranges, and trigger conditions to find the right balance for your game.
