# Hub Staging (The Junkyard) — Requirements & Design

> The pre-run staging ground (Track O · O1): the main menu's **Journey** leads to the **Hub
> scene** — a single **walkable hex platform** (a regular platform in the Hub's own biome look)
> where the hero walks up to the **junk-keeper NPC** (F → the starting-part cards drawn from the
> tasted-forms catalog) and to one of three **labelled portals** (F → launch into that homeland),
> with the cauldron's commentary throughout; **death returns here** (the Hades reform point — no
> game-over screen). Realizes the MVP of the canonical "direction + floor, not a vending machine"
> dig (`design/narrative/hub-junkyard.md`).
> Status: current as of 2026-07-06 (walkable-platform rework, same day).
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.
>
> Product brief: `product-requirements/hub-staging-and-launch.md`, consumed with the 2026-07-06
> owner revisions: the starting offer is **drawn from the tasted pool** (not 3 fixed authored
> organs), **bare launch is always allowed** (cold start = empty pool), and all three homelands
> are authored at **tier 1** (the entry pool) while keeping the tier-2 climb pool.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** The main menu's Journey enters the Hub scene; the run launches **from** the Hub. Continue
  still resumes straight into the Area; Arena is unaffected.
- **R2** The **new-run commit point is the Hub's portal launch**, not the menu click (revises the
  P2-2 PO note): entering the Hub touches nothing; using a portal writes the one-shot
  `run-setup.json` and deletes any abandoned `run.json`. Backing out of the Hub keeps Continue
  alive. A second launch is guarded (one commit per visit).
- **R2a** **The Hub is a walkable world**: one regular hex platform (the shared shape profile,
  fixed seed, perimeter walls) with the Hub's own ground material (`HubSceneConfig`); the scene
  hero free-walks it with the standard movement controller (WASD + dash).
- **R3** The Hub deals a **starting-part offer of up to 3 cards** from the pool of **tasted**
  parts (`world.<partId>.arena_tasted`, meta-persistent) that fit the base skeleton
  (`!GovernsBodyPlan`). The draw is deterministic per (pool, upcoming-run index) and
  variety-greedy: unseen race > unseen slot > flavored (race-tagged or ability-bearing) > plain.
- **R4** Picking a card is **optional** — launching bare is always allowed; an empty pool (cold
  start, nothing tasted yet) is the same bare-launch state, not an error.
- **R5** The offer is presented on the **shared mutation card panel** (`MutationChoicePanel`
  prefab / `IMutationChoiceView`) with card faces from the part catalog, race-belonging tints,
  the mini-model preview and the ability-preview popover — **dealt by TALKING to the junk-keeper
  NPC** (a figure slightly left of the platform centre with an overhead name + `[F] Talk`
  prompt). Talking again re-opens the deal, so the pick can be revised until launch. No back
  face (a launch installs into a freshly reformed body).
- **R6** The player picks the **entry homeland** by walking into one of three **portals** —
  small labelled interaction discs on the platform's far arc, one per roster home biome
  (Fox→Forest, Lizard→Desert, Ibex→Mountain) — and pressing **F**; the F prompt is the biome's
  name. Independent of the part's race (cross allowed); walking into a portal bare launches bare.
- **R7** The launch starts the run in the chosen biome: `BiomeJourney` forces **only the window-0
  stretch** to the chosen theme (burn-the-draw — the entry stretch keeps its seeded length), and
  the climb proceeds via the shipped seeded pool, fully deterministic per (seed, override). The
  chosen biome rides `RunSaveSnapshot.StartingBiome` (v2) so a resume replays the same journey.
- **R8** The chosen part is installed on the hero at run start (`StartingPartApplier`, fresh runs
  only, once, after `CharacterAssembled`); a race-tagged part makes the hero a **1-marker
  tolerated freak** via the unchanged passport projection. The part then rides `HeroBodySnapshot`
  (run-scoped — lost on death).
- **R9** The **cauldron voice** comments on the staging moments (part pick per race / empty offer
  / launch / death return) with **data-authored** lines (`HubVoiceLinesConfig`), picked
  deterministically (FNV-1a over moment+race+run index — no RNG stream is touched). Empty pools
  are a quiet cauldron, never an error.
- **R10** **Death returns the player to the Hub**: after the shipped meta-flush + run consume,
  `RunLifecycleService` marks `hub-arrival.json` and loads the Hub; the voice greets the return.
  No game-over screen.

### 1.2 Non-functional requirements

- **N1** Core logic (selector, model, voice selection) is pure C# and unit-tested.
- **N2** All dependencies wired through Zenject (`HubInstaller` + `CharacterSystemInstaller` on
  the Hub SceneContext); no service locators.
