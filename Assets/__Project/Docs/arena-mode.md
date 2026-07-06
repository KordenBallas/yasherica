# Arena Mode (Multiplayer) — Requirements & Design

> The second game mode: from a boot main menu the player picks **Journey** (the unchanged
> single-player campaign) or **Arena** (a 2–4 player free-for-all on one hex platform: hidden
> simultaneous planning → simultaneous deterministic resolve, last hero standing wins). PO brief:
> `product-requirements/arena-mode-mvp.md`; design intent: `/design/arena-mode.md`.
> Status: current as of 2026-07-03 — **the MVP is complete** (main menu, the full symmetric round
> loop, NGO host/join, and the match HUD / spectate / disconnect handling). Post-MVP polish (VFX,
> camera, anti-cheat) is on the ROADMAP.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

Menu & mode flow (brief R1–R3 — implemented):

- **R1** The game boots into a main menu presenting exactly two modes: **Journey** and **Arena**.
- **R2** **Journey** loads the current combat/exploration scene (`Area`) with no behavioral change.
- **R3** **Arena** loads the Arena scene.

The symmetric round (brief R7–R12 — implemented):

- **R7** Each round every player builds their commitment **hidden** — plans exist only on their
  own machine until locked (hidden by construction, not by masking).
- **R8** A terminal action **locks the round in**; the round holds until every alive player locked.
- **R9** All committed actions **resolve together** in the one round, in a deterministic order.
- **R10** Same match seed + same committed actions → **identical resolution** (lockstep; verified
  by a per-round state hash).
- **R11** **Whiffs are real**: committed cells are frozen at lock time and never recomputed — a
  dodged blow misses, a unit stepping into committed cells is hit, blocked moves fizzle.
- **R12** Simultaneous conflicts resolve by the deterministic rules in §2.7.
- **R13** Abilities are unchanged from PvE — only round timing/commitment differ.
- **R14** **Last hero standing wins**; zero heroes left is a draw.

Arena session (brief R4–R6 — implemented):

- **R4–R5** 2–4 players: one player **hosts**, others **join by address** (direct connect over
  NGO; no lobby/matchmaking/reconnect — approval rejects a full or already-started match). An
  offline mode (config flag) replaces the network with 1–3 seeded AI dummies for dev testing.
- **R6** Every player spawns the **same default hero** at deterministic, distinct spawn cells.

### 1.2 Non-functional requirements

- **N1** The round domain is pure C# and unit-tested (39 arena tests incl. a determinism replay,
  a two-client lockstep suite, and the conflict-edge sweep).
- **N2** All dependencies wired through Zenject (`ArenaInstaller`); no service locators.
- **N3** PvE stays byte-identical: the only PvE file changes are the behavior-preserving
  `RoundLifecycleProcessor` extraction and an optional-injection seam in
  `CharacterCombatCoordinator` (PvE regression suite green, 54 tests).
- **N4** No `UnityEngine.Random` anywhere on the arena path; all seeds derive from the match seed
  via `LootSeed.Derive`.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Combat/Arena/Core/   — pure C# round domain: ArenaCommit(+Builder), ArenaCommitCollector,
                               IArenaResolutionOrder + RotatingInitiativeOrder, ArenaRoundBundle,
                               LastHeroStandingWinCondition, ArenaStateHash, ArenaSpawnPlanner,
                               ArenaSpawnSlot, IArenaTransport + LoopbackArenaTransport
Scripts/Combat/Arena/        — application: ArenaCombatController (: ICombatController),
                               ArenaMatchHost, ArenaAICommitSource, ArenaMatchLauncher,
                               ArenaConnectPresenter
Scripts/Combat/Arena/Data/   — ArenaMatchConfig SO (dials only)
Scripts/Combat/Arena/Networking/ — infrastructure: the wire structs (INetworkSerializable),
                               ArenaWireCodec (domain ↔ wire, buffer-free), ArenaSessionService
                               (host/join/approval over NetworkManager), NgoArenaTransport
                               (: IArenaTransport via NGO custom named messages)
