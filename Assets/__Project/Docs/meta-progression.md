# Meta-Progression Spine — Requirements & Design

> The cross-run "unlock the vocabulary, never the power" layer (Track R core, the Isaac model —
> brief `product-requirements/meta-progression-spine.md`): content tokens (part forms, artifacts,
> recipes, blanks) can be **meta-gated** behind in-fiction **deeds**; the world remembers what the
> cauldron has tasted (the P2-2 `meta.json` store) and widens the possibility space of future runs.
> A **direction bias** — a sliding-window tally over recently installed parts and consumed
> reagents — raises the hub dig's odds toward the pursued direction, hard-capped below certainty.
> Status: current as of 2026-07-07.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

Numbered to mirror the brief's FRs where they map 1:1.

- **R1** A persisted meta vocabulary: unlocked tokens derive from Meta-horizon facts in `meta.json`
  (no separate unlocked list — the facts ARE the state), surviving death and quit.
- **R2** Base vs meta-gated is per-token data: every gateable SO carries a `MetaGatingAuthoring`
  block; an unmarked token defaults to **base** (existing content keeps working, FR15).
- **R3** A meta-gated, deed-unmet token is absent from **every** draw: the hub dig pool, biome loot
  tables (platform finds, enemy drops, quest tables), the quest-reward pools, the recipe book, and
  the mutation variant candidates.
- **R4** Deeds are authored fact predicates (the story/thread precondition grammar,
  `FactPredicateSerial`, AND over the list) over the meta facts. MVP channels: **tasting a form**
  (`world.<partId>.arena_tasted`, `$self` binds the owning token id) and **reveal-spine milestones**
  (`world.<storyId>.spine_seen`, `world.run_count`). Adding a deed = data only.
- **R5** Deed-satisfied ⇒ unlocked immediately (owner call 2026-07-07 — no progress meter); the
  FR11 pacing curve is per-`UnlockTier` **run floors**: a gated token needs `deed met AND
  effective run count ≥ floor[tier]`.
- **R6** The hub dig pool is **Tasted ∩ Eligible** (owner call 2026-07-07): tasting stays the dig's
  membership rule (run 1 = bare launch, O1 unchanged for unmarked content); the gate withholds
  tasted-but-locked forms.
- **R7** Direction bias: a sliding-window tally (last `DirectionWindowRuns` recorded runs, anchored
  at the newest) over two data axes — installed parts' race markers and consumed reagents'
  substance+property traits — weights the dig's tie-break.
- **R8** Never-guarantee ceiling: every biased weight is clamped so no single candidate's tie-pick
  share can reach the configured ceiling, itself hard-clamped `< 1` in Core — no configuration can
  make the dig deterministic (FR9).
- **R9** Dilution is real (FR10): a bigger unlocked pool spreads the draw; the dilution exponent is
  a tuning dial.
- **R10** Deterministic (FR13): the vocabulary is frozen per scene from the meta store on disk
  (same store + seed ⇒ identical pool/bias/offer); a deed done in run N changes run N+1.
- **R11** Graceful degrade (FR14): empty/corrupt meta ⇒ base-only vocabulary + neutral direction; a
  scene without the meta bindings (Arena) filters nothing; the game never crashes over the ledger
  (an old `meta.json` without it loads with an empty one — `MetaMemorySnapshot.CurrentVersion`
  **stays 1**, additive field only, because `JsonSaveFile` quarantines on version mismatch).
- **R12** The direction ledger: per-run installed part ids (equipped, on every `PartsChanged`) and
  consumed reagent artifact ids (at the unseal commit, `ISocketingModel.OnSocketsConsumed`) persist
  in `meta.json` (`MetaRunLedgerSnapshot`), raw ids only, pruned to `LedgerRetentionRuns`.
- **R13** One master tuning asset (`MetaProgressionConfig`) slides the whole curve
  generous↔minimal without a rebuild (FR: pacing floors, window, axis weights, bias strength +
  ceiling, dig shape, dilution, retention).
- **R14** A demonstrator gated set exists (FR16): 2 frame-changer parts + 2 recipes of existing
  content marked gated across both deed channels (§4.3).

### 1.2 Non-functional requirements

