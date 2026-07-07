# Developer Tools — Requirements & Design

> An in-game developer overlay that displays live game state in sections (quests, director facts, …)
> for debugging. Editor / development-build only; never ships in a release build. It is a diagnostic
> read-out — it does not mutate game state.
> Status: current as of 2026-06-25.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** A key-toggled on-screen overlay (default **F1**) renders live game state as titled sections.
- **R2** **Quests** section: every quest recorded this run as `DisplayName (questId): Status`, grouped
  Active / Completed / Failed, with a count header. Ids are mapped to display names via the fragment
  library; an unmapped id falls back to showing the bare id.
- **R3** **Director facts** section: every current fact as `<key> = <value> [<Type>]`, in the fact
  store's stable order, with a count header; an "(none set)" line when empty.
- **R4** The overlay is built **only** in the editor and development builds; release builds exclude it.
- **R5** Sections are generic so a new section is one builder method — no view or wiring changes.
- **R6** **Input** section (input-foundation R7): the currently **active input source** and each named
  action's cue on that source (`Interact: F`), with consciously deferred pairs marked
  `— (deferred gap)`; the section re-reads live, so switching device updates it.

### 1.2 Non-functional requirements

- **N1** Section content is produced by a pure-C# presenter (no UnityEngine) and unit-tested.
- **N2** The view is a thin MonoBehaviour with no logic beyond rendering and the toggle.
- **N3** Wired through Zenject; no service locators. Reads existing singletons; binds nothing new in
  the gameplay layer.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/DevTools/
  Core/   DevPanelSection, IDevStateSource          — pure C# view-model + source contract
  DevStatePresenter.cs                              — pure C#: builds sections from live reads
  View/   DevOverlayView                            — thin IMGUI MonoBehaviour (OnGUI + key toggle)
Scripts/Core/DI/DevToolsInstaller.cs                — non-Mono Installer<T>, spawns the view
```

### 2.2 Core types

| Type | Responsibility |
|---|---|
| `DevPanelSection` | A titled block: `Title` + `IReadOnlyList<string> Rows`. Pure data. |
| `IDevStateSource` | `IReadOnlyList<DevPanelSection> BuildSections()` — the view's only dependency. |
| `DevStatePresenter` | `IDevStateSource`; reads `IRunProgressionRecord`, `IFragmentLibrary`, `IFactStore`, `IActiveInputSource`, `InputBindingCatalog` and assembles the Quests + Director-facts + Input sections. |
| `DevOverlayView` | `MonoBehaviour`; `OnGUI` draws the sections in a boxed scroll area; toggles visibility on an IMGUI `F1` key event. Renders only — no logic. |

### 2.3 Runtime flow

`DevToolsInstaller` binds `DevStatePresenter` as `IDevStateSource` (`AsSingle`) and spawns
`DevOverlayView` on a new GameObject (`FromNewComponentOnNewGameObject`). Each `OnGUI` the view calls
`BuildSections()` (immediate-mode, so it is always current) and renders. The presenter reads:
- quest ids from `IRunProgressionRecord.ActiveQuests/CompletedQuests/FailedQuests`, names from
  `IFragmentLibrary.FindQuests(Array.Empty<string>())` (returns all quests);
- facts from `IFactStore.Snapshot()` (stable `FactKey.Comparer` order).

### 2.4 DI wiring

`DevToolsInstaller` is a non-Mono `Installer<DevToolsInstaller>` (mirrors `LoggingInstaller`). It is
installed from `AreaInstaller.InstallBindings()` inside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, so the
overlay is created only in the editor and dev builds. The presenter resolves the three gameplay
singletons (all bound in the same Area scene container: progression record in `AreaInstaller`, fact
store and fragment library in `NarrativeSliceInstaller`).

---

## 3. ScriptableObject Reference

None. Developer Tools own no ScriptableObjects and no authorable content.

---

## 4. Adding a section (developer recipe)

Sections are code, not assets. To add one:

1. In `DevStatePresenter`, add a `Build<Name>Section()` that reads the live source(s) it needs
   (constructor-inject any new dependency) and returns a `DevPanelSection(title, rows)`.
2. Add it to the list returned by `BuildSections()`.
3. Cover it in `DevStatePresenterTests` with a fake of each new source.

No view or installer change is needed — `DevOverlayView` renders whatever sections it receives.

---

## 5. Tests

Edit-mode suite in `Assets/__Project/Tests/EditMode/`:

- `DevStatePresenterTests` — quests grouped by status with id→name mapping; id fallback when unmapped;
  the empty-quests and empty-fact-store messages; facts mirror the snapshot with typed values; the
  Input section shows the active source, per-action cues, and deferred-gap marks. Uses fakes for
  `IRunProgressionRecord`, `IFragmentLibrary`, `IFactStore`, `IActiveInputSource`.

`DevOverlayView` (IMGUI rendering + toggle) is verified manually: enter play, press **F1**.

---

## 6. Known limitations / open points

- Three sections only (Quests, Director facts, Input). Inventory contents, mutation feed tally, planner
  window/horizon + live-actor registry, and live `ActiveQuest` objective progress are open (ROADMAP).
- Read-only — no on-screen fact editing or quest-state forcing.
- IMGUI rebuilds the sections every `OnGUI` (allocates per call); acceptable for a dev tool, not tuned.
- The toggle key (F1) is fixed; not yet configurable.