Scripts/Combat/Arena/View/   — infrastructure: ArenaPlatformBuilder, ArenaHeroSpawner,
                               ArenaSceneEntrypoint (thin Mono), ArenaConnectView
Scripts/Core/DI/ArenaInstaller.cs — the Arena scene's MonoInstaller
Scenes/Arena.unity           — SceneContext (ArenaInstaller + CharacterSystemInstaller), fixed
                               camera, light, the CombatActionPanel canvas (cribbed from Area),
                               HUD status line, NetworkManager + UnityTransport, connect panel
```

### 2.2 The in-match HUD & spectate

`ArenaMatchHudPresenter` (over `IArenaMatchHudView`) drives the HUD from the controller's events:
the status line reads the round and whether the local player has locked in ("plan your actions" →
"locked in, waiting…" → "resolving…"); when the local hero falls but the match continues, input is
disabled and a **Spectating** label appears (the player watches it end — brief R14); on match end
a winner/draw banner shows with **Leave → main menu** (session shutdown first). A joined client
that loses the host sees "Connection to the host was lost" + Leave; a host-side lockstep-hash
mismatch (`ArenaMatchHost.DesyncDetected`, R10) surfaces a HUD warning.

### 2.3 The reuse contract

`ArenaCombatController` implements the unchanged **`ICombatController`** seam, so the whole PvE
presentation stack works against it untouched: the combat action panel, the planning input
(`CharacterCombatCoordinator` + presenters), the overhead plan icons, the ghost telegraph, and the
resolve pacing (`EnemyRoundController`). Resolution reuses **`EnemyIntentResolver`** verbatim —
`EnemyIntent` is the committed-intent record for *any* unit (whiff / fizzle / skip-dead semantics
are the PvE-tested ones), and round bookkeeping reuses the extracted **`RoundLifecycleProcessor`**
(the exact code `CombatController` runs). Phase mapping onto the PvE `RoundPhase` values:
`PlayerAct` = planning (hidden), `EnemyResolve` = the simultaneous resolution.

### 2.4 The match flow (networked)

The Arena scene boots into the **connect panel** (`ArenaConnectPresenter`): **Host** opens a
session (`ArenaSessionService.StartHost` — listens on the config port, connection approval caps
players and rejects joins once started); **Join** connects to `ip[:port]`. When the host presses
**Start Match** (≥2 players), `ArenaMatchLauncher` seats every connected client (host first, then
joiners by ascending clientId — player/unit ids 1..N), rolls the match seed, and broadcasts the
`MatchSetup`. Every client (host included) then builds the identical world locally from the seed:
its own seat becomes the local `HumanPlayer`, remote seats become `NetworkPlayer`s, and the shared
match build runs (platform → controller → telegraph → spawns → rounds). **Nothing per-unit is
replicated** — no NetworkObjects, no scene sync; the only traffic is the lockstep messages below,
sent as NGO **custom named messages** (`NgoArenaTransport`): the local machine's own messages are
raised directly, remote ones travel **reliable-fragmented-sequenced** (fragmented because the
draft board and a full 4-player bundle exceed the ~1264-byte unfragmented MTU; one shared
delivery pipeline keeps all messages mutually ordered).

| Message | Direction | Payload |
|---|---|---|
| `yash.arena.setup` | host → joiners | match seed + seat roster (spawns/platform derive from the seed) |
| `yash.arena.commit` | joiner → host | round number + the locked commit + previous round's state hash (R10) |
| `yash.arena.bundle` | host → joiners | the canonical round: commits by ascending PlayerId + departed players |
| `yash.arena.tasted` | joiner → host | the client's tasted-forms catalog (part ids), sent on session start |
| `yash.arena.draftstart` | host → joiners | the composed draft board + slot loadout + pick timer (§2.9) |
| `yash.arena.draftpick` | joiner → host | one draft pick request (pick index, player, entry) |
| `yash.arena.draftapplied` | host → joiners | one canonically applied pick + departures since the last one |

Between the setup and the round loop sits the **parts draft** (§2.9): the match build
(`ArenaSceneEntrypoint.BeginCombat`) only runs once the draft's confirmed result — each seat's
slot → part loadout — is in hand.

### 2.5 The round loop

```
StartRound  — host opens the gather (alive players owe a commit), phase → PlayerAct
Planning    — the local player plans with the normal combat UI:
              · free actions (turn, reorder) run locally;
              · ScheduleAbility / EndUnitTurn are terminal but board-effect-free: they execute
                locally (the queue is local planning state no other client reads) and lock the
                round in with an EMPTY commitment — a growing volley stays hidden until executed;
              · Move / ExecuteAbilityQueue are INTERCEPTED: never executed locally, snapshotted
                into an ArenaCommit (per-ability committed cells frozen from position + final
                facing) and sent to the host. AI dummies commit via the same path.
