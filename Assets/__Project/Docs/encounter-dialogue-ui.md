# Encounter Dialogue UI — Requirements & Design

> The Hades-style presentation layer for NPC encounters: a bottom-centre dialogue box with the
> speaker's portrait + name, the current line revealed **word by word** at a reading pace, choice
> **cards** (quest / attack / exit) that appear once the line finishes, and author-marked key words
> (`[[ ]]`) highlighted in both lines and cards. It is a presentation/feel layer over the unchanged
> conversation engine (`DialogueRunner`, facts, quests, combat — see `narrative-procedural.md`).
> Status: current as of 2026-06-28.
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.

---

## 1. Requirements

Product brief: `Docs/product-requirements/encounter-dialogue-ui.md` (Verified). Requirement ids
below mirror that brief.

### 1.1 Functional requirements

- **R1** A dialogue box is anchored bottom-centre for the whole conversation.
- **R2** It shows the NPC portrait and name plus the current line.
- **R3** The portrait comes from the NPC's archetype identity; a portrait-less NPC falls back to a
  neutral placeholder and the box still works (name + text).
- **R4** Each line is revealed **word by word** (whole words, never letter-by-letter) at a tunable,
  global reading speed.
- **R5** A tap while a line is revealing immediately completes that line.
- **R6** Choices do not appear/act until the current line has fully revealed.
- **R7** When choices are available they appear as cards centred above the box.
- **R8** The hand holds up to three card kinds: **quest** (the offer), **attack** (when fighting is
  possible), **exit** (always).
- **R9** The quest card is labelled with the **job**: the quest's title + objective/summary, not just
  the reply text.
- **R10** The three card kinds read differently at a glance (per-type frame tint). Per-tier glow is
  out of scope (§6).
- **R11** Author-marked key words — primarily NPC names — are highlighted wherever they appear, in
  the spoken line and on the cards.
- **R12** Highlighting is author-controlled: a key word is wrapped `[[like this]]`; the markers are
  never shown to the player; unmarked words render normally.

### 1.2 Non-functional requirements

- **N1** The keyword formatter is pure C# (no UnityEngine) and unit-tested; the presenter stays
  UnityEngine-free (it passes an archetype **id**, not a `Sprite`).
- **N2** All dependencies wired through Zenject; the view resolves the portrait via the already-bound
  `INpcArchetypeCatalog`.
- **N3** The conversation engine is unchanged — only presentation + the data the view reads.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Narrative/Encounter/
  KeywordHighlightFormatter.cs   — pure C#: [[word]] -> <color> rich text (N1)
  EncounterCardViewData.cs       — view DTO (+ QuestTitle / QuestObjective)
  EncounterCardType.cs           — QuestOffer / Attack / Leave / Talk
  IEncounterCardHandView.cs      — view contract (+ SetPortrait)
  EncounterCardHandPresenter.cs  — pure-C# MVP presenter (composition + chrome)
Scripts/Narrative/View/
  EncounterCardHandView.cs       — MonoBehaviour: box, TMP reveal, portrait, tap
  EncounterCardView.cs           — MonoBehaviour: one card (title + objective / label)
Scripts/Core/DI/NarrativeSliceInstaller.cs   — binds view + presenter
```

This sits on top of §2.7 of `narrative-procedural.md` (the encounter card-hand). It keeps that
presenter/view seam and the runner unchanged; it adds the box chrome (portrait, word-by-word reveal),
the quest-card job text, and keyword highlighting.

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `KeywordHighlightFormatter` | Static pure-C# `ToRichText(raw, hexColor)`: wraps each `[[word]]` in a TMP `<color=#hex>` span and strips the markers; unmarked text and an unterminated `[[` pass through verbatim; `[[]]` yields nothing. Used by both the line view and the card view so a key word reads the same everywhere. |
| `EncounterCardViewData` | View DTO. Adds optional `QuestTitle` / `QuestObjective` (set only for a quest card; null for Attack/Leave, which render `Label`). |
| `EncounterCardHandPresenter` | Pushes per-encounter chrome (portrait id + default NPC name) once when the box first appears, and labels each quest card with the offered quest's title + summary. Card composition + pick routing are unchanged from §2.7. |

### 2.3 Runtime flow

1. The runner pumps a line → presenter `HandleLine`: `SetVisible(true)`, `EnsureEncounterChrome()`
   (once per encounter: `SetPortrait(runner.EncounterArchetypeId)`, and `SetSpeaker(runner.
   EncounterDisplayName)` unless a `#speaker:` tag already set one), then `ShowSituation(text)` and
   `ShowContinueAffordance(true)`.
2. The view formats the line through `KeywordHighlightFormatter`, sets it on the TMP text with
   `maxVisibleCharacters = 0`, and a coroutine steps `maxVisibleCharacters` to each **word boundary**
   at `_wordsPerSecond` (R4). TMP's `characterCount` excludes rich-text tags, so the `<color>` spans
   never shift the boundaries. The continue glyph is shown only when the reveal finishes (R6).
