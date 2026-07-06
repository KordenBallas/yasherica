# Narrative Generation — RETIRED (legacy pipeline)

> ⚠️ **RETIRED — this described a deleted system.** The level-narrative pipeline this document once
> specified — `LevelNarrativeGenerator` selecting `StoryDefinition`/`NpcDefinition`/`RewardDefinition`
> per level, `RewardResolver`, `CompositeDialoguePresenter`, `LevelNarrativeConfig`, `ScenarioGenerator`
> → `PlatformGraphGenerator`, wired via `NarrativeInstaller` — was **replaced by the story-first
> streaming director** in the Phase-3 cutover and **deleted** (ROADMAP "Delete legacy (Phase 3)"). None
> of those types exist in the codebase anymore.
>
> **Live docs — read these instead:**
> - **`narrative-procedural.md`** — the streaming director **as implemented**: the windowed planner,
>   first-class threads, the fact system, world-content density, and D19 escalation.
> - **`narrative-director-requirements.md`** — the forward-looking director requirements.
>
> This file is retained only as a tombstone plus the **Residue** section below, which the **P3-8** item
> audits. Do not treat anything here as a description of a running system.

## What it was (one paragraph, for history)

The legacy pipeline formed each level by selecting `StoryDefinition` assets from an `IStoryPool` (filtered
by theme / difficulty / required-and-excluded tags + per-run cooldowns), assigning **exactly one**
`NpcDefinition` per story in a two-pass, combat-first match, rolling per-slot `RewardDefinition`s through a
`RewardResolver`, and layering story + character Ink in a `CompositeDialoguePresenter`. `ScenarioGenerator`
→ `PlatformGraphGenerator` → `AreaGenerator` turned the assignments into platforms, all wired by
`NarrativeInstaller`. The **streaming director superseded this wholesale**: content is streamed per window
against a live fact store, stories carry their own typed slots + fact preconditions (there is no separate
`NpcDefinition`; actors are minted from `NpcArchetype`), threads and continuity are first-class, and rewards
live in the quest subsystem. See `narrative-procedural.md`.

## Residue — the P3-8 audit (planned design, NOT implemented)

> This is the original **§4 "Planned design (P1–P4)"**, preserved because **P3-8** ("narrative-gen residue
> folded into the streaming director") audits it. **Re-scope (2026-07-05):** most of P1–P4 is already
> **delivered or superseded** by the streaming director; the **live residue** is P4 (+ optional P1). Each
> item below keeps its original statement followed by its current status.

**P1. Story↔NPC compatibility scoring.** *(original)* Stories declare *preferred* and *required* NPC traits
(tags, faction) beyond the hard combat constraint; the generator scores every valid story+NPC pair (tag
overlap, faction fit, combat capability) and assigns by best score, with hard requirements pruning pairs and
preferences only affecting the score.
— **Status: partially delivered / optional residue.** The streaming director already applies the "hard
requirements prune, preferences only weight" principle as a soft archetype-tag preference
(`RunWindowPlanner.MatchArchetype`, `narrative-procedural.md` §2.6). The fuller multi-trait / faction-fit
scoring model is the **optional** residue P3-8 may pick up as actor-casting tuning.

**P2. Rule-based outcome quotas.** *(original)* `LevelNarrativeConfig` gains quotas (minimum combat-capable
stories, minimum peaceful stories) derived from level settings, satisfied first, then remaining slots filled
by best compatibility score; shortfalls reported, not silently dropped.
— **Status: superseded.** Replaced by the two **content-density budgets** (quest rarity/spacing + the
empty/loot/combat ambient mix, `WorldContentDensityConfig` — D8/D9). No further work.

**P3. Progression-driven selection.** *(original)* `GameContext` (`CharacterLevel`, `Progress`,
`StoryState`) becomes an input to narrative generation; difficulty windows and theme filters derive from
progression and story weighting prefers stage-appropriate content.
— **Status: superseded / delivered.** Progression enters selection through **facts**, not a `GameContext`
input: **D19 run-escalation tier gating** (shipped 2026-07-06 — `run_escalation_tier` bands on stories +
creatures) plus passport/race facts consumed as preconditions.

**P4. Soft cooldowns.** *(original)* Cooldown and repeatability fold into the **scoring** (recently seen
stories score lower) instead of acting purely as hard filters, so a **small content pool degrades
gracefully** rather than producing empty levels.
— **Status: the live residue (the main body of P3-8).** The streaming director uses **hard** continuity
gates today (`StoryRunLedger`: a beat placed/resolved/retired this run never re-places; ambient-colour
chatter is exempt and may repeat). Folding recency into a soft score for graceful small-pool degradation is
net-new. Exact scope is set by **P6-1**'s reconcile audit (this file).
