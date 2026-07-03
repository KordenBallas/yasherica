# Arena Mode (Multiplayer) — Requirements & Design

> The second game mode: from a boot main menu the player picks **Journey** (the unchanged
> single-player campaign) or **Arena** (a 2–4 player free-for-all on one hex platform: hidden
> simultaneous planning → simultaneous deterministic resolve, last hero standing wins). PO brief:
> `product-requirements/arena-mode-mvp.md`; design intent: `/design/arena-mode.md`.
> Status: current as of 2026-07-03 — **Phases 1–2 implemented** (main menu + the full symmetric
> round loop, playable offline vs seeded AI dummies); the NGO host/join network layer and the
> spectate/match-end polish are planned (§6).
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

The symmetric round (brief R7–R12 — implemented, offline transport):

- **R7** Each round every player builds their commitment **hidden** — plans exist only on their
  own machine until locked (hidden by construction, not by masking).
- **R8** A terminal action **locks the round in**; the round holds until every alive player locked.
- **R9** All committed actions **resolve together** in the one round, in a deterministic order.
- **R10** Same match seed + same committed actions → **identical resolution** (lockstep; verified
  by a per-round state hash).
- **R11** **Whiffs are real**: committed cells are frozen at lock time and never recomputed — a
  dodged blow misses, a unit stepping into committed cells is hit, blocked moves fizzle.
- **R12** Simultaneous conflicts resolve by the deterministic rules in §2.5.
- **R13** Abilities are unchanged from PvE — only round timing/commitment differ.
- **R14** **Last hero standing wins**; zero heroes left is a draw.

Arena session (brief R4–R6 — offline part implemented; networking planned, §6):

- **R4–R5** 2–4 players, host / join by address *(planned — currently 1 local player + 1–3
  seeded AI dummies over the in-process loopback transport)*.
- **R6** Every player spawns the **same default hero** at deterministic, distinct spawn cells.

### 1.2 Non-functional requirements

- **N1** The round domain is pure C# and unit-tested (26 arena tests incl. a determinism replay).
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
                               ArenaMatchHost, ArenaAICommitSource
Scripts/Combat/Arena/Data/   — ArenaMatchConfig SO (dials only)
Scripts/Combat/Arena/View/   — infrastructure: ArenaPlatformBuilder, ArenaHeroSpawner,
                               ArenaSceneEntrypoint (thin Mono)
Scripts/Core/DI/ArenaInstaller.cs — the Arena scene's MonoInstaller
Scenes/Arena.unity           — SceneContext (ArenaInstaller + CharacterSystemInstaller), fixed
                               camera, light, the CombatActionPanel canvas (cribbed from Area),
                               HUD status line
```

### 2.2 The reuse contract

`ArenaCombatController` implements the unchanged **`ICombatController`** seam, so the whole PvE
presentation stack works against it untouched: the combat action panel, the planning input
(`CharacterCombatCoordinator` + presenters), the overhead plan icons, the ghost telegraph, and the
resolve pacing (`EnemyRoundController`). Resolution reuses **`EnemyIntentResolver`** verbatim —
`EnemyIntent` is the committed-intent record for *any* unit (whiff / fizzle / skip-dead semantics
are the PvE-tested ones), and round bookkeeping reuses the extracted **`RoundLifecycleProcessor`**
(the exact code `CombatController` runs). Phase mapping onto the PvE `RoundPhase` values:
`PlayerAct` = planning (hidden), `EnemyResolve` = the simultaneous resolution.

### 2.3 The round loop

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

### 2.4 Determinism

The match seed is the single root: the platform surface
(`LootSeed.Derive(seed, "arena-platform")` → `DeterministicRandom` → `PlatformSurfaceGenerator`),
the spawn cells (greedy farthest-point over the surface, ties by (Q,R)), and the offline dummies'
decisions (`LootSeed.Derive(seed, "arena-ai:{unitId}")` → seeded `TacticalAI`). Unit ids are
roster-assigned 1..N (never `UnityEngine.Random`). The per-round **`ArenaStateHash`** (FNV-1a over
UnitId-ordered id/position/HP/facing/cooldowns/effects) is the lockstep safety net: commits
piggyback the previous round's hash and the host logs a loud error on mismatch.

### 2.5 Deterministic conflict rules (brief R12)

- **Order:** `IArenaResolutionOrder` strategy (replaceable — PO decision). Default
  `RotatingInitiativeOrder`: round N starts at index (N−1) mod aliveCount of the PlayerId-sorted
  commits and cycles — initiative rotates, nobody holds it permanently; within a unit, steps keep
  committed order.
- **Whiff:** ability steps fire at their frozen committed cells; nothing re-targets.
- **Same-hex moves:** a committed move fizzles iff its destination is invalid/occupied at its
  step — earlier in the order enters, later fizzles; a hex vacated earlier the same round can be
  entered. (Swap edge: the earlier unit fizzles, the later then succeeds.)
- **Mutual blows:** sequential — both land unless the earlier blow was lethal; a unit dead or
  stunned when its step arrives has its whole remaining commitment skipped.
- **Cooldowns:** reset per resolved ability step; decremented once at round end.
- **Win/draw:** `LastHeroStandingWinCondition` (armed after the full roster spawned) — one player
  with a living unit wins; zero is a draw (null winner, `Defeat` phase); remaining intents are not
  resolved after match end.
- **Departure:** the host folds departed players into the next bundle; their units die at
  normalization on every client — deterministic because it rides the bundle, never local timing.

### 2.6 DI wiring

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

Deliberately absent: narrative, mutation, loot, inventory, streaming-world installers.

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

### `ArenaMatchConfig`  (asset menu: `Yasherica → Arena → Match Config`)

Loaded from `Resources/Arena/ArenaMatchConfig` (or wired on the scene's `ArenaInstaller`).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_maxPlayers` | int (2–4) | Hard cap on players in a match | 4 |
| `_port` | int | Direct-connect port for the networked session | 7777 (consumed in Phase 3) |
| `_offlineDummyCount` | int (1–3) | AI dummies joining the local player in offline mode | 2 |
| `_offlineMatchSeed` | int | Offline match seed; 0 = fresh seed each launch (logged) | 0 |
| `_platformMaterial` | Material | Arena platform mesh material | the shared platform material; empty = plain lit fallback |