- **N1** Core logic is pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject; no service locators.
- **N3** Data definitions are data-only ScriptableObjects.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/MetaProgression/
  Core/         — GatingMark, MetaGate, SnapshotPredicateEvaluator, IMetaVocabulary/MetaVocabulary,
                  MetaProgressionSettings, EffectiveRunCount, RunLedger, DirectionTally/DirectionProfile
  Data/         — MetaGatingAuthoring (embedded SO block), MetaProgressionConfig (+ mapper)
  Integration/  — InstalledPartsLedgerRecorder, SocketedArtifactsLedgerRecorder,
                  MetaGateLootFilter, DirectionAxisProjection
Scripts/Core/DI/MetaProgressionInstaller.cs   — static Install(), called by AreaInstaller + HubInstaller
Scripts/Core/Persistence/MetaRunLedgerSnapshot.cs — the ledger DTO inside MetaMemorySnapshot
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `MetaGate` | Immutable per-token gate: mark, deed (list of `FactPredicate`), unlock tier, draw weight, plus the Track Y min-Heat gate (`MinHeat`, `HeatKey`) |
| `SnapshotPredicateEvaluator` | Evaluates deed predicates against the serialized meta facts; `$self` → token id; **fails closed** on missing facts / unresolvable context tokens (no registry defaults — deeds are earned facts) |
| `MetaVocabulary` | The frozen per-scene `IsUnlocked(tokenId, gate)` answer: base ⇒ true; gated ⇒ min-Heat (when authored) + tier run-floor + deed. Overrides `world.run_count` with the **effective run count** so run-counter deeds and floors read the run being served. Takes an optional `IHeatLens` (Track Y): min-Heat compares against the current pact or the high-water record per `HeatKey` (fails closed without the lens), and the effective floor is relieved by `FloorReliefRuns` (never below 0). Facts stay frozen — only the heat readings are live (the Hub lens follows the pact being staged) |
| `IHeatLens` / `HeatGateKey` | R's narrow Heat seam (implemented by `Heat.Core.MetaHeatLens`; bound only where Track Y is installed — Hub + Area) so the dependency points Heat → MetaProgression, never back |
| `EffectiveRunCount` | stored `run_count` + 1 for a fresh/upcoming run; stored as-is on a continue |
| `RunLedger` | In-memory direction ledger; idempotent recording; `Capture(retention)` emits a sorted, pruned, byte-stable snapshot |
| `DirectionTally` / `DirectionProfile` | Window tally over the ledger → normalized 0..1 race/trait scores; a candidate's match = the stronger axis read |
| `StartingPartSelector` (Hub.Core) | The dig draw; the Track-R weighted tie-break + ceiling clamp + dilution + optional direction-floor slot live here (uniform weights = the legacy pick, byte-identical) |

### 2.3 Runtime flow

**Area boot.** `MetaProgressionInstaller.Install` (from `AreaInstaller`) binds settings (SO→Core),
the `RunLedger` (preloaded from `meta.json`), and the frozen `IMetaVocabulary` (meta snapshot +
effective run count; a pending restore serves the stored count). Catalog projections consult it
once: `MutationPartCatalog` skips locked parts, `RecipeBookBuilder` (via `InventoryInstaller`)
skips locked recipes, `QuestRewardPoolsBuilder` (via `LootInstaller`) skips locked
artifacts/blanks, and `MetaGateLootFilter` joins the `ILootEntryFilter` chain. During play the two
recorders feed the ledger; every meta flush (autosave / defeat / quit) persists it through the
extended `MetaMemoryFlushService`.

**Hub boot.** Same installer (no restore context ⇒ the vocabulary serves the **upcoming** run =
stored count + 1). `HubInstaller` additionally binds the `DirectionProfile` (ledger ×
`DirectionAxisProjection` maps × settings) and a read-only artifact catalog (the Hub has no
`InventoryInstaller`). `HubStartingPoolSource` builds Tasted ∩ Eligible with per-candidate trait
ids + draw weight; `HubStagingPresenter` draws `DigOfferSize` cards through the biased selector.

**The tie-break math.** `w = pow(drawWeight · (1 + biasStrength · directionScore),
dilutionExponent)`, then every weight is clamped to `ceiling/(1−ceiling) · Σ(others)` (iterated to
a fixpoint), making any single pick share provably `< ceiling < 1`. Uniform weights take the legacy
`NextInt(tieSet.Count)` path — the zero-diff guarantee for O1.

### 2.4 DI wiring

