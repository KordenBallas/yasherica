# Arena Mode (Multiplayer) — Requirements & Design

> The second game mode: from a boot main menu the player picks **Journey** (the unchanged
> single-player campaign) or **Arena** (a 2–4 player free-for-all on one hex platform: hidden
> simultaneous planning → simultaneous deterministic resolve, last hero standing wins). PO brief:
> `product-requirements/arena-mode-mvp.md`; design intent: `/design/arena-mode.md`.
> Status: current as of 2026-07-07 — **the MVP is complete** (main menu, the full symmetric round
> loop, NGO host/join, and the match HUD / spectate / disconnect handling), and the first two
> Track X robustness slices shipped on top of it: **X1** (reconnect + disconnect grace + desync
> recovery + host migration, §2.10) and **X2** (host-side commit validation + the P4-3
> resolution/damage alternatives, §2.11 / §2.7). Post-MVP polish (VFX, camera, online services)
> is on the ROADMAP.
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
  NGO; no lobby/matchmaking — approval rejects a full or already-started match, with the one X1
  exception: a payload claiming a gracing seat with the right rejoin token, R16). An offline mode
  (config flag) replaces the network with 1–3 seeded AI dummies for dev testing.
- **R6** Every player spawns the **same default hero** at deterministic, distinct spawn cells.

Online robustness (Track X · X1 — implemented, §2.10):

- **R15** **Disconnect grace / auto-pass:** a dropped player's seat is auto-passed by the host for
  `_disconnectGraceRounds` round-opens — the match never stalls, the unit stays alive in place —
  and only then departs for good (the MVP kill-on-drop semantic).
- **R16** **Mid-match rejoin:** within grace, the player reconnects with a **derived rejoin token**
  (`ArenaRejoinToken` off the match seed — no token storage, any authority can validate) and is
  brought to the authoritative round-start state by a **state snapshot transfer**; it re-enters the
  open round when the bundle has not broadcast yet, else replays the round's bundle.
- **R17** **Desync recovery:** a hash mismatch (R10) is no longer just detected — the host pushes
  the diverged client a targeted round-start snapshot; the client adopts it, re-plans, and reports
  the host's own hash from then on.
- **R18** **Host migration (best-effort):** a host drop no longer ends the match — after the retry
  window every survivor runs the same deterministic election (lowest connected PlayerId off the
  bundle-fed seat mirror), the winner re-hosts with the match rolled back to the current round's
  planning start, and everyone else dials it as a rejoiner via the broadcast address book.
  Reachability is LAN/best-effort until X3 brings a relay.

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
                               ArenaSpawnSlot, IArenaTransport + LoopbackArenaTransport;
                               X1: ArenaMatchContext, ArenaSeatLedger, ArenaRejoinToken,
                               ArenaStateSnapshot(+Restorer + IArenaStatusReconstructor),
                               ArenaReconnectMessages, ArenaConnectPayload, IArenaSessionControl
                               (+ IArenaRejoinGate), ArenaSeatStatusMirror, ArenaHostElection,
                               IArenaReconnectClock + IArenaLocalEndpointSource;
                               X2: SeededShuffleResolutionOrder + ArenaResolutionOrderMode,
                               ArenaCommitValidator, ArenaQueueCreditLedger,
                               IArenaCanonicalStateSource, ArenaStepBatcher, ArenaBatchEligibility
Scripts/Combat/Arena/        — application: ArenaCombatController (: ICombatController),
                               ArenaMatchHost, ArenaAICommitSource, ArenaMatchLauncher,
                               ArenaConnectPresenter; X1: ArenaReconnectHost, ArenaReconnectClient
                               (+ ArenaReconnectTicker), CatalogStatusReconstructor
Scripts/Combat/Arena/Data/   — ArenaMatchConfig SO (dials only)
Scripts/Combat/Arena/Networking/ — infrastructure: the wire structs (INetworkSerializable),
                               ArenaWireCodec (domain ↔ wire, buffer-free), ArenaSessionService
                               (host/join/approval over NetworkManager), NgoArenaTransport
                               (: IArenaTransport via NGO custom named messages), LanEndpointSource
