# NPC Proximity Interaction — Requirements & Design

> Walking up to an NPC is now a deliberate act: a talkable NPC shows an **F** prompt in range and opens
> its conversation only when the player presses **F**, while a hostile NPC starts its battle on its own as
> the player crosses an aggro radius. A **camp boss** has a noticeably larger engagement circle that
> auto-engages on cross (talk if he carries a job, fight if not), and **all** engagement is scoped to the
> player's current platform. Floating `?` / `!` markers and a name label sit above each NPC, and three
> global radii drive everything with a dev overlay for tuning.
> Status: current as of 2026-07-05 (bandit-camp brief: boss engagement + platform scoping).
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6. The product-owner brief is
> `product-requirements/npc-proximity-interaction.md`.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** NPC intent is **derived from the placement-time casting + story**, never an authored flag: a
  filled quest slot → **quest-bearer**; else a **required (non-optional) combat slot** → **hostile**;
  else → **plain**. An *optional* combat slot is a dialogue branch (fight-or-talk), so the NPC stays
  talkable (plain) and the fight is reached through the conversation, not by approach.
- **R2** Landing on / standing on a platform **never** starts a dialogue or a battle — for **any**
  content, ambient monsters included (PO decision 2026-07-05). Combat is entered by engagement only;
  an engaged platform latches `EnemyContent.Engaged` so re-landing resumes the begun battle
  (`CombatAutoStartRule`).
- **R3** A quest-bearer or plain NPC shows an **F prompt** while the player is inside the interaction
  radius; pressing **F** opens its conversation (the existing encounter flow, unchanged).
- **R4** If several eligible NPCs are in range, the prompt targets the **nearest** one.
- **R5** A hostile NPC starts its **battle immediately** when the player crosses its aggro radius — no
  prompt, no dialogue. Hostile NPCs are not talkable.
- **R6** Markers are **always visible** (not distance-gated) and billboard to the camera: `?` for a
  quest-bearer, `!` for a hostile NPC, none for a plain NPC. A started encounter clears the marker.
- **R7** Each NPC shows its **name label** above the model, always facing the camera.
- **R8** The interaction radius and aggro radius are **global config values** (one SO), applied to all
  NPCs. A general per-NPC override surface is out of scope; the single carve-out is R10.
- **R9** A toggleable **dev overlay** draws the radii as distinct on-ground circles in play mode.
- **R10** A **camp boss** (`NpcContent.IsCampBoss`, placed by the boss-led camp path — `world-sites.md`)
  uses the larger **boss engagement radius** for **both** intents, and crossing it **auto-engages**: a
  hostile boss starts the camp fight, a quest-bearer/plain boss **opens his dialogue by itself** (no F
  press — his circle is the encounter trigger, not a prompt offer). He never competes for the F prompt.
- **R11** Engagement is **platform-scoped for every NPC**: an NPC only prompts / aggros / auto-talks while
  the player stands on **its own platform**. Unknown platforms (either side) fail safe as eligible.
- **R12** Starting an encounter (talk or aggro) latches `NpcContent.EncounterStarted`; the fight itself
  latches `EnemyContent.Engaged` on every enemy of the platform (`CombatAutoStartRule`, see
  `world-sites.md`).
- **R13** A **lone enemy body** (an ambient monster platform with no NPC) registers a **hostile
  proximity handle** of its own when spawned (`INpcInteractionService.BindEnemy` from
  `CombatIdleState`): name + `!` overhead, the global aggro radius, platform-scoped. Crossing it
  engages the **whole platform's** fight (all sibling enemy handles are consumed — one trigger, one
  battle). A **camp crew behind a boss is never bound** — the boss's circle owns the trigger.

### 1.2 Non-functional requirements

