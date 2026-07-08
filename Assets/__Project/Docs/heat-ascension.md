# Heat / Ascension (Track Y) — Requirements & Design

> Player-chosen difficulty as the cauldron's dare: before a run, the player accepts a **pact of
> independent rules-modifiers** (each 1–N ranks with authored Heat values; total Heat = the sum) at a
> cauldron F-spot on the Hub. The pact is locked for the run and pays out **entirely through the
> meta-progression spine (Track R)**: per-token min-Heat gates, accelerated unlock pacing (tier
> run-floor relief), and a lifted dig direction-bias — never past R's never-guarantee ceiling. No
> stat inflation, no new currency; Heat 0 reproduces today's game; the Arena is untouched.
> Consumed brief: `product-requirements/heat-ascension-difficulty.md`.
> Status: current as of 2026-07-08.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** Heat is a **menu of independent modifiers**, each with 1–N authored **rank steps**; a rank
  step contributes an authored Heat value and an effect magnitude (both per-step; a pact at rank k
  sums steps 1..k). The run's **total Heat = the sum** of every taken step's Heat value.
- **R2** The pact is chosen **at the Hub** (a dedicated cauldron F-spot, separate from the
  junk-keeper) and **locked for the run** at the portal commit; it rides the Continue image and is
  consumed with it on death.
- **R3** Choice is **free every run** — any affordable pact, including zero; no ratchet. Dialing a
  modifier cycles 0→1→…→max→0; a step the soft cap cannot afford is refused (the modifier wraps to
  0 on the next cycle instead), and the refusal is **never silent**: the capped card's hover says
  the next step is over the cap and why, and the seal card reads "Heat N / cap M" while a cap is
  active. Dialing DOWN is always possible.
- **R4** The **hottest cleared total Heat persists** as the Meta-horizon fact
  `world.heat_high_water` — written when a hot run survives to a savepoint at window index ≥ the
  configured clear floor, never lowered (the MVP "cleared" criterion; see §6).
- **R5** A meta token may carry an authored **min-Heat gate** (`MetaGatingAuthoring._minHeat` +
  `_heatKey`): it stays locked until the compared reading — the **current pact** or the
  **high-water record**, per the gate — meets the threshold. Without Heat installed the gate fails
  closed.
- **R6** Higher total Heat **accelerates R's pacing** — the effective tier run-floor is
  `max(0, FloorForTier(tier) − totalHeat × floorReliefRunsPerHeat)` — and **lifts the dig bias**
  (`BiasStrength + lift × totalHeat`, plus enabling the reserve-direction slot at a threshold).
  The **bias ceiling is untouchable**: the settings constructor re-clamps it below certainty
  whatever the heat. The lift is **dig-scoped** (R's bias is dig-only by design).
- **R7** Heat has **no reward outside R** — no currency, score, prestige, or cosmetic.
- **R8** Every modifier is a **rule/pool change, never a stat multiplier**. The shipped effect
  kinds: **EnemiesActFirst** (enemies resolve before the player every round),
  **RaisedCreatureFloor** (the D19 escalation tier is lifted at its one write site — pool shift,
  *which* creatures), **StingyCauldron** (fewer unseal variant options, floored at 1),
  **FewerSockets** (every blank carries fewer sockets, floored at 1).
- **R9** Modifiers **compose deterministically**: magnitudes of taken modifiers sum per effect
  kind into one immutable `HeatRules` lens; same (settings, pact) ⇒ same rules.
- **R10** The seams consume **per-system neutral rule records** (`CombatRuleModifiers`,
  `JourneyRuleModifiers`, `MutationRuleModifiers`), not Heat itself; the Area installer projects
  the pact onto them. Systems keep their own invariants (one write site for the tier fact, one
  blank source for socket counts, the turn-order strip consults the same lead rule as the flow).
- **R11** **Deterministic**: same seed + same pact ⇒ the same run; a pact change at the Hub
  re-deals the dig deterministically from the same seed. No unseeded randomness.
- **R12** **Save-correct & degrading**: the pact rides `run-setup.json` (Hub→Area) and `run.json`
  (every savepoint) as **additive fields at unchanged snapshot versions** — old files load to
  Heat 0, never quarantine. A persisted pact naming a re-authored menu degrades gracefully
  (unknown ids drop, ranks clamp, the total is always recomputed from the menu).
- **R13** **Data-authored & additive**: adding/re-valuing a modifier instance or Heat-gating a
  token is asset-only; content with no Heat data behaves as today; a missing `HeatConfig` asset
  degrades to an inert system (no cauldron spot, no gates satisfied, no relief).
- **R14** **Arena untouched**: Heat is never installed on the Arena container; the neutral rule
  records default everywhere Heat is absent.
- **R15** The pact is **framed by the cauldron's voice** (three new authored moments: the dare on
  opening, sealed-hot, declined-cold), picked by the existing deterministic FNV selector.

