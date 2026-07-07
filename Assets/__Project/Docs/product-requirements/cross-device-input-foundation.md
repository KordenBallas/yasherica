# Cross-Device Input Foundation — Product Requirements

> Status: **Shipped** 2026-07-07 — implemented as `Docs/input-foundation.md` (see CHANGELOG); the
> deferred design briefs it flags live under ROADMAP "Input / Cross-Device".
> ⚠ Reconcile with `design/parking-lot.md`: this brief was marked "Parked — future work" in the design
> lane on the same day the owner had the code track implement it in-session — the parking-lot entry
> (2026-07-07, Multiplatform support) is now stale for the *foundation* slice; only the later slices
> (mobile perf, consoles, screen redesigns) remain parked.
> Level: product-owner (what & feel). The code track owns the technical "how".

## Goal

Make the game playable — end to end — with **any of three input sources**: **keyboard + mouse**,
**gamepad**, and **touch**. Today the game is effectively keyboard-and-mouse only (walk with the
keyboard, **F** to interact, hold-to-aim / release-to-fire with mouse + Enter, mouse hover and drag
in the cauldron). That is fine for one platform and a wall for every other one.

This brief establishes the **foundation**, not the whole port: the game must be played through a
fixed set of named **actions** (Move, Interact, Confirm, Cancel, navigate a menu, Aim, Fire) rather
than through specific keys, and **every core-gameplay action must be reachable on each of the three
sources**. The game must also always know **which source the player is using right now** and show the
matching on-screen prompt (an **F** label vs a controller button vs a touch cue), switching instantly
when the player changes device mid-session.

This is deliberately the **plumbing + prompt layer**. It does **not** redesign the pointer-heavy
screens (the cauldron drag-and-drop, inventory hover, mutation cards) for gamepad — that is genuine
interaction *design* and rides its own briefs. What this brief guarantees is that the input source and
the prompts underneath every screen are device-agnostic, so those later screen briefs have solid
ground to stand on.

Getting this right now — while there are a handful of systems, not fifty — is far cheaper than
retrofitting device-agnostic input across a finished game.

## Why this slice first

"Make the game multiplatform" is not one job; it is several of very different natures (input, screen
interaction design, mobile performance tuning, console SDK/certification). This brief is the **first
and most architectural slice**: the shared input spine that every later platform slice depends on. The
others are explicitly out of scope here (see below) and, where they are design decisions, are the
product owner's to specify before they are handed off.

## User stories

- As a player on a **gamepad**, every action I need to *play* — move, interact with an NPC, confirm,
  go back, move between menu/card options, aim, and fire — is on the controller. I never have to reach
  for a mouse or keyboard to perform a core action.
- As a player, the on-screen prompts show the cue for the device I am **actually using right now** —
  an **F** when I'm on the keyboard, the controller's button when I pick up a gamepad, a touch cue on
  a touchscreen — and they **switch the instant I change device**, with no menu or restart.
- As a player on **touch**, I can perform the same core actions by touch on the screens that are
  already pointer-driven.
- As a designer, I can see, on a dev overlay, **which source is currently active** and what each named
  action is bound to, so I can sanity-check coverage while tuning.

## Functional requirements

### A single action vocabulary
1. The game is played through a fixed, named set of **actions** — at minimum: **Move**, **Interact**
   (the world **F**-prompt verb), **Confirm/Select**, **Cancel/Back**, **Navigate** (moving focus
   between options/cards/menu items), **Aim**, and **Fire/Commit** (the combat volley). Gameplay reads
   *actions*, never specific keys or buttons.
2. Every action has a binding on **each** supported source (keyboard+mouse, gamepad, touch). A named
   action with no binding on a source that is otherwise supported on that screen is a gap, not a
   silent omission.

### Three supported sources, full core coverage
3. The three supported sources are **keyboard + mouse**, **gamepad**, and **touch**. Every
   **core-gameplay** action (movement, world interaction, dialogue choice, combat aim/fire, menu
   navigate/confirm/cancel) must be reachable on each of them — except on screens explicitly deferred
   below.
4. **No core action may be exclusive to one source.** Where a screen today only works through a
   pointer (mouse **hover** or **drag** with no non-pointer path), that screen is **flagged as needing
   a dedicated interaction brief** for gamepad — it is **not** faked or half-solved here. Note that
   **touch already satisfies these pointer screens** (touch is a pointer): the open gap on the
   drag/hover screens is **gamepad**, and that gap is design, not plumbing.

### Active-source detection & prompts
5. The game continuously tracks **which source the player last used** and treats that as the active
   source. Changing device mid-session (e.g. dropping the mouse and picking up a controller) updates
   the active source **immediately**.
6. **Every on-screen prompt derives its cue from the active source** — the world interaction prompt,
   the combat aim/fire hint, and any menu/navigation hints all show the glyph/label of the device in
   use, and re-render the moment the active source changes. This supersedes the "controller prompt
   glyphs are out of scope" notes left in `npc-proximity-interaction.md` and `encounter-dialogue-ui.md`.

### Dev visibility
7. A **dev overlay** (toggleable, not shipped player UX — consistent with the existing debug-overlay
   pattern) shows the **currently active source** and the binding of each named action on that source,
   so coverage can be checked by eye while tuning.

## Acceptance criteria (demo)

- **Gamepad-only run:** with only a controller connected, the player can move the hero, walk up to an
  NPC and see a **controller-button** prompt (not "F"), open the dialogue, move between and pick a
  choice card, enter combat, aim, and fire — all without touching a keyboard or mouse, on every screen
  **except** those explicitly deferred below.
- **Live source switch:** while playing, moving from keyboard to gamepad flips **every visible prompt**
  to controller cues at once; picking the keyboard back up flips them all back — no menu, no restart.
- **Touch:** the same core actions are performable by touch on the screens that are already
  pointer-driven (world movement/interaction, dialogue choices, cauldron/inventory pointer actions).
- **Dev overlay:** toggling it on shows the current active source and updates as the player switches
  devices.

## Out of scope / open points (do not build now — separate briefs/tracks)

- **Gamepad interaction design for the pointer-heavy screens** — the cauldron drag-and-drop / brew
  layout, inventory hover, and mutation choice cards under a controller (focus/navigation model). This
  brief provides the source + prompts; **how you brew or socket with a stick** is its own interaction
  brief and is a **product-owner design decision** to be specified first.
- **Player-facing rebinding / remap UI** — deferred; this brief fixes bindings, it does not let the
  player change them.
- **Mobile performance, thermal/GPU budget, safe-area and adaptive layout** — a separate mobile track;
  needs on-device profiling that cannot be done here.
- **Console (Switch / PlayStation) ports** — out of reach of this work entirely: they require licensed
  platform SDKs, dev kits, and certification with a human platform partner. This brief only keeps the
  input source device-agnostic so a later port is not fighting the input layer.
- **Haptics, gyro/motion, adaptive triggers, and controller-specific glyph *art*** — deferred.
