# Cauldron Barks — Requirements & Design

> The cauldron's reactive, in-the-moment voice (P1-10): short lines barked at authored moments — a
> strong/monstrous mutation offered, a dark offer or the attack verb presented, a modest part taken,
> a socketing trend forming — whose tone shifts with the run's path lean. This is the **live bark
> channel**, deliberately separate from the curated spine reveal beats (P3-1/P3-3) about past hosts,
> which are capped, gated, and ride the reveal lane, never fired live.
> Status: current as of 2026-07-07 (Track H).
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.
>
> ⚠ **Verification status (2026-07-07):** code-complete and edit-mode green, but **not yet
> play-tested** — the on-screen timing/feel of the bark bubble is unconfirmed. Line content is
> RU placeholders in the cauldron register, pending an authoring pass.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** The voice fires a short bark at authored **slots**: **Temptation** (a strong/monstrous
  mutation offered), **DarkOffer** (the Monster-lean offer of a fork or the attack card presented),
  **Restraint** (a modest / marker part taken — the sour counterpoint), **SocketingTrend** (a
  consistent socketing pattern detected).
- **R2** A bark is a **short, terse line** in the plain chatter register; it never blocks input and
  self-hides after a display window.
- **R3** Each slot resolves its line against the player's current **path lean** — Indulgent
  (monster leads) vs Restrained (friendship leads / the start) — read from existing path facts, with
  **no new meter**: the lean is only ever `world.path_conquest > world.path_restraint`.
- **R4** Selection is **deterministic** under the run seed; consecutive barks from one slot do not
  repeat back-to-back where the pool allows it.
- **R5** Line content is **data-authored** (a pool keyed by slot × lean) plus a data-authored
  vocabulary of which reward-belonging ids read as "dark" (Monster-lean). A designer adds/edits lines
  and dark ids without code.

### 1.2 Non-functional requirements

- **N1** `CauldronBarkService`, `CauldronBarkLines`, `PathLeanEvaluator` are pure C# (no UnityEngine)
  and unit-tested.
- **N2** All wiring through Zenject; the service and presenter are constructor-injected; the triggers
  subscribe via `NonLazy` bindings.
- **N3** Lines are authored as one `CauldronBarkLinesConfig` ScriptableObject; no code per line.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Narrative/Barks/
  Core/   CauldronBarkSlot, BarkLean               — the slot + register enums
          CauldronBarkLines                        — slot×lean line pools + dark-belonging set (pure)
          ICauldronBarkService, CauldronBarkService — deterministic selection + OnBark event
          PathLeanEvaluator                        — lean from the two path facts (no meter)
  Data/   CauldronBarkLinesConfig, CauldronBarkLinesMapper — SO + the only SO→Core bridge
  View/   ICauldronBarkView, CauldronBarkView      — the self-hiding bubble (MonoBehaviour)
  CauldronBarkPresenter                            — relays OnBark → view
  SocketingTrendBarkTrigger                        — fires the trend slot off ISocketingTrendSource