Scripts/Combat/Arena/View/   — infrastructure: ArenaPlatformBuilder, ArenaHeroSpawner,
                               ArenaSceneEntrypoint (thin Mono), ArenaConnectView,
                               UnityArenaReconnectClock
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
a winner/draw banner shows with **Leave → main menu** (session shutdown first). A host-side
lockstep-hash mismatch (`ArenaMatchHost.DesyncDetected`, R10) surfaces a HUD warning. Since X1 a
lost connection **in combat** is not terminal: the reconnect machine drives the status line
("Connection lost — reconnecting…" / "Host lost — connecting to player N…" / "You are the new
host"), input freezes for the duration, and only `ReconnectFailed` lands on the old terminal
"Connection lost" + Leave; other players' drops surface as a **seat notice** off the bundle's
auto-passed list ("Player N disconnected — auto-passing"). A drop **before** combat (connect/draft)
keeps the MVP terminal behavior.

### 2.3 The reuse contract

`ArenaCombatController` implements the unchanged **`ICombatController`** seam, so the whole PvE
presentation stack works against it untouched: the combat action panel, the planning input
(`CharacterCombatCoordinator` + presenters), the overhead plan icons, the ghost telegraph, and the
resolve pacing (`EnemyRoundController`). Since **A1** it shares the whole round/state core, not just
the resolver: both controllers are thin `ICombatController` adapters over one **`CombatRoundEngine`**
(unit-list mutation, battlefield lifecycle, phase sequencing, the resolve loop, the end-of-round
lifecycle, and the win check are single-sourced there). Arena supplies its differences through an
**`ArenaCombatFlow`** (`ICombatRoundFlow`): the host gather + hidden planning open, the
execute-or-intercept-into-`ArenaCommit` action path, the bundle→`IArenaResolutionOrder` resolution
source, and the end-of-round `ArenaStateHash` publish. Resolution still runs through
**`EnemyIntentResolver`** verbatim — `EnemyIntent` is the committed-intent record for *any* unit
(whiff / fizzle / skip-dead semantics are the PvE-tested ones) — and the round bookkeeping is the same
**`RoundLifecycleProcessor`** the engine runs for PvE. Phase mapping onto the shared `RoundPhase`
values: `PlayerAct` = planning (hidden), `EnemyResolve` = the simultaneous resolution.

The Arena-only surface that is **not** on `ICombatController` — `SetHostRole` (before `Initialize`),
`ArmWinCondition` (after the roster spawns), and the `LastRoundHash` read — stays on
`ArenaCombatController` and forwards into `ArenaCombatFlow`; `ArenaSceneEntrypoint` drives them through
the concrete type exactly as before.

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
| `yash.arena.rejoin` | host → one rejoiner | the full stand-up: setup + loadouts + round-start snapshot + the current round's bundle when already broadcast (X1) |
| `yash.arena.resync` | host → one client | a targeted round-start snapshot — the R10 desync heal (X1) |
| `yash.arena.resyncack` | joiner → host | the state transfer landed; the seat goes live again (X1) |
| `yash.arena.endpoint` | joiner → host | the client's self-reported reachable address (X1 migration) |
| `yash.arena.addrbook` | host → joiners | every claimed endpoint — where survivors find the elected host (X1) |

The bundle also carries the round's **auto-passed players** (X1 R15) beside its departures.
Connection approval reads the NGO `ConnectionData` payload: empty = fresh join (rejected once the
match started), a 14-byte `ArenaConnectPayload` rejoin claim = validated against the seat ledger
through the `IArenaRejoinGate` (§2.10).

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
Round end   — RoundLifecycleProcessor ticks effects/cooldowns/acted-flags once for all units —
              the ONE deterministic status resolve point (combat-status-effects.md R3); an
              explicit win check settles a DoT kill here, identically on every peer; then the
              ArenaStateHash is computed and logged and the next round opens.
```

### 2.6 Determinism

The match seed is the single root: the platform surface
(`LootSeed.Derive(seed, "arena-platform")` → `DeterministicRandom` → `PlatformSurfaceGenerator`),
the spawn cells (greedy farthest-point over the surface, ties by (Q,R)), and the offline dummies'
decisions (`LootSeed.Derive(seed, "arena-ai:{unitId}")` → seeded `SimulationTacticalAI` with the
free-for-all hostility policy, `combat-enemy-ai.md`). Unit ids are
roster-assigned 1..N (never `UnityEngine.Random`). The per-round **`ArenaStateHash`** (FNV-1a over
UnitId-ordered id/position/HP/facing/cooldowns/effects) is the lockstep safety net: commits
piggyback the previous round's hash and the host logs a loud error on mismatch.

### 2.7 Deterministic conflict rules (brief R12)

- **Order:** `IArenaResolutionOrder` strategy, a **config pick** (`ArenaMatchConfig._resolutionOrderMode`).
  Default `RotatingInitiativeOrder`: round N starts at index (N−1) mod aliveCount of the
  PlayerId-sorted commits and cycles — initiative rotates, nobody holds it permanently; within a
  unit, steps keep committed order. The alternative `SeededShuffleResolutionOrder` (P4-3a) is a
  per-round Fisher–Yates over the PlayerId-sorted commits seeded `LootSeed.Derive(seed,
  "arena-resolve:{round}")` — unpredictable to players (rotation can be planned around; a shuffle
  cannot) yet identical on every client. Off by default.
- **Whiff:** ability steps fire at their frozen committed cells; nothing re-targets.
- **Same-hex moves:** a move can only be **committed to a cell empty at plan time** (the action
  validator rejects a move onto an occupied cell), so swaps and chases into an occupant can never
  be locked in. The only reachable conflict is two units committing to the **same empty cell**: at
  resolve the earlier in initiative enters, the later finds it occupied and **fizzles in place** —
  a fizzled move never re-targets.
- **Mutual blows:** sequential by default — both land unless the earlier blow was lethal; a unit
  dead or stunned when its step arrives has its whole remaining commitment skipped. With
  **per-step damage batching** on (`_simultaneousDamageBatching`, P4-3b — off by default), the
  round resolves **step-major** instead: every commit's k-th step forms a batch resolved against a
  batch-start eligibility snapshot (`ArenaStepBatcher` + `ArenaBatchEligibility`), so a unit killed
  mid-batch still fires its same-batch blow and a mutual lethal exchange kills **both** (a real
  draw). The win check is suspended inside a batch and settles at each boundary
  (`LastHeroStandingWinCondition.Suspend/Resume`). Only *death* is simultaneous — moves inside a
  batch still resolve in order (the same-empty-cell fizzle needs a winner). A rules change, so it
  is strictly config-gated; PvE is untouched.
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

### 2.10 Reconnect, disconnect grace & host migration (X1)

The production layer over the MVP's "a drop is fatal" stance. Design spine: **all resync
alignment happens at round-start boundaries** (never mid-resolve), the **auto-pass is a
bundle-level fact** (a passed unit is simply absent from `Commits` — normalization needs no new
rule), and the **rejoin credential is derived, not stored** (`ArenaRejoinToken.For(seed,
playerId)` — every participant can validate every seat, which is exactly what migration needs).

**Seat states (host-side `ArenaSeatLedger`).** `Connected → Gracing → (Resyncing → Connected) |
Departed`. A vanished connection puts the seat into **Gracing**: `ArenaMatchHost` marks it passed
for the open round (the round completes without it) and keeps auto-passing it for
`_disconnectGraceRounds` round-opens; expiry folds it into the next bundle's departures — the MVP
kill semantic, just delayed. A valid rejoin claim moves it to **Resyncing** (still auto-passed)
until the state transfer is acknowledged.

**The rejoin dance.** The dropped client dials with `ArenaConnectPayload` (playerId + token) in
NGO `ConnectionData`; `ArenaSessionService.ApproveConnection` passes it to the
`IArenaRejoinGate` (= `ArenaReconnectHost` over the ledger). On the connect callback the host
ships one `ArenaRejoinPackage`: the original setup, the draft loadouts, the **round-start
snapshot** (`ArenaStateSnapshot` — the exact `ArenaStateHash` field set: UnitId-ordered
position/HP/facing/cooldowns/statuses + round number + last hash; ability queues, acted flags,
intents, battlefield, and loadouts are deliberately NOT in it — queues are local planning state,
the rest re-derives), and the current round's bundle when it already broadcast. The rejoiner
restores by **overlay** (`ArenaSnapshotRestorer`): every snapshot unit must already exist locally
(spawned from seed + loadouts); statuses rebuild by id through `IArenaStatusReconstructor`
(production = the status-definition catalog + factory); any mismatch fails the whole restore —
never a half-restored sim. It adopts the state (`ArenaCombatFlow.AdoptState` — sim, resync
anchor, and lockstep hash all become the transferred truth), re-opens planning, replays the
bundle if present, and acks (`resyncack`) — the host reinstates it into the open round
(`ArenaCommitCollector.Reinstate`) or picks it up next round.

**Desync heal (R17).** `DesyncDetected` now triggers a targeted `resync` (same snapshot machinery,
seat stays Connected) instead of only a warning. The diverged client guards against stale
commands (snapshot round < local round), adopts, re-plans, and acks. Its already-accepted commit
for the round stands — the heal converges its *board*; message order on the one sequenced
pipeline guarantees the resync arrives before the round's bundle.

**The drop machine (client, `ArenaReconnectClient`).** One state machine for every way the
connection dies, because a local disconnect is ambiguous (own blip vs host death):
`InMatch → RetryingHost → (rejoined | election) → (promote-self | ConnectingToCandidate) →
(rejoined | Failed)`. It retries the original host address every `_reconnectRetryIntervalSeconds`
for `_reconnectAttemptSeconds` (a live host ⇒ the retry succeeds and migration never engages),
then elects over the **seat mirror** (`ArenaSeatStatusMirror` — every client's liveness view fed
purely by the broadcast stream: commits = connected, auto-passed = absent, departures sticky;
plus the address book). `ArenaHostElection` = lowest connected PlayerId excluding the lost host —
deterministic on every survivor. The winner **promotes**: re-host with `MatchStarted` kept true,
re-seed the seat book with every other seat in grace (the dead host's included — it may return
like anyone else), activate its own `ArenaReconnectHost`, and **roll the current round back to
its planning anchor** (`ReopenCurrentRound` — in-flight commits died with the old host; lockstep
made every retained round-start copy identical, so everyone re-plans round N). Losers dial the
winner as ordinary rejoiners off the address book (`LanEndpointSource` self-reports; LAN
best-effort until X3). Timeout ⇒ the old terminal "connection lost".

**Config dials** (§3): `_disconnectGraceRounds` (default 3), `_reconnectAttemptSeconds` (10),
`_reconnectRetryIntervalSeconds` (2), `_migrationConnectTimeoutSeconds` (12).

### 2.11 Host-side commit validation (X2 anti-cheat)

The MVP relayed peer commits untouched; X2 closes that trust boundary. `ArenaCommitValidator`
(pure) re-checks every accepted commit against the host's **canonical round-start state**
(`IArenaCanonicalStateSource` = the arena controller over the flow's retained anchor — the same
state X1 snapshots): ownership + seat, the unit is alive and not stunned, the facing is a defined
direction, shape (one move XOR an ability volley within the queue-size cap **and** the seat's
banked scheduling rounds), move legality (origin = the unit's position, distance ≤
`MovementRange.EffectiveFor`, destination on the platform), and ability legality (known + off
cooldown). The load-bearing check re-derives each ability step's committed cells through the same
`ArenaCommitBuilder.RebuildAbilityIntent` the client used at lock time and demands exact equality —
**tampered cells cannot enter the bundle**.

An invalid commit is **replaced with a pass** (`ArenaCommitCollector.MarkPassed`) and loudly
logged (`CommitRejected`) — no kick, so a false positive from a validation bug cannot eject an
honest player; the seat just wastes its round. The volley budget (`ArenaQueueCreditLedger`) banks
one scheduling round per accepted empty commit and spends it on a fired volley — an upper bound
only (schedule vs end-turn are wire-indistinguishable, both empty by design), the queue-size cap
does the rest; reset on rejoin/resync (the transferred queue is empty). Gated by
`ArenaMatchConfig._validateCommits` (**on by default**); the host's own short-circuited commit runs
the same path (a free self-check). Validation runs only on whichever machine is the active host —
including one promoted by X1 migration.

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
| `_disconnectGraceRounds` | int (0–10) | Round-opens a dropped seat is auto-passed before it departs (X1 R15) | 3 |
| `_reconnectAttemptSeconds` | float | How long a dropped client retries the known host before electing (X1 R18) | 10 |
| `_reconnectRetryIntervalSeconds` | float | Delay between reconnect attempts to the same address | 2 |
| `_migrationConnectTimeoutSeconds` | float | How long a client tries the elected host before the terminal fallback | 12 |
| `_resolutionOrderMode` | enum | `RotatingInitiative` (default) or `SeededShuffle` per-round order (P4-3a, §2.7) | RotatingInitiative |
| `_validateCommits` | bool | Host re-validates every relayed commit against canonical state; invalid → pass (X2, §2.11) | on |
| `_simultaneousDamageBatching` | bool | Per-step simultaneous damage (P4-3b, §2.7): a mutual lethal kills both. OFF keeps sequential skip-dead | off |
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
- `ArenaSeatLedgerTests` — the X1 seat book: exact grace arithmetic (N round-opens then expiry),
  rejoin-token gate (wrong token / live seat / departed seat rejected, grace refresh on a later
  drop), migration seeding (unreachable seats start in grace), the connect-payload codec, and the
  collector's MarkPassed/Reinstate pair.
- `ArenaSnapshotTests` — the X1 state transfer invariant: capture → overlay-restore → identical
  `ArenaStateHash`; positions/HP/facing/cooldowns/status stacks transfer, local queues + acted
  flags are discarded, version/unit/status mismatches fail the whole restore, dead units arrive dead.
- `ArenaReconnectFlowTests` — X1 end-to-end over the loopback bus: a drop auto-passes (bundle
  fact, unit alive in place, hashes converge), grace expiry departs on all clients, a rejoiner is
  stood up from the package and re-enters lockstep (both before and after the round's broadcast),
  and a diverged client heals off the targeted resync.
- `ArenaHostMigrationTests` — the election rule (lowest connected, gracing/departed excluded),
  the address-book mirror, the drop machine (host retried before any election), the full
  promote-self path (round rolls back to its anchor, the survivor plays on and wins after the
  dead host's grace), and the elected-peer-unreachable terminal fallback.
- `SeededShuffleResolutionOrderTests` — P4-3a: same seed + round → identical order on two
  independent instances (the lockstep contract), different rounds/seeds reshuffle, per-commit step
  order preserved.
- `ArenaCommitValidatorTests` — X2: spoofed ownership / dead / stunned-only-empty / out-of-range
  or wrong-origin move / off-platform / unknown or cooling ability / over-budget volley / tampered
  committed cells all rejected; legal move, banked volley, and the empty commitment accepted.
- `ArenaCommitValidationFlowTests` — X2 over the loopback: a forged teleport envelope is
  substituted with a pass (loud log, match continues, hashes converge), honest commits (the host's
  own included) flow through, the schedule→volley credit dance works, and the config-off gate
  restores the trusting relay.
- `ArenaDamageBatchingTests` — P4-3b: a mutual lethal exchange kills both → draw with batching on
  vs one survivor under the default sequential rule; the batcher groups k-th steps with correct
  boundaries; the eligibility policy mirrors the default outside batch mode.
- `AbilityPreviewShapeTests` — the shared popover's mock-ground geometry (line length, 6R ring,
  determinism).

PvE + Arena regression: after the **A1** combat-core reconvergence (both controllers now share one
`CombatRoundEngine`), the whole EditMode combat + arena suite stays green — including
`ArenaRoundFlowTests`, `ArenaLockstepTests`, and `ArenaEdgeCaseTests` (the lockstep `ArenaStateHash`
determinism is the safety net), the PvE round suites, and the new `CombatRoundEngineTests` that covers
the shared core directly. The NGO layer itself (named-message delivery, session approval) is
play-tested: run
the editor as host and 1–3 standalone dev builds as joiners on `127.0.0.1` (Build Profiles →
Windows), then grep each instance's log for the per-round `ArenaStateHash` lines — they must match
every round. (Unity's Multiplayer Play Mode would run the joiners as in-editor virtual players, but
it is intentionally not a project dependency — it transitively pulls a Newtonsoft-JSON package that
Unity's registry currently reports with an invalid signature.)

**X1 LAN checklist (play-mode, owner):** two machines (or editor + standalone build on one) —
(1) kill the joiner's process mid-round → the host shows "Player N disconnected — auto-passing"
and the round completes; relaunch and rejoin within grace → the joiner lands in the open round
and the per-round `ArenaStateHash` lines match again; (2) let grace expire → the unit dies on the
next bundle; (3) kill the **host** mid-round → the survivor's HUD walks retry → election → "You
are the new host" (or dials the elected peer), the round re-opens at its planning start, and the
match plays to a winner; (4) force a hash divergence (dev tools / a debug write) → the host log
shows the LOCKSTEP DESYNC error followed by the heal, and the diverged client's next commit
reports the host's hash.

---

## 6. Known limitations / open points

- **Commit validation is an upper bound, not proof.** The host now re-validates every commit
  (X2, §2.11), but the volley budget is a ceiling — schedule and end-turn are wire-indistinguishable,
  so a passing round also banks a credit; tightening it would put queue mutations on the wire
  (deliberately out of scope). The cell re-derivation, ownership, range, and cooldown checks are exact.
- **The rejoin token blocks outsiders, not participants.** `ArenaRejoinToken` derives from the
  match seed, so anyone who ever held the setup can compute every seat's token — a malicious
  *participant* could impersonate another disconnected seat. Real per-player auth arrives with
  the X3 online stack; accepted for direct-connect play.
- **Migration reachability is LAN/best-effort.** The address book carries self-reported local
  addresses (`LanEndpointSource`); NAT defeats them — a survivor that cannot reach the elected
  host falls back to the terminal "connection lost" until X3 brings a relay. A split-brain
  election loser times out the same way.
- **A gracing seat can win.** Its unit stays alive during grace, so a disconnected player wins if
  everyone else dies first — accepted (the alternative kills a player for a network blip).
- **A resynced player loses its unexecuted volley.** Ability queues are local planning state and
  deliberately not in the snapshot; the healed/rejoined player re-plans from an empty queue.
- **NGO teardown/re-host in one process is play-mode territory.** All X1 protocol logic is
  loopback-tested; the `Shutdown() → StartHost()` port rebind on migration and the approval
  `ConnectionData` payload need the two-machine LAN checklist (§5) before the feature counts as
  play-verified.
- **AI dummies fire on the PvE enemy cadence.** A dummy's schedule-action fires the same round
  (PvE enemy semantics), while humans build queues across rounds — the offline mode is a dev
  fallback, PvP (all-human) is symmetric by construction.
- **Arena camera is a fixed scene camera.** Framing at arena scale untuned (ROADMAP).
- **Initial facing is uniform** (east) rather than toward the platform center — deterministic but
  unpolished.
- **`EnemyIntent` naming.** The committed-intent machinery is player-agnostic; the PvE-shaped
  names (`EnemyIntent`, `RoundPhase.EnemyResolve`) are a deferred mechanical rename (ROADMAP).
- **Mutual-kill is a draw only with batching on.** By default (sequential skip-dead, R4) the
  earlier unit in initiative survives a mutual lethal exchange and wins. Turning on
  `_simultaneousDamageBatching` (P4-3b, §2.7) makes a mutual lethal kill both (a real draw); it is
  off by default because it changes the felt combat rule, pending a playtest call.
- **The draft has no dedicated desync checkpoint.** A diverged draft replica logs a loud
  `REPLICA DIVERGENCE` error locally but is otherwise only caught by round 1's `ArenaStateHash`;
  a loadout hash piggybacked on draft completion is a ROADMAP item.
- **Bind-pose part display.** The draft board shows part meshes extracted at bind pose
  (standalone skinned meshes don't deform), bounds-normalised; odd silhouettes are possible.
  Real part thumbnails are a ROADMAP item; the icon-sprite fallback covers mesh-less parts.
- **Hero-part click targets are a name heuristic.** Colliders on the assembling monster map
  renderers to slots by part-prefab name prefix; an unmatched part just isn't clickable on the
  hero (the text readout stays the reliable info path).
- **Reconnect covers combat rounds only.** A drop during the connect/draft phase stays permanent
  (the seat auto-drafts and its unit folds into round 1 dead) — the X1 grace machinery arms at
  `BeginCombat`. Draft-phase grace is a possible follow-up if playtests want it.
- **No cross-process rejoin.** The reconnect machine lives in the running scene; closing the app
  forfeits the seat. A persisted rejoin ticket ("Rejoin last match" on the connect panel, the
  `JsonSaveFile` pattern) is filed ROADMAP debt.
