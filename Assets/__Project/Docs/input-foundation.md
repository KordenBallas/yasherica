# Cross-Device Input Foundation — Requirements & Design

> The shared input spine: the game is played through a fixed vocabulary of named **actions** (Move,
> Interact, Confirm, Cancel, Navigate, Aim, Fire + the combat gesture family), every action is bound
> on **keyboard+mouse, gamepad, and touch** simultaneously, the **active source** (last device used)
> is tracked live, and every on-screen prompt derives its cue from it — "[F] Talk" flips to
> "[Y] Talk" the instant the player picks up a controller. Implements the verified brief
> `product-requirements/cross-device-input-foundation.md`.
> Status: current as of 2026-07-07.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1 (action vocabulary)** Gameplay reads named actions, never raw keys: `GameAction` = Move,
  Interact, Confirm, Cancel, Navigate, Aim, Fire, CombatMoveMode, CombatChangeDirection,
  AbilitySlot1–6. Consumers poll them via `IGameActions` (runtime) and describe them via
  `InputBindingCatalog` (declarative).
- **R2 (coverage rule)** Every action × source pair is either bound in the catalog or sits on the
  explicit deferred-gap allowlist with a reason + ROADMAP reference. An edit-mode test fails on any
  unlisted gap AND on any allowlisted pair that has quietly become bound.
- **R3 (three sources, merged)** Keyboard+mouse, gamepad, and touch are all live **simultaneously** —
  each `InputAction` carries all its per-source bindings, so there is no platform detection or
  controller switching. Touch's Move/Interact ride an on-screen overlay (stick + button) that drives
  a virtual gamepad the same actions listen to.
- **R4 (pointer screens)** Screens that are pointer-driven (dialogue cards, cauldron, inventory,
  mutation) already work by touch through `InputSystemUIInputModule`; the open gamepad gap on the
  drag/hover screens is design, not plumbing, and is flagged (§6), not faked.
