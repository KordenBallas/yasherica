# Shared Ability-Preview Popover

> One preview mechanism, two surfaces (`mutation-choice-cards.md` FR5 · `arena-draft-ui.md`
> req 12): hovering an ability icon opens a popover with the ability's **name + description**
> plus a **3D hero on a mock battlefield demonstrating the cast** — the D3 placeholder cell
> sweep over the ability's shape, the animator cast pose when the rig has one, an idle hero
> for a passive. Shipped 2026-07-06 with the Arena parts draft (G4).

## 1. Requirements

- **R1** One shared popover serves every surface that shows ability icons (today: the mutation
  choice cards and the arena draft's part-info popover); the two surfaces must not depend on
  each other.
- **R2** Name + description always show; the 3D stage shows only when a hero can be built —
  otherwise the popover degrades to a text tooltip (no crash, no empty frame).
- **R3** Which hero demonstrates the cast is a per-scene decision (Mutation: the live hero's
  body; Arena: the base assembly + the parts drafted so far).
- **R4** The cast demonstration reuses the D3 placeholder treatment (`AbilityAreaSweep` cell
  flashes) — production clips/VFX stay P5-12.
- **R5** All look/pacing tunables are data-driven (one config SO), per CLAUDE.md §7.

## 2. Architecture

`Scripts/UI/AbilityPreview/` (namespace `UI.AbilityPreview`) — shared UI infrastructure,
depending only on CharacterSystem (the factory), `Combat.Data` (`AbilityDefinition`), and
`Combat.View` (`AbilityAreaSweep`). Neither Mutation nor Arena is referenced.

| Class | Responsibility |
|---|---|
| `AbilityPreviewData` | Plain display payload (name, description, passive flag, shape, animator trigger); factories `FromAbility` / `FromPassive` |
| `IAbilityPreviewPopover` / `AbilityPreviewPopoverView` | The screen-clamped popover (prefab `Resources/Prefabs/UI/AbilityPreviewPopover`): text labels + a `RawImage` stage row that hides when the rig can't build (R2) |
| `IAbilityPreviewStage` / `AbilityPreviewStageRig` | Hidden far-offset stage: hero clone via `IModularCharacterFactory`, mock ground quad, camera → RenderTexture; loops the cast (trigger fired only if the Animator actually has the parameter) + the D3 cell sweep; passive = idle, no sweep |
| `AbilityPreviewShape` | Pure hex geometry for the sweep cells (line = length cells ahead; ring = the 6R hex ring) — unit-tested |
| `IAbilityPreviewHeroSource` | The per-scene seam (R3): `MutationAbilityPreviewHeroSource` (live hero assembly + equipped parts) · `ArenaDraftHeroSource` (draft base assembly + drafted loadout) |
| `NullAbilityPreviewPopover` | Missing-prefab fallback: hovers do nothing, callers never null-check |
| `AbilityPreviewInstaller` | Reusable `Installer<>`: config + stage rig + popover-from-prefab (with the graceful missing-prefab guard). **The calling installer binds the scene's hero source first.** |

Consumers: `MutationChoiceView` (ability-icon hover — replaced the old text-only
`AbilityTooltipView`, now deleted) and `ArenaDraftPresenter` (ability rows of the part-info
popover). Installed by `MutationInstaller` and `ArenaInstaller`.

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

### `AbilityPreviewConfig`  (asset menu: `Yasherica → UI → Ability Preview Config`)

Loaded from `Resources/UI/AbilityPreviewConfig`.

| Field | Type | Meaning | Default |
|---|---|---|---|
| `_rigWorldOffset` | Vector3 | Stage position, far from every gameplay camera | (4000, 0, −4000) |
| `_textureSize` | int | RenderTexture size (square) | 384 |
| `_cameraHeight` / `_cameraDistance` / `_lookAtHeight` / `_fieldOfView` / `_farClipPlane` | float | Camera framing | 1.6 / 3.4 / 0.9 / 40 / 50 |
| `_backgroundColor` | Color | Stage clear colour | dark violet-grey |
| `_modelYawDegrees` | float | Hero yaw so the cast reads three-quarter | 200 |
| `_lightLocalPosition` / `_lightRange` / `_lightIntensity` | — | Stage point light | (1.5, 2.5, −1.5) / 12 / 1.4 |
| `_groundSize` / `_groundColor` | float / Color | Mock ground quad | 8 / dark grey |
| `_cellSize` | float | Hex metric of the sweep cells | 0.5 |
| `_sweepTint` / `_sweepPeakAlpha` / `_sweepSeconds` | — | The D3 cell-flash treatment | orange / 0.85 / 0.6 |
| `_loopIntervalSeconds` | float | How often the demonstration repeats while open | 1.4 |

## 4. Adding Content  *(asset-only)*

**Give an ability a cast pose in the preview:** set `AnimationTrigger` on the
`AbilityDefinition` to a Trigger parameter that exists on the hero's Animator controller. No
parameter (today's placeholder rigs) → the hero idles and only the cell sweep plays.

**Tune the preview look:** edit `Resources/UI/AbilityPreviewConfig.asset` per §3. No code.

## 5. Tests

`AbilityPreviewShapeTests` (edit-mode): line cell count + outward march, the 6R ring count,
distinctness, near-circularity, zero-radius empty, determinism. The rig/popover halves are
thin Unity adapters — play-mode verified.

## 6. Known limitations / open points

- **No cast clips exist yet** — `AnimationTrigger` is authored on abilities but no controller
  has the trigger states, so the pose half idles until production clips land (P5-12).
- **One stage per scene** — concurrent popovers are not supported (hovers are exclusive by
  construction today).
- `PointerHoverRelay` (the hover surfacing component the consumers use) still lives in
  `Mutation.View`; relocating it into a shared UI namespace is a ROADMAP cleanup.
