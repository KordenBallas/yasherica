# Refactor Prompt — Data-Driven Procedural Narrative System

## Role & Context

You are working inside an existing Unity project (C#, ScriptableObject-driven content, Zenject DI, MVP presentation, Ink for dialogue). The project already contains a **basic** narrative system built from ScriptableObjects (e.g. `Story`, `NPC`) that is simple and rigidly coupled. Your job is to **refactor it** into a data-driven, recombinable, procedurally-composed narrative system that satisfies the requirements below.

This document specifies **requirements, not an implementation.** A suggested decomposition is given in *Reference Design (non-binding)*. You may adopt it, adapt it, or replace it with a different model — **as long as every requirement in the *Requirements* section is met.** Where you deviate from the reference, briefly justify why your approach serves the requirements better.

## Process (follow in order)

1. **Inspect the existing system.** Locate the current narrative ScriptableObjects and runtime code, map their responsibilities and coupling, and inventory existing content assets. Produce a short **findings summary** first.
1. **Propose a target design** that satisfies the requirements. State whether you are using the reference model or an alternative, justify any deviations, and include a **migration path** for existing content. If the change is sweeping, present this proposal before large-scale edits.
1. **Implement incrementally.** Start with one **end-to-end vertical slice** (one actor → one story template → dialogue → quest → optional combat → fact effects) before generalizing. Do not build the full generic engine up front.
1. **Keep changes ready-to-commit** and follow the repo’s existing documentation governance (CLAUDE.md conventions, doc templates, CHANGELOG/ROADMAP) where present.

## Requirements

### R1 — Separation of concerns (orthogonal building blocks)

Decompose narrative content into independent, separately-authored fragments, with no fragment hard-referencing another:

- **Actor / NPC identity (archetype):** appearance, name, personality, base disposition. Carries no dialogue/quest/combat content.
- **Dialogue:** the interaction medium (Ink-based).
- **Quest:** goal structure with its own lifecycle/state.
- **Enemy / combat definition:** stats, abilities, loot — decoupled from NPC identity.
- **Story:** the connective narrative unit.

Each fragment must be reusable across NPCs, quests, and runs.

### R2 — Definition vs instance

ScriptableObjects are **immutable shared templates** and must hold no per-run mutable state. All mutable state lives in **runtime instances** created per run/encounter. For Ink: the template stores the compiled story; each conversation runs a fresh **per-session** Ink instance with its own serializable state for save/load.

### R3 — Casting / binding layer (where recombination happens)

A runtime binding object (a “casting” / “encounter”) associates an **actor instance** with the content it presents this run: dialogue session, optional quest instance, optional combat fallback, and a context bag of parameters. **Identity and role are separate** — an NPC’s hostility/role is a property of the *casting in this run*, not of the archetype. The same archetype must be castable into different stories/roles across runs, and the same quest/dialogue/enemy must be usable with different archetypes.

### R4 — Ink integration

- **Parameterize** Ink via variables / external functions; the binding layer injects run-specific names, items, locations, etc. Ink files must not hardcode specific NPCs or items, so they stay reusable.
- Use **Ink tags (or equivalent) as the bridge to game systems** (e.g. offer quest, start combat, set fact). A dialogue runner interprets these and dispatches to the relevant systems. Ink must not reference C# types directly.
- **Quest and combat logic live outside Ink.** Ink only triggers them. Quest/combat outcomes are written back into Ink variables so the conversation can branch on results.

### R5 — Story as a template with typed slots

A story is a **skeleton of beats with typed slots** (e.g. dialogue slot, quest slot, combat slot), not a fixed authored script. Slots declare requirements; fragments are matched into slots by **semantic tags/traits**, not by hard references. This is the mechanism that yields per-run variety from a shared fragment library.

### R6 — Run-level composition (Director + shared run-state)

Coherence across a run lives **one level above the story.** A story template must remain atomic and know nothing about other stories. A **run-level director** selects and sequences stories per run (by tags, pacing, and thread balance). A **shared run-state** holds the facts that stories read and write.

### R7 — Fact-based emergent coupling (QBN / storylet model)

Stories must not reference each other directly; they read and write a **shared fact space**. Each story template declares:

- **Preconditions:** predicates over facts that gate eligibility.
- **Effects / footprint:** the facts it may mutate. Exact values may be emitted at play-time via Ink tags based on player choice; the template still declares its footprint so the director can plan.

Cross-story connections must **emerge from overlapping reads/writes**, never from authored edges between stories.

### R8 — First-class threads / arcs

Narrative threads/arcs are **first-class runtime entities** with an id and a current stage, so the director can balance them and the player can read them. “A choice in thread A affects thread C” must be expressed as: an **effect written by A’s story is read by a precondition of C’s story.** No direct thread-to-thread coupling.

### R9 — Multi-namespace facts, uniform machinery

The fact space spans at least three namespaces in **one unified store**: `world` (events/locations), `actor` (per-NPC), `faction` (reputation/power). Preconditions, effects, and the director must operate **uniformly** over all namespaces — do **not** build three parallel systems. A single story may touch any combination of namespaces.

### R10 — Actor ↔ faction interlock

Actors belong to factions. Preconditions/effects may reference both an individual actor’s state and its faction’s state (e.g. faction reputation tints an actor’s disposition unless a personal override exists). This connects faction-level and actor-level narrative without a separate mechanism.

### R11 — Consequence cascades: explicit first

Cross-category cascades (e.g. *refuse fire quest → village burns → an NPC dies → a faction gains power*) must be expressible. **Start with explicit effects** authored in the triggering story. Design so that a thin, optional **reactive-rule layer** can later derive cascades centrally — but do **not** build a general rules engine up front.

### R12 — Recurring actors

The same actor instance may appear across multiple threads/stories within a run, and facts about it (helped / betrayed / alive) must carry over and change its later appearances. Treat this as a primary source of felt interconnection.

### R13 — Debuggability & authoring safety

Because connections are emergent, provide:

- An **inspector / debug view** of the live run-state (facts, threads, castings).
- A **deliberately authored, typed fact vocabulary** — a single source of truth for fact keys — rather than ad-hoc string flags scattered through content.
- Support for optional authored **“spine” beats** that can guarantee a structural backbone while the rest stays emergent.

### R14 — Engine / architecture constraints

- ScriptableObjects remain the **primary content-extension surface**: designers add content by creating/wiring SOs without code changes where possible.
- Core services (dialogue runner, director, casting/binding, fact store) are **Zenject-injected**; runtime narrative entities fit the existing **MVP** layering (presenters over casting/instance models).
- **Integrate with existing systems where present:** the modular character/appearance system for actor visuals, the artifact/loot system for quest/combat rewards, and procedural level generation for placement.
- Provide **save/load** for all runtime state: per-session Ink state, run-state facts, thread stages, active quests, and castings.

## Reference Design (non-binding)

A suggested decomposition you may adopt or replace:

- **SO templates:** `NpcArchetype`, `Dialogue` (wraps compiled Ink + declared parameters/tags), `Quest`, `EnemyDefinition`, `StoryTemplate` (slots + preconditions + effect footprint + tags), `Thread/Arc` definition.
- **Runtime:** namespaced/typed `WorldState` fact store, `NpcInstance`, `DialogueSession`, `QuestInstance`, `Casting`/`Encounter`, `Thread` instance, `RunDirector`, `DialogueRunner` (tag dispatch), and an optional `ReactionLayer` (added later).

You are free to choose different boundaries, names, and mechanisms. Justify deviations against the requirements.

## Deliverables

1. Findings summary of the existing narrative system.
1. Target design + migration plan (old SOs/content mapped to the new model).
1. Incremental implementation, vertical slice first.
1. Updated documentation per repo governance; ready-to-commit changes.

## Acceptance Criteria (the refactor is done when…)

- Fragments (actor, dialogue, quest, enemy, story) are independently authorable and reusable; none hard-references another.
- The same archetype produces different experiences across runs; the same quest/dialogue works with different archetypes.
- Stories couple **only** through the shared fact space; no story references another story.
- A choice in one thread **provably** changes eligibility/content in another, via facts.
- World, actor, and faction facts flow through **one** uniform precondition/effect/director pipeline.
- Ink files are parameterized and free of C# coupling; quests/combat run outside Ink and report results back into it.
- Existing narrative content is migrated, or has a documented migration path.
- Run-state is inspectable and fully save/load-capable.