Assembly    — ArenaMatchHost accepts one commit per alive player (duplicates/stale ignored) and
              broadcasts the canonical ArenaRoundBundle (commits by ascending PlayerId).
Normalize   — every client makes the pre-resolution board canonical: departed players' units die,
              every commit's lock-time facing is applied, executed volleys clear their queue.
Reveal      — the bundle's intents go out through OnEnemyPlansRevealed → the PvE overhead icons
              show everyone's committed round.
Resolve     — phase → EnemyResolve; EnemyRoundController paces ResolveNextEnemyIntent() through
              the ordered intents via EnemyIntentResolver; win check after every intent.
Round end   — RoundLifecycleProcessor ticks effects/cooldowns/acted-flags once for all units;
              the ArenaStateHash is computed and logged; the next round opens.
```

### 2.6 Determinism

The match seed is the single root: the platform surface
(`LootSeed.Derive(seed, "arena-platform")` → `DeterministicRandom` → `PlatformSurfaceGenerator`),
the spawn cells (greedy farthest-point over the surface, ties by (Q,R)), and the offline dummies'
decisions (`LootSeed.Derive(seed, "arena-ai:{unitId}")` → seeded `TacticalAI`). Unit ids are
roster-assigned 1..N (never `UnityEngine.Random`). The per-round **`ArenaStateHash`** (FNV-1a over
UnitId-ordered id/position/HP/facing/cooldowns/effects) is the lockstep safety net: commits
piggyback the previous round's hash and the host logs a loud error on mismatch.

### 2.7 Deterministic conflict rules (brief R12)

- **Order:** `IArenaResolutionOrder` strategy (replaceable — PO decision). Default
  `RotatingInitiativeOrder`: round N starts at index (N−1) mod aliveCount of the PlayerId-sorted
  commits and cycles — initiative rotates, nobody holds it permanently; within a unit, steps keep
  committed order.
- **Whiff:** ability steps fire at their frozen committed cells; nothing re-targets.
- **Same-hex moves:** a move can only be **committed to a cell empty at plan time** (the action
  validator rejects a move onto an occupied cell), so swaps and chases into an occupant can never
  be locked in. The only reachable conflict is two units committing to the **same empty cell**: at
  resolve the earlier in initiative enters, the later finds it occupied and **fizzles in place** —
  a fizzled move never re-targets.
- **Mutual blows:** sequential — both land unless the earlier blow was lethal; a unit dead or
  stunned when its step arrives has its whole remaining commitment skipped.
- **Cooldowns:** reset per resolved ability step; decremented once at round end.
- **Win/draw:** `LastHeroStandingWinCondition` (armed after the full roster spawned) — one player
  with a living unit wins; zero is a draw (null winner, `Defeat` phase); remaining intents are not
  resolved after match end.
- **Departure:** the host folds departed players into the next bundle; their units die at
  normalization on every client — deterministic because it rides the bundle, never local timing.

### 2.8 DI wiring

`ArenaInstaller` (MonoInstaller on the Arena SceneContext, beside `CharacterSystemInstaller` which
the Hero prefab's `ModularCharacterVisual` needs): logging home; configs auto-loaded from
Resources when unwired (`Configs/CombatMovementConfig`, `Configs/InputConfig`,
`Configs/HexDirectionConfig`, `LevelGeneration/PlatformShapeConfig`, `Heroes/TestHeroDefinition`,
`Arena/ArenaMatchConfig`); the combat execution subset of `AreaInstaller` (validator, executors,
shape calc, damage, telegraph support, battlefield factory, turn manager, resolve pacer); and the
Arena bindings — `ICombatController → ArenaCombatController` (FromResolve over the concrete
singleton), `IArenaTransport → LoopbackArenaTransport` (Phase 3 swaps in the NGO transport),
`ArenaMatchHost`, commit builder/collector, `IArenaResolutionOrder → RotatingInitiativeOrder`,
win condition, spawn planner, platform builder, hero spawner, AI commit source, and the
entrypoint (`BindInterfacesTo<ArenaSceneEntrypoint>.FromComponentInHierarchy`).

The draft slice (§2.9) adds: `PersistenceInstaller` (save stores only — the tasted-catalog
read), `ArenaDraftConfig` (auto-loaded from `Arena/ArenaDraftConfig`, mapped to Core settings
via `ArenaDraftConfigMapper`), the catalog reader/sender/registry, `ArenaDraftHost` (+
`IArenaDraftClock → UnityArenaDraftClock`, `IArenaDraftPartInfoSource →
PartCatalogDraftInfoSource`), `ArenaDraftFlow`, `IPartAbilityResolver`, the draft stage rig,
the panel/part-info popover from prefabs (missing prefab → headless degrade, never a crash),
the draft presenter, and the **shared ability-preview popover** (`AbilityPreviewInstaller`,
hero source = `ArenaDraftHeroSource` — never the scene's `ModularCharacterVisual`, which is
ambiguous once several heroes spawn).

Deliberately absent: narrative, loot, inventory, streaming-world installers (the mutation
subsystem stays absent too — the shared ability-preview module lives in `UI.AbilityPreview`,
not in Mutation).

### 2.9 The parts draft (P4-5 model + G4 screen)

Every match opens with a **snake-order parts draft** off a **shared board** before any hero
spawns (`arena-part-draft-and-catalog.md` + `arena-draft-ui.md`). The layer sits entirely above
the round loop — transport, commits, resolution order, and win condition are untouched.

**The tasted-forms catalog.** Journey records every part the hero has ever carried (equipped or
dormant) as a Meta-horizon fact `world.<partId>.arena_tasted` — written by `TastedFormsRecorder`
(Area scene, on `CharacterAssembled`/`PartsChanged`, idempotent), persisted by the existing
`meta.json` flush points. The Arena scene reads it back with `ArenaTastedCatalogReader`
(`IMetaMemoryStore.LoadOrEmpty()` directly — no narrative fact-store bootstrap), dropping ids the
part catalog no longer knows and frame-changing parts (the draft stays on the base body-plan).
The catalog is **read-only for Arena** and never feeds back into Journey. Since O1 the same
reader also serves the **Hub** (`hub-staging.md`): the starting-part offer is drawn from the
identical tasted + base-skeleton view — relocating `ArenaTastedCatalogReader` to a shared
tasted-catalog home is filed ROADMAP debt.

**Board composition (host-authoritative).** Each client submits its catalog on session start
(`ArenaTastedCatalogSender` → host's `ArenaTastedCatalogRegistry`). On Start Match the host
composes the board once (`ArenaDraftBoardComposer`): the **common floor** in full — an authored
list where duplicates are copies, guaranteeing every seat a complete body (req 6) — plus a
seeded sample (`LootSeed.Derive(seed, "arena-draft-board")`) of the participants' **catalog
union** (single-copy — that is where denial bites). The composed board travels in
`draftstart`; clients never recompute it.

**The draft loop (lockstep by construction).** `ArenaDraftModel` (pure C#) is the state
machine: snake order (`ArenaSnakeOrder`), pick legality (turn / entry available / slot open),
denial, completion at seats × slots. The host (`ArenaDraftHost`) validates every `draftpick`
request against its own model and broadcasts the applied pick; **every replica — the host's own
`ArenaDraftFlow` included — advances only on `draftapplied` broadcasts**. Pick deadlines are
host-only (`IArenaDraftClock` seam): humans get the generous soft timer, offline AI dummies a
short pacing delay, departed seats fill immediately — all through the deterministic auto-pick
(lowest loadout-slot order, then lowest entry id), so the draft can never hang (G4 req 13).
Mid-draft departures piggyback on `draftapplied`; the entrypoint seeds them into
`ArenaMatchHost` so round 1's bundle kills their (still deterministically spawned) units.

**The screen (presentation only).** `ArenaDraftPresenter` (pure C#) reads the flow's replica and
drives: `ArenaDraftStageRig` — a procedural far-offset 3D stage (one camera → RenderTexture)
with the board's part models on a slot-grouped pedestal grid and the local monster assembling
live via `IModularCharacterFactory`; `ArenaDraftView` (prefab
`Resources/Prefabs/UI/ArenaDraftPanel`) — whose-pick banner, snake-order line, cosmetic
countdown, the local dual readout (3D + name/parts text) and per-opponent name + parts readouts,
remote-pick flights; `ArenaPartInfoPopoverView` — click a part model (board or hero) → part
info + ability rows whose hover opens the **shared ability-preview popover**
(`ability-preview-popover.md`) with the Draft button when the pick is legal right now (an
illegal pick shows its reason — req 7). Completion shows the "your monster" beat
(auto-continues after `_beatSeconds`), then the untouched match build runs. Missing prefabs
degrade to a headless draft (host auto-picks) — never a crash.

**Drafted body → combat.** `ArenaHeroSpawner` maps each seat's loadout through the same
part→combat path PvE uses: actives/passives via `IPartAbilityResolver`, the visual via
`SwapPart` per drafted part on the hero's modular rig; MaxHP stays `HeroDefinition.MaxHP`, and
the HeroDefinition kit remains only as a loudly-logged fallback. Identical loadouts on every
client ⇒ identical ability sets ⇒ the round loop's determinism holds unchanged.

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

### `ArenaMatchConfig`  (asset menu: `Yasherica → Arena → Match Config`)

Loaded from `Resources/Arena/ArenaMatchConfig` (or wired on the scene's `ArenaInstaller`).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_maxPlayers` | int (2–4) | Hard cap on players in a match (connection approval enforces it) | 4 |
| `_port` | int | Direct-connect port for the networked session | 7777 |
| `_offlineMode` | bool | Skip the host/join flow; start an offline match vs AI dummies immediately | off |
| `_offlineDummyCount` | int (1–3) | AI dummies joining the local player in offline mode | 2 |
| `_offlineMatchSeed` | int | Offline match seed; 0 = fresh seed each launch (logged) | 0 |
| `_platformMaterial` | Material | Arena platform mesh material | the shared platform material; empty = plain lit fallback |

