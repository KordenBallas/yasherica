# Save / Continue & Cross-Run Memory — Requirements & Design

> The persistence layer (P2-2): one implicit autosaved **continue** that a death consumes, and a
> separate always-on **cross-run world memory** carrying Meta-horizon facts across deaths — the two
> halves of the Hades frame. Brief: `product-requirements/save-continue-run.md`.
> Status: current as of 2026-07-05.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** One implicit in-progress run save, no slots, no manual save action. A present save offers
  **Continue** in the main menu; absent means a fresh run. (Brief A1/A3)
- **R2** Death consumes the run save: after a Defeat the next launch cannot reload any point of the
  dead run — save-scumming is impossible by construction. The world memory is flushed **before**
  the save is deleted, so the world remembers the fatal run. (A2/FR2)
- **R3** Continue restores the whole run as one image: the generated world behind **and the same
  beats ahead**, the hero's current platform, the built body (frame + equipped/dormant parts),
  inventory + part stash + blank rack with in-progress socketing, the quest log at exact stages,
  all facts, live threads, met actors with per-actor state, and the seed + every generator cursor.
  (FR4/FR5)
- **R4** Savepoints are platform entries **plus the graceful exit** (application quit / editor
  play-mode stop), so progress made on the current platform — a finished conversation, its facts
  and quest stages — survives a quit. Quitting mid-encounter resumes at the platform's clean start:
  the quit save is skipped while any dialogue is open (and the regular savepoint while one awaits
  an external step, W3-1), so the entry-time save stands and the encounter re-begins. Everything
  completed on platforms the player has **left** is preserved and never respawns. (FR6–8)
- **R5** Deterministic resume: after Continue, all procedural generation (window plans, castings,
  loot rolls, site blocks) plays out identically to the un-interrupted run. (FR12)
- **R6** A separate cross-run memory file persists the `FactHorizon.Meta` partition across every
  death and fresh run, loaded into the fact store at every Area boot. (FR9/FR10)
- **R7** Fail-safe files: a corrupt/missing/stale-version run save is discarded (fresh run, world
  memory untouched); a corrupt world memory is **quarantined** (renamed `.corrupt`) and the game
  runs on an empty memory; the two files are independently recoverable; writes are atomic
  (temp file + swap). Nothing ever hard-crashes on bad save data. (FR13/FR14)
- **R8** Journey starts an explicitly NEW run — **revised by O1**: the menu's Journey now stages on
  the Hub, and the run save is deleted at the Hub's **LAUNCH** (the commit point), not at the menu
  click. Backing out of the Hub keeps Continue alive; abandoning a run at launch is allowed and is
  not save-scumming. (PO decision, revised 2026-07-06)
- **R9** One demo Meta fact (`world.barn_bounty_honored`, set on barn-bounty completion) proves the
  cross-run memory end-to-end: set in run 1, readable in run 2's store. (FR11)

### 1.2 Non-functional requirements

- **N1** All capture/restore logic is pure C# and unit-tested; Unity enters only through
  `UnityJsonSaveSerializer` (JsonUtility) and `Application.persistentDataPath` in the installer.
- **N2** All wiring through Zenject; the save **file on disk is the cross-scene carrier** — no
  ProjectContext, no statics, every scene stays a self-contained SceneContext.
