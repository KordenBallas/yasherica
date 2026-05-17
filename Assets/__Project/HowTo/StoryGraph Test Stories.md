# StoryGraph Test Stories - Setup Documentation

This document describes the 5 simple test stories created for testing the StoryGraph-based scenario generation system.

## Created Assets

### NPCs (in `Assets/__Project/Resources/Test/`)

1. **TestNPC_Merchant** (ID: 100)
   - Display Name: Merchant Aldric
   - Description: A traveling merchant with rare goods
   - Faction: Merchant
   - Cannot become enemy

2. **TestNPC_Guard** (ID: 101)
   - Display Name: Guard Captain Thorne
   - Description: Vigilant city guard captain
   - Faction: Friendly
   - Can become enemy

3. **TestNPC_Traveler** (ID: 102)
   - Display Name: Wanderer Elara
   - Description: A mysterious traveler with tales to tell
   - Faction: Neutral
   - Cannot become enemy

4. **TestNPC_Sage** (ID: 103)
   - Display Name: Sage Mortimer
   - Description: Ancient keeper of forgotten knowledge
   - Faction: Quest Giver
   - Cannot become enemy

5. **TestNPC_Blacksmith** (ID: 104)
   - Display Name: Blacksmith Greta
   - Description: Master weaponsmith and armorer
   - Faction: Friendly
   - Cannot become enemy

### Dialogue Stories (in `Assets/__Project/Resources/Stories/`)

1. **Story_MerchantGreeting** (`merchant_greeting`)
   - NPC: Merchant Aldric (100)
   - Description: Meet a traveling merchant offering goods for sale
   - Possible Outcome: Trade
   - Repeatable: Yes (3 encounter cooldown)
   - Priority: 50
   - Relationships: None
   - Prerequisites: None

2. **Story_GuardPatrol** (`guard_patrol`)
   - NPC: Guard Captain Thorne (101)
   - Description: Encounter a vigilant guard captain on patrol
   - Possible Outcome: Combat
   - Repeatable: No
   - Priority: 60
   - Relationships: Branches to Merchant Greeting
   - Prerequisites: None

3. **Story_TravelerTale** (`traveler_tale`)
   - NPC: Wanderer Elara (102)
   - Description: Listen to a wanderer's story of dragons and adventure
   - Possible Outcome: Continue
   - Repeatable: Yes (5 encounter cooldown)
   - Priority: 40
   - Relationships: Sequences to Sage Wisdom
   - Prerequisites: None

4. **Story_SageWisdom** (`sage_wisdom`)
   - NPC: Sage Mortimer (103)
   - Description: Seek ancient knowledge from a wise sage
   - Possible Outcome: Quest (prophecy_quest)
   - Repeatable: No
   - Priority: 70
   - Key Progression: **YES**
   - Relationships: Sequences to Blacksmith Forge
   - Prerequisites: None

5. **Story_BlacksmithForge** (`blacksmith_forge`)
   - NPC: Blacksmith Greta (104)
   - Description: Visit a master blacksmith to commission new equipment
   - Possible Outcome: Quest (sword_craft_quest)
   - Repeatable: Yes (10 encounter cooldown)
   - Priority: 55
   - Relationships: None
   - Prerequisites: **Requires guard_patrol to be completed**

## StoryGraph Relationships

The stories create the following graph structure:

```
guard_patrol (101)
    |
    ├─[Branch]─> merchant_greeting (100)

traveler_tale (102)
    |
    └─[Sequence]─> sage_wisdom (103) [KEY PROGRESSION]
                      |
                      └─[Sequence]─> blacksmith_forge (104)
                                        [Requires: guard_patrol completed]
```

## Story Attributes

Each story has NPC and tone attributes for dynamic matching:

- **merchant_greeting**: npc=100, tone=friendly
- **guard_patrol**: npc=101, tone=tense
- **traveler_tale**: npc=102, tone=mysterious
- **sage_wisdom**: npc=103, tone=wise
- **blacksmith_forge**: npc=104, tone=practical

## NarrativeInstaller Configuration

The NarrativeInstaller in the Area scene has been configured to:
- Include all 5 test NPCs in `_npcDefinitions`
- Use `ScenarioGeneratorType.StoryGraph` mode
- Auto-discover DialogueStoryDefinition assets from `Resources/Stories/`

## Testing the StoryGraph

When you run the game:

1. The StoryGraph will build a graph with 5 dialogue story nodes
2. The graph will establish edges based on relationships:
   - Guard Patrol → Merchant Greeting (branch relationship)
   - Traveler Tale → Sage Wisdom → Blacksmith Forge (sequence chain)
3. Node states will be evaluated based on prerequisites:
   - Initially, 4 stories will be available (all except blacksmith_forge)
   - blacksmith_forge becomes available after completing guard_patrol
4. The StoryGraphScenarioGenerator will select stories based on:
   - Node availability (prerequisites met)
   - Story priority
   - Edge activation (relationship conditions)
   - Story attributes and theme matching

## Ink Dialogue Structure

Each story has a simple branching dialogue:

- **Merchant**: Trade or leave
- **Guard**: Peaceful conversation or combat escalation
- **Traveler**: Listen to tale or decline
- **Sage**: Learn prophecy (quest) or leave
- **Blacksmith**: Commission sword (quest) or browse

## Expected Behavior

With StoryGraph scenario generation:

1. **Initial Availability**: merchant_greeting, guard_patrol, traveler_tale, sage_wisdom
2. **After completing guard_patrol**: blacksmith_forge becomes available
3. **Story Selection**: Priority-based selection favors sage_wisdom (70) and guard_patrol (60)
4. **Repeatable Stories**: merchant_greeting and traveler_tale can appear multiple times
5. **Quest Chain**: traveler_tale → sage_wisdom → blacksmith_forge forms a narrative progression

## Next Steps

To further test the StoryGraph:

1. Add more complex prerequisite conditions
2. Create conditional edges based on Ink variables
3. Test chapter-based progression
4. Add story templates for procedural generation
5. Implement relationship score tracking between NPCs and player