- **N1** Intent classification and proximity geometry are pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject; no service locators.
- **N3** The radius config is a data-only ScriptableObject with a code-default fallback.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Narrative/Interaction/
  Core/   — NpcIntent, NpcIntentResolver, ProximityEvaluator, PlanarPoint, NpcInteractionSettings (pure C#)
  Data/   — NpcInteractionConfig (SO) + NpcInteractionConfigMapper
  (root)  — NpcProximityPresenter (ITickable), NpcEncounterStarter, registry, handle, service, IInteractionInput
  View/   — NpcOverheadView, NpcInteractionInput, NpcRadiusDebugView (MonoBehaviour adapters)
Scripts/Core/DI/NpcInteractionInstaller.cs
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `NpcIntent` | `Plain` / `QuestBearer` / `Hostile`. |
| `NpcIntentResolver` | `(Casting, Story) → NpcIntent`: quest wins, else a **required** combat slot ⇒ hostile, else plain (an optional combat slot is a dialogue branch). |
| `ProximityEvaluator` | Pure XZ geometry over `NpcProximitySample`s → nearest prompt NPC + aggro ids + boss auto-talk ids. Off-platform samples are ineligible; boss samples use the boss radius for both intents. |
| `PlanarPoint` | UnityEngine-free ground-plane (x,z) point with squared distance. |
| `NpcInteractionSettings` | The three global radii (interaction / aggro / boss engagement), mapped from the SO. |

### 2.3 Runtime flow

1. **Placement (intent fixed).** `RunStreamingCoordinator.MapWindow` casts the planned story against the
   **live facts** when the window is generated (`ICastingFactory.Cast`) and resolves the intent. Both the
   `Casting` and `NpcIntent` are carried on `NpcContent`. Casting happens **once** here and is reused by
   the encounter, so the seeded fragment/name picks stay deterministic and the marker matches what plays.
2. **Spawn + bind.** `Platform.Initialize` (at generation) calls `NpcContent.Initialize`, which spawns the
   modular body and calls `INpcInteractionService.Bind`: it creates an `NpcOverheadView` above the head
   (name + marker, prompt hidden) and registers an `NpcInteractionHandle` in the `INpcInteractionRegistry`.
   Because this runs at generation, markers/names are visible from across the room.
3. **Per-frame (`NpcProximityPresenter.Tick`).** Samples the player position (`ICharacterRegistry`) and the
   registered NPCs — each sample carries `IsBoss` (from `NpcContent.IsCampBoss`) and `OnPlayerPlatform`
   (the presenter tracks the player's current platform via `PlatformEvents.OnPlatformEntered`; the
   presenter is `IDisposable` and unsubscribes) — asks `ProximityEvaluator`, then in order: auto-aggros
   any hostile in range; **auto-opens** any crossed talkable boss's dialogue; shows the F prompt on the
   single nearest eligible non-boss NPC; and, on F press for that nearest NPC, starts the talk.
4. **Start (`NpcEncounterStarter`).** *Talk* transitions the NPC's platform into `DialogueActiveState`,
   which reuses `NpcContent.Casting` (`DialogueRunner.Begin`). *Aggro* on an NPC handle spawns an
   `EnemyContent` from the cast `OptionalEnemyId` (Engaged), latches every enemy on the platform,
   destroys the NPC body, and transitions to combat — the same path `DialogueActiveState` uses for an
   in-dialogue combat trigger, minus the dialogue. *Aggro* on an **enemy-body** handle (R13) latches
   every `EnemyContent`, consumes + clears every sibling enemy handle, and transitions to combat.
   Either way the handle is consumed (no re-trigger) and its marker clears.

`PlatformStateFactory` no longer maps NPC content to a dialogue state on land, and enemy content no
longer auto-starts combat on land (R2): activation enters combat only for an already-engaged platform
(`CombatAutoStartRule` over `EnemyContent.Engaged`). It exposes `CreateDialogueState()` for the
on-demand F path and the explicit `CreateActiveState(platform, ContentType.Enemy)` for engagement.
The legacy `ContentSpawner` prefab path for Enemy/Npc content (the lingering **capsule** source,
double-spawning over the real humanoid paths) is retired.

### 2.4 DI wiring

`NpcInteractionInstaller` (installed by `AreaInstaller`) binds: `NpcInteractionSettings` (from
`Resources/Narrative/NpcInteractionConfig`, code defaults if absent), `NpcIntentResolver`,
`ProximityEvaluator`, `NpcEncounterStarter`, `INpcInteractionRegistry`, `INpcInteractionService`,
`IInteractionInput` (a new-GameObject component), and `NpcProximityPresenter` as an `ITickable`. The
`NpcRadiusDebugView` is bound only under `UNITY_EDITOR || DEVELOPMENT_BUILD`.

---

## 3. ScriptableObject Reference

### `NpcInteractionConfig`  (asset menu: `Create → Narrative → NPC Interaction Config`)

Loaded from `Resources/Narrative/NpcInteractionConfig`. If absent, `NpcInteractionConfigMapper` supplies
the defaults below.

| Field | Type | Meaning | Default |
|---|---|---|---|
| `_interactionRadius` | float | F-prompt range for talkable NPCs (world units) | `3.5` |
| `_aggroRadius` | float | Auto-battle range for hostile NPCs (world units) | `2.5` |
| `_bossEngagementRadius` | float | A camp boss's engagement circle (both intents; auto-engage on cross) | `6` |

References no other assets.

---

## 4. Adding Content

The system itself exposes **no new authored type** beyond the single global `NpcInteractionConfig`. NPC
intent is produced by the **narrative content shape**, so the recipes are about authoring facts/stories.

### Make an NPC read as hostile (auto-battle on approach)

1. Author (or reuse) a story whose eligible variant under the relevant facts fills a **required
   (non-optional) combat slot** (an enemy fragment matches its tag) and fills **no quest slot**. The
   *required* slot is what makes the fight unavoidable — that is the signal for approach-aggro. The
   dialogue slot must still be filled (casting requires it), but it is never shown for a hostile NPC —
   the aggro path spawns the enemy directly. No such story ships in the demo today (see §6).
2. Ensure a **sibling variant** offers a quest under friendlier facts (e.g. `DemoStory_BarnVictim`, quest
   tag `bounty`). When that variant is the eligible one, the same character reads as a quest-bearer (`?`)
   and is talkable instead — intent is derived, not fixed.

> **Talkable raider vs. hostile raider.** `DemoStory_BarnRaid` is a *talkable* encounter: its combat slot
> is **optional**, because the raider's Ink offers a choice ("drop the grain" → fight, or "take a cut" →
> bribe). It therefore reads as **plain** (F to talk), not hostile. To get an auto-aggro raider instead,
> mark the combat slot **non-optional** (and remove the non-combat dialogue branch) — but note the
> streaming-planner caveat in §6 before adding an always-eligible combat story.

