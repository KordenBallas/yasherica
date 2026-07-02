# Logging — Requirements & Design

> Runtime logging is routed through one abstraction (`IGameLogger`) that tags every line with the
> system it came from (`LogCategory`) and consults a designer-authored config asset (`LoggingConfig`)
> before writing to the Unity Console. This lets a developer mute the noise of unrelated systems and
> keep only the logs of the feature under test.
> Status: current as of 2026-06-30.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

### 1.1 Functional requirements

- **R1** Every runtime log line is attributed to exactly one **system** via a `LogCategory`.
- **R2** A developer can set a **per-system verbosity ceiling** (Off / Error / Warning / Info) from a
  single ScriptableObject asset, without code changes.
- **R3** A **global master ceiling** can dial the whole game down at once (e.g. Off to silence all,
  Error to keep only errors), independent of per-system settings.
- **R4** A muted system produces **no Console output** (the line is suppressed before `Debug.*`).
- **R5** Lines are prefixed with their category (`[Combat] …`) so they remain greppable/filterable.
- **R6** If no config asset exists, logging falls back to **everything at Info** (never hard-fails).

### 1.2 Non-functional requirements

- **N1** The filtering rule (`LogLevelPolicy`) is pure C# (no UnityEngine) and unit-tested.
- **N2** The logger is wired through Zenject; no service locators, no static log state.
- **N3** The config is a data-only ScriptableObject; the only behaviour on it is the explicit
  Data → Core mapper (`ToPolicy`), as sanctioned by CLAUDE.md §7.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Core/Logging/
  IGameLogger.cs      — pure abstraction (Info/Warning/Error, each takes a LogCategory)
  LogCategory.cs      — pure enum: one value per system
  LogLevel.cs         — pure enum: Off < Error < Warning < Info
  LogLevelPolicy.cs   — pure C# decision object (ShouldLog); Unity-free, unit-tested
  LoggingConfig.cs    — ScriptableObject (data + ToPolicy mapper)
  UnityGameLogger.cs  — infrastructure adapter → UnityEngine.Debug, gated by the policy
Scripts/Core/DI/LoggingInstaller.cs — loads the config, binds the policy + logger
Resources/Configs/LoggingConfig.asset — the authored switchboard
```

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `LogCategory` | Identifies the originating system of a log line. |
| `LogLevel` | Severity ordered least→most verbose: `Off`, `Error`, `Warning`, `Info`. A system's configured level is a **ceiling**. |
| `LogLevelPolicy` | Pure decision object: `ShouldLog(category, level)` applies the per-category ceiling and the master ceiling. |
| `IGameLogger` | Logging abstraction used by all domain/application code. |

### 2.3 Runtime flow

1. `LoggingInstaller` loads `Resources/Configs/LoggingConfig` and calls `ToPolicy()` to build a
   `LogLevelPolicy` (or `LogLevelPolicy.AllEnabled()` if the asset is missing).
2. It binds the `LogLevelPolicy` and `IGameLogger` (→ `UnityGameLogger`) as singletons.
3. A system logs via its injected `IGameLogger`, e.g. `_logger.Info(LogCategory.Combat, "…")`.
4. `UnityGameLogger` asks the policy `ShouldLog(category, level)`; if true it writes
   `Debug.Log("[Combat] …")`, otherwise the line is dropped.

`ShouldLog` rule: a line passes when `level != Off`, `level <= masterLevel`, and
`level <= the category's ceiling` (or the default ceiling if the category is unlisted). Because the
enum is ordered, "ceiling = Error" keeps errors and drops warnings/info; "ceiling = Off" drops all.

### 2.4 DI wiring

`AreaInstaller.InstallBindings()` runs `LoggingInstaller.Install(Container)` first; every other
installer only **resolves** `IGameLogger`/`LogLevelPolicy` and must never re-bind them (Zenject 6
forbids `AsSingle` on the same concrete type twice). Pure-C# consumers receive the logger by
constructor injection (the global binding resolves automatically); scene MonoBehaviours receive it
by `[Inject]` field injection.

---

## 3. ScriptableObject Reference

### `LoggingConfig`  (asset menu: `Create → Config → Logging Config`)

Loaded from `Resources/Configs/LoggingConfig.asset` by `LoggingInstaller`. One asset for the whole
game.

| Field | Type | Meaning | Default / notes |
|---|---|---|---|
| `_masterLevel` | `LogLevel` | Global ceiling applied on top of every system. | `Info`. Set `Off` to silence all logs at once. |
| `_defaultCategoryLevel` | `LogLevel` | Ceiling for any system not present in the list below. | `Info`. |
| `_categories` | `List<CategorySetting>` | Per-system ceilings. | Shipped seeded with every `LogCategory` at `Info`. |
| `CategorySetting.Category` | `LogCategory` | Which system the row controls. | — |
| `CategorySetting.MaxLevel` | `LogLevel` | Most verbose level emitted for that system. | `Off`=silent, `Error`=only errors, `Warning`=+warnings, `Info`=everything. |

This SO references no other assets.

---

## 4. Adding Content

### Mute / raise a system while testing

1. Select `Assets/__Project/Resources/Configs/LoggingConfig`.
2. In the **Per-system ceilings** list, find the system's row and set its **MaxLevel** (`Off` to
   silence, `Error`/`Warning` to thin it out, `Info` for everything).
3. To focus on one feature: set **_masterLevel** or every row to `Error`/`Off`, then raise just the
   system you are testing back to `Info`. No recompile — it takes effect on next play.

### Add a new system category

1. Add a value to `LogCategory` (code change — categories are an enum, not authored content).
2. Add a matching row to the `LoggingConfig` asset's list (or rely on `_defaultCategoryLevel`).
3. Use it at the call site: `_logger.Info(LogCategory.NewSystem, "…")`.

**Authoring constraints:** the enum order is irrelevant to filtering but the serialized asset stores
categories by their integer value — do not reorder existing `LogCategory` members, only append.

---

## 5. Tests

Edit-mode suite in `Assets/__Project/Tests/EditMode/`:

- `LogLevelPolicyTests` — per-category ceilings (Off/Error/Warning/Info), the default-level fallback
  for unlisted categories, the master-ceiling override, and the `AllEnabled` fallback.

`UnityGameLogger` (the `Debug.*` adapter) and `LoggingConfig` serialization are verified manually in
the editor; only the pure `LogLevelPolicy` rule is unit-tested (per N1).

---

## 6. Known limitations / open points

- **A few call sites deliberately stay on `Debug.*`** (not category-filterable, by design):
  - `UnityGameLogger` — the infrastructure adapter that forwards to `Debug.*` (its whole job).
  - `LoggingInstaller` — the bootstrap warning emitted when the config asset is missing (logged before
    the logger is built).
  - Zenject feature installers — one-shot auto-load diagnostics at scene init; routing them would need
    an install-time `Container.Resolve<IGameLogger>()` (service-locator), discouraged by CLAUDE.md §4.
  - `Scripts/Editor/**` — editor tooling, runs on menu actions, not part of play-mode flood, not in the
    runtime DI graph.
- **Some scene/prefab-instantiated MonoBehaviours log via null-safe `_logger?.`** and a few domain
  leaves created in tight loops (hex-cell states on the grid-build path) are threaded a logger only
  from their interactive owner — so those specific lines are silent unless a logger is provided. This
  is intentional: it mutes grid-setup spam while keeping interactive transitions controllable.
- **No runtime UI.** Levels are changed by editing the asset in the Inspector; there is no in-game
  console/hotkey to flip a system live. Possible future dev-tools panel — see ROADMAP.
