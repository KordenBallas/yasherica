# Mutation Subsystem — Requirements & Design

> The mutation subsystem implements the **Socketed Blanks** model
> (`product-requirements/crafting-mutation-socketed-blanks.md`): the player **builds the organ**.
> A **Part-Blank** (a socketed recipe carrying the organ's character slot + a species/passport
> marker) sits in a scarce **rack** left of the cauldron; the player **drags artifacts into its
> sockets** on the one open-inventory screen; **filling the last socket unseals** the blank into a
> small **menu of variant mutations** (authored body parts of the blank's slot, deterministically
> scored against the socketed reagents' traits); the pick installs via the character system's
> `SwapPart`, consumes the reagents, and spends the blank (commit-on-unseal). The variant scoring
> runs the socketed profiles through the **same emergent fusion grammar as the cauldron**
> (`inventory-subsystem.md` R16), so sockets interact — an emergent third property counts.
> Part-derived ability grants are unchanged: a swapped part changes the character's combat ability
> set because combat rebuilds that set from the live equipped parts at combat start
> (ability-subsystem.md §2.6). The earlier feed→tally→digestion→stage-up loop is **deleted**
> (2026-07-02). Status: current as of 2026-07-02.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- R1. **Blanks are the only mutation source.** A mutation is obtained by socketing artifacts into a
  racked Part-Blank and unsealing it; there is no other path (feeding is removed).
- R2. **The blank fixes type, not ability.** A blank honestly determines the organ (its character
  slot) and the socket count; the *ability* is shaped by the reagents. The blank also carries the
  **species/passport marker** (an archetype id) — the reagents never retag species.
- R3. **Scarce rack.** Blanks live in a capped rack (`MutationConfig.BlankRackCapacity`), separate
  from the cauldron's artifact inventory; the cap is the multi-track incubation tension.
- R4. **Socketing over the inventory.** Socketing pulls the artifact out of the inventory (the
  crafting-staging pattern); unsocketing returns it. Rearranging is free **only while a socket is
  still open** — filling the last socket is the commit (auto-unseal; no rearrange after).
- R5. **Unseal → menu → pick.** Unsealing offers up to `MutationConfig.MaxVariantOptions` variants:
  non-equipped authored parts of the blank's slot, scored by trait-affinity overlap with the
  socketed reagents' combined (post-grammar) profile × a rarity gate that unlocks with the combined
  tier. Deterministic (same sockets → same menu); **no zero-score filter** — raw-only socketing is
  weak, never dead.
- R6. **Commit-on-unseal.** Picking a variant swaps the part on the live character
  (`IMutationCharacter.SwapPart`), consumes the socketed artifacts, and removes the blank; the
  unchosen variants are lost. A failed swap keeps the menu up for a retry.
- R7. **Sockets interact.** The socketed profiles are combined through the cauldron's
  `TraitFusionRuleSet` grammar before scoring, so authored rules can add emergent traits the parts'
  affinities respond to.
- R8. **Trend seam.** Every socket change publishes the combined post-grammar trait trend
  (`ISocketingTrendSource`) — the hint channel the cauldron voice will speak from (no consumer yet).
- R9. **Data-driven content.** New blanks, archetypes, and variant parts are added as assets only
  (§4); startup validation warns about broken authoring, never crashes.
- R10. **Incubation persists.** Socket state survives inventory open/close (several blanks ripen in
  parallel across excursions).

### 1.2 Non-functional requirements

- N1. The domain (`Scripts/Mutation/Core/`) is **pure C#** (no UnityEngine) and edit-mode tested.
- N2. Zenject-only wiring (`MutationInstaller`); MVP: pure-C# presenters, thin MonoBehaviour views.
- N3. ScriptableObjects carry data only; SO → Core conversion happens in explicit catalogs.

---

## 2. Architecture

### 2.1 Layer map

| Layer | Responsibility | Key files |
|---|---|---|
| Core (pure C#) | Blank data/instances, the capped rack, socketing state, variant scoring, trend seam, character port | `Scripts/Mutation/Core/`: `PartBlankData.cs`, `IPartBlankDataSource.cs`, `BlankInstance.cs`, `IBlankRack.cs`/`BlankRack.cs`, `ISocketingModel.cs`/`SocketingModel.cs`, `IBlankVariantBuilder.cs`/`BlankVariantBuilder.cs`, `VariantScoringParameters.cs`, `MutationOption.cs`, `MutationCandidatePart.cs`, `SocketingTrend.cs`/`ISocketingTrendSource.cs`/`SocketingTrendEvaluator.cs`, `IMutationCharacter.cs` |
| Data (SO + bridges) | Authoring assets and the SO→Core catalogs | `Scripts/Mutation/Data/Definitions/`: `PartBlankDefinition.cs`, `ArchetypeDefinition.cs`, `MutationConfig.cs`; `Scripts/Mutation/Data/`: `PartBlankCatalog.cs` (`IPartBlankCatalog`), `ArchetypeCatalog.cs` (`IArchetypeCatalog`), `MutationPartCatalog.cs` (`IMutationPartCatalog`) |
| Application | Startup authoring validation | `Scripts/Mutation/Application/MutationContentValidator.cs` |
| Presenter (pure C#) | Rack rendering + socketing gestures; the unseal variant choice | `Scripts/Mutation/Presenter/BlankRackPresenter.cs`, `MutationVariantPresenter.cs` |
| View (thin MonoBehaviour) | Rack entries/sockets on the inventory stage; the variant card panel | `Scripts/Mutation/View/`: `IBlankRackView.cs`/`BlankRackView.cs`, `BlankEntryView.cs`, `SocketView.cs`, `BlankRackViewData.cs`, `IMutationChoiceView.cs`/`MutationChoiceView.cs`, `MutationChoiceButton.cs`, `MutationChoiceViewData.cs` |
| Infrastructure | Adapter onto the live modular character | `Scripts/Mutation/Infrastructure/ModularCharacterMutationAdapter.cs` |

Cross-system reuse: the variant builder and trend evaluator consume the **Inventory** fusion grammar
(`EmergentFusionCalculator`, `TraitFusionRuleSet`, `FusionSettings`, `IArtifactTraitSource` — see
`inventory-subsystem.md` §2.2); part trait affinities live on the CharacterSystem `PartDefinition`
(§3, layering note in §6).

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `PartBlankData` | Pure record of an authored blank: definition id, display name, slot id (the organ), species archetype id (passport marker + card tint), socket count. |
| `IPartBlankDataSource` | Core port onto the authored blank pool (implemented by `PartBlankCatalog`). |
| `BlankInstance` | One racked blank: unique `InstanceId` + `DefinitionId` (its own id space — a blank never enters the artifact inventory). |
| `IBlankRack` / `BlankRack` | Capped blank container (`TryAdd`/`Remove`/`TryGet`, `OnChanged`). |
| `ISocketingModel` / `SocketingModel` | Socketing state per blank: `TrySocket` pulls the artifact from `IInventoryModel`; `TryUnsocket` returns it (rejected once full — **the last drop is the commit**); filling the last socket raises `OnBlankReady`; `ConsumeSockets` destroys the reagents on pick; `ReturnAll` is a reset seam (deliberately not wired to inventory close — R10). |
| `IBlankVariantBuilder` / `BlankVariantBuilder` | The unseal menu: combined post-grammar target profile → each non-equipped candidate of the blank's slot scores `Σ traitAffinity[t] for t ∈ target` × rarity multiplier `1 + RarityWeight·tier·unlock`, where `unlock = clamp01(targetTier / (rarityTier·TierUnlockPerRarityTier))`. Deterministic: score desc, ordinal part-id tie-break; top `maxOptions`; no zero filter. Options carry the blank's slot + species archetype. |
| `MutationCandidatePart` | A part in Core terms: slot/part ids, display label, rarity as int tier, trait-affinity map (built by `MutationPartCatalog`). |
| `MutationOption` | One offered variant (slot id, part id, blank species archetype id for the tint, display name). |
| `SocketingTrend` / `ISocketingTrendSource` / `SocketingTrendEvaluator` | The cauldron-voice seam: recomputes a blank's post-grammar trait profile on every socket change and raises `OnTrendChanged`. No consumer ships yet (ROADMAP). |
| `IMutationCharacter` | Port onto the live character: `SwapPart(slotId, partId)`, `TryGetEquippedPartId(slotId)`. Implemented by `ModularCharacterMutationAdapter` over `ModularCharacterVisual` (lazy — an unassembled rig just fails the swap). |

### 2.3 Operating-table UI (the rack left of the cauldron)

Crafting and the operating table share **one screen** — the open cauldron stage; there is no mode
switch. World-space rack objects live on `InventoryStage.prefab` under `BlankRackArea` (local X left
of the cauldron, layer `InventoryFocus`): three `BlankAnchor`s and a disabled `BlankEntryTemplate`
(icon quad + code-built 3D-TMP name label + a disabled `SocketTemplate` cloned per socket).

- **`IBlankRackView` / `BlankRackView`** — instantiates one `BlankEntryView` per racked blank at its
  anchor; forwards drop/click events. **`BlankEntryView`** — icon quad (species tint when no icon is
  authored), name label, socket row sized to the blank's socket count. **`SocketView`** — a
  translucent disc (species tint; brighter when filled) showing the socketed artifact's icon;
  implements `IStageClickable` (click a **filled** socket = unsocket gesture) and
  `IArtifactDropTarget` (drag release).
- **`StageDragRouter`** (Inventory view layer; renamed from `StageClickRouter`, same meta GUID):
  press-and-release below `_dragThresholdPixels` = click → `IStageClickable`; pressing a pot bubble
  and moving past the threshold = **drag** — the bubble follows the pointer on its camera-distance
  plane (`BubbleView.IsDragged`; the pot drift skips it, its collider turns off so the release
  raycast sees the socket under it); release over an `IArtifactDropTarget` drops the artifact,
  anywhere else snaps the bubble back into the pot.
- **`BlankRackPresenter`** (pure C#, NonLazy) — seeds `MutationConfig.StartingBlanks` into the rack
  (only while empty), rebuilds the rack view on every rack/socket change, and routes drops →
  `TrySocket` / filled-socket clicks → `TryUnsocket`. Rejections (full/committed blank, item not in
  the inventory — e.g. a dragged *staged* crafting bubble) are logged and no-op.

### 2.4 Unseal variant choice

`MutationVariantPresenter` (pure C#, NonLazy) reuses the `IMutationChoiceView` panel
(`MutationChoicePanel/Button` prefabs) as the variant-card menu:

1. **Trigger** — `ISocketingModel.OnBlankReady`. Blanks that ripen while a menu is showing queue and
   open after the pick.
2. **Build** — socketed profiles (via `IArtifactTraitSource`) → `IBlankVariantBuilder` against
   `IMutationPartCatalog.AllCandidates`, excluding the part equipped in the blank's slot; top
   `MutationConfig.MaxVariantOptions`. Card tint = the blank's species archetype `Tint`; icon = the
   part's `ChoiceIcon`.
3. **Pick** — `IMutationCharacter.SwapPart`; on success `ConsumeSockets` + `IBlankRack.Remove` +
   hide. A failed swap keeps the cards up. An empty menu (nothing authored for the slot) is logged
   and skipped — the validator warns about such blanks at startup.

### 2.5 DI wiring

`Scripts/Core/DI/MutationInstaller.cs` (a `MonoInstaller` on the Area scene's `SceneContext`):

- `IArchetypeCatalog` → `ArchetypeCatalog` (definitions auto-load from `Resources/Mutation/Archetypes`).
- `MutationConfig` bound via `BindInstance` (auto-load from `Resources/Mutation/MutationConfig`;
  missing config fails fast).
- `IMutationPartCatalog` → `MutationPartCatalog` (builds candidates from the CharacterSystem
  `IPartCatalog`).
- `ModularCharacterVisual` `FromComponentInHierarchy`; `IMutationCharacter` →
  `ModularCharacterMutationAdapter`.
- `IMutationChoiceView` → `MutationChoiceView` `FromComponentInNewPrefab`
  (`Resources/Prefabs/UI/MutationChoicePanel`, inspector override supported). If the prefab is
  missing the installer **logs a warning and skips** the view + `MutationVariantPresenter` (the rest
  of the scene runs).
- **Socketed Blanks**: `IPartBlankCatalog` + `IPartBlankDataSource` → one `PartBlankCatalog`
  (auto-load from `Resources/Mutation/Blanks`); `IBlankRack` → `BlankRack`
  (`MutationConfig.BlankRackCapacity`); `ISocketingModel` → `SocketingModel`; `IBlankVariantBuilder` →
  `BlankVariantBuilder` (consumes the InventoryInstaller's fusion bindings from the shared
  `SceneContext`); `SocketingTrendEvaluator` via `BindInterfacesAndSelfTo` + `NonLazy`;
  `IBlankRackView` → `BlankRackView` `FromComponentInHierarchy` (on the InventoryStage scene
  instance) and `BlankRackPresenter` via `BindInterfacesAndSelfTo` + `NonLazy`.
- `MutationContentValidator` via `BindInterfacesAndSelfTo` + `NonLazy` (§4 authoring warnings).
- `IGameLogger` is **not** bound here — `InventoryInstaller` provides the single `UnityGameLogger`
  for the shared `SceneContext`.

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

### `PartBlankDefinition`  (asset menu: `Create → Mutation → Part Blank`)

Loaded from `Resources/Mutation/Blanks/` (or wired into the `MutationInstaller` list field).

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `Id` | string | Stable identity (e.g. `blank.skull`). | empty/duplicate ids fail fast in `PartBlankCatalog` |
| `DisplayName` | string | Human-readable name for the rack entry. | empty → falls back to the asset name |
| `Description` | string | Flavor / designer note. | `[TextArea]` |
| `Slot` | `SlotDefinition` | The character slot the unsealed mutation installs into — the blank honestly fixes the organ. | missing → validator warning (can never unseal) |
| `SpeciesArchetypeId` | string | Passport marker (`ArchetypeDefinition.Id`); also the rack tint and variant-card tint. | unknown/empty → validator warning |
| `SocketCount` | int | Artifacts to socket; filling the last socket unseals. | `2`; `[Min(1)]` |
| `Icon` | Sprite | Rack icon (species tint stands in when none). | none |

Shipped blanks: `blank.skull` (head, reptile, 2 sockets), `blank.arm.left` (arm, insect, 1),
`blank.leg.left` (leg, mammal, 2).

### `ArchetypeDefinition`  (asset menu: `Create → Mutation → Archetype`)

Loaded from `Resources/Mutation/Archetypes/`. The authorable species set — blanks' passport markers.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `Id` | string | Stable identity referenced by `PartBlankDefinition.SpeciesArchetypeId`. | empty id fails fast in `ArchetypeCatalog` |
| `DisplayName` | string | Human-readable name for UI. | — |
| `Description` | string | Flavor / designer note. | `[TextArea]` |
| `Tint` | Color | The species colour: rack entries, socket rings, variant cards. | white |

Shipped archetypes: `reptile`, `insect`, `aquatic`, `mammal`, `avian`.

### `MutationConfig`  (asset menu: `Create → Mutation → Mutation Config`)

Single asset loaded from `Resources/Mutation/MutationConfig.asset`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `RarityWeight` | float | How strongly a part's rarity tier multiplies its variant score once unlocked. | `0.5`; `[Min(0)]` |
| `BlankRackCapacity` | int | How many Part-Blanks the rack holds at once. | `3`; `[Min(1)]` |
| `MaxVariantOptions` | int | How many variants an unsealed blank offers at most. | `3`; `[Min(1)]` |
| `TierUnlockPerRarityTier` | float | Socketed target tier required per rarity tier before rare variants are favoured. | `1`; `[Min(0)]` |
| `StartingBlanks` | `PartBlankDefinition[]` | Blanks seeded into the rack at startup (dev seed until blanks drop as loot). | shipped: skull / claw-arm / haunch |

### Per-part mutation data on `PartDefinition`  (CharacterSystem — `Create → Character System/Part`)

The variant data lives on the body part itself (see `character-system.md` for the full
`PartDefinition` reference). `MutationPartCatalog` reads every part from the CharacterSystem
`IPartCatalog`.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `TraitAffinities` | `TraitAffinity[]` | Per-trait affinity (`TraitId` referencing an Inventory `TraitDefinition.Id` + `Weight` 0..1) scored against the socketed reagents at unseal. Duplicate ids summed; empty ids / weights ≤ 0 dropped. | empty → part scores 0 as a variant (still offered — no zero filter) |
| `Rarity` | `MutationRarity` | Rarity tier (`Common`…`Mythical`, ordinal 0..5); higher tiers are favoured once the socketed tier is high enough. | `Common` |
| `ChoiceIcon` | Sprite | Icon shown on the variant card when this part is offered. | none |
| `DisplayName` | string | Friendly label on the card (general `PartDefinition` field). | empty → falls back to the asset name |

`MutationContentValidator` warns at startup if a part trait affinity names an empty/unknown trait
id, or if a blank has a broken slot, an unknown species archetype, or fewer than two candidate
parts for its slot.

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### Add a Part-Blank

1. `Create → Mutation → Part Blank` under `Resources/Mutation/Blanks/`.
2. Set a unique `Id` (e.g. `blank.tail`), `DisplayName`, `Description`, the **Slot** (which organ it
   grows — reference a `SlotDefinition`), the **Species Archetype Id** (an existing
   `ArchetypeDefinition.Id` — the passport marker and tint), and the **Socket Count**.
3. Make sure at least two `PartDefinition`s exist for that slot with **Trait Affinities** authored
   (below), or the unseal menu is no choice — the validator warns.
4. To have it in the rack at startup, add it to `MutationConfig.StartingBlanks` (dev seed until
   blanks drop as loot).

### Author a part's trait affinities (make it an unseal variant)

On the `PartDefinition` asset, under **Mutation (part-driven affinity)** → **Trait Affinities**, add
an entry per function trait the part expresses: `Trait Id` (an existing Inventory
`TraitDefinition.Id`, e.g. `sharp`) and `Weight` (0..1). Set **Rarity** (rarer parts need
higher-tier reagents to be favoured — `TierUnlockPerRarityTier`) and optionally **Choice Icon** /
**Display Name**. No registration step: `MutationPartCatalog` reads every part automatically.

### Add an archetype (species)

1. `Create → Mutation → Archetype` under `Resources/Mutation/Archetypes/`.
2. Set a unique `Id` (lower-case, stable), `DisplayName`, `Description`, and `Tint`.
3. Duplicate or empty ids fail fast at startup.

### Tune the puzzle

`Resources/Mutation/MutationConfig.asset` — rack capacity (multi-track pressure), max variant
options (menu size), rarity weight + tier-unlock (how hard rare variants gate on reagent potency).
The trait vocabulary and fusion rules the sockets speak are Inventory content
(`inventory-subsystem.md` §4).

### Wire the variant card panel

Run **Tools → Mutation → Setup Stage-Up Choice UI** once: it builds
`Resources/Prefabs/UI/MutationChoicePanel.prefab` + `MutationChoiceButton.prefab`; the installer
instantiates the panel at runtime (no scene wiring). Until the prefab exists the unseal choice is
disabled (one startup warning, no crash). The rack itself is authored into `InventoryStage.prefab`
(`BlankRackArea`) — no editor tool needed.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `ArchetypeCatalogTests` — `TryGet`/`Contains`/`All`; throws on empty id, duplicate id, null list.
- `BlankRackTests` — capacity floor throws; unique instance ids + `OnChanged`; add rejected at
  capacity / on empty id; remove frees capacity; `TryGet`.
- `SocketingModelTests` — socketing moves the artifact out of the inventory; unknown blank/artifact
  rejected; filling the last socket raises `OnBlankReady` (incl. a 1-socket blank); a full blank
  rejects further socketing **and unsocketing** (the last drop is the commit); unsocket returns to
  the inventory while open; `ConsumeSockets` destroys without returning; `ReturnAll` refunds.
- `BlankVariantBuilderTests` — ranks by trait-affinity overlap; filters to the blank's slot;
  excludes equipped; zero-scoring parts still offered; a fusion rule's emergent trait counts toward
  affinity (sockets interact); high-tier sockets unlock rare parts; caps at max options with an
  ordinal tie-break; options carry the blank's slot + species archetype.
- `SocketingTrendEvaluatorTests` — socket changes raise the combined post-grammar trend; the fusion
  grammar applies; unsocketing re-raises; `Dispose` unsubscribes.
- `MutationPartCatalogTests` — maps slot/part/rarity/trait-affinity from a `PartDefinition`
  (aggregation rules; display-name fallback; icon lookup; null catalog throws).
- `MutationVariantPresenterTests` — a partially filled blank shows nothing; filling the last socket
  shows the variant cards (slot-filtered); a pick swaps, consumes the reagents, spends the blank,
  and hides; a failed swap keeps cards + blank + reagents; the equipped part is excluded;
  trait-less reagents still yield a menu; a blank ripening while a menu shows queues and opens
  after the pick.

The fusion grammar the sockets reuse is covered by the Inventory suites
(`EmergentFusionCalculatorTests`, `TraitFusionRuleSetTests`, `ArtifactTraitProfileTests`).

---

## 6. Known limitations / open points

- **Blank loot.** Blanks only enter the rack via the `MutationConfig.StartingBlanks` dev seed;
  Part-Blank drops (monster remains, finds, relics) are a ROADMAP item.
- **Placeholder rack look.** Tinted quads + code-built TMP labels; no blank icons authored, no drag
  ghost/unseal effects, no tier-glow on bubbles ("tier by glow"). A confirm step before the
  auto-unseal commit is a possible later tweak — today the last drop is the commit (user decision).
- **Cauldron-voice delivery.** The trend seam (`ISocketingTrendSource`) has no consumer; the bark
  delivery is a ROADMAP item.
- **A committed blank with no variants is stuck.** If every part of its slot is equipped (or none is
  authored), the ready blank stays committed with no menu; the validator warns at authoring time,
  but there is no runtime escape hatch (`ReturnAll` is not player-facing).
- **Variant menus are small.** Only A/B parts exist per slot, so menus are ≤ 2 (minus the equipped
  part); authoring more variant parts per slot is content work (ROADMAP: emergent authoring density).
- **Layering trade-off (M2, accepted; trait-based since the cutover):** per-part mutation data
  (`TraitAffinity`, `MutationRarity`, `ChoiceIcon`) lives on the CharacterSystem `PartDefinition`
  for single-asset authoring (a §2 inward-only deviation, mirroring the accepted
  `PartDefinition → Combat` ability coupling). Core stays type-decoupled (id strings + int tier).
  A future cleanup could host this on a Mutation-layer companion SO keyed by part id.
- **Part-derived abilities are pulled at combat start**, not pushed by the swap
  (ability-subsystem.md §2.6) — unchanged from M1.
- The variant card panel prefab must be built once (menu tool, §4); until then the unseal choice is
  disabled (a startup warning, no crash).