`MetaProgressionInstaller.Install(DiContainer)` — requires `PersistenceInstaller` on the same
container; loads `Resources/Configs/MetaProgressionConfig` (missing asset ⇒ Core defaults). The
vocabulary probes `RunRestoreContext` (Area-only binding) to decide fresh-vs-continue. Ledger
recorders are Area-bound (`AreaInstaller.InstallPersistenceBindings`, next to
`TastedFormsRecorder`). Scenes without these bindings (Arena, MainMenu) resolve null vocabularies
via `TryResolve` and filter nothing.

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

### `MetaProgressionConfig`  (asset menu: `Create → MetaProgression → Config`)

Loaded from `Resources/Configs/MetaProgressionConfig`. One asset; sliding it moves the game
generous↔minimal with no rebuild. Exact values are playtest tuning (§6).

| Field | Type | Meaning | Default |
|---|---|---|---|
| `_unlockTierRunFloors` | int[] | Earliest run (`world.run_count`) at which each unlock tier may open; index = a token's `_unlockTier`; tiers past the end share the last floor | `[1, 1, 5, 12]` |
| `_directionWindowRuns` | int | How many recent recorded runs the direction tally reads | 4 |
| `_raceAxisWeight` / `_artifactAxisWeight` | float | The two axes' relative weight in the tally | 1 / 1 |
| `_biasStrength` | float | How strongly direction raises a matching candidate's draw weight; 0 = off | 1 |
| `_biasCeiling` | float (0–0.95) | Max share of a draw any single candidate can reach; re-clamped `< 1` in Core | 0.75 |
| `_digOfferSize` | int | Cards the hub dig offers | 3 |
| `_reserveDirectionSlot` | bool | Floor rule: swap the weakest pick for the best direction match when the offer has none | off |
| `_dilutionExponent` | float | Exponent over the final weights (sharpen/flatten) | 1 |
| `_ledgerRetentionRuns` | int | Past runs the ledger keeps (never below the window) | 8 |

### `MetaGatingAuthoring`  (embedded block — not a standalone asset)

Hosted under a `Meta gating (Track R)` header on `PartDefinition`, `ArtifactDefinition`,
`RecipeDefinition`, and `PartBlankDefinition`. The serialized defaults ARE the unmarked/base state.

| Field | Type | Meaning | Default |
|---|---|---|---|
| `_mark` | GatingMark | `Base` (from run 1) / `MetaGated` (absent until the deed is met) | Base |
| `_deed` | List\<FactPredicateSerial\> | The earning milestone (AND over the list; empty = the tier run-floor alone gates). Subject `$self` = the owning token id; a literal subject (a part/story id) reads that entity's fact | empty |
| `_unlockTier` | int | Indexes `_unlockTierRunFloors` (FR12 stratification: 0 = small/early, high = late run-shaping) | 0 |
| `_drawWeight` | float | Relative draw weight once unlocked (the dig tie-break) | 1 |
| `_minHeat` | int | Minimum total Heat the token demands (Track Y, `heat-ascension.md` R5); 0 = no Heat gate | 0 |
| `_heatKey` | HeatGateKey | What `_minHeat` compares against: `CurrentPact` / `HighWaterMark` | CurrentPact |

**Token id per SO type:** a part's `_id`; an artifact's `_id`; a blank's `_id`; a **recipe's token
id is its OUTPUT artifact's id** (recipes have no id field — the convention lives in
`RecipeBookBuilder`).

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### 4.1 Gate a token (mark existing content meta-gated)

1. Open the content asset (part / artifact / recipe / blank) and find `Meta gating (Track R)`.
2. Set `_mark` = `Meta Gated`.
3. Author the deed (§4.2) — or leave it empty and set `_unlockTier` so the pacing run-floor alone
   gates it ("opens from run N").
4. Pick `_unlockTier` per stratification: 0–1 for small early breadth, 2+ for run-shaping unlocks.
5. Done — every draw surface consults the gate automatically. No code, no registration.

### 4.2 Author a deed (the two MVP channels)

*Channel 1 — taste a form:* one predicate, namespace `World`, subject `$self` (the token unlocks by
tasting itself) **or** a literal part id (e.g. a recipe gated on tasting `part.spine.serpent`), key
`arena_tasted`, op `Eq`, value Bool `true`.

