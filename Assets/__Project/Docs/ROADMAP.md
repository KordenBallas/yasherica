# Roadmap

Planned work and open gaps, grouped by system, plus a cross-cutting backlog. Checkbox items.

Maintenance rules (CLAUDE.md §8.3):
- New requirement from the user → add an unchecked item under its system.
- AI proposes an enhancement / notices a limitation → add it here (don't just mention it in chat).
- Item implemented → check it off and move the substance into `CHANGELOG.md`.
- Items here mirror the "Known limitations / open points" sections of the system docs; keep them in sync.

Suggested tags: `[content]` data/authoring, `[arch]` structural, `[debt]` cleanup, `[perf]`,
`[rule]` fixes a CLAUDE.md violation.

---

## Cross-cutting

- [ ] `[arch]` **Non-item reward types have no receiving system.** `Currency`, `Experience`,
  `Ability` rewards are rolled by the narrative system and skipped by the loot granter (logged only).
  Decide owners and wire receiving systems. *(narrative + loot)*

---

## Narrative Generation

Planned design (from `narrative-generation.md` §4):
- [ ] `[arch]` **P1 — Story↔NPC compatibility scoring.** Score valid story+NPC pairs (tag overlap,
  faction fit, combat capability); assign by best score instead of randomly. Hard requirements prune,
  preferences only weight.
- [ ] `[arch]` **P2 — Rule-based outcome quotas.** Config quotas (min combat stories, min peaceful);
  satisfy quotas first, then fill by compatibility score; report shortfalls.
- [ ] `[arch]` **P3 — Progression-driven selection.** Feed `GameContext` (`CharacterLevel`,
  `Progress`, `StoryState`) into generation; derive difficulty/theme filters and weighting from progression.
- [ ] `[content]` **P4 — Soft cooldowns.** Fold cooldown/repeatability into scoring (recently seen →
  lower score) so a small pool degrades gracefully instead of producing empty levels.

Known limitations (§5):
- [ ] `[arch]` Skipped story (no candidate NPC) is not backfilled, so a level may fall below `MinStories`.
- [ ] `[arch]` `GameContext` not consulted by `LevelNarrativeGenerator`; `ScenarioGenerator` hardcodes difficulty to 10.
- [ ] `[content]` `RewardSlot.Condition` is stored but never evaluated.
- [ ] `[rule]` Generator logs via `Debug.Log/LogWarning` directly — violates the logger-abstraction rule (§9).

---

## Loot Subsystem

Known limitations (from `loot-subsystem.md` §4):
- [ ] `[content]` Explicit Ink "quest completed" signal — today walking away from a dialogue still
  completes the platform and grants rewards.
- [ ] `[arch]` Filler platforms (beyond scenario requirements) never carry loot; only requirement-driven ones roll it.
- [ ] `[arch]` Non-item `RewardType`s have no receiving system (see Cross-cutting).
- [ ] `[arch]` Inventory capacity feedback (R11) is unreachable while inventory is unlimited;
  `UnlimitedCapacityPolicy` is the rebinding point.
- [ ] `[content]` Progression gating (R13: `minPlayerLevel`, `requiredAchievements`, `requiredPastQuests`)
  is surfaced in the roll context but no `ILootEntryFilter` consumes it yet.
- [ ] `[debt]` `WorldArtifactView` duplicates `BubbleView`'s depth-stack construction; extract a shared
  builder once a third consumer appears.
- [ ] `[arch]` World pickups are not despawned when leaving a platform; they persist until collected or scene unload.

---

## Character System

Known points (from `character-system.md`):
- [ ] `[perf]` Runtime skinned-mesh combining is not implemented (design keeps it possible — the
  controller owns the live per-slot renderers a combiner would consume).
- [ ] `[debt]` `PartSwapExecutor`, `SocketMounter`, and the factory are verified only manually (demo
  bootstrap + preview window); consider play-mode test coverage.

---

## Character Locomotion

Known limitations (from `character-locomotion.md` §6):
- [ ] `[content]` Run clip is a procedural placeholder; no walk tier (blend is idle↔run only).
- [ ] `[arch]` `Speed` has no acceleration smoothing — it tracks input instantly; add damping if the
  blend looks abrupt.
- [ ] `[arch]` Velocity reflects movement intent (`moveDir * moveSpeed`), so the character runs in
  place when pushing into a wall; consider deriving from actual displacement.
- [ ] `[arch]` Idle does not resync the solver's yaw from forced facing, so a turn-to-camera pose is
  not preserved as the new heading once movement resumes.
- [ ] `[debt]` The Hero is composed by an editor menu (`Place Moving Hero In Scene`), not shipped as a prefab.

---

## Ability Subsystem

- [ ] _seed from `ability-subsystem.md` "Known limitations" on next pass._

## Inventory Subsystem

- [ ] _seed from `inventory-subsystem.md` "Known limitations" on next pass._

---

## Backlog (unsorted)

- [ ] _new ideas land here, then get sorted into a system above._