Referenced assets: the platform `Material` only. The hero and its abilities come from the
existing `HeroDefinition` / `AbilityDefinition` SOs (see `ability-subsystem.md`).

### `ArenaDraftConfig`  (asset menu: `Yasherica → Arena → Draft Config`)

Loaded from `Resources/Arena/ArenaDraftConfig` (or wired on the scene's `ArenaInstaller`).
Mapped to Core `ArenaDraftSettings` at install time by `ArenaDraftConfigMapper`, which drops
misauthored rows loudly (frame-changers, slot-less parts, parts outside the loadout) and warns
when the floor cannot cover a full lobby (P4-5 req 6).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_slotLoadout` | List\<SlotDefinition\> | The fixed Arena slot loadout — the draft completes when every seat fills each | the 7 base-biped slots |
| `_floorParts` | List\<PartDefinition\> | The common floor, stocked in full; **list a part N times for N copies** | 4 copies per slot |
| `_catalogSampleSize` | int | How many distinct tasted-catalog parts are sampled onto the board (single-copy) | 8 |
| `_pickTimerSeconds` | float | Generous soft limit per human pick; on expiry the host auto-picks (G4 req 13) | 45 |
| `_aiPickDelaySeconds` | float | Pacing delay before an offline AI dummy's pick lands | 1.5 |
| `_beatSeconds` | float | How long the "your monster" beat holds before auto-continue | 4 |
| `_baseAssembly` | CharacterAssemblyDefinition | The base body every drafted monster assembles onto (stage + spawn) | `PlaceholderAssembly_A` |

### `AbilityPreviewConfig`

Owned by the shared ability-preview popover — see `ability-preview-popover.md` §SO Reference.

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### Tune the arena match

1. Open `Resources/Arena/ArenaMatchConfig.asset` (or create one via
   `Create → Yasherica → Arena → Match Config` and wire it on the scene's `ArenaInstaller`).
2. Set port / max players / material per §3. For offline dev testing set `_offlineMode`; a fixed
   `_offlineMatchSeed` then reproduces the exact platform, spawns, and dummy decisions every launch.

### Change the arena hero or its abilities

Since the parts draft (P4-5/G4) an arena body is **drafted**, not fixed: abilities come from the
drafted parts (`PartDefinition.ActiveAbilities` / `PassiveAbilities` — see
`character-system.md`). The `HeroDefinition` at `Resources/Heroes/TestHeroDefinition` still
supplies **MaxHP** and the loudly-logged ability fallback for a seat with no drafted actives.

### Add a floor part / tune the draft board

1. Author the part as usual (`character-system.md` §Adding Content) — any non-frame-changing
   `PartDefinition` whose slot is in the loadout is draftable.
2. Open `Resources/Arena/ArenaDraftConfig.asset` and add the part to `_floorParts` — **once per
   copy** you want stocked (the floor must cover `_maxPlayers` per slot; the mapper warns if not).
3. Tune `_catalogSampleSize` / `_pickTimerSeconds` / `_aiPickDelaySeconds` / `_beatSeconds` per §3.
   No code — parts a player merely *tasted in Journey* appear on the board automatically.

### Change the arena slot loadout

Edit `_slotLoadout` on `ArenaDraftConfig` (SlotDefinition refs). The draft completes when every
seat fills every listed slot; floor parts targeting removed slots are dropped with a warning.

### Change the arena platform size/shape

### Change the arena platform size/shape

The arena reuses the **Combat** `ShapeProfile` + battlefield minimum from the one
`PlatformShapeConfig` SO (`platform-generation.md` §3) — tuning combat platforms tunes the arena.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/` (all pure, runnable via the Roslyn runner):

- `ArenaCoreTests` — rotation of initiative (round 1 / round 2 / wrap; step order), commit
  collector (completion, duplicates, stale rounds, departure completes a round, canonical order),
  spawn planner (determinism, distinctness, spread), state hash (stability + sensitivity),
  last-hero-standing (unarmed guard, win, draw, ongoing).
- `ArenaCommitBuilderTests` — volley flattening with frozen committed cells, move step,
  schedule/pass empty commitments, the AI schedule-and-face shape.
- `ArenaRoundFlowTests` — the full symmetric round over the loopback transport with production
  wiring: nothing resolves before all lock in; whiff on dodge; step-into-cells punished; same-hex
  conflict by initiative; lethal-first skips the return blow + winner declared; initiative
  rotation observable across rounds; the determinism replay (same commits twice → identical
  `ArenaStateHash`).
- `ArenaWireCodecTests` — every step kind round-trips domain → wire structs → domain with the
  committed snapshot intact (cells/facing/origin/destination), plus envelope hash, bundle
  order/departures, and the setup roster.
- `ArenaLockstepTests` — two independent client sims (host + joiner, each with its own state and
  seat objects) over one shared transport: identical positions/HP after committed moves and a
  committed volley across two rounds, identical per-round `ArenaStateHash` (R10), the joiner never
  assembles.
- `ArenaEdgeCaseTests` — the conflict corners: a move onto an occupied cell is rejected at plan
  time (so swaps/chases can't be committed); two units racing to the same empty cell (earlier
  enters, later fizzles); a caster is never in its own committed cells; mutual non-lethal blows
  both land; a stunned caster's committed step is skipped; a mid-planning departure completes the
  round and kills the departed unit on the bundle.
- `MainMenuPresenterTests` — menu routing (Phase 1).
- `ArenaDraftModelTests` — snake order (wrap + the double pick at the turn), the pick legality
  matrix (out of turn / taken / slot filled / stale index / after completion), denial removes
  for everyone, completion at seats × slots, loadout correctness, auto-pick determinism +
  slot-priority rule + the impossible-board throw.
- `ArenaDraftBoardComposerTests` — board determinism (same seed + catalogs → identical), input
  order independence, floor-only degrade on empty catalogs, duplicate floor stock as separate
  entries, sample-size respect, floor/catalog dedupe, loadout filtering, sequential entry ids.
- `ArenaDraftWireCodecTests` — the four draft messages round-trip (catalogs, board + loadout +
  timer, pick requests, applied picks with departures).
- `ArenaDraftFlowTests` — the full draft over the loopback transport with production wiring
  (host + flow + registry): human picks + AI auto-picks to completion, host/replica loadout
  agreement, out-of-turn rejection without state change, human-timeout auto-pick via the fake
  clock, mid-draft departure auto-fill + departure surfacing, two independent same-seed drafts
  composing identical boards.
- `TastedFormsCatalogTests` — the recorder core (marks carried ids, idempotent, skips empties)
  and the reader core (extracts exactly the tasted subjects from a fact snapshot).
- `AbilityPreviewShapeTests` — the shared popover's mock-ground geometry (line length, 6R ring,
  determinism).

