# Ink Authoring Contract

This document defines the rules and conventions for content authors writing Ink files compatible with the procedural narrative generation system.

---

## 1. Required Variables

All runtime-bound variables must be declared at the **top of the file** with default values.

### Rules

- Use `snake_case` for all variable names
- Default values are **required** for Inky editor testing
- Variable names must match the `InkVariableName` field in `TemplateParameterSlot`

### Example

```ink
// === Variables ===
VAR npc_name = "Stranger"
VAR reward_amount = "50"
VAR item_name = "mysterious item"
VAR location_name = "this place"
VAR quest_target_count = "3"
```

---

## 2. Parameter Slot Conventions

The `TemplateParameterSlot.InkVariableName` field defines which Ink variable receives the bound value at runtime.

### Parameter Type to Ink Value Mapping

| ParameterType | Value Injected | Ink Type | Example Value |
|---------------|----------------|----------|---------------|
| `Npc` | NPC's `DisplayName` | `string` | `"Merchant Tomas"` |
| `Location` | Location display name | `string` | `"the village"` |
| `Reward` | Calculated value | `string` | `"50"` |
| `ItemName` | Generated item name | `string` | `"ancient artifact"` |
| `Quantity` | Integer as string | `string` | `"3"` |
| `Custom` | Slot's `DisplayName` | `string` | varies |

### Important Notes

- **All injected values are strings** - Ink will receive string values
- For numeric comparisons, parse strings: `{reward_amount > 10: ...}` works because Ink auto-converts
- Default values must be valid for the expected type

### Template Definition Example

```csharp
// In StoryTemplateDefinition ScriptableObject
ParameterSlots:
  - ParameterId: "quest_giver"
    ParameterType: Npc
    InkVariableName: "npc_name"  // <-- This must match VAR in Ink
    IsRequired: true
```

### Corresponding Ink

```ink
VAR npc_name = "Unknown NPC"

=== start ===
# speaker: {npc_name}
"Greetings, traveler!"
```

---

## 3. Tag Reference

Tags control runtime behavior and story metadata. Use the format `# tag_name: value`.

### 3.1 Metadata Tags (Knot-Level)

Place these at the **start of a knot**, immediately after the knot declaration.

| Tag | Description | Values | Example |
|-----|-------------|--------|---------|
| `# platform_type:` | Platform content type | `simple`, `dialogue`, `combat`, `cutscene` | `# platform_type: dialogue` |
| `# npc:` | Associated NPC ID | NPC identifier string | `# npc: merchant_tomas` |
| `# key_node:` | Marks progression point | `true`, `false` | `# key_node: true` |
| `# priority:` | Selection priority | Integer (higher = more important) | `# priority: 5` |

#### Example

```ink
=== merchant_intro ===
# platform_type: dialogue
# npc: merchant_tomas
# key_node: true
# priority: 1

# speaker: Merchant Tomas
"Welcome to my shop!"
```

### 3.2 Prerequisite Tags

Control when a knot becomes available.

| Tag | Description | Example |
|-----|-------------|---------|
| `# require_quest_completed:` | Requires quest completion | `# require_quest_completed: guild_initiation` |
| `# require_npc_met:` | Requires previous NPC encounter | `# require_npc_met: merchant_tomas` |

#### Example

```ink
=== guild_mission ===
# platform_type: dialogue
# npc: guild_captain
# require_quest_completed: guild_initiation

# speaker: Guild Captain
"You've proven yourself. Here's your next mission."
```

### 3.3 Combat Tags

Configure combat-capable NPCs and encounters.

| Tag | Description | Example |
|-----|-------------|---------|
| `# npc_can_become_enemy:` | NPC can turn hostile | `# npc_can_become_enemy: true` |
| `# enemy_id:` | Enemy type identifier | `# enemy_id: 1` |

#### Example

```ink
=== bandit_encounter ===
# platform_type: combat
# npc: bandit_leader
# npc_can_become_enemy: true
# enemy_id: 1
# key_node: true

# speaker: Bandit
"Your gold or your life!"
```

### 3.4 Runtime Tags (Inline)

These tags are processed during dialogue execution.