- **N3** No new ScriptableObject types: the content surface is the existing
  `FactKeyDefinition.Horizon` field (§3).

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Core/Persistence/            — the persistence system (pure C# except the serializer)
  JsonSaveFile.cs                    — versioned, atomic, corrupt-policy file gateway
  RunSaveStore / MetaMemoryStore     — run.json (Delete policy) / meta.json (Quarantine policy)
  RunSetupStore / HubArrivalStore    — run-setup.json / hub-arrival.json (O1 one-shot carriers,
                                       Delete policy; Hub → Area launch picks / Area → Hub death marker)
  RunStartConditions                 — "how does this run start": restore wins, fresh consumes the
                                       setup file on read, nothing = defaults (O1)
  RunSaveSnapshot + section DTOs     — the whole-run image (World/Body/Stuff + narrative sections)
  RunStateService                    — explicit whole-run capture/restore aggregator (D4)
  RunRestoreContext                  — the lazy "is this boot a continue?" decision
  RunRestoreCoordinator              — first-ordered IInitializable driving RestoreAll
  AutosaveService                    — platform-entry savepoints + meta flush (D7)
  RunLifecycleService                — Defeat → meta flush → run-save delete (D8)
  MetaMemoryBootstrap / -FlushService — world memory load-at-boot / Meta-partition writer (D9)
Scripts/Narrative/Runtime/Snapshots/ — Actor/Quest/Casting snapshot mappers (id-replay)
Scripts/LevelGeneration/Area/WorldStatePersistenceBridge.cs — world section + allocator cursors
Scripts/CharacterSystem/Runtime/HeroBodyRestorer.cs         — hero body capture/staged re-apply
Scripts/Combat/Integration/CombatOutcomeRelay.cs            — per-fight outcome fan-in
Scripts/Core/DI/PersistenceInstaller.cs                     — store bindings (Area + MainMenu)
```

### 2.2 Core decisions

| Decision | Why |
|---|---|
| Two independent files under `persistentDataPath/Saves/` (`run.json`, `meta.json`) | FR13 independent recoverability; death deletes one, never the other |
| Two O1 one-shot carriers beside them (`run-setup.json`, `hub-arrival.json`), consume-on-read | the Hub must never write run.json (`RunRestoreContext.IsRestoring` keys off its existence); a lost setup only ever belonged to a run that does not exist yet, a lost arrival marker costs one voice line |
| JsonUtility, version field, temp-file + atomic swap | DTOs were shaped for it; parse-or-discard covers FR14; a crash mid-write never destroys the previous good file |
| Realized windows are **recorded at plan time** and rebuilt by id — never re-planned (D5) | windows are planned against live facts and realized with seeded draws; re-planning against end-state facts is not reproducible, and re-casting would consume RNG draws that already happened |
| Savepoint = platform **entry** + graceful exit | exit fires first (window lock + next-window planning), so at entry the RNG/allocator/ledger state is post-planning and the entered platform IS the clean start (FR6/7); the `Application.quitting` savepoint (dialogue-free moments only) keeps current-platform progress from falling back to the entry-time save |
| Consumed = per node, marked when its platform is **exited** (sticky) | the platform the hero stands on stays live (its encounter re-begins, FR7), unvisited platforms keep their content, and everything the hero walked away from never respawns (FR8) |
| Meta flush at savepoints + death + never write-through (D9) | the loss window is bounded by the run-save boundary and stays consistent with it |
| One explicit aggregator (`RunStateService`), not plugin sections | restore order matters (facts → actors → quests → stuff → body); explicit sequence is simpler to read and test |

### 2.3 Runtime flow

**Autosave (every platform entry):** `PlatformEvents.OnPlatformEntered` →
`AutosaveService.Save()` → `RunStateService.TryCaptureAll(dialogueState)` (refused while a
dialogue awaits an external step, W3-1) → `RunSaveStore.Save` + `IMetaMemoryFlush.Flush`.
The capture composes: the narrative snapshot (facts partitioned Run/Meta, RNG state, thread +
story ledgers — existing P2-3 boundary) + actors + quests + the world section (recorded windows,
streaming cursors, allocator cursors, current platform) + hero body + player stuff. A mid-staging
cauldron session is normalized at capture (staged items + uncollected result ride as plain
inventory items; the live session is never touched — A3). The very first savepoint is taken by the
Area entrypoint right after the world exists (skipped on a continue boot, where run.json is
already savepoint-true). `QuitSavepointHook` adds the graceful-exit savepoint on
`Application.quitting` via `AutosaveService.SaveGraceful()` — written only when no dialogue is
open, so a mid-conversation quit deliberately keeps the entry-time save (FR7 clean re-begin).
The dev starting-inventory and starting-blank seeds (`InventoryPresenter`, `BlankRackPresenter`)
are skipped on a continue boot — a restored stash is savepoint-true even when legitimately empty.

**Continue (Area boot with run.json present):** `RunRestoreContext` (lazy, first resolved by the
run-seed provider during container build) reads + validates the file once. `LootInstaller`'s seed
provider then seeds from `RunSaveSnapshot.RunSeed` — every derived stream re-derives identically.
`RunRestoreCoordinator` (execution order −200) replays `RestoreAll`: narrative restore → actors
(re-`Register` by id) → quest lifecycle replay (transition effects discarded — the facts are
already restored; the recorder bridge repopulates the progression record) → progression extras →
inventory/stash/rack + re-socketing through the live `TrySocket` flow → hero body staged.
`MetaMemoryBootstrap` (order −100) applies the world memory on top (the always-on file wins).
Then `AreaSceneEntrypoint.GenerateArea` calls `coordinator.BeginRestored(world)`: per recorded
window, `ApplyForWindow` (the biome journey is seeded + idempotent — it needs **no** persisted
state) then nodes map back by id with **zero** draws from the shared stream — consumed nodes
content-free with their original surface shape pinned (`GraphNode.ShapeKindOverride`), story nodes
re-cast via `CastingSnapshotMapper` (actor from the registry, fragments by id, the context bag
rebuilt by the factory's exact recipe), loot nodes re-roll their stateless per-node context.
The hero is placed on `CurrentPlatformNodeId`; `HeroBodyRestorer` re-applies the saved body once
the rig assembles (skipped when it equals the authored initial assembly).

**Death:** per-fight combat controllers fan into `ICombatOutcomeRelay` (wired by
`CombatControllerFactory`); on `Defeat`, `RunLifecycleService` flushes meta **then** deletes
run.json, **then (O1)** marks `hub-arrival.json` and loads the Hub — the death's front-end: the
junkyard reforms the player, still no game-over screen.

**Menu / Hub (O1):** `MainMenuPresenter` shows Continue iff `IRunSaveStore.Exists()`; Continue
loads Area (the file is the carrier); Journey loads the **Hub** without touching the save; the
Hub's launch writes `run-setup.json` (chosen part + biome + the Track Y Heat pact), deletes
run.json (R8's commit point), and loads Area. On the Area boot `RunStartConditions.Resolve`
consumes the setup one-shot (a restore wins and deletes a stale setup); the chosen biome then
rides `RunSaveSnapshot.StartingBiome` (**snapshot version 2**) on every savepoint, because the
biome journey replays from the seed and persists no cursor. The chosen part needs no extra
field — once installed it is part of the body snapshot. Arena touches nothing. See
`hub-staging.md`.

**Heat (Track Y):** the pact rides both carriers as **additive fields at unchanged versions**
(`RunSetupSnapshot.Heat` at v1, `RunSaveSnapshot.Heat` at v2 — a version bump would consume every
player's save, the `MetaMemorySnapshot` additive-field rule): a pre-Heat file deserializes to an
empty pact = Heat 0. `RunStateService.TryCaptureAll` stamps the pact on every savepoint (the
StartingBiome precedent); the total is never persisted — always recomputed from the authored menu
on load, so a re-authored menu can never leave a stale total. The **hottest cleared** total
persists as the Meta-horizon fact `world.heat_high_water`, written by `HeatHighWaterRecorder` — an
`ISavepointObserver` `AutosaveService` notifies after the run write and **before** the meta flush,
so the record rides that very flush and survives the death that consumes the pact. See
`heat-ascension.md`.

### 2.4 DI wiring

`PersistenceInstaller` (Zenject `Installer<>`, installed by `AreaInstaller` and
`MainMenuInstaller`) binds `ISaveSerializer`, `IRunSaveStore`, `IMetaMemoryStore` over
`persistentDataPath/Saves`. `AreaInstaller.InstallPersistenceBindings` adds the run-scoped stack:
`RunRestoreContext` (lazy FromMethod), `MetaMemoryBootstrap` (+`IMetaMemoryFlush`),
`IRunStateService`, `WorldStatePersistenceBridge`, `HeroBodyRestorer`, `AutosaveService`
(dialogue state fed as a delegate), `RunLifecycleService`, `RunRestoreCoordinator` with
`BindExecutionOrder` −200/−100. `ICombatOutcomeRelay` is bound with the combat controller
bindings. Scenes without the persistence stack (Arena, BodyPlanDemo) are unaffected: the seed
provider `TryResolve`s the restore context and the blank-rack presenter takes it `[InjectOptional]`.

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

This system owns **no SO type**. Its content surface is one field on the fact vocabulary's
existing `FactKeyDefinition` (see `narrative-procedural.md` for the full type):

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `Horizon` | `FactHorizon` | `Run` = the fact resets on death; `Meta` = the fact is carried by the cross-run memory (this system persists it in `meta.json`) | `Run` |

A key missing from the vocabulary partitions as Run — the conservative side: a mistagged fact is
lost on death rather than leaking across runs.

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### Make a fact persist across runs (a "world memory" fact)

1. Create the fact key: `Create → Narrative → Facts → Fact Key`; set Namespace/Scope/Key/ValueType
   as usual and **Horizon = Meta**.
2. Add the asset to the fact registry (`Resources/Narrative/Facts/DemoFactKeyRegistry`, `_keys`).
3. Set the fact in-fiction the normal way — a quest's `OnCompleteEffects`, a story effect, or an
   Ink `fact:` tag. Nothing else: the autosave flushes the Meta partition automatically.
4. Validate: set the fact in a run, die (or quit), start a new run — the fact is present in the
   DevTools state overlay (editor/dev-build).

The shipped example (FR11): `MetaFact_BarnBountyHonored` (`world.barn_bounty_honored`, Bool,
Horizon = Meta) + an `OnCompleteEffects` entry on `DemoQst_BarnBounty`. A second shipped meta fact
rides this seam since P3-1: `MetaFact_RunCount` (`world.run_count`, Int, Horizon = Meta) — the
number of runs started, 1-based; written once per **fresh** boot by `RunCounterService`
(`Scripts/Core/Persistence/`, execution order −90: after `MetaMemoryBootstrap` loads the memory,
before the entrypoint plans window 0). A continue never increments — it is the same run resuming.
The spine reveal lane's soft floors read it (`narrative-procedural.md` R15/D7).

Since P3-3 two more ride it: `MetaFact_SpineSeen` (`world.<storyId>.spine_seen`, Bool, PerStory,
Horizon = Meta) — the **cross-run spine cursor**, written only by `SpineSeenRecorder`
(`Narrative.Runtime.Core`) when a spine reveal-beat's dialogue ends, read back by the reserved
lane so a **seen** reveal never repeats across deaths; and `MetaFact_RaiderPactSworn`
(`world.raider_pact_sworn`, Bool, Horizon = Meta) — a demo landmark deed the mirror-lore echoes
read (`narrative-procedural.md` §4 "Author a mirror-lore echo").

Since P4-5 (the arena parts draft): `MetaFact_ArenaTasted` (`world.<partId>.arena_tasted`, Bool,
PerPart, Horizon = Meta) — the **tasted-forms catalog**: every part the hero has carried
(equipped or dormant), written only by `TastedFormsRecorder` (`CharacterSystem.Integration`,
Area-bound) as a passive side effect of play, read only by the Arena scene
(`ArenaTastedCatalogReader` loads `meta.json` directly — the Arena installs `PersistenceInstaller`
but not the narrative fact-store bootstrap) to widen the shared draft board (`arena-mode.md` §2.9).

**Authoring constraints / gotchas:** the horizon lives on the KEY, not the write — every write to a
Meta key persists. Per-actor Meta facts persist by subject id, but run 2 mints different actor
instance ids; cross-run per-actor memory only works for stable ids — deliberately out of scope for
the P3-3 consumers (all their meta flags are world-scoped); it becomes relevant only if a future
system wants per-actor memory across deaths.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `PersistenceStoreTests` — file round-trips against temp dirs, `ulong` RNG-state JSON fidelity,
  corrupt/version-mismatch discard (run) vs quarantine (meta), atomic overwrite, delete-consumes,
  file independence (FR13), the run-1 → run-2 meta fact end-to-end, flush purity.
- `RunStateServiceTests` — whole-aggregate round trip across fresh service instances (facts,
  threads, actors + per-actor facts, quest stages + recorder bridge + reward guard, inventory,
  rack + re-socketing, progression extras, post-restore RNG continuation), id-counter continuity,
  crafting normalization without touching the live session, W3-1 aggregate guard, casting
  restore-by-id with the canonical context bag + missing-fragment degradation.
- `WorldRestoreTests` — allocator cursors (quest spacing, pending site blocks, site instance ids)
  round-trip so the allocation stream continues identically (FR12); bridge cursor rehydration.
- `HubPersistenceTests` (O1) — run-setup round-trip/consume/corrupt/foreign-version; arrival
  marker mark → consume once; `RunStartConditions.Resolve` (restore wins + stale setup deleted,
  fresh consume-on-read, empty defaults, garbage biome names rejected).
- `RunLifecycleTests` — Defeat flushes meta **before** consuming the run save, then (O1) marks the
  arrival and loads the Hub in that order (null loader/marker tolerated); Victory touches/loads
  nothing; autosave writes run + meta and skips W3-1 non-savepoints; the graceful-quit save writes
  only when no dialogue is open. `RunStateServiceTests` additionally cover the O1 StartingBiome
  stamp (with and without conditions).
- `MainMenuPresenterTests` — Continue visibility/routing, Journey → Hub **without** deleting the
  save (the delete moved to the Hub launch, O1), Arena untouched.
- `SpineCursorPersistenceTests` (P3-3) — the spine cursor across the file boundary: flushed on the
  defeat path, restored with its per-story subject intact, run facts stay behind; a corrupt memory
  quarantines and the cursor degrades to empty.

Verified manually in play mode (the user's final check): quit mid-run → Continue resumes the same
world/body/stuff/quests; death removes Continue and keeps the meta fact; corrupting either file
degrades per FR13; `BeginRestored` rebuilds identical platforms.

---

## 6. Known limitations / open points

- **Currency is not persisted — no currency model exists anywhere yet** (the brief lists it in
  FR4). When a wallet model lands, it gets a `PlayerStuffSnapshot` section. (ROADMAP)
- **A platform is "consumed" when exited, whole**: un-picked loot on a platform the player walked
  away from is lost on resume (nothing respawns; consistent with FR7/8's "encounter re-begins
  clean"), and a resolved encounter on the *current* (never-exited) platform re-begins on resume —
  including a picked-but-not-left loot find, which would re-spawn its pickup (the FR7 gray zone).
- **Suspended dialogues are non-savepoints (W3-1 option a)**: the Ink `Sessions` DTO field exists
  but is unpopulated; serializing an in-progress dialogue (option b) is deferred. (ROADMAP)
- **`UnityEngine.Random` is reset to the run seed on every Area boot** (`AreaSceneEntrypoint`),
  so its stream position is not resumed — all gameplay determinism rides `DeterministicRandom`
  /`LootSeed` streams; treat `UnityEngine.Random` as cosmetic-only in the Area scene.
- **Static `PlatformEvents` seam** — the autosave subscribes the existing static event bus, the
  same pattern the streaming coordinator uses; migrating to Zenject signals is tracked debt.
  (ROADMAP)
- **Local persistence only** — no cloud/cross-device sync (out of scope by the brief).
