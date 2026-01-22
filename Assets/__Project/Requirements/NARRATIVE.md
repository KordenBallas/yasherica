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