| Tag | Description | Processed By | Example |
|-----|-------------|--------------|---------|
| `# speaker:` | Current dialogue speaker | `DialoguePresenter` | `# speaker: Guard` |
| `# outcome:` | Terminal outcome type | `DialoguePresenter` | `# outcome: Combat` |
| `# quest:` | Triggers quest event | `DialoguePresenter` | `# quest: fetch_herbs` |
| `# combat:` | Triggers combat event | `DialoguePresenter` | `# combat: wolf_pack` |

#### Speaker Tag

Use `# speaker:` for **every dialogue line** to identify who is speaking.

```ink
# speaker: Merchant Tomas
"Hello there!"

# speaker: Player
"What do you have for sale?"

# speaker: Merchant Tomas
"Many fine wares!"
```

#### Outcome Tags

Outcome tags signal how the dialogue ends. **Must appear BEFORE `-> END`**.

| Outcome Value | Effect |
|---------------|--------|
| `Combat` | Ends dialogue, triggers combat |
| `Quest` | Ends dialogue, triggers quest |
| `Trade` | Ends dialogue, opens trade |
| `Exit` | Normal dialogue exit |

---

## 4. External Functions

External functions bridge Ink to game systems. Always provide stubs for Inky editor testing.

### Declaration Pattern

```ink
// At file top, after variables
EXTERNAL trigger_combat(enemy_count)

// Stub implementation for editor testing
=== function trigger_combat(enemy_count) ===
~ return
```

### Available External Functions

| Function | Parameters | Description |
|----------|------------|-------------|
| `trigger_combat(enemy_count)` | `int` - Number of enemies | Initiates combat encounter |

### Usage

```ink
=== battle_start ===
# outcome: Combat
The enemy attacks!
~ trigger_combat(1)
-> END
```

---

## 5. Outcome Reporting

Outcomes determine what happens after dialogue ends. Proper outcome reporting is critical for game state transitions.

### Combat Outcome

```ink
=== fight ===
# speaker: Enemy
"You'll pay for that!"

# outcome: Combat
~ trigger_combat(1)
-> END
```

**Requirements:**
1. `# outcome: Combat` tag
2. Call `trigger_combat(n)` with enemy count
3. End with `-> END`

### Quest Outcome

```ink
=== accept_quest ===
# speaker: Quest Giver
"Thank you for accepting!"

# outcome: Quest
# quest: rescue_villagers
-> END
```

**Requirements:**
1. `# outcome: Quest` tag
2. `# quest: <quest_id>` tag
3. End with `-> END`

### Trade Outcome

```ink
=== open_shop ===
# speaker: Merchant
"Let me show you my wares."

# outcome: Trade
-> END
```

### Exit Outcome

```ink
=== farewell ===
# speaker: NPC
"Safe travels!"

# outcome: Exit
-> END
```

### Important Rules

- Outcome tags must appear **BEFORE** `-> END`
- Only use `# outcome:` for **terminal knots** (knots that end the conversation)
- A knot without outcome tags that reaches `-> END` will use default `Continue` outcome

---

## 6. Do's and Don'ts

### DO

- **Declare all runtime variables** with sensible defaults at file top
- **Use `# speaker:`** for every line of dialogue
- **Place metadata tags** at the start of knots (immediately after `===`)
- **End outcome knots** with `-> END`
- **Provide stubs** for all external functions
- **Match variable names** exactly with `InkVariableName` in template definition
- **Test in Inky editor** before committing

### DON'T

- **Use undeclared variables** - all variables must be declared with `VAR`
- **Place outcome tags after `-> END`** - they won't be processed
- **Hardcode NPC names** that should be parameterized - use variables
- **Use `# outcome:`** for non-terminal knots (knots that continue to other knots)
- **Forget the `# speaker:` tag** - dialogue without speakers displays incorrectly
- **Use numeric variables directly** for injected values - they're always strings

### Common Mistakes

```ink
// WRONG: Undeclared variable
=== start ===
# speaker: {mystery_npc}  // Error: mystery_npc not declared
"Hello!"

// CORRECT: Variable declared
VAR mystery_npc = "Stranger"
=== start ===
# speaker: {mystery_npc}
"Hello!"
```