- **R5 (active source)** `IActiveInputSource` tracks the device family the player last actuated
  (`ActiveInputSourceTracker` over `InputSystem.onEvent`), switching immediately mid-session.
  Stick drift is ignored (actuation threshold) and the touch overlay's **virtual** gamepad counts as
  Touch, not Gamepad (`InputSourceClassifier`, pure C#).
- **R6 (device-aware prompts)** Every prompt call site reads `IPromptCueProvider` (action → cue text
  for the active source) and re-renders on its `CuesChanged` event: the NPC overhead "[F] Talk", the
  Hub keeper/portal prompts, and the combat action panel keybind labels.
- **R7 (dev overlay)** The F1 dev overlay (editor/dev-build only) shows an Input section: the active
  source and each action's cue on it, with deferred gaps marked.

### 1.2 Non-functional requirements

- **N1** Core logic (`Scripts/GameInput/Core`) is pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject (`InputInstaller`); no service locators.
- **N3** Bindings are a **code contract** (catalog + `.inputactions` asset), not designer content —
  rebinding UI is deliberately out of scope, so no SO holds key assignments.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/GameInput/
  Core/   — pure C#: GameAction, InputSource, BindingEntry, InputBindingCatalog,
            DeferredBindingGap, InputCoverageValidator, InputSourceClassifier,
            InputDeviceKind, IActiveInputSource, IPromptCueProvider, PromptCueProvider
  View/   — infrastructure: GameActionsProvider (loads/enables the actions asset),
            ActiveInputSourceTracker (InputSystem.onEvent → classifier),
            TouchControlsView (procedural on-screen stick + interact button)
Scripts/Core/DI/InputInstaller.cs   — installed by AreaInstaller, HubInstaller, ArenaInstaller
Resources/Input/GameActions.inputactions — the ONE runtime binding artifact
```

(The folder is `GameInput`, not `Input`, to avoid resolution clashes with `Combat.Input` and
`UnityEngine.Input`.)

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `GameAction` / `InputSource` | The action vocabulary and the three sources. |
| `BindingEntry` | One catalog row: action × source → Input System control path + prompt cue text. Touch rows have no control path (overlay/pointer-backed). |
| `InputBindingCatalog` | The declarative binding table + deferred-gap allowlist; read by prompts, the dev overlay, and tests. |
| `InputCoverageValidator` | The R2 rule: unlisted gaps and stale allowlist entries → named failures. |
| `InputSourceClassifier` | Pure R5 rules: device kind + native flag + actuation magnitude → source or "ignore". Non-native gamepad = Touch (on-screen controls). |
| `PromptCueProvider` | catalog + `IActiveInputSource` → cue for the active source; re-raises source switches as `CuesChanged`. |

### 2.3 Runtime flow

1. `InputInstaller` binds everything; `GameActionsProvider.Initialize()` loads
   `Resources/Input/GameActions`, maps each `GameAction` to its `InputAction`
   (`Player/*` for gameplay, `UI/Submit|Cancel|Navigate` for menu verbs) and enables the asset.
2. `ActiveInputSourceTracker.Initialize()` subscribes `InputSystem.onEvent`; on each state event it
   takes the strongest changed-control actuation (mouse position/delta counts as full actuation),
   classifies the device, and raises `Changed` only on an actual source switch.
3. Consumers poll actions: `NpcInteractionInput` (Interact), `CombatInputController` (the combat
   gesture machine — unchanged tap/hold semantics, device seam swapped to actions;
   aim direction via `IAimDirectionResolver`: cursor→ground raycast on pointer, left stick in camera
   space on gamepad). `CharacterMovementController` keeps its serialized `InputActionReference`s
   into the same asset (Hero.prefab), `InventoryHudView._cancelAction` points at `UI/Cancel`.
4. Prompts: `NpcProximityPresenter`, `HubSceneEntrypoint`, and `CombatActionPanelPresenter` inject
   `IPromptCueProvider`, compose their labels from `GetCue(...)`, and re-render on `CuesChanged`.
5. `TouchControlsView` (spawned by the installer, self-hides without a touchscreen) builds a canvas
   with an `OnScreenStick` → `<Gamepad>/leftStick` (Move) and an `OnScreenButton` →
   `<Gamepad>/buttonNorth` (Interact) procedurally — no prefab, same precedent as `NpcOverheadView`.

### 2.4 DI wiring

`InputInstaller` (non-Mono `Installer<T>`) binds: `InputBindingCatalog`, `InputSourceClassifier`,
`ActiveInputSourceTracker` (as `IActiveInputSource` + `IInitializable`/`IDisposable`),
`GameActionsProvider` (as `IGameActions`), `PromptCueProvider` (as `IPromptCueProvider`), and
`TouchControlsView` (`FromNewComponentOnNewGameObject`, `NonLazy`). Installed by `AreaInstaller`,
`HubInstaller`, and `ArenaInstaller`. `IAimDirectionResolver` → `AimDirectionResolver` is bound next
to the combat controller in `AreaInstaller`/`ArenaInstaller` (combat scenes only).

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

The system deliberately owns **no binding SO** (N3). Two data artifacts:

### `GameActions.inputactions`  (Input System actions asset — JSON, not an SO)

Lives at `Resources/Input/GameActions.inputactions`, GUID `052faaac586de48259a63d0c4782560b`
(the original project asset moved — GUID preserved so `Hero.prefab`'s `moveAction`/`dashAction`
references and `InventoryHud.prefab`'s `_cancelAction` keep resolving).

| Map/Action | Bindings | Consumed by |
|---|---|---|
| `Player/Move` | WASD/arrows composite, `<Gamepad>/leftStick` | `CharacterMovementController` (Hero.prefab reference); touch via overlay stick |
| `Player/Interact` | `<Keyboard>/f`, `<Gamepad>/buttonNorth` | `NpcInteractionInput`; touch via overlay button |
| `Player/Aim` | `<Mouse>/position`, `<Gamepad>/leftStick` | declared bindings for aim (read through `AimDirectionResolver`) |
| `Player/VolleyFire` | `<Keyboard>/enter`, `<Gamepad>/rightTrigger` | `CombatInputController` (Fire) |
| `Player/MoveMode` | `<Keyboard>/m`, `<Gamepad>/leftTrigger` | `CombatInputController` |
| `Player/ChangeDirection` | `<Keyboard>/s`, `<Gamepad>/buttonWest` | `CombatInputController` |
| `Player/Ability1..6` | `q w e r t y`, d-pad up/right/down/left + LB/RB | `CombatInputController` |
| `UI/Submit` | `*/{Submit}` | Confirm |
| `UI/Cancel` | `*/{Cancel}`, `<Mouse>/rightButton` | Cancel — inventory close AND the combat gesture abort (right-click abort preserved) |
| `UI/Navigate` | arrows/WASD/d-pad/sticks composite | Navigate (EventSystem focus) |
| `Player/Jump` | space, `<Gamepad>/buttonSouth` | Hero dash (`dashAction` reference) |

The template's remaining actions (Look, Attack, Crouch, Previous/Next, Sprint, Point/Click…) are
untouched; the UI map also feeds `InputSystemUIInputModule` conventions.

### `InputConfig`  (asset menu: `Create → Combat → Input Config`)

Loaded from `Resources/Configs/InputConfig.asset`, bound by `AreaInstaller`/`ArenaInstaller`.
Slimmed to gesture-feel only — all KeyCode/axis fields were removed with the migration:

| Field | Type | Meaning | Default |
|---|---|---|---|
| `volleyAimHoldThresholdSeconds` | float | Fire held at least this long = volley aim mode; shorter tap = fire along facing (D7) | `0.25` |
| `inputDeadzone` | float | Min cursor distance (world units) / min stick deflection for aim to register | `0.1` |

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

### Add or change a binding on an existing action

1. Edit `Resources/Input/GameActions.inputactions` (JSON): add/replace a binding block under the
   action, with a fresh GUID for the binding `id`. **Never** change existing action `id`s or names —
   sub-asset fileIDs derive from them and prefab references would break.
2. Mirror the change in `InputBindingCatalog` (control path and/or cue text for that action × source).
3. Run the edit-mode input tests — `InputActionsAssetConsistencyTests` fails if the catalog and the
   asset drift apart; `InputCoverageValidatorTests` fails on coverage gaps.

### Add a NEW named action

1. Add a `GameAction` enum value and its catalog rows (one per source; if a source is consciously
   deferred, add a `DeferredBindingGap` with a reason + ROADMAP item — the coverage test enforces one
   or the other).
2. Add the action + bindings to the `.inputactions` JSON (fresh GUIDs for action and bindings).
3. Map it in `GameActionsProvider.Initialize()` (`MapAction(GameAction.X, "Player/X")`).
4. Consume it via `IGameActions.Get(GameAction.X)`; show its cue via `IPromptCueProvider.GetCue`.

**Authoring constraints / gotchas:** the asset must stay BOM-less UTF-8; keep the existing GUID in
the `.meta`; binding `groups` are cosmetic (control schemes are not enforced — all devices are live);
prompt cues are plain text ("F", "RT", "Tap") — glyph art is a deferred brief.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/` (pure C#, dotnet-runnable):

- `InputBindingCatalogTests` — per-source cues, no duplicate rows, touch rows overlay-backed,
  gap entries carry reason + roadmap ref.
- `InputCoverageValidatorTests` — the live R2 rule over the real catalog: no unlisted gaps, no stale
  allowlist entries.
- `InputSourceClassifierTests` — device-kind mapping, stick-drift threshold, **virtual gamepad
  (on-screen controls) classifies as Touch**.
- `PromptCueProviderTests` — cue follows the active source, `CuesChanged` re-raise, deferred gap →
  empty cue, ability-slot mapping, unsubscribe on dispose.
- `InputActionsAssetConsistencyTests` — drift guard: every catalog control path appears in
  `GameActions.inputactions` (text containment; deliberately a heuristic).
- `DevStatePresenterTests` — the overlay's Input section (active source, cues, gap marks).

Verified manually in play mode: gamepad-only run (move → "[Y] Talk" prompt → dialogue → combat
aim/fire on stick + RT), live prompt flip on device switch, touch overlay appearance, F1 overlay.

---

## 6. Known limitations / open points

- **Gamepad on the pointer-heavy screens** — cauldron drag-and-drop, inventory hover, and mutation
  choice cards have no controller interaction model; each needs its own PO interaction brief
  (ROADMAP: Input / Cross-Device). Touch already works there (touch is a pointer).
- **Touch combat** — Aim/Fire/MoveMode/ChangeDirection/AbilitySlot1–6 are allowlisted deferred gaps
  on Touch; how you aim and volley with fingers is a dedicated design brief (ROADMAP).
- **Gamepad menu focus** — `UI/Navigate` is bound, but list/card screens do not yet set an initial
  `EventSystem` selection, so d-pad focus traversal on the encounter cards is inert until a focus
  pass (rides the mutation-cards/gamepad-screens briefs).
- **Player-facing rebinding UI** — out of scope by the brief; bindings are fixed in the asset.
- **Aim reads devices directly** in `AimDirectionResolver` (mouse position / left stick) because a
  screen *position* and a direction *vector* cannot share one action value unambiguously; the
  declared bindings live on `Player/Aim` and the drift guard covers them.
- **Glyph art** — cues are text ("F", "RT", "D-Up"); per-platform button art is deferred.
- The touch overlay checks for a touchscreen once at startup; a touchscreen hot-plugged mid-session
  does not summon the overlay until the next scene load.