3. A tap (`_tapArea` or `_continueButton`): mid-reveal → snap the line to full (R5); once revealed →
   raise `OnContinueRequested` → `runner.Continue()`.
4. At a decision point the runner fires `OnChoices` → presenter composes the hand. A `QuestOffer`
   card is labelled from `runner.OfferedQuest` (the casting's filled quest slot — read **before** the
   offer-quest tag mints the live instance) using `DisplayName` (title) + `Summary` (objective). The
   view renders cards centred above the box; quest cards run their title + objective through the same
   highlighter.
5. `OnDialogueEnded` → presenter hides the box and resets the chrome guards for the next encounter.

### 2.4 DI wiring

`NarrativeSliceInstaller` binds `IEncounterCardHandView` from
`Prefabs/UI/Encounter/EncounterCardHandView` and the `EncounterCardHandPresenter`
(`AsSingle().NonLazy()`) — unchanged from §2.7. The view `[Inject]`s the already-bound
`INpcArchetypeCatalog` to resolve the portrait sprite from the archetype id. No new bindings.

### 2.5 Reading-speed / highlight tuning

`_wordsPerSecond` and `_keywordColor` are serialized fields on the `EncounterCardHandView` prefab —
the single global reading speed and the accent tint. They are not authored per line.

---

## 3. ScriptableObject Reference

This UI owns **no new ScriptableObject type**. It reads existing data:

- **Portrait** — `NpcArchetype._portrait` (`Sprite`), asset menu `Create → Narrative → Actors →
  Archetype`; resolved by id via `INpcArchetypeCatalog`. See `narrative-procedural.md` §3.
- **Quest title / objective** — `QuestDefinition._displayName` + `_summary`, asset menu
  `Create → Narrative → Quests → Quest`. See `quest-subsystem.md` / `narrative-procedural.md` §3.

---

## 4. Adding Content

No code changes for any of the below.

### Highlight a key word in a line or choice
1. In the story's `.ink` (and the compiled `.json`, edited in lockstep — no inklecate in repo), wrap
   the word in double brackets: `[[Garrick]]`. The markers are stripped at render; the word shows in
   the accent tint.
2. The same syntax works in choice text and in a quest's `_summary` / objective `_description`, so a
   name reads the same on the card as in the line.

### Give an NPC a portrait
1. Assign a `Sprite` to the archetype's **Portrait** field (`NpcArchetype`). It appears in the box for
   any encounter cast from that archetype. Leave it empty to use the neutral placeholder.

### Label the quest card
1. The quest card automatically shows the attached quest's **title** (`_displayName`) and
   **objective/summary** (`_summary`) — do not re-type the objective into the choice text.

**Authoring constraints / gotchas:** edit `.ink` and `.json` together (the runtime loads the compiled
`.json`). A `# card: attack` tag must still follow **shown** (non-bracketed) choice text (see
`narrative-procedural.md` §2.7). Hand-authored `.asset` / `.json` must be BOM-less UTF-8.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `KeywordHighlightFormatterTests` — single/multiple marks, no marks, `[[]]`, unterminated `[[`,
  null/empty color, leading `#` on the color, null/empty input; exact rich-text output and that the
  markers are stripped.
- `EncounterCardHandPresenterTests` — a `QuestOffer` card carries the offered quest's `DisplayName` /
  `Summary` (and Leave carries none); `SetPortrait` is forwarded the casting's archetype id and the
  speaker falls back to the NPC's display name; existing card-composition + routing assertions stay
  green.

Verified manually in the editor (the demo barn-victim encounter): bottom-centre box + portrait/name,
word-by-word reveal, tap-to-complete, cards centred above with a quest card showing title + summary,
the `[[Raiders]]` key word tinted the same in the line and on the card, and the portrait-less
placeholder fallback.

---

## 6. Known limitations / open points

- **Per-tier card glow / belonging color** — quest cards use a placeholder per-type frame tint only;
  rarity/tier glow is gated on the Crafting tier model (ROADMAP `## Quests` / `## Crafting`).
- **One quest card per encounter** — a story carries a single Quest slot; several offers per NPC are a
  separate authoring-shape item (ROADMAP).
- **No automatic name detection** — highlighting is only via the author's `[[ ]]` markers.
- **One portrait per NPC** — no emotion/portrait changes mid-conversation.
- **Prefab visuals verified in-editor** — the box layout, TMP text components, portrait `Image`, tap
  catcher, and card-anchor placement are authored in `EncounterCardHandView.prefab` /
  `EncounterCardView.prefab` (all serialized references wired); the on-screen look/feel is confirmed in
  play mode rather than by edit-mode tests.
