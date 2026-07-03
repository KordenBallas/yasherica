# Arena Mode (Multiplayer) — Requirements & Design

> The second game mode: from a boot main menu the player picks **Journey** (the unchanged
> single-player campaign) or **Arena** (a networked 2–4 player free-for-all on one hex platform,
> hidden simultaneous planning → simultaneous resolve, last hero standing wins). PO brief:
> `product-requirements/arena-mode-mvp.md`; design intent: `/design/arena-mode.md`.
> Status: current as of 2026-07-03 — **Phase 1 (main menu + scene flow) implemented**; the Arena
> session itself is planned (§6).
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

Menu & mode flow (implemented — brief R1–R3):

- **R1** The game boots into a main menu presenting exactly two modes: **Journey** and **Arena**.
- **R2** **Journey** loads the current combat/exploration scene (`Area`) with no behavioral change.
- **R3** **Arena** loads the networked Arena scene.

Arena session (planned — brief R4–R14, see §6):

- **R4–R6** 2–4 player FFA, host / join by address, default hero per player at distinct spawns.
- **R7–R12** Hidden simultaneous commit → simultaneous resolve; deterministic per match seed;
  whiffs are real (committed actions never re-target); deterministic symmetric conflict rules.
- **R13–R14** Abilities unchanged from PvE; last hero standing wins.

### 1.2 Non-functional requirements

- **N1** Core logic is pure C# (no UnityEngine) and unit-tested.
- **N2** All dependencies wired through Zenject; no service locators.
- **N3** Journey must stay byte-identical: no PvE combat/narrative file changes beyond
  behavior-preserving extractions explicitly listed in the CHANGELOG.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Core/SceneFlow/       — ISceneLoader (contract) + SceneLoader (SceneManager adapter)
                                + SceneNames (build-settings scene name constants)
Scripts/MainMenu/             — MainMenuPresenter (pure C#) + IMainMenuView + MainMenuView (thin Mono)
Scripts/Core/DI/MainMenuInstaller.cs — the MainMenu scene's MonoInstaller
Scenes/MainMenu.unity         — boot scene (build index 0): Camera, EventSystem, Canvas with the
                                two mode buttons, SceneContext → MainMenuInstaller
```

### 2.2 Core types

| Type | Responsibility |
|---|---|
| `ISceneLoader` / `SceneLoader` | Engine-free scene-switch seam; the only `SceneManager` touchpoint |
| `SceneNames` | `MainMenu` / `Area` / `Arena` constants, kept in sync with EditorBuildSettings |
| `MainMenuPresenter` | Routes view clicks to scene loads (Journey → `Area`, Arena → `Arena`) |
| `IMainMenuView` / `MainMenuView` | Two-button view adapter; forwards clicks as events, no logic |

### 2.3 Runtime flow

Boot → `MainMenu.unity` (build index 0) → SceneContext runs `MainMenuInstaller` →
`MainMenuPresenter.Initialize()` subscribes to the view → click loads the mode scene via
`ISceneLoader` (single mode; each scene owns its self-contained SceneContext, so no cross-scene
container plumbing is involved). The Area scene is loaded exactly as before — its own installers
bootstrap the run unchanged.

### 2.4 DI wiring

`MainMenuInstaller` (MonoInstaller on the MainMenu SceneContext): `LoggingInstaller.Install`
(per-container logging home), `ISceneLoader → SceneLoader` (`AsSingle`),
`IMainMenuView → MainMenuView` (`FromComponentInHierarchy`), `BindInterfacesTo<MainMenuPresenter>`
(`AsSingle().NonLazy()`).

Build settings (`ProjectSettings/EditorBuildSettings.asset`): `MainMenu` (0), `Area` (1); the
stale entry for the nonexistent `Demo.unity` was removed. The `Arena` scene is added in the next
phase.

---

## 3. ScriptableObject Reference  *(mandatory — CLAUDE.md §7/§8)*

None yet. The Arena session phase introduces `ArenaMatchConfig` (max players, port, pacing,
platform profile) — documented here when it lands.

---

## 4. Adding Content  *(mandatory — CLAUDE.md §8.1)*

The menu exposes no authorable content. Arena content (abilities, the default hero) flows through
the existing Ability / Hero SO recipes (`ability-subsystem.md`); nothing Arena-specific yet.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `MainMenuPresenterTests` — Journey click loads `Area`, Arena click loads `Arena`, dispose
  unsubscribes (stub view + stub loader).

Verified manually: menu scene boot, button click-through to the Area scene in play mode.

---

## 6. Known limitations / open points

- **Arena button leads to a not-yet-existing scene.** Until the Arena session phase lands,
  clicking Arena logs Unity's "scene not found" error and stays in the menu. *(Resolved by the
  next phase.)*
- **No dedicated Hub scene.** Journey goes straight to the current Area scene per the brief; a Hub
  is an acknowledged later need (ROADMAP).

> **Planned design (NOT implemented) — the Arena session (brief R4–R14).** One platform generated
> from the match seed; 2–4 heroes; the symmetric round: every player plans hidden with the
> existing planning UI (terminal actions intercepted into a committed round), all lock in, the
> host assembles and broadcasts the round bundle, every client normalizes and resolves it
> deterministically through the PvE committed-intent resolver (whiff/fizzle/skip-dead semantics
> unchanged); rotating-initiative resolution order behind a replaceable strategy; last hero
> standing wins; NGO host-relay lockstep networking (no per-unit NetworkObjects). Delivered in
> three follow-up phases: offline symmetric round vs seeded AI dummies → NGO host/join +
> commit/bundle relay → spectate/match-end polish.