Referenced assets: the platform `Material` only. The hero and its abilities come from the
existing `HeroDefinition` / `AbilityDefinition` SOs (see `ability-subsystem.md`).

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### Tune the arena match

1. Open `Resources/Arena/ArenaMatchConfig.asset` (or create one via
   `Create → Yasherica → Arena → Match Config` and wire it on the scene's `ArenaInstaller`).
2. Set dummy count / seed / material per §3. A fixed `_offlineMatchSeed` reproduces the exact
   platform, spawns, and dummy decisions every launch.

### Change the arena hero or its abilities

Asset-only through the existing recipes: the hero is the `HeroDefinition` at
`Resources/Heroes/TestHeroDefinition` (stats + ability list), abilities are `AbilityDefinition`
assets (`ability-subsystem.md` §Adding Content). Every player spawns the same hero (brief R6).

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
- `MainMenuPresenterTests` — menu routing (Phase 1).

PvE regression: the combat suite (54 tests) stays green after the `RoundLifecycleProcessor`
extraction. Verified manually in play mode: menu → Arena → offline match vs dummies.

---

## 6. Known limitations / open points

- **Offline transport only.** The session is 1 local player + seeded dummies over
  `LoopbackArenaTransport`; host/join by address (brief R4–R5) is the next phase — the transport
  seam and `ArenaMatchHost` are already network-shaped (commit envelopes, canonical bundles,
  departed-player handling, hash piggyback).
- **Defeat/spectate UX.** A defeated local player's input simply goes dead (unit `CanAct` false);
  the explicit spectate presentation and Leave flow are the polish phase.
- **AI dummies fire on the PvE enemy cadence.** A dummy's schedule-action fires the same round
  (PvE enemy semantics), while humans build queues across rounds — the offline mode is a dev
  fallback, PvP (all-human) is symmetric by construction.
- **Arena camera is a fixed scene camera.** Framing at arena scale untuned (ROADMAP).
- **Initial facing is uniform** (east) rather than toward the platform center — deterministic but
  unpolished.
- **`EnemyIntent` naming.** The committed-intent machinery is player-agnostic; the PvE-shaped
  names (`EnemyIntent`, `RoundPhase.EnemyResolve`) are a deferred mechanical rename (ROADMAP).

> **Planned design (NOT implemented) — networked session (brief R4–R5) + polish.**
> Phase 3: NGO host/join by address — a single scene-placed `NetworkBehaviour` relay translating
> RPCs ↔ `IArenaTransport`; match setup (seed + roster) broadcast on start; per-round
> commit/bundle exchange; joined clients never assemble rounds (`_isHost` role seam in
> `ArenaCombatController`). Phase 4: match HUD (locked-in count, winner banner, Leave), defeat →
> spectate, disconnect end-to-end, hash-mismatch surfacing, edge-rule test sweep.