### 1.2 Non-functional requirements

- **N1** Heat Core is pure C# (no UnityEngine) and unit-tested; the dependency points one way —
  Heat → MetaProgression (R exposes the narrow `IHeatLens` seam; R never references `Heat.*`).
- **N2** All wiring through Zenject installers; consumers take optional deps defaulting to Neutral.
- **N3** `HeatConfig` is a data-only ScriptableObject.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Heat/
  Core/         — pure C#: HeatEffectKind, HeatRank, HeatModifier, HeatSettings, HeatPact,
                  HeatRules, IHeatLevels (+ AreaHeatLevels), MetaHeatLens, HeatDialAdjuster,
                  HeatFactReader
  Data/         — HeatConfig (SO) + HeatConfigMapper (the only SO → Core bridge)
  Integration/  — HeatHighWaterRecorder (savepoint observer), HeatPactDtoMapper,
                  HeatRulesProjection (pact → per-system rule records)
Scripts/Core/DI/HeatInstaller.cs          — settings binding (Hub + Area, never Arena)
Scripts/Hub/Core/HubHeatModel.cs          — the pact under construction (+ HubPanelArbiter,
                                            HubHeatLevels)
Scripts/Hub/Presenter/HeatPactPresenter.cs — the dare over the shared mutation card panel
```

Rule-record seams live in their own systems: `Combat/Core/CombatRuleModifiers.cs`,
`LevelGeneration/Journey/JourneyRuleModifiers.cs`, `Mutation/Core/MutationRuleModifiers.cs` (+
`Mutation/Core/SocketAdjustedBlankSource.cs`). R's side: `MetaProgression/Core/IHeatLens.cs`,
`HeatGateKey.cs`, and the `MinHeat`/`HeatKey` fields on `MetaGate`.

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `HeatEffectKind` | The four code-implemented rule seams a modifier can pull |
| `HeatRank` | One rank step: per-step Heat value + effect magnitude + rule text |
| `HeatModifier` | One menu entry: id, name, kind, rank steps; `HeatAtRank`/`MagnitudeAtRank` sum steps 1..k |
| `HeatSettings` | Validated image of the config: the menu + the R-mapping dials; `Defaults` = inert |
| `HeatPact` | The sealed per-run pact; `From()` degrades gracefully and recomputes the total |
| `HeatRules` | The composed rule lens (total, lead flag, tier lift, variant cut, socket cut) |
| `IHeatLevels` | Current pact total + high-water reading (Hub: live model; Area: fixed) |
| `MetaHeatLens` | Heat's implementation of R's `IHeatLens` (adds the floor-relief curve) |
| `HeatDialAdjuster` | Total Heat → derived `MetaProgressionSettings` (bias lift; ceiling untouched) |
| `HeatFactReader` | Pure high-water read straight from a persisted meta-fact snapshot |
| `HubHeatModel` | The pact being dialed at the cauldron: cycle/soft-cap/seal + staging events |
| `HubPanelArbiter` | Ownership of the ONE shared card panel (part offer vs pact cross-talk guard) |

### 2.3 Runtime flow

1. **Hub.** `HeatInstaller.InstallSettings` + `HubHeatModel` + a live `HubHeatLevels` →
   `MetaHeatLens` bound as R's `IHeatLens` **before** `MetaProgressionInstaller` (its binding probe
   picks the lens up). `HubSceneEntrypoint.PlaceCauldron` raises the cauldron F-spot (skipped when
   no menu is authored). F → `HeatPactPresenter.ShowPact()`: claims the panel arbiter and deals one
   card per modifier (rank = the rarity glow; pips + Heat in the name; the rank's rule text as a
   passive-ability hover) plus a closing **"Seal the pact — Heat N"** card. Confirming a modifier
   card cycles its rank and re-deals in place; the seal card closes the panel and fires
   `PactSealed`. Every `PactChanged` re-deals the **dig** through `HeatDialAdjuster` + the live
   lens (min-Heat gates and relieved floors re-answer; the chosen part resets) — same seed,
   deterministic per (pact, seed). The voice speaks the dare/sealed/declined moments.
2. **Launch.** `HubStagingPresenter.LaunchInto` writes the pact entries into `run-setup.json`
   alongside the part and biome.
3. **Area boot.** `RunStartConditions.Resolve` carries the pact (restore wins — the pact rides
   `run.json`); the installer builds `HeatPact` → `HeatRules` → the three per-system records +
   `AreaHeatLevels`/`MetaHeatLens` for the vocabulary. The four seams read their records:
   `RoundLeadPolicy` (flow + turn-order strip), `BiomeStretchDirector` (tier fact write),
   `MutationVariantPresenter` (option count), `SocketAdjustedBlankSource` (blank source decorator).
4. **Savepoints.** `RunStateService.TryCaptureAll` stamps the pact into every `run.json` write;
   `AutosaveService` then notifies `HeatHighWaterRecorder` (an `ISavepointObserver`) **before** the
   meta flush, so a new high-water fact rides that same flush.
5. **Death.** The run save (and the pact with it) is consumed; the high-water fact survives in
   `meta.json`.

### 2.4 DI wiring

- `HeatInstaller.InstallSettings(container)` — binds `HeatSettings` from
  `Resources/Configs/HeatConfig` via `HeatConfigMapper` (missing asset → `Defaults`). Called by
  `HubInstaller` and `AreaInstaller` **before** `MetaProgressionInstaller.Install`; **never** by
  `ArenaInstaller`.
- `MetaProgressionInstaller` probes `IHeatLens` with `HasBinding` — scenes without Heat get the
  pre-Track-Y vocabulary byte-identically.
- `AreaInstaller` — `HeatPact` (from `RunStartConditions.HeatPact` via `HeatPactDtoMapper`) →
  `HeatRules` → `IHeatLevels` (`AreaHeatLevels`; high-water via `HeatFactReader` over the meta
  store) → `IHeatLens` (`MetaHeatLens`) → `ISavepointObserver` (`HeatHighWaterRecorder`, passed
  into `AutosaveService`) → the three rule records via `HeatRulesProjection`.
- `HubInstaller` — `HubHeatModel`, `HubPanelArbiter`, `IHeatLevels` (`HubHeatLevels`, live),
  `IHeatLens`, `HeatPactCardStyle` (the config's ember tint), `HeatPactPresenter` (NonLazy).
- `MutationInstaller` — binds `IPartBlankDataSource` to `SocketAdjustedBlankSource` wrapping the
  catalog; without a bound `MutationRuleModifiers` (Hub/Arena/menu) it is a pure passthrough.

---

## 3. ScriptableObject Reference

### `HeatConfig`  (asset menu: `Create → Heat → Config`)

Loaded from `Resources/Configs/HeatConfig`. The ONE Heat balance surface — the modifier menu is
embedded here (deliberately small, R1/§6) rather than split into per-modifier SO assets.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_modifiers` | list | The menu; one row per modifier | see sub-rows below |
| `_modifiers[]._id` | string | Stable id — persisted in run pacts; renaming orphans saved pacts (they degrade cooler) | required (empty row dropped) |
| `_modifiers[]._displayName` | string | The pact card's name | |
| `_modifiers[]._kind` | `HeatEffectKind` | Which rule seam it pulls (see R8) | a new KIND is code, not data |
| `_modifiers[]._ranks[]._heatValue` | int ≥ 0 | Heat this step adds (per step, summed 1..k) | 1 |
| `_modifiers[]._ranks[]._magnitude` | int ≥ 0 | Effect this step adds (meaning per kind) | 1 |
| `_modifiers[]._ranks[]._description` | string | The rule text the player reads on hover | |
| `_floorReliefRunsPerHeat` | float ≥ 0 | Runs of tier run-floor relief per point of total Heat (R6) | 0.5 |
| `_biasStrengthLiftPerHeat` | float ≥ 0 | Additive dig `BiasStrength` lift per point of total Heat (R6) | 0.15 |
| `_reserveDirectionSlotMinHeat` | int ≥ 0 | Total Heat that enables the dig's reserve-direction slot; 0 = never | 4 |
| `_softCapTotalHeat` | int ≥ 0 | Total the hub pact refuses to exceed (a capped step is marked on the card); 0 = uncapped | 0 (owner call 2026-07-08: uncapped while the demo menu is playtested; the initial 9 forced a which-pain choice over the 11-point menu) |
| `_clearWindowFloor` | int ≥ 0 | Window index a hot run must reach at a savepoint to count as "cleared" (R4) | 2 |
| `_pactCardTint` | Color | The pact cards' (and the hub cauldron prop's) ember tint | ember orange |