- **N3** The scene hand-offs cross via disk (the D3 convention): `run-setup.json` (Hub → Area,
  consume-on-read) and `hub-arrival.json` (Area → Hub, consume-on-read). No cross-scene container.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Hub/
  Core/       — StartingPartCandidate, StartingPartSelector, HubStagingModel, HubHomeland,
                HubInteractionSpot + HubProximity (nearest-in-radius), IStartingPartPoolSource,
                CauldronVoiceMoment/Lines/Selector (pure C#)
  Data/       — HubVoiceLinesConfig + HubSceneConfig (SOs) + HubVoiceLinesMapper (SO -> Core),
                HubStartingPoolSource, RaceTintCatalog/IRaceTintCatalog, HubMetaReader
  Presenter/  — HubStagingPresenter, CauldronVoicePresenter, HubProximityPresenter (pure C#)
  View/       — HubPlatformBuilder (standalone hex platform, the ArenaPlatformBuilder treatment),
                HubSceneEntrypoint (world assembly: platform + keeper + portals),
                HubOverheadLabelView/IHubPromptView (billboard name + F prompt),
                HubStagingView (chosen-part readout), HubVoicePlaqueView
Scripts/Core/DI/HubInstaller.cs
Scripts/Core/Persistence/   — RunSetupSnapshot/Store, HubArrivalSnapshot/Store, RunStartConditions
Scripts/CharacterSystem/Integration/StartingPartApplier.cs   (Area side)
Scenes/Hub.unity
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `StartingPartCandidate` | One poolable part: id, race tag (empty = kindless), slot, has-active-ability |
| `StartingPartSelector` | The deterministic variety-greedy draw of N cards from the pool (R3) |
| `HubStagingModel` | Staging state: dealt offer, chosen part (null = bare), chosen biome, launch signal |
| `IStartingPartPoolSource` | Core port for "what can be offered"; Data implements it over the tasted catalog |
| `CauldronVoiceLines` / `CauldronVoiceSelector` | Moment-keyed line pools (race sub-pools with generic fallback) + the stable-hash pick (R9) |
| `RunStartConditions` (Core.Persistence) | The one Area-scoped answer to "how does this run start" — restore wins, fresh consumes the setup file, nothing = defaults |

### 2.3 Runtime flow

1. **Menu → Hub.** `MainMenuPresenter.HandleJourneyClicked` loads `SceneNames.Hub` (no delete).
2. **Hub boot.** `HubStagingPresenter.Initialize` (execution order −10): builds the homeland list
   from `IRaceRoster` (fallback: the three starting themes when the roster is empty), reads
   `world.run_count` from the meta snapshot (`HubMetaReader`, the Arena-precedent direct read),
   and deals `StartingPartSelector.Draw(pool, 3, runCount + 1)` over
   `HubStartingPoolSource.BuildPool()` (tasted ∩ catalog ∩ base-skeleton) into the model — the
   card panel stays hidden until the keeper is talked to. `HubSceneEntrypoint.Initialize`
   (order −5) then assembles the world: `HubPlatformBuilder.Build()` (the shared shape profile
   from the config's fixed seed; mesh + walkable colliders + perimeter walls; the ground material
   from `HubSceneConfig`), the keeper NPC (the placeholder humanoid assembly, slightly left of
   the platform's centre cell) and one portal disc per homeland on the far arc, each with a
   billboarded overhead name + F prompt — all registered as F-spots on `HubProximityPresenter`.
   `CauldronVoicePresenter.Initialize` finally consumes the death-return marker (greeting) or
   speaks the empty-offer line. The scene hero simply walks (the prefab's own move/dash actions;
   `PlatformRegistry` absence is a tolerated no-op).
3. **Walk-up interactions.** `HubProximityPresenter.Tick` samples the player (via
   `ICharacterRegistry`) and asks the pure `HubProximity.FindNearest` for the in-range nearest
   spot; that spot's prompt shows; **F** (`IInteractionInput`, the NPC-interaction key) fires the
   spot: the keeper → `ShowOffer()` (re-openable card deal; pick → readout label + race-keyed
   voice line), a portal → `LaunchInto(theme)`.
4. **Launch (commit, once).** `LaunchInto`: `ChooseBiome` →
   `RunSetupStore.Save({StartingPartId, StartingBiome})` → `IRunSaveStore.Delete()` → launch
   voice line → `Load(SceneNames.Area)`; re-entry is guarded.
5. **Area boot.** `RunStartConditions.Resolve(RunRestoreContext, IRunSetupStore)` (lazy, bound in
   `AreaInstaller`): restoring → biome from `RunSaveSnapshot.StartingBiome` (stale setup deleted);
   fresh → setup consumed on read; nothing → `Empty` (direct editor play keeps working). The
   `IBiomeJourney` binding threads `TryGetStartingTheme` into the `BiomeJourney` ctor;
   `StartingPartApplier` (fresh-only) installs the part after `CharacterAssembled` via
   `SwapPart` — `PartsChanged` then drives the passport and the tasted recorder for free.
   `RunStateService.TryCaptureAll` stamps `StartingBiome` into every savepoint.
6. **Death.** `RunLifecycleService` on Defeat: meta flush → run delete →
   `IHubArrivalStore.MarkDeathReturn()` → `Load(SceneNames.Hub)`.

### 2.4 DI wiring

`Scenes/Hub.unity` SceneContext carries **`HubInstaller` + `CharacterSystemInstaller`** (the
Area/Arena precedent; the character installer provides `IPartCatalog`, the factory — which also
builds the keeper's body — and binds the scene's Hero prefab instance, which both walks the
platform and seeds the card previews). `HubInstaller` installs Logging + Persistence (all four
stores), `ISceneLoader`, `ICharacterRegistry` (hero prefab dep), the card stack
(`IPartAbilityResolver`, `MutationPartCatalog`, `MutationConfig` from
`Resources/Mutation/MutationConfig`, `MutationModelPreviewRig`, `AbilityPreviewInstaller`, the
`MutationChoicePanel` prefab → `IMutationChoiceView` with a missing-prefab guard), the staging
domain (tasted reader, pool source, selector, `HubMetaReader`, `IRaceRoster` +
`IRaceTintCatalog` from `Resources/World/Races`), the voice
(`Resources/Hub/HubVoiceLines` → mapper → `CauldronVoiceLines`), and the **world**
(`PlatformShapeSettings` from `Resources/LevelGeneration/PlatformShapeConfig`, `HubSceneConfig`
from `Resources/Hub/HubSceneConfig` — missing = code defaults, `HubPlatformBuilder`,
`IInteractionInput` → the NPC-interaction F key, `HubProximityPresenter` (ITickable), and
`HubSceneEntrypoint` pinned between the staging presenter and default order). The scene itself
carries only the camera, light, EventSystem, canvas (title + voice plaque + chosen-part
readout), the Hero prefab, and the SceneContext — the platform, keeper, and portals are built
at boot.

Deliberately absent: `MutationInstaller` (its blank rack drags the inventory fusion graph),
narrative slice, combat, `MetaMemoryBootstrap` (the Hub reads meta.json snapshots directly).

**Persistence files** (all under `persistentDataPath/Saves`, all versioned/atomic via
`JsonSaveFile`, corrupt policy Delete): `run-setup.json` (the launch hand-off, one-shot),
`hub-arrival.json` (the death-return marker, one-shot). `RunSaveSnapshot` is **version 2**
(added `StartingBiome`; v1 files are the FR14 foreign-version case — discarded, fresh run).

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

### `HubVoiceLinesConfig`  (asset menu: `Create → Hub → Cauldron Voice Lines`)

Loaded from `Resources/Hub/HubVoiceLines` (or wired into the `HubInstaller._voiceLines` field).
The single authored instance: `Resources/Hub/HubVoiceLines.asset`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_partPickedByRace` | `List<RaceLinePool>` | Per-race pools for picking a race-tagged organ | race pool empty → generic fallback |
| `RaceLinePool._raceId` | string | Must match a `RaceDefinition.RaceId` (e.g. `fox`) | empty = pool dropped |
| `RaceLinePool._lines` | `List<string>` | The lines; one is picked deterministically | blank lines dropped |
| `_partPickedGeneric` | `List<string>` | Fallback for any part pick (incl. kindless parts) | |
| `_noPartAvailable` | `List<string>` | Spoken on an empty offer (the first, bare launch) | |
| `_launch` | `List<string>` | Spoken at the launch/descent | |
| `_deathReturn` | `List<string>` | Spoken on a death return (consumes the arrival marker) | |

No referenced assets. An entirely empty asset (or a missing one) is a valid quiet cauldron.

### `HubSceneConfig`  (asset menu: `Create → Hub → Scene Config`)

Loaded from `Resources/Hub/HubSceneConfig` (or wired into the `HubInstaller._sceneConfig` field);
missing = code defaults. The single authored instance: `Resources/Hub/HubSceneConfig.asset`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_platformMaterial` | `Material` | The Hub platform's ground look — its "own biome" texture | empty = code-fallback junkyard tint |
| `_platformSeed` | int | Deterministic hex-shape seed (the Hub always looks the same) | 777 |
| `_npcDisplayName` | string | The junk-keeper's overhead name | "Junk Keeper" |
| `_npcInteractRadius` | float | The keeper's F-interaction circle (world units) | 2.5 |
| `_portalInteractRadius` | float | Each portal's F-interaction circle — deliberately small | 2 |

Referenced assets: the ground `Material` (the designer's texture swap point — the platform mesh
itself is the shared hex pipeline).

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### Reskin the Hub platform / retune the Hub world

1. Author a `Material` for the ground (the junkyard texture) and assign it to
   `Resources/Hub/HubSceneConfig.asset` → `_platformMaterial`.
2. In the same asset: retune the platform seed (a different island shape), the keeper's name,
   and the two interaction radii. No code changes.

### Add or retune cauldron-voice lines

1. Open `Resources/Hub/HubVoiceLines.asset` (or `Create → Hub → Cauldron Voice Lines` and wire it
   into the Hub scene's `HubInstaller`).
2. Edit the pools per §3 — add/remove/reword lines freely; add a new race pool by adding a
   `RaceLinePool` entry whose `_raceId` matches the `RaceDefinition`.
3. Enter the Hub in play mode and pick/launch to hear the moments. No code changes.

### Make a part offerable on the Hub

The offer is **derived**, not authored: any `PartDefinition` that (a) the hero has ever carried
(the tasted catalog writes itself), and (b) does not govern a body plan (`_governsBodyPlan` off)
enters the pool automatically. To make a part *attractive* in the offer, author its card surface:
`_displayName`, `_choiceIcon`, `_activeAbilities` (the visible opening move), and `_raceId` (the
direction lean + belonging tint). A part with neither race nor active ability is offered only
when nothing flavored remains.

**Authoring constraints / gotchas:** frame-changers (serpent spine, spider legs) never appear —
the launch body is the base skeleton. The authored initial body's own parts become tasted after
the first Area entry, so plain base parts enter the pool from run 2 on (the selector ranks
flavored candidates above them).

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `StartingPartSelectorTests` — draw determinism, pool-order independence, race/slot variety,
  flavored-over-plain ranking, small/empty/invalid pools.
- `HubPersistenceTests` — run-setup round-trip/consume/corrupt/foreign-version, arrival marker
  mark-consume-once, `RunStartConditions.Resolve` (restore wins + stale-setup delete,
  consume-on-read, empty defaults, garbage theme names rejected).
- `CauldronVoiceTests` — deterministic pick, salt cycling, race pool + generic fallback, quiet
  empties, per-moment pools.
- `HubStagingPresenterTests` — offer dealt at init but panel hidden until `ShowOffer` (the keeper
  talk), tints, pick/readout/hide + revisable pick, homelands from the roster (+ empty-roster
  fallback), `LaunchInto` side-effects (setup saved, old run consumed, Area loaded), bare launch,
  double-launch guard, empty pool no-op, missing panel tolerated, dispose.
- `HubProximityTests` — nearest in-range F-spot wins, out-of-range = none, inclusive radius
  boundary, per-spot radii, planar-only distance, null tolerance.
- `CauldronVoicePresenterTests` — death greeting consumes the marker and wins over the empty-offer
  line, pick/launch lines, quiet on empty content, dispose.
- `StartingPartApplierTests` — fresh-run-only decision, one swap, unknown/empty ids degrade to a
  bare launch.
- Extended: `BiomeJourneyTests` (window-0 override + burn-the-draw stream property),
  `BiomeProgressionConfigMapperTests` ((theme, tier) dedupe), `RunStateServiceTests` (biome
  stamp), `RunLifecycleTests` (flush → delete → mark → load Hub), `MainMenuPresenterTests`
  (Journey → Hub, no delete).

Verified manually (play mode): the Hub scene look (placeholder), the card panel over the scene
hero, the full menu → Hub → launch → death → Hub loop.

---

## 6. Known limitations / open points

- **No hub meta-progression, no recurring cast** — MVP is staging + return only (brief scope).
- **Placeholder scene dressing** — the platform ground is a code-fallback tint until a junkyard
  material is authored (`HubSceneConfig._platformMaterial`); the portals are flat tinted discs;
  the keeper is the placeholder humanoid assembly. The junkyard art pass is deferred (Track M).
- **Movement is not locked while the card panel is open** — the hero can walk with the cards up;
  harmless today, an `IMovementInputLock` hookup is a polish item.
- **The camera is static** — fine for one platform; no follow rig.
- **The launch voice line is effectively unseen** (the scene loads immediately); a short
  linger/fade is a ROADMAP polish item.
- **No match/cross soft hint** — the brief's optional "at-home vs marked-outsider" hint on the
  homeland pick is deferred.
- **Single commit-on-launch pick** — no re-pick of the chosen part before launch (brief: a tuning
  decision, not structural).
- **Base-body parts enter the pool from run 2 on** (they are honestly "tasted"); acceptable, the
  selector ranks flavored parts above them.
- **`ArenaTastedCatalogReader` is consumed cross-system** (Arena → Hub); relocating it to a
  shared tasted-catalog home is ROADMAP debt.
- **Voice audio** — deferred (text plaque only, per brief).