```

The other three slots fire from their owning presenters: **Temptation** and **Restraint** from
`MutationVariantPresenter` (an offered variant at/above `MutationConfig._temptationRarityTier` is a
temptation; installing a race-marker or below-temptation-tier part is restraint, and also increments
`world.path_restraint`); **DarkOffer** from `EncounterCardHandPresenter` (once per encounter when the
hand first shows the attack card or an offer whose belonging is "dark").

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `CauldronBarkSlot` | The four authored moments: `Temptation`, `DarkOffer`, `Restraint`, `SocketingTrend`. |
| `BarkLean` | The register: `Restrained` (default / friendship leads) vs `Indulgent` (monster leads). |
| `CauldronBarkLines` | Pure line pools keyed `(slot, lean)`, with a missing-lean fallback to the other register (a half-authored slot still speaks; an empty slot stays quiet), plus the set of dark-belonging ids. |
| `PathLeanEvaluator` | `Evaluate(IFactStore)` → `Indulgent` iff `path_conquest > path_restraint` (a tie, including the untouched start, reads `Restrained` — boldness is earned). |
| `CauldronBarkService` | `Bark(slot)` resolves the line against the live lean, selects deterministically (FNV-1a over `(slot, lean, runSeed)` anchors the sequence; a per-slot fire counter walks the pool), and raises `OnBark`. `IsDarkBelonging(id)` exposes the vocabulary. |

### 2.3 Runtime flow

1. A system reports a moment: `MutationVariantPresenter` on unseal (Temptation) / on install
   (Restraint), `EncounterCardHandPresenter` on composing a hand with a dark offer (DarkOffer),
   `SocketingTrendBarkTrigger` on `ISocketingTrendSource.OnTrendChanged` with a formed direction
   (SocketingTrend).
2. `CauldronBarkService.Bark(slot)` re-reads the lean from the path facts, picks the pool for
   `(slot, lean)` (falling back to the other register, or staying quiet on an empty pool), selects a
   line deterministically, and raises `OnBark(line)`.
3. `CauldronBarkPresenter` relays the line to `ICauldronBarkView`; `CauldronBarkView` paints the
   bubble and hides it after `_displaySeconds`. A new bark restarts the window.

The lean is re-read at **every** fire, so the voice tracks the run live (bold as `path_conquest`
leads, sour as `path_restraint` retakes it). The Monster verb increments `path_conquest`
(`MonsterVerbConsequences`, quest-subsystem R11); a modest/marker install increments `path_restraint`.

### 2.4 DI wiring

`NarrativeSliceInstaller.InstallBarks()` maps `CauldronBarkLinesConfig` →
`CauldronBarkLines` (from `Resources/Narrative/CauldronBarkLines`; a missing config = quiet cauldron),
binds `ICauldronBarkService` to `CauldronBarkService` (seeded from the run seed), instantiates the
`Prefabs/UI/CauldronBarkView` prefab as `ICauldronBarkView` + `CauldronBarkPresenter` (`NonLazy`;
missing prefab logs a warning and the bubble is absent), and binds `SocketingTrendBarkTrigger`
(`NonLazy`). The `ISocketingTrendSource` seam it consumes is bound by `MutationInstaller` in the same
Area-scene container; the Temptation/Restraint/DarkOffer triggers resolve the service as an **optional**
constructor arg on `MutationVariantPresenter` / `EncounterCardHandPresenter`, so a scene without the
bark bindings simply never barks.

---

## 3. ScriptableObject Reference

### `CauldronBarkLinesConfig`  (asset menu: `Create → Narrative → Cauldron Bark Lines`)

Loaded from `Resources/Narrative/CauldronBarkLines`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_temptationIndulgent` / `_temptationRestrained` | string[] | Temptation lines per register. | empty = quiet |
| `_darkOfferIndulgent` / `_darkOfferRestrained` | string[] | Dark-offer / attack lines per register. | |
| `_restraintIndulgent` / `_restraintRestrained` | string[] | Restraint (modest/marker part) lines per register. | |
| `_socketingTrendIndulgent` / `_socketingTrendRestrained` | string[] | Socketing-trend lines per register. | |
| `_darkBelongingIds` | string[] | Reward-belonging ids that read as the Monster-lean currency (e.g. `power`); an offer card carrying one fires the DarkOffer slot. | demo: `power` |

A missing register (only one of the two lists filled for a slot) falls back to the other; a fully
empty slot is a silent moment, never an error.

---

## 4. Adding Content

### Add or retune bark lines

1. Open `Resources/Narrative/CauldronBarkLines` (or `Create → Narrative → Cauldron Bark Lines`).
2. For each slot you want the voice to react at, fill **both** the indulgent and restrained lists so
   the tone can track the lean (indulgent = bolder/proprietary, restrained = sour/clipped). Keep each
   line short and in the cauldron register (`design/narrative/cauldron-voice.md`, Variant A — no
   fourth-wall, never states what the cauldron *is*).
3. To make a reward belonging read as "dark" (firing the DarkOffer slot when its offer card shows),
   add its id to `_darkBelongingIds`.
4. No code change — the engine already fires each slot where the event is raised.

**Authoring constraints / gotchas:** do **not** author "past hosts" / tyrant / origin reveals here —
those are spine reveal beats (`director-meta-consumers.md`), capped and gated, not this live channel.
The lean facts `world.path_conquest` / `world.path_restraint` must be in the `FactKeyRegistry` (the
demo registry declares them).

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `CauldronBarkServiceTests` — the lean tracks the two path counters and flips a slot's register;
  the same seed replays the same sequence; consecutive fires cycle the pool with no back-to-back
  repeat; a missing-lean pool falls back to the other register; an unauthored slot stays quiet; the
  dark-belonging vocabulary is exactly the authored set.
- The DarkOffer trigger is covered in `CompetingOffersForkTests` (the bark fires once when the
  Monster-lean offer is presented); the Temptation/Restraint triggers on `MutationVariantPresenter`
  and the bubble view timing are **manual-verified in play mode** (pending the gameplay pass).

The full EditMode suite is **1605/1605 green** (2026-07-07).

---

## 6. Known limitations / open points

- ⚠ **Gameplay-untested (2026-07-07)** — the bubble timing/feel and the Temptation/Restraint triggers
  are not yet confirmed in play mode; line content is RU placeholder. Tracked in ROADMAP "Quests".
- The socketing-trend trigger fires on **every** formed-direction change with no throttle — a rapid
  re-socket could bark repeatedly (a cooldown is a follow-up).
- Voice **audio** is out of scope — text barks only (audio direction later, `design/audio/`).
- Hub / den presence lines are deferred (they depend on the hub voice SO, `hub-staging.md`).
- The full reactive line pool per beat × lean is content authoring that continues past this pass —
  the code establishes the slots + the data shape.

> **Planned design (NOT implemented).** A soft cooldown / recently-said suppression across slots, and
> a widening of the trigger set (e.g. a bark when a passport door opens/closes) as the fiction grows.