### Heat fields on `MetaGatingAuthoring` (embedded on parts / artifacts / recipes / blanks)

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_minHeat` | int ≥ 0 | Minimum total Heat this token demands (R5); 0 = no Heat gate | 0; read only on Meta Gated tokens |
| `_heatKey` | `HeatGateKey` | What `_minHeat` compares against: `CurrentPact` or `HighWaterMark` | CurrentPact |

### `HubVoiceLinesConfig` — Heat pools (existing asset `Resources/Hub/HubVoiceLines`)

| Field | Meaning |
|---|---|
| `_heatDare` | Spoken when the pact panel opens (the tempter's dare) |
| `_heatSealed` | Spoken when the pact closes hot (total > 0) — a bargain |
| `_heatDeclined` | Spoken when the pact closes cold (total 0) — prudishness noted |

The related meta fact asset: `Resources/Narrative/Facts/MetaFact_HeatHighWater.asset`
(`world` / global / `heat_high_water` / Int / Meta horizon), registered in `DemoFactKeyRegistry`.

---

## 4. Adding Content

### Add or re-value a Heat modifier instance (asset-only)

1. Open `Resources/Configs/HeatConfig`.
2. Add a row to `_modifiers`: a **stable `_id`** (never rename a shipped id — saved pacts degrade),
   a display name, one of the four `_kind`s, and 1–N rank rows (per-step `_heatValue`,
   `_magnitude`, and a rule text the player can *feel and name*).
3. Tune the R-mapping dials (`_floorReliefRunsPerHeat`, `_biasStrengthLiftPerHeat`,
   `_reserveDirectionSlotMinHeat`) and the pact shape (`_softCapTotalHeat`) as needed.
4. Validate: enter the Hub, dare the cauldron, and confirm the new card cycles and the total sums.

### Heat-gate a token (asset-only)

1. Open the token's definition asset (a part, artifact, recipe, or part blank).
2. In its **Meta gating (Track R)** block: set `_mark` to `Meta Gated` (if not already), then set
   `_minHeat` to the demanded total and `_heatKey` to `CurrentPact` (play hot NOW) or
   `HighWaterMark` (have EVER cleared hot).
3. Reserve min-Heat gates for the rarer run-shaping tokens — never the base grammar (the meta-spine
   rule); base and early tokens stay open at Heat 0.
4. Validate: with a cold pact the token is absent from every draw surface; hot enough, it opens.

**Authoring constraints / gotchas:**

- A **new effect kind** (a new rule seam) is a **code change** — the kinds enumerate code-implemented
  seams; only instances/ranks/values over the existing kinds are data (R13).
- A modifier row with an empty id or no ranks is dropped by the mapper (with a warning).
- The tier lift can outrun authored content: a lifted floor only pays off if monster pools and
  story tier-bands are authored at the lifted tiers — check with `/balance-ledger` after raising
  `RaisedCreatureFloor` magnitudes.
- The variant and socket cuts floor at 1 by design — a stingier cauldron narrows choice, never
  removes it; authoring bigger magnitudes past the floor buys nothing.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `HeatPactTests` — total math over rank steps, unknown-id drop, rank clamp, duplicate collapse.
- `HeatRulesTests` — per-kind magnitude composition, `Neutral`, determinism.
- `HeatDialAdjusterTests` — bias lift; **ceiling never exceeded at absurd heat**; reserve-slot
  threshold; heat 0 returns the base instance.
- `HeatConfigMapperTests` — null asset → inert defaults; the authored demonstrator menu maps.
- `MetaVocabularyHeatTests` — min-Heat per key kind; fails closed without the lens; floor relief
  (never below 0); **null lens ⇒ byte-identical to the pre-Track-Y vocabulary** (the zero-diff
  guarantee).
- `HubHeatModelTests` — cycle up/wrap, soft-cap early wrap, the capped-step marker
  (`NextStepIsCapped` — never a silent refusal), events, seal, inert empty menu.
- `SocketAdjustedBlankSourceTests` — cut applied uniformly, floor 1, cut-0 passthrough (same
  instances), misses forwarded.
- `HeatHighWaterRecorderTests` — writes at/over the clear floor, never lowers, heat 0 never writes.
- `HeatPactPersistenceTests` — pre-Heat JSON loads to an empty pact at unchanged versions;
  round-trips; restore-wins carrier; DTO degradation recomputes the total.
- `RoundLeadPolicyTests` (extended) — always-lead every round; the default preserves the D2 rule.
- `BiomeStretchDirectorTests` (extended) — effective tier = authored + lift at the one write site;
  neutral zero-diff.
- `MutationVariantPresenterTests` (extended) — the stingy cut narrows the offer, floored at 1.
- `MetaGatingDemonstratorTests` (updated) — the Stinger recipe now demands its taste **and** a hot
  pact (min-Heat 2, CurrentPact).

Verified manually (owner play pass, pending): the cauldron F-spot and card feel, the visible dig
re-deal on a pact change, the voice lines, and the four rules felt in a real run.

---

## 6. Known limitations / open points

- **"Cleared" is a window-floor proxy.** There is no run-completion/victory event yet (the Track Z
  apex is unbuilt), so the high-water mark records at a savepoint at window ≥ `_clearWindowFloor`.
  Rebase onto the real run apex when Track Z lands (ROADMAP).
- **Rank cycling costs two clicks per step** — the shared card panel's two-step select→confirm is
  reused as-is (owner call: reuse, no fork); a dedicated rank affordance is presentation polish
  (ROADMAP).
- **No in-run HUD pact readout** — the pact is visible at the hub only; a "Heat: N" label in the
  Area HUD is deferred to avoid scene surgery (ROADMAP).
- **No per-modifier icons/art** — the pact cards are text + tint + rarity glow; art pass deferred
  (ROADMAP).
- **The modifier menu is embedded on the one config asset** — deliberate while the menu is a
  handful of rows; if the menu grows past legibility, split into per-modifier SO assets then.
- **Pacing relief is floor-relief only** — the rejected alternative (bumping the effective run
  count) would silently satisfy authored run-count *deeds* too; if a future design wants deeds to
  accelerate under Heat, that is a separate, explicit decision.