### Make a quest-bearer / plain NPC

- **Quest-bearer (`?`):** the eligible story fills a **quest slot** (a quest fragment matches its tag).
- **Plain (no marker):** the eligible story fills **neither** a quest nor a combat slot — just dialogue.

### Make a camp boss (larger reach, gates the camp fight)

The boss is not authored here: he is placed by the **boss-led camp** path (`world-sites.md` — a site
whose `_bossStoryFlavor` is set). His `NpcContent.IsCampBoss` flag gives him the boss engagement radius
and the auto-engage-on-cross behaviour, and holds the platform's fight until he is engaged.

### Tune the radii

1. Edit `Resources/Narrative/NpcInteractionConfig` (`_interactionRadius`, `_aggroRadius`,
   `_bossEngagementRadius`), **or**
2. In play mode, press **F2** for the dev overlay and adjust by eye; the on-screen circles follow the
   config values (boss rings draw in a third colour at the boss radius).

**Authoring constraints:** intent is evaluated from the facts **as of when the encounter is placed**. A
story is hostile only when it fills a **required** combat slot and **no** quest slot; an optional combat
slot keeps the NPC talkable (the fight is a dialogue branch).

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `NpcIntentResolverTests` — quest ⇒ quest-bearer; no-quest+enemy ⇒ hostile; neither ⇒ plain; quest+enemy
  ⇒ quest-bearer (quest wins); null casting ⇒ plain.
- `ProximityEvaluatorTests` — nearest-eligible prompt selection, interaction vs aggro thresholds, hostile
  never prompts, consumed NPCs ignored; boss engages at the boss radius (both intents), quest-bearer boss
  auto-talks instead of prompting, consumed boss never re-engages; off-platform NPCs ineligible at any
  distance.
- `CombatAutoStartRuleTests` — the boss-gated landing rule (enemy-only auto-fights; an un-engaged NPC
  holds the fight; an engaged one releases it).

Verified manually in play mode (per the brief's acceptance criteria): markers/name billboards, F-prompt
talk, aggro-on-approach, and the F2 radius overlay.

---

## 6. Known limitations / open points

- The `?` clears after the NPC's encounter is started (consumed), not by live re-evaluation of quest
  availability against the fact store. Full per-frame fact-driven marker refresh is a ROADMAP item.
- Radii are still **global values**; the camp boss's larger reach is the single role carve-out
  (`_bossEngagementRadius`). A **general** per-archetype / per-NPC radius override surface stays open
  (ROADMAP).
- Hostility is read from the combat slot's **optionality**, not from in-dialogue offers: a story that
  offers a quest only via an in-dialogue `#offer-quest` tag (no quest *slot*) and forces combat via a
  **required** combat slot would still read as hostile. Author the quest as a slot, or keep the combat
  slot optional, to stay talkable.
- The forced-combat subject now exists: the **hostile camp boss** (`DemoStory_CampBossHostile`, a
  required combat slot). Because boss stories are **ambient colour** (matched by the site catalog's boss
  flavors), they never enter the quest channel and so do **not** perturb the fact-ordered narrative
  threads the way an always-eligible quest-channel combat story would.
- After a **peaceful boss talk** (job declined or accepted without a fight) the boss's handle is consumed:
  the camp stands, its crew idles, and the fight can no longer be started (PO-accepted for the demo;
  boss re-engagement is a ROADMAP item).
- The overhead view is built from 3D `TextMeshPro` in code (no art polish/animation); marker art is a
  plain glyph.