*Channel 2 — reveal-spine / run-counter milestones:* namespace `World`, subject = the story id
(key `spine_seen`, Eq true) or empty subject with key `run_count`, op `Gte`, Int value N.

Any other **Meta-horizon** fact key works the same way — adding a deed channel is authoring a
predicate, not code. A deed over a Run-horizon fact never persists, so it can never unlock: keep
deeds on Meta facts.

### 4.3 The demonstrator gated set (FR16, shipped)

| Asset | Deed | Tier |
|---|---|---|
| `Part_LegsSpider` | `world.run_count >= 3` (channel 2) | 1 |
| `Part_SpineSerpent` | `world.story_spine_cauldron_hint.spine_seen` (channel 2) | 1 |
| `Recipe_FireWater_Snake` | `world.part.spine.serpent.arena_tasted` (channel 1) | 1 |
| `Recipe_NeedleBacteria_Stinger` | `world.part.legs.spider.arena_tasted` (channel 1) | 1 |

### 4.4 Tune generous ↔ minimal

Edit `Resources/Configs/MetaProgressionConfig.asset` only: lower floors / fewer gated marks =
generous; higher floors, stronger bias, sharper dilution = minimal/directed. No rebuild.

**Authoring constraints / gotchas:** never gate the base grammar (the three races' base parts, core
artifacts/traits, base tiers) — gate edges/axes only; never author a guarantee (the ceiling clamps
anyway); a `MetaGated` token with an empty deed and tier 0 unlocks from run 1 (floor `[0] = 1`) —
that is effectively base, so give it a real deed or tier.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `MetaVocabularyTests` / `SnapshotPredicateEvaluatorTests` — unlock semantics, `$self` binding,
  fail-closed evaluation, tier floors, degrade, determinism.
- `MetaProgressionConfigMapperTests` — ceiling clamp `< 1`, null-SO defaults, retention ≥ window.
- `RunLedgerTests` — idempotence, sorted/pruned capture, round-trip, `EffectiveRunCount`.
- `MetaMemorySnapshotCompatTests` — a version-1 `meta.json` **without** the ledger loads with an
  empty one (no quarantine); round-trip through the save serializer.
- `DirectionTallyTests` — window slide, axis weights, unknown-id skip, normalization, determinism.
- `StartingPartSelectorBiasTests` — the legacy-draw zero-diff regression, bias raises frequency,
  ceiling holds under extreme settings (incl. multi-dominant), dilution, exponent, the
  reserve-slot floor, seeded determinism.
- `MetaGatingSurfaceTests` — per-surface gating (mutation catalog, recipe book, quest pools, loot
  filter, hub Tasted ∩ Eligible) incl. null-vocabulary pass-through.
- `MetaGatingDemonstratorTests` — the FR16 assets asserted end-to-end: deed in run N ⇒ token
  possible in run N+1; unmarked siblings stay base.

Verified manually (play mode): the felt dig bias and the demonstrator unlock beats (§4.3) on a
fresh profile.

---

## 6. Known limitations / open points

- **Progress-meter pacing alternative (dropped for MVP).** The brief's "deed→progress contribution
  weights" dial is not built — pacing = deed authoring + tier run-floors. Revisit if playtest wants
  a smoother global pace. *(ROADMAP)*
- **The vocabulary is frozen per scene.** A deed done mid-run opens the token only from the next
  scene build (by design, FR13); a "newly unlocked" toast (Track T readout) would diff two
  snapshots. *(ROADMAP — Track T)*
- **Bias is dig-only.** World loot / quest rolls consult eligibility but not the direction bias
  (per the brief — the dig is the MVP bias home). *(ROADMAP)*
- **Arena draft ignores gates.** The arena board is the tasted-catalog union (its own budget
  guard); a tasted-but-gated form can appear there. Owner call whether Arena should consult the
  vocabulary. *(ROADMAP)*
- **`ArenaTastedCatalogReader` relocation.** Now three cross-system consumers (Arena, Hub, this
  spine) — the shared-home move is overdue debt. *(ROADMAP, pre-existing)*
- **Wiped-meta edge.** Deleting `meta.json` under an existing `run.json` can leave the rack holding
  a now-locked blank; socketing rejects it gracefully (no crash), the blank is dead weight.
- **Exact tuning values** (floors, ceiling, window, dilution) are playtest tuning; defaults are the
  owner's generous starting stance.