PvE regression: the combat suite (54 tests) stays green after the `RoundLifecycleProcessor`
extraction. The NGO layer itself (named-message delivery, session approval) is play-tested: run
the editor as host and 1–3 standalone dev builds as joiners on `127.0.0.1` (Build Profiles →
Windows), then grep each instance's log for the per-round `ArenaStateHash` lines — they must match
every round. (Unity's Multiplayer Play Mode would run the joiners as in-editor virtual players, but
it is intentionally not a project dependency — it transitively pulls a Newtonsoft-JSON package that
Unity's registry currently reports with an invalid signature.)

---

## 6. Known limitations / open points

- **Trusted peers.** Commits are relayed, not re-validated against the canonical state on the
  host — host-side commit validation (anti-cheat) is a ROADMAP item.
- **Desync has no recovery.** A hash mismatch (R10) is surfaced (host log + HUD warning) but the
  match cannot resynchronize — the PRD excludes reconnect.
- **AI dummies fire on the PvE enemy cadence.** A dummy's schedule-action fires the same round
  (PvE enemy semantics), while humans build queues across rounds — the offline mode is a dev
  fallback, PvP (all-human) is symmetric by construction.
- **Arena camera is a fixed scene camera.** Framing at arena scale untuned (ROADMAP).
- **Initial facing is uniform** (east) rather than toward the platform center — deterministic but
  unpolished.
- **`EnemyIntent` naming.** The committed-intent machinery is player-agnostic; the PvE-shaped
  names (`EnemyIntent`, `RoundPhase.EnemyResolve`) are a deferred mechanical rename (ROADMAP).
- **True simultaneous mutual-kill is not a draw.** Sequential skip-dead resolution (R4) means the
  earlier unit in initiative survives a mutual lethal exchange and wins; the draw rule only fires
  when a round genuinely leaves zero units (a defensive path, not reachable via committed blows).
- **The draft has no dedicated desync checkpoint.** A diverged draft replica logs a loud
  `REPLICA DIVERGENCE` error locally but is otherwise only caught by round 1's `ArenaStateHash`;
  a loadout hash piggybacked on draft completion is a ROADMAP item.
- **Bind-pose part display.** The draft board shows part meshes extracted at bind pose
  (standalone skinned meshes don't deform), bounds-normalised; odd silhouettes are possible.
  Real part thumbnails are a ROADMAP item; the icon-sprite fallback covers mesh-less parts.
- **Hero-part click targets are a name heuristic.** Colliders on the assembling monster map
  renderers to slots by part-prefab name prefix; an unmatched part just isn't clickable on the
  hero (the text readout stays the reliable info path).
- **Reconnect during the draft is excluded** (as in the whole MVP): a drop is permanent — the
  seat auto-drafts and its unit folds into round 1 dead.
