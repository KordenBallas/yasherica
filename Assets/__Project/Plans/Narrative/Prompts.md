You are a senior game systems architect and technical designer.

I am developing a procedural narrative system for a Unity roguelike game using Ink.
The current system is partially implemented but inconsistent and difficult to extend.
I want a comprehensive technical requirements and architecture document to guide a refactor. Store the document under __Project/Requirements folder.

High-level goals:
- Narrative is authored as Ink story templates.
- The game generates unique narrative runs through procedural parameterization.
- NPCs, areas, and rewards are generated at runtime.
- NPCs can change roles over time (quest giver, ally, antagonist).
- Narrative selection must respond to player progress, world state, and area context.
- The system must be maintainable, data-driven, and extensible.

Key design constraints:
- Narrative templates (stories) are static definitions.
- Quests are runtime instances created from story templates.
- The same story template may generate different quests in different runs.
- Rewards, NPCs, and locations are bound at runtime.
- Ink files must be parameterized and injected with runtime values.

Narrative Generation Logic (must be explicitly defined and formalized):

The system must describe and implement a narrative generation pipeline similar to:

Generate Area
↓
Generate NPC Pool
↓
Evaluate Player State and World State
↓
Assemble Narrative Context
(area tags, progression flags, NPC relationships, past choices)
↓
Select StoryTemplates matching:
- progression requirements
- area type and tags
- NPC availability and roles
- world and player state
  ↓
  Bind runtime parameters:
- NPC instances
- locations
- rewards
  ↓
  Instantiate Story
  ↓
  Generate one or more QuestInstances from Story
  ↓
  Attach and run Ink narrative files
  ↓
  Apply outcomes to world state and NPC relationships

Main Story vs Side Stories:
- The system must explicitly support segregation between Main Story and Side Stories.
- Both must use the same core pipeline but differ in constraints, priority, and guarantees.
- Main Story provides the narrative spine and progression gating.
- Side Stories are optional, opportunistic, and procedurally abundant.
- The document should define how StoryTemplates are categorized (e.g., NarrativeTier),
  and how the generator prioritizes and schedules them.

Tasks for this document:
1. Define core domain entities and responsibilities:
    - NPCDefinition vs NPCInstance
    - StoryDefinition vs QuestInstance
    - NarrativeContext
    - Reward templates and instances
2. Define relationships between entities and lifecycle ownership.
3. Specify the narrative generation pipeline in detail.
4. Describe how Ink files are structured, parameterized, and bound at runtime.
5. Propose system modules:
    - Narrative Resolver
    - Quest System
    - NPC Relationship System
    - Reward Generator
6. Include Unity-specific implementation considerations:
    - ScriptableObjects
    - serialization
    - runtime instantiation
    - Ink integration
7. Address maintainability and extensibility concerns:
    - data-driven configuration
    - modular design
    - event-driven world state updates
8. Provide example flow diagrams and sequence descriptions in text form.

The document should be technical, implementation-oriented, and suitable for long-term development by multiple engineers and narrative designers.







1.
Read narrative_system_spec.md.

Using all sections, produce a high-level architecture outline only.
No deep explanations.

The outline should include:
- Core systems
- Data ownership
- Runtime vs static responsibilities


2.
Using section 3 of narrative_system_spec.md,
expand the Narrative Generation Pipeline into a detailed runtime flow.

Requirements:
- Step-by-step order of operations
- Decision points and filters
- Differences between main story and side stories
- Clear separation between template selection and instantiation


3.
Using sections 2 and 5 of narrative_system_spec.md,
define the core domain entities.

For each entity:
- Purpose
- Key fields
- Lifecycle (creation, mutation, destruction)
- Relationships to other entities

Focus on:
NPCDefinition
NPCInstance
StoryDefinition
QuestInstance
NarrativeContext
Reward


4.
Using section 4 of narrative_system_spec.md,
define the policy differences between Main Story and Side Stories.

Include:
- Selection priority
- Progression constraints
- Failure handling
- World impact
- Scheduling rules


5.
Using sections 2, 5, and 6 of narrative_system_spec.md,
define an authoring contract for Ink files.

Include:
- Required variables
- Naming conventions
- How parameters are injected
- How outcomes are reported back to the game
- Do’s and Don’ts for procedural compatibility


6.
Using all sections of narrative_system_spec.md,
propose Unity-specific implementation patterns.

Include:
- ScriptableObject usage
- Runtime instantiation
- Serialization concerns
- Ink runtime integration
- Event-driven architecture suggestions


7.
Assume an existing, partially implemented narrative system
that violates some of the principles in narrative_system_spec.md.

Propose a refactoring strategy:
- Incremental steps
- Risk areas
- Temporary adapters
- How to migrate existing content