```ink
// WRONG: Outcome after END
=== bad_outcome ===
# speaker: Enemy
"You die!"
-> END
# outcome: Combat  // This is never processed!

// CORRECT: Outcome before END
=== good_outcome ===
# speaker: Enemy
"You die!"
# outcome: Combat
-> END
```

```ink
// WRONG: Outcome on non-terminal knot
=== talk ===
# outcome: Quest  // Wrong! This knot continues
# speaker: NPC
"Let me think..."
-> decision  // Not ending here

// CORRECT: Outcome only on terminal
=== talk ===
# speaker: NPC
"Let me think..."
-> decision

=== decision ===
# speaker: NPC
"I'll give you the quest."
# outcome: Quest
# quest: my_quest
-> END
```

---

## 7. Validation Checklist

Before submitting an Ink file, verify:

### Variables
- [ ] All `VAR` declarations at file top
- [ ] All variables have default values
- [ ] Variable names match `InkVariableName` in template definition
- [ ] Uses `snake_case` naming convention

### Structure
- [ ] Entry knot matches `StartingKnot` in template definition
- [ ] First/main knot has `# platform_type:` tag
- [ ] Metadata tags placed at knot start (after `===`)

### Dialogue
- [ ] Every dialogue line has `# speaker:` tag
- [ ] Parameterized NPC names use variables, not hardcoded strings

### Outcomes
- [ ] `# outcome:` tags appear before `-> END`
- [ ] `# outcome: Quest` paired with `# quest:` tag
- [ ] `# outcome: Combat` paired with `trigger_combat()` call
- [ ] No `# outcome:` tags on non-terminal knots

### External Functions
- [ ] All external functions declared with `EXTERNAL`
- [ ] Stub implementations provided for all externals

### Testing
- [ ] File compiles without errors in Inky editor
- [ ] All story paths are reachable
- [ ] Default variable values produce sensible test dialogue

---

## 8. Complete Example

```ink
// Parameterized Side Story Template
// Template: fetch_quest_template

// === Variables ===
VAR quest_giver_name = "Unknown NPC"
VAR target_item = "mysterious item"
VAR item_quantity = "3"
VAR reward_gold = "50"
VAR target_location = "the wilderness"

// === External Functions ===
EXTERNAL trigger_combat(enemy_count)

=== function trigger_combat(enemy_count) ===
~ return

// === Story Entry Point ===
=== fetch_quest_start ===
# platform_type: dialogue
# npc: quest_giver
# key_node: true
# priority: 1

# speaker: {quest_giver_name}
"Traveler! I need your help. I require {item_quantity} {target_item} from {target_location}."

+ ["What's in it for me?"]
    -> discuss_reward
+ ["I'll help you."]
    -> accept_quest
+ ["Not interested."]
    -> decline_quest

=== discuss_reward ===
# speaker: {quest_giver_name}
"I can offer you {reward_gold} gold for your troubles. A fair price, wouldn't you say?"

+ ["Deal."]
    -> accept_quest
+ ["That's not enough."]
    # speaker: {quest_giver_name}
    "I'm afraid that's all I can offer. Take it or leave it."
    + ["Fine, I'll do it."]
        -> accept_quest
    + ["No deal."]
        -> decline_quest

=== accept_quest ===
# speaker: {quest_giver_name}
"Excellent! Return to me when you have the items."

# outcome: Quest
# quest: fetch_quest_instance
-> END

=== decline_quest ===
# speaker: {quest_giver_name}
"A pity. If you change your mind, you know where to find me."

# outcome: Exit
-> END
```

---

## 9. Reference

### Files

| File | Purpose |
|------|---------|
| `StoryTemplateDefinition.cs` | Defines parameter slots and template config |
| `ParameterBinder.cs` | Binds runtime values to Ink variables |
| `StorySession.cs` | Injects variables and manages story execution |
| `DialoguePresenter.cs` | Processes runtime tags during dialogue |

### Tag Processing Flow

1. **Story Load**: `StorySession.Start()` injects bound parameters
2. **Knot Entry**: Metadata tags read by platform system
3. **Dialogue Line**: `DialoguePresenter.ProcessTag()` handles `speaker`, `outcome`, `quest`, `combat`
4. **Story End**: Outcome tags determine post-dialogue behavior